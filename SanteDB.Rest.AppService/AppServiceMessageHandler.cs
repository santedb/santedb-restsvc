/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-5-30
 */
using RestSrvr;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Behavior;
using System;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;

namespace SanteDB.Rest.AppService
{
    /// <summary>
    /// Implementation of <see cref="IApiEndpointProvider"/> for the Application Service REST service
    /// </summary>
    /// <remarks>
    /// <para>The application service manager is used for end-user facing CDR deployments and provides methods for manipulating 
    /// the user environment</para>
    /// </remarks>
    [Description("Application Service")]
    [ApiServiceProvider("Application Interaction Interface", typeof(AppServiceBehavior))]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // Model classes - ignored
    public class AppServiceMessageHandler : IDaemonService, IApiEndpointProvider
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "App Service";

        /// <summary>
        /// Gets the contract type
        /// </summary>
        public Type BehaviorType => this.m_webHost.BehaviorType;

        /// <summary>
        /// Name of the service in the configuration file
        /// </summary>
        internal const string ConfigurationName = "APP";

        /// <summary>
        /// Resource handler tool
        /// </summary>
        internal static ResourceHandlerTool ResourceHandler { get; private set; }

        // HDSI Trace host
        private readonly Tracer m_traceSource = Tracer.GetTracer(typeof(AppServiceMessageHandler));
        private readonly IServiceManager m_serviceManager;
        private readonly IRestServiceFactory m_restFactory;


        // web host
        private RestService m_webHost;

        /// <summary>
        /// DI constructor
        /// </summary>
        public AppServiceMessageHandler(IServiceManager serviceManager, IRestServiceFactory restServiceFactory)
        {
            this.m_serviceManager = serviceManager;
            this.m_restFactory = restServiceFactory;
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
        /// Fired when the object is starting up
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Fired when the object is starting
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
        /// Capabilities
        /// </summary>
        public ServiceEndpointCapabilities Capabilities => (ServiceEndpointCapabilities)ApplicationServiceContext.Current.GetService<IRestServiceFactory>().GetServiceCapabilities(this.m_webHost);

        /// <summary>
        /// Get the API type
        /// </summary>
        public ServiceEndpointType ApiType => ServiceEndpointType.ApplicationControlService;

        /// <summary>
        /// The urls of the service
        /// </summary>
        public string[] Url => this.m_webHost.Endpoints.Select(o => o.Description.ListenUri.ToString()).ToArray();

        /// <summary>
        /// Start the service
        /// </summary>
        public bool Start()
        {
            // Don't start if we're in a test context
            if (ApplicationServiceContext.Current.HostType == SanteDBHostType.Server ||
                ApplicationServiceContext.Current.HostType == SanteDBHostType.Test)
            {
                return true;
            }

            try
            {
                this.Starting?.Invoke(this, EventArgs.Empty);

                AppServiceMessageHandler.ResourceHandler = new ResourceHandlerTool(
                    this.m_serviceManager.GetAllTypes()
                    .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IApiResourceHandler).IsAssignableFrom(t))
                    .ToList(), typeof(IAppServiceContract)
                );

                this.m_webHost = this.m_restFactory.CreateService(ConfigurationName);
                this.m_webHost.AddServiceBehavior(new ErrorServiceBehavior());

                // Add service behaviors
                foreach (ServiceEndpoint endpoint in this.m_webHost.Endpoints)
                {
                    this.m_traceSource.TraceInfo("Starting APP on {0}...", endpoint.Description.ListenUri);
                }

                // Start the webhost
                ApplicationServiceContext.Current.Started += (o, e) => this.m_webHost.Start();

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