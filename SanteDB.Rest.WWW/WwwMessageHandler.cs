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
using SanteDB.Core.Interfaces;
using SanteDB.Core.Interop;
using SanteDB.Core.Services;
using SanteDB.Messaging.HDSI.Wcf;
using SanteDB.Rest.Common;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using SanteDB.Rest.Common.Behavior;
using SanteDB.Rest.WWW.Configuration;
using SanteDB.Rest.WWW.Behaviors;

namespace SanteDB.Rest.WWW
{
    /// <summary>
    /// Implementation of <see cref="IApiEndpointProvider"/> for the World Wide Web service
    /// </summary>
    /// <remarks>
    /// <para>The world wide web message handler is responsible for serving HTTP requests for web pages 
    /// </remarks>
    [ApiServiceProvider("WWW Interface", typeof(WwwServiceBehavior), configurationType: typeof(WwwServiceConfigurationSection), required: true)]
    public class WwwMessageHandler : IDaemonService
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "World-Wide-Web Interface";

        /// <summary>
        /// DI constructor
        /// </summary>
        public WwwMessageHandler(IServiceManager serviceManager, IRestServiceFactory restServiceFactory)
        {
            this.m_serviceManager = serviceManager;
            this.m_restServiceFactory = restServiceFactory;
        }
      
        // HDSI Trace host
        private readonly Tracer m_traceSource = Tracer.GetTracer(typeof(WwwMessageHandler));
        private readonly IServiceManager m_serviceManager;
        private readonly IRestServiceFactory m_restServiceFactory;


        /// <summary>
        /// Configuration name
        /// </summary>
        public const string ConfigurationName = "WWW";

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
        /// Start the service
        /// </summary>
        public bool Start()
        {
            // Don't start if we're in a test context
            if(ApplicationServiceContext.Current.HostType == SanteDBHostType.Test)
                return true;

            try
            {
                this.Starting?.Invoke(this, EventArgs.Empty);

                this.m_webHost = this.m_restServiceFactory.CreateService(ConfigurationName);
                this.m_webHost.AddServiceBehavior(new WebErrorBehavior());

                // Add service behaviors
                foreach (ServiceEndpoint endpoint in this.m_webHost.Endpoints)
                    this.m_traceSource.TraceInfo("Starting HDSI on {0}...", endpoint.Description.ListenUri);

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