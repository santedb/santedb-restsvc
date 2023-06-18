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
 * Date: 2023-5-19
 */
using RestSrvr.Attributes;
using SanteDB.Client.Configuration;
using SanteDB.Client.Services;
using SanteDB.Client.Tickles;
using SanteDB.Core;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SanteDB.Rest.AppService
{
    /// <summary>
    /// Application service behavior (APP)
    /// </summary>
    [ServiceBehavior(Name = AppServiceMessageHandler.ConfigurationName, InstanceMode = ServiceInstanceMode.Singleton)]
    public partial class AppServiceBehavior : IAppServiceContract, IDisposable
    {
        /// <summary>
        /// The configuration manager
        /// </summary>
        protected readonly IConfigurationManager m_configurationManager;
        /// <summary>
        /// The service manager injected into the instance
        /// </summary>
        protected readonly IServiceManager m_serviceManager;
        /// <summary>
        /// The policy enforcement service injected into the instance
        /// </summary>
        protected readonly IPolicyEnforcementService m_policyEnforcementService;
        /// <summary>
        /// The upstream management service injected into the instance
        /// </summary>
        protected readonly IUpstreamManagementService m_upstreamManagementService;
        /// <summary>
        /// The tickle service injected into the 
        /// </summary>
        protected readonly ITickleService m_tickleService;
        /// <summary>
        /// The patch service injected into the instance
        /// </summary>
        protected readonly IPatchService m_patchService;
        /// <summary>
        /// The applet manager service injected into the instance
        /// </summary>
        protected readonly IAppletManagerService m_appletManagerService;
        /// <summary>
        /// The localization service injected into the instance
        /// </summary>
        protected readonly ILocalizationService m_localizationService;
        /// <summary>
        /// the update manager service injected into the instance
        /// </summary>
        protected readonly IUpdateManager m_updateManager;
        /// <summary>
        /// The security repository service injected into the instance
        /// </summary>
        protected readonly ISecurityRepositoryService m_securityRepositoryService;
        /// <summary>
        /// The user preference manager injected into the instance
        /// </summary>
        protected readonly IUserPreferencesManager m_userPreferenceManager;
        private readonly IEnumerable<IUpstreamIntegrationPattern> m_integrationPatterns;
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(AppServiceBehavior));
        private readonly Timer m_onlineStateTimer;
        private readonly IEnumerable<IClientConfigurationFeature> m_configurationFeatures;
        private bool m_onlineState;
        private bool m_hdsiState;
        private bool m_amiState;
        private IUpstreamRealmSettings m_upstreamSettings;

        /// <summary>
        /// Instantiates a new instance of the behavior.
        /// </summary>
        public AppServiceBehavior()
            : this(
                  ApplicationServiceContext.Current.GetService<IConfigurationManager>(),
                  ApplicationServiceContext.Current.GetService<IServiceManager>(),
                  ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>(),
                  ApplicationServiceContext.Current.GetService<IUpstreamManagementService>(),
                  ApplicationServiceContext.Current.GetService<IIdentityProviderService>(),
                  ApplicationServiceContext.Current.GetService<IAppletManagerService>(),
                  ApplicationServiceContext.Current.GetService<ILocalizationService>(),
                  ApplicationServiceContext.Current.GetService<IUpdateManager>(),
                  ApplicationServiceContext.Current.GetService<IUserPreferencesManager>(),
                  ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>(),
                  ApplicationServiceContext.Current.GetService<ITickleService>(),
                  ApplicationServiceContext.Current.GetService<IPatchService>()
                  )
        { }


        /// <summary>
        /// DI constructor.
        /// </summary>
        public AppServiceBehavior(IConfigurationManager configurationManager,
            IServiceManager serviceManager,
            IPolicyEnforcementService policyEnforcementService,
            IUpstreamManagementService upstreamManagementService,
            IIdentityProviderService identityProvider,
            IAppletManagerService appletManagerService,
            ILocalizationService localizationService,
            IUpdateManager updateManager,
            IUserPreferencesManager userPreferencesManager = null,
            ISecurityRepositoryService securityRepositoryService = null,
            ITickleService tickleService = null,
            IPatchService patchService = null)
        {
            this.m_configurationManager = configurationManager;
            this.m_serviceManager = serviceManager;
            this.m_policyEnforcementService = policyEnforcementService;
            this.m_upstreamManagementService = upstreamManagementService;
            this.m_tickleService = tickleService;
            this.m_patchService = patchService;
            this.m_appletManagerService = appletManagerService;
            this.m_localizationService = localizationService;
            this.m_updateManager = updateManager;
            this.m_securityRepositoryService = securityRepositoryService;
            this.m_userPreferenceManager = userPreferencesManager;
            this.m_integrationPatterns = serviceManager.CreateInjectedOfAll<IUpstreamIntegrationPattern>();
            // The online status timer refresh
            this.m_onlineStateTimer = new Timer((e) =>
            {
                try
                {
                    var netService = ApplicationServiceContext.Current.GetService<INetworkInformationService>();
                    var upstreamService = ApplicationServiceContext.Current.GetService<IUpstreamAvailabilityProvider>(); // This can change when we join a realm

                    this.m_onlineState = netService.IsNetworkAvailable && netService.IsNetworkConnected;
                    this.m_hdsiState = upstreamService?.IsAvailable(ServiceEndpointType.HealthDataService) == true;
                    this.m_amiState = upstreamService?.IsAvailable(ServiceEndpointType.AdministrationIntegrationService) == true;
                }
                catch { }
            }, null, 0, 60000); //TODO: Config setting or constant defined elsewhere with a semantic name?

            this.m_configurationFeatures = this.m_serviceManager.CreateInjectedOfAll<IClientConfigurationFeature>().ToList();
            this.m_upstreamSettings = this.m_upstreamManagementService.GetSettings();
            this.m_upstreamManagementService.RealmChanging += (o, e) => this.m_upstreamSettings = e.UpstreamRealmSettings;
            this.m_appletManagerService.Changed += (o, e) => this.m_routes = null;
        }

        /// <summary>
        /// Dispose the state time
        /// </summary>
        public void Dispose()
        {
            this.m_onlineStateTimer.Dispose();
        }

    }
}
