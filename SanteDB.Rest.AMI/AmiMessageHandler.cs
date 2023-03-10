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
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Serialization;
using SanteDB.Core.Services;
using SanteDB.Rest.AMI.Configuration;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Behavior;
using System;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;

namespace SanteDB.Rest.AMI
{

    /// <summary>
    /// An implementation of the <see cref="IApiEndpointProvider"/> which hosts and manages the 
    /// <see href="https://help.santesuite.org/developers/service-apis/administration-management-interface-ami">Administrative Management Interface</see> REST services.
    /// </summary>
    /// <remarks>
    /// <para>This service is responsible for starting up and shutting down the REST services for the AMI, as well as </para>
    /// </remarks>
    [Description("The AMI provides administrative operations for SanteDB over HTTP")]
    [ApiServiceProvider("Administrative Management Interface", typeof(AmiServiceBehavior), ServiceEndpointType.AdministrationIntegrationService, Configuration = typeof(AmiConfigurationSection), Required = true)]
    public class AmiMessageHandler : IDaemonService, IApiEndpointProvider
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "Administrative Management Interface Daemon";

        /// <summary>
        /// Gets the contract type
        /// </summary>
        public Type BehaviorType => this.m_webHost.BehaviorType;

        /// <summary>
        /// Name of the service in the configuration file
        /// </summary>
        public const string ConfigurationName = "AMI";

        // Configuration
        private readonly AmiConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<AmiConfigurationSection>();

        /// <summary>
        /// Resource handler tool
        /// </summary>
        internal static ResourceHandlerTool ResourceHandler { get; private set; }

        /// <summary>
        /// The internal reference to the trace source.
        /// </summary>
        private readonly Tracer m_traceSource = Tracer.GetTracer(typeof(AmiMessageHandler));

        // web host
        private RestService m_webHost;

        /// <summary>
        /// Fired when the object is starting up.
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Fired when the object is starting.
        /// </summary>
        public event EventHandler Starting;

        /// <summary>
        /// Fired when the service has stopped.
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Fired when the service is stopping.
        /// </summary>
        public event EventHandler Stopping;

        /// <summary>
        /// Gets the API type
        /// </summary>
        public ServiceEndpointType ApiType
        {
            get
            {
                return ServiceEndpointType.AdministrationIntegrationService;
            }
        }

        /// <summary>
        /// Capabilities
        /// </summary>
        public ServiceEndpointCapabilities Capabilities
        {
            get
            {
                return (ServiceEndpointCapabilities)ApplicationServiceContext.Current.GetService<IRestServiceFactory>().GetServiceCapabilities(this.m_webHost);
            }
        }

        /// <summary>
        /// True if running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return this.m_webHost?.IsRunning == true;
            }
        }

        /// <summary>
        /// URL of the service
        /// </summary>
        public string[] Url
        {
            get
            {
                return this.m_webHost.Endpoints.OfType<ServiceEndpoint>().Select(o => o.Description.ListenUri.ToString()).ToArray();
            }
        }

        /// <summary>
        /// Start the service
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
                this.Starting?.Invoke(this, EventArgs.Empty);

                //Setup the res handler before the service is instantiated.
                if (this.m_configuration?.ResourceHandlers.Count() > 0)
                {
                    AmiMessageHandler.ResourceHandler = new ResourceHandlerTool(this.m_configuration.ResourceHandlers, typeof(IAmiServiceContract));
                }
                else
                {
                    AmiMessageHandler.ResourceHandler = new ResourceHandlerTool(
                        ApplicationServiceContext.Current.GetService<IServiceManager>().GetAllTypes()
                        .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IApiResourceHandler).IsAssignableFrom(t))
                        .ToList(),
                        typeof(IAmiServiceContract)
                    );
                }

                this.m_webHost = ApplicationServiceContext.Current.GetService<IRestServiceFactory>().CreateService(ConfigurationName);
                this.m_webHost.AddServiceBehavior(new ErrorServiceBehavior());

                // Add service behaviors
                foreach (ServiceEndpoint endpoint in this.m_webHost.Endpoints)
                {
                    this.m_traceSource.TraceInfo("Starting AMI on {0}...", endpoint.Description.ListenUri);
                }

                // Start the webhost
                this.m_webHost.Start();
                ModelSerializationBinder.RegisterModelType(typeof(SecurityPolicyInfo));

                this.Started?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error, e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Stop the HDSI service
        /// </summary>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);

            if (this.m_webHost != null)
            {
                this.m_webHost.Stop();
                this.m_webHost = null;
            }

            this.Stopped?.Invoke(this, EventArgs.Empty);

            return true;
        }
    }
}