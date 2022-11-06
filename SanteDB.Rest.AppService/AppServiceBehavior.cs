using Newtonsoft.Json.Linq;
using RestSrvr;
using RestSrvr.Attributes;
using RestSrvr.Exceptions;
using SanteDB.Client.Disconnected.Data.Synchronization;
using SanteDB.Client.Services;
using SanteDB.Client.Tickles;
using SanteDB.Core;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Patch;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.AppService.Configuration;
using SanteDB.Rest.AppService.Model;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml.XPath;

namespace SanteDB.Rest.AppService
{
    /// <summary>
    /// Application service behavior (APP)
    /// </summary>
    [ServiceBehavior(Name = AppServiceMessageHandler.ConfigurationName, InstanceMode = ServiceInstanceMode.Singleton)]
    public partial class AppServiceBehavior : IAppServiceContract, IDisposable
    {
        private readonly IConfigurationManager m_configurationManager;
        private readonly IServiceManager m_serviceManager;
        private readonly IPolicyEnforcementService m_policyEnforcementService;
        private readonly IUpstreamManagementService m_upstreamManagementService;
        private readonly ISynchronizationQueueManager m_synchronizationQueueManager;
        private readonly ISynchronizationService m_synchronizationService;
        private readonly ISynchronizationLogService m_synchronizationLogService;
        private readonly ITickleService m_tickleService;
        private readonly IPatchService m_patchService;
        private readonly IAppletManagerService m_appletManagerService;
        private readonly ILocalizationService m_localizationService;
        private readonly IUpdateManager m_updateManager;
        private readonly IIdentityProviderService m_identityProvider;
        private readonly ISecurityRepositoryService m_securityRepositoryService;
        private readonly IUserPreferencesManager m_userPreferenceManager;
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(AppServiceBehavior));
        private readonly Timer m_onlineStateTimer;
        private readonly IEnumerable<IRestConfigurationFeature> m_configurationFeatures;
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
                  ApplicationServiceContext.Current.GetService<ISynchronizationQueueManager>(),
                  ApplicationServiceContext.Current.GetService<ISynchronizationService>(),
                  ApplicationServiceContext.Current.GetService<ISynchronizationLogService>(),
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
            ISynchronizationQueueManager synchronizationQueueManager = null,
            ISynchronizationService synchronizationService = null,
            ISynchronizationLogService synchronizationLogService = null,
            ITickleService tickleService = null,
            IPatchService patchService = null)
        {
            this.m_configurationManager = configurationManager;
            this.m_serviceManager = serviceManager;
            this.m_policyEnforcementService = policyEnforcementService;
            this.m_upstreamManagementService = upstreamManagementService;
            this.m_synchronizationQueueManager = synchronizationQueueManager;
            this.m_synchronizationService = synchronizationService;
            this.m_synchronizationLogService = synchronizationLogService;
            this.m_tickleService = tickleService;
            this.m_patchService = patchService;
            this.m_appletManagerService = appletManagerService;
            this.m_localizationService = localizationService;
            this.m_updateManager = updateManager;
            this.m_identityProvider = identityProvider;
            this.m_securityRepositoryService = securityRepositoryService;
            this.m_userPreferenceManager = userPreferencesManager;

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
            }, null, 0, 60000);

            this.m_configurationFeatures = this.m_serviceManager.CreateInjectedOfAll<IRestConfigurationFeature>().ToList();
            this.m_upstreamSettings = this.m_upstreamManagementService.GetSettings();
            this.m_upstreamManagementService.RealmChanging += (o, e) => this.m_upstreamSettings = e.UpstreamRealmSettings;
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
