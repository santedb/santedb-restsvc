﻿/*
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
using SanteDB.Messaging.HDSI.Wcf;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Behavior;
using SanteDB.Rest.HDSI.Configuration;
using System;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;

namespace SanteDB.Rest.HDSI
{
    /// <summary>
    /// Implementation of <see cref="IApiEndpointProvider"/> for the Health Data Services Interface REST service
    /// </summary>
    /// <remarks>
    /// <para>The HDSI message handler is responsible for the maintenance and lifecycle of SanteDB's 
    /// <see href="https://help.santesuite.org/developers/service-apis/health-data-service-interface-hdsi">Health Data Service Interface</see>. The service
    /// starts the necessary REST and query services on start and tears them down on system shutdown.</para>
    /// </remarks>
    [Description("The primary iCDR Health Data Messaging Service (HDSI) allows sharing of RIM objects in XML or JSON over HTTP")]
    [ApiServiceProvider("Health Data Services Interface", typeof(HdsiServiceBehavior), configurationType: typeof(HdsiConfigurationSection), required: true)]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // Model classes - ignored
    public class AppServiceMessageHandler : IDaemonService, IApiEndpointProvider
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "iCDR Primary Clinical Messaging Interface";

        /// <summary>
        /// Gets the contract type
        /// </summary>
        public Type BehaviorType => this.m_webHost.BehaviorType;

        /// <summary>
        /// Name of the service in the configuration file
        /// </summary>
        internal const string ConfigurationName = "HDSI";

        /// <summary>
        /// Resource handler tool
        /// </summary>
        internal static ResourceHandlerTool ResourceHandler { get; private set; }

        // HDSI Trace host
        private readonly Tracer m_traceSource = new Tracer(HdsiConstants.TraceSourceName);

        // configuration
        private HdsiConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<HdsiConfigurationSection>();

        // web host
        private RestService m_webHost;

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
        /// Gets the API type
        /// </summary>
        public ServiceEndpointType ApiType
        {
            get
            {
                return ServiceEndpointType.HealthDataService;
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
        /// Start the service
        /// </summary>
        public bool Start()
        {
            // Don't start if we're in a test context
            if (!Assembly.GetEntryAssembly().GetName().Name.StartsWith("SanteDB"))
            {
                return true;
            }

            try
            {
                this.Starting?.Invoke(this, EventArgs.Empty);

                var serviceManager = ApplicationServiceContext.Current.GetService<IServiceManager>();
                // Force startup
                if (this.m_configuration?.ResourceHandlers.Count() > 0)
                {
                    AppServiceMessageHandler.ResourceHandler = new ResourceHandlerTool(this.m_configuration.ResourceHandlers.Select(o => o.Type), typeof(IHdsiServiceContract));
                }
                else
                {
                    AppServiceMessageHandler.ResourceHandler = new ResourceHandlerTool(

                        serviceManager.GetAllTypes()
                        .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IApiResourceHandler).IsAssignableFrom(t))
                        .ToList(), typeof(IHdsiServiceContract)
                    );
                }

                this.m_webHost = ApplicationServiceContext.Current.GetService<IRestServiceFactory>().CreateService(ConfigurationName);
                this.m_webHost.AddServiceBehavior(new ErrorServiceBehavior());

                // Add service behaviors
                foreach (ServiceEndpoint endpoint in this.m_webHost.Endpoints)
                {
                    this.m_traceSource.TraceInfo("Starting HDSI on {0}...", endpoint.Description.ListenUri);
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