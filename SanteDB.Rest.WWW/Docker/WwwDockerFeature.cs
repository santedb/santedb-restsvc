using SanteDB.Core.Configuration;
using SanteDB.Core.Exceptions;
using SanteDB.Docker.Core;
using SanteDB.Rest.Common.Behavior;
using SanteDB.Rest.Common.Configuration;
using SanteDB.Rest.Common.Security;
using SanteDB.Rest.WWW.Behaviors;
using SanteDB.Rest.WWW.Configuration;
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

            var wwwConfiguration = configuration.GetSection<WwwServiceConfigurationSection>();
            if (wwwConfiguration == null)
            {
                configuration.AddSection(new WwwServiceConfigurationSection());
            }

            if (settings.TryGetValue(DefaultSolutionSetting, out var defaultApp))
            {
                wwwConfiguration.Solution = defaultApp;
            }
            if (settings.TryGetValue(AllowClientCachingSetting, out var allowCachingString) && Boolean.TryParse(allowCachingString, out var allowCaching))
            {
                wwwConfiguration.AllowClientCaching = allowCaching;
            }
        }
    }
}
