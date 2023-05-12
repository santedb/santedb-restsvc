/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-3-10
 */
using RestSrvr;
using SanteDB;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Security;
using SanteDB.Rest.OAuth.Rest;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Tracing;
using System.Linq;

namespace SanteDB.Rest.OAuth
{
    /// <summary>
    /// Represents a <see cref="IApiEndpointProvider"/> which serves OpenID Connect and 
    /// OAUTH requests
    /// </summary>
    /// <remarks>
    /// <para>This service is responsible for starting and maintaining the <see cref="OAuthServiceBehavior"/> REST service which 
    /// is responsible for supporting SanteDB's <see href="https://help.santesuite.org/developers/service-apis/openid-connect">OpenID Connect</see> interface</para>
    /// </remarks>
    [ExcludeFromCodeCoverage]
    [ApiServiceProvider("OAuth 2.0 Messaging Provider", typeof(OAuthServiceBehavior), ServiceEndpointType.AuthenticationService)]
    public class OAuthMessageHandler : IDaemonService, IApiEndpointProvider
    {

        /// <summary>
        /// Configuration name of the rest service
        /// </summary>
        public const string ConfigurationName = "OAuth2";

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "OAuth 2.0 Messaging Service";

        // Trace source
        private readonly Tracer m_traceSource = new Tracer(OAuthConstants.TraceSourceName);

        // Service host
        private RestService m_serviceHost;

        /// <summary>
        /// Gets the contract type
        /// </summary>
        public Type BehaviorType => typeof(OAuthServiceBehavior);

        /// <summary>
        /// True if is running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return m_serviceHost?.IsRunning == true;
            }
        }

        /// <summary>
        /// API type
        /// </summary>
        public ServiceEndpointType ApiType
        {
            get
            {
                return ServiceEndpointType.AuthenticationService;
            }
        }

        /// <summary>
        /// Access control
        /// </summary>
        public string[] Url
        {
            get
            {
                return m_serviceHost.Endpoints.Select(o => o.Description.ListenUri.ToString()).ToArray();
            }
        }

        /// <summary>
        /// Capabilities
        /// </summary>
        public ServiceEndpointCapabilities Capabilities
        {
            get
            {
                var caps = ServiceEndpointCapabilities.None;
                if (m_serviceHost.ServiceBehaviors.OfType<ClientAuthorizationAccessBehavior>().Any())
                {
                    caps |= ServiceEndpointCapabilities.BasicAuth;
                }

                return caps;
            }
        }

        /// <summary>
        /// Fired when the service is starting
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Fired when the service is stopping
        /// </summary>
        public event EventHandler Starting;

        /// <summary>
        /// Fired when the service has stopped
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Fired when the service is stopping
        /// </summary>
        public event EventHandler Stopping;

        /// <summary>
        /// Start the specified message handler service
        /// </summary>
        public bool Start()
        {
            // Don't startup unless in SanteDB
            if (ApplicationServiceContext.Current.HostType == SanteDBHostType.Test)
            {
                return true;
            }

            try
            {
                Starting?.Invoke(this, EventArgs.Empty);

                m_serviceHost = ApplicationServiceContext.Current.GetService<IRestServiceFactory>().CreateService(ConfigurationName);
                m_serviceHost.AddServiceBehavior(new OAuthErrorBehavior());

                // Handles BASIC and X-SanteDB-DeviceAuthorization
                if (!m_serviceHost.ServiceBehaviors.OfType<ClientAuthorizationAccessBehavior>().Any())
                {
                    m_serviceHost.AddServiceBehavior(new ClientAuthorizationAccessBehavior());
                }
                // Handles when the user calls with a bearer token
                if (!m_serviceHost.ServiceBehaviors.OfType<TokenAuthorizationAccessBehavior>().Any())
                {
                    m_serviceHost.AddServiceBehavior(new TokenAuthorizationAccessBehavior());
                }

                // Start the webhost
                m_serviceHost.Start();
                m_traceSource.TraceInfo("OAUTH2 On: {0}", m_serviceHost.Endpoints.First().Description.ListenUri);

                Started?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception e)
            {
                m_traceSource.TraceEvent(EventLevel.Error, e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Stop the handler
        /// </summary>
        public bool Stop()
        {
            Stopping?.Invoke(this, EventArgs.Empty);

            if (m_serviceHost != null)
            {
                m_serviceHost.Stop();
                m_serviceHost = null;
            }

            Stopped?.Invoke(this, EventArgs.Empty);
            return true;
        }
    }
}