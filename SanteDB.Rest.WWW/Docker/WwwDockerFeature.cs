/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you
 * may not use this file except in compliance with the License. You may
 * obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 *
 * User: fyfej
 * Date: 2023-6-21
 */
using SanteDB.Core.Applets.Configuration;
using SanteDB.Core.Configuration;
using SanteDB.Core.Exceptions;
using SanteDB.Docker.Core;
using SanteDB.Rest.Common.Behavior;
using SanteDB.Rest.Common.Configuration;
using SanteDB.Rest.Common.Security;
using SanteDB.Rest.WWW.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Rest.WWW.Docker
{
    /// <summary>
    /// Web hosting docker feature
    /// </summary>
    public class WwwDockerFeature : IDockerFeature
    {

        /// <summary>
        /// Setting ID for listen address
        /// </summary>
        public const string ListenUriSetting = "LISTEN";

        /// <summary>
        /// Default solution to render pages from
        /// </summary>
        public const string DefaultSolutionSetting = "SOLUTION";

        /// <summary>
        /// Default applet to render pages from
        /// </summary>
        public const string DefaultAppletSetting = "APPLET";

        /// <summary>
        /// Whether to allow caching
        /// </summary>
        public const string AllowClientCachingSetting = "CACHE";

        /// <summary>
        /// Set ID for authentication
        /// </summary>
        public const string AuthenticationSetting = "AUTH";

        /// <summary>
        /// Map the settings to the authentication behavior
        /// </summary>
        private readonly IDictionary<String, Type> authSettings = new Dictionary<String, Type>()
        {
            { "TOKEN", typeof(TokenAuthorizationAccessBehavior) },
            { "BASIC", typeof(BasicAuthorizationAccessBehavior) },
            { "COOKIE", typeof(CookieAuthenticationBehavior) },
            { "NONE", null }
        };


        /// <summary>
        /// Gets the identifier of the docker feature
        /// </summary>
        public string Id => "WWW";

        /// <summary>
        /// Allows settings
        /// </summary>
        public IEnumerable<string> Settings => new String[] { ListenUriSetting, AuthenticationSetting, DefaultSolutionSetting, AllowClientCachingSetting };


        /// <summary>
        /// Create an endpoint config
        /// </summary>
        private RestEndpointConfiguration CreateEndpoint(String endpointUrl) => new Common.Configuration.RestEndpointConfiguration()
        {
            Address = endpointUrl,
            Contract = typeof(IWwwServiceContract),
            Behaviors = new List<Common.Configuration.RestEndpointBehaviorConfiguration>()
                            {
                                new RestEndpointBehaviorConfiguration(typeof(MessageCompressionEndpointBehavior)),
                                new RestEndpointBehaviorConfiguration(typeof(AcceptLanguageEndpointBehavior)),
                                new RestEndpointBehaviorConfiguration(typeof(WebCachingBehavior))
                            }
        };

        /// <inheritdoc cref="IDockerFeature.Configure(SanteDBConfiguration, IDictionary{string, string})"/>
        public void Configure(SanteDBConfiguration configuration, IDictionary<string, string> settings)
        {
            var restConfiguration = configuration.GetSection<SanteDB.Rest.Common.Configuration.RestConfigurationSection>();
            if (restConfiguration == null)
            {
                throw new ConfigurationException("Error retrieving REST configuration", configuration);
            }

            var wwwRestConfiguration = restConfiguration.Services.FirstOrDefault(o => o.ServiceType == typeof(IWwwServiceContract));
            if (wwwRestConfiguration == null) // add fhir rest config
            {
                wwwRestConfiguration = new RestServiceConfiguration()
                {
                    ServiceType = typeof(WwwServiceBehavior),
                    Behaviors = new List<RestServiceBehaviorConfiguration>()
                    {
                        new RestServiceBehaviorConfiguration(typeof(CookieAuthenticationBehavior)),
                        new RestServiceBehaviorConfiguration(typeof(WebErrorBehavior)),
                    },
                    ConfigurationName = WwwMessageHandler.ConfigurationName,
                    Endpoints = new List<Common.Configuration.RestEndpointConfiguration>()
                    {
                        this.CreateEndpoint("http://0.0.0.0:8080/")
                    }
                };
                restConfiguration.Services.Add(wwwRestConfiguration);
            }


            // Listen address
            if (settings.TryGetValue(ListenUriSetting, out string listen))
            {
                if (!Uri.TryCreate(listen, UriKind.Absolute, out Uri listenUri))
                {
                    throw new ArgumentOutOfRangeException($"{listen} is not a valid URL");
                }

                // Setup the endpoint
                wwwRestConfiguration.Endpoints.Clear();
                wwwRestConfiguration.Endpoints.Add(this.CreateEndpoint(listen));
            }

            // Authentication
            if (settings.TryGetValue(AuthenticationSetting, out string auth))
            {
                if (!this.authSettings.TryGetValue(auth.ToUpperInvariant(), out Type authType))
                {
                    throw new ArgumentOutOfRangeException($"Don't understand auth option {auth} allowed values {String.Join(",", this.authSettings.Keys)}");
                }

                // Add behavior
                if (authType != null)
                {
                    wwwRestConfiguration.Behaviors.Add(new RestServiceBehaviorConfiguration() { Type = authType });
                }
                else
                {
                    wwwRestConfiguration.Behaviors.RemoveAll(o => this.authSettings.Values.Any(v => v == o.Type));
                }
            }

            // Add services
            var serviceConfiguration = configuration.GetSection<ApplicationServiceContextConfigurationSection>().ServiceProviders;
            if (!serviceConfiguration.Any(s => s.Type == typeof(WwwMessageHandler)))
            {
                serviceConfiguration.Add(new TypeReferenceConfiguration(typeof(WwwMessageHandler)));
            }

            var appletConfiguration = configuration.GetSection<AppletConfigurationSection>();
            if (appletConfiguration == null)
            {
                configuration.AddSection(new AppletConfigurationSection());
            }

            if (settings.TryGetValue(DefaultSolutionSetting, out var defaultApp))
            {
                appletConfiguration.DefaultSolution = defaultApp;
            }
            if (settings.TryGetValue(DefaultAppletSetting, out defaultApp))
            {
                appletConfiguration.DefaultApplet = defaultApp;
            }

        }
    }
}
