using Newtonsoft.Json.Linq;
using SanteDB.Client.Disconnected.Data.Synchronization;
using SanteDB.Client.Disconnected.Data.Synchronization.Configuration;
using SanteDB.Client.Repositories;
using SanteDB.Client.Upstream.Management;
using SanteDB.Client.Upstream.Repositories;
using SanteDB.Client.Upstream.Security;
using SanteDB.Core.Configuration;
using SanteDB.Core.Data;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.AMI.Configuration;
using SanteDB.Rest.BIS.Configuration;
using SanteDB.Rest.HDSI.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace SanteDB.Rest.AppService.Configuration
{
    /// <summary>
    /// Synchronization configuration feature
    /// </summary>
    public class SynchronizationConfigurationFeature : IRestConfigurationFeature
    {
        private readonly IConfigurationManager m_configurationManager;
        private readonly ISubscriptionRepository m_subscriptionRepository;
        private readonly SynchronizationConfigurationSection m_configuration;

        public const string MODE_SETTING = "mode";
        public const string OVERWRITE_SERVER_SETTING = "overwriteServer";
        public const string POLL_SETTING = "pollInterval";
        public const string BIG_BUNDLES_SETTING = "bigBundles";
        public const string ENABLED_SUBSCRIPTIONS_SETTING = "subscription";
        public const string SUBSCRIBED_OBJECT_TYPE_SETTING = "subscribeTo";
        public const string SUBSCRIBED_OBJECTS_SETTING = "subscribeObjects";
        public const string USE_PATCHES_SETTING = "usePatch";
        public const string FORBID_SYNC_SETTING = "forbidSync";

        /// <summary>
        /// DI ctor
        /// </summary>
        public SynchronizationConfigurationFeature(IConfigurationManager configurationManager, ISubscriptionRepository subscriptionRepository)
        {
            this.m_configurationManager = configurationManager;
            this.m_subscriptionRepository = subscriptionRepository;
            this.m_configuration = this.m_configurationManager.GetSection<SynchronizationConfigurationSection>();
        }

        /// <inheritdoc/>
        public int Order => 100;

        /// <inheritdoc/>
        public string Name => "sync";

        /// <inheritdoc/>
        public RestConfigurationDictionary<string, object> Configuration => this.GetConfiguration();

        /// <inheritdoc/>
        public String ReadPolicy => PermissionPolicyIdentifiers.Login;

        /// <inheritdoc/>
        public String WritePolicy => PermissionPolicyIdentifiers.AccessClientAdministrativeFunction;

        /// <summary>
        /// Get configuration
        /// </summary>
        private RestConfigurationDictionary<string, object> GetConfiguration() => new RestConfigurationDictionary<string, object>()
            {
                { MODE_SETTING, this.m_configuration?.Mode ?? SynchronizationMode.Online },
                { OVERWRITE_SERVER_SETTING, this.m_configuration?.OverwriteServer ?? false },
                { POLL_SETTING, this.m_configuration?.PollIntervalXml ?? "PT15M" },
                { BIG_BUNDLES_SETTING, this.m_configuration?.BigBundles ?? false },
                { ENABLED_SUBSCRIPTIONS_SETTING, this.m_configuration?.Subscriptions?.ToList() },
                { SUBSCRIBED_OBJECT_TYPE_SETTING, this.m_configuration?.SubscribeToResource?.TypeXml },
                { SUBSCRIBED_OBJECTS_SETTING, this.m_configuration?.SubscribedObjects?.ToList()  },
                { USE_PATCHES_SETTING, this.m_configuration?.UsePatches ?? false },
                { FORBID_SYNC_SETTING, this.m_configuration?.ForbidSending?.Select(o => o.TypeXml).ToList() }
            };

        /// <inheritdoc/>
        public bool Configure(SanteDBConfiguration configuration, IDictionary<string, object> featureConfiguration)
        {
            var configSection = configuration.GetSection<SynchronizationConfigurationSection>();
            var appSection = configuration.GetSection<ApplicationServiceContextConfigurationSection>();

            if (configSection == null)
            {
                configSection = new SynchronizationConfigurationSection()
                {
                    SubscribedObjects = new List<Guid>(),
                    Subscriptions = new List<Guid>()
                };
                configuration.AddSection(configSection);
            }

            // Copy subscription settings over
            configSection.OverwriteServer = (bool?)featureConfiguration[OVERWRITE_SERVER_SETTING] ?? configSection.OverwriteServer;
            configSection.BigBundles = (bool?)featureConfiguration[BIG_BUNDLES_SETTING] ?? configSection.BigBundles;

            if (Enum.TryParse<SynchronizationMode>(featureConfiguration[MODE_SETTING]?.ToString(), out var syncMode))
            {
                configSection.Mode = syncMode;
            }
            configSection.PollIntervalXml = featureConfiguration[POLL_SETTING]?.ToString() ?? configSection.PollIntervalXml;
            configSection.Subscriptions = ((IEnumerable)featureConfiguration[ENABLED_SUBSCRIPTIONS_SETTING])?.OfType<String>().Select(o => Guid.Parse(o)).ToList();
            configSection.SubscribedObjects = ((IEnumerable)featureConfiguration[SUBSCRIBED_OBJECTS_SETTING])?.OfType<String>().Select(o => Guid.Parse(o)).ToList();
            configSection.ForbidSending = ((IEnumerable)featureConfiguration[FORBID_SYNC_SETTING])?.OfType<String>().Select(o => new ResourceTypeReferenceConfiguration(o)).ToList();

            // TODO: Change the services for those in the modes - ALL - ONLINE - SUBSCRIBE
            appSection.ServiceProviders.RemoveAll(o =>
                {
                    try
                    {
                        return o.Type.Implements(typeof(IRepositoryService)) ||
                        o.Type.Implements(typeof(IIdentityProviderService)) ||
                        o.Type.Implements(typeof(IDeviceIdentityProviderService)) ||
                        o.Type.Implements(typeof(IApplicationIdentityProviderService)) ||
                        o.Type.Implements(typeof(ISynchronizationQueueManager)) ||
                        o.Type.Implements(typeof(IAliasProvider)) ||
                        o.Type.Implements(typeof(IDataPersistenceService)) ||
                        o.Type.Implements(typeof(ISynchronizationLogService)) ||
                        o.Type.Implements(typeof(ISynchronizationService)) ||
                        o.Type.Implements(typeof(ITagPersistenceService)) ||
                        o.Type.Implements(typeof(IAuditDispatchService)) ||
                        o.Type.Implements(typeof(IMailMessageService)) ||
                        o.Type.Implements(typeof(IFreetextSearchService)) ||
                        o.Type.Implements(typeof(IPolicyInformationService)) ||
                        o.Type.Implements(typeof(IRoleProviderService)) ||
                        o.Type.Implements(typeof(ISecurityRepositoryService)) ||
                        typeof(UpstreamRepositoryFactory).IsAssignableFrom(o.Type) ||
                        o.Type.Implements(typeof(ISecurityChallengeIdentityService));
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException($"Error determining service validity for {o.TypeXml}", e);
                    }
                });

            switch (configSection.Mode)
            {
                case SynchronizationMode.Online:
                    appSection.ServiceProviders.AddRange(new TypeReferenceConfiguration[]
                    {
                        new TypeReferenceConfiguration(typeof(UpstreamJobManager)),
                        new TypeReferenceConfiguration(typeof(UpstreamRepositoryFactory)),
                        new TypeReferenceConfiguration(typeof(UpstreamIdentityProvider)),
                        new TypeReferenceConfiguration(typeof(UpstreamApplicationIdentityProvider)),
                        new TypeReferenceConfiguration(typeof(UpstreamPolicyInformationService)),
                        new TypeReferenceConfiguration(typeof(UpstreamRoleProviderService)),
                        new TypeReferenceConfiguration(typeof(UpstreamSecurityRepository)),
                        new TypeReferenceConfiguration(typeof(UpstreamSecurityChallengeProvider)),
                        new TypeReferenceConfiguration(typeof(RepositoryEntitySource))
                    });
                    var hdsiConfigurationSection = configuration.GetSection<HdsiConfigurationSection>() ?? configuration.AddSection(new HdsiConfigurationSection());
                    var amiConfigurationSection = configuration.GetSection<AmiConfigurationSection>() ?? configuration.AddSection(new AmiConfigurationSection());
                    var bisConfigurationSection = configuration.GetSection<BisServiceConfigurationSection>() ?? configuration.AddSection(new BisServiceConfigurationSection());
                    hdsiConfigurationSection.AutomaticallyForwardRequests = true;
                    amiConfigurationSection.AutomaticallyForwardRequests = false;
                    bisConfigurationSection.AutomaticallyForwardRequests = true;
                    break;
                case SynchronizationMode.All:
                    // Add a subscription for the all type 
                    configSection.Subscriptions = m_subscriptionRepository.Find(o => true).Where(d => d.ClientDefinitions.Any(c => c.Mode.HasFlag(Core.Model.Subscription.SubscriptionModeType.All))).Select(o => o.Uuid).ToList();
                    goto case SynchronizationMode.Subscription;
                case SynchronizationMode.Subscription:
                    if (featureConfiguration.TryGetValue(ENABLED_SUBSCRIPTIONS_SETTING, out var subscriptionValueRaw) && subscriptionValueRaw is JArray subscriptionValueJarray)
                    {
                        configSection.Subscriptions = subscriptionValueJarray.Select(o => Guid.Parse(o.ToString())).ToList();
                        configSection.SubscribeToResource = new ResourceTypeReferenceConfiguration(featureConfiguration[SUBSCRIBED_OBJECT_TYPE_SETTING].ToString());
                        configSection.SubscribedObjects = ((JArray)featureConfiguration[SUBSCRIBED_OBJECTS_SETTING])?.Select(o => Guid.Parse(o.ToString())).ToList();
                    }

                    // Add synchronization services
                    appSection.ServiceProviders.AddRange(new TypeReferenceConfiguration[]
                    {
                        new TypeReferenceConfiguration(typeof(UpstreamSynchronizationService)),
                        new TypeReferenceConfiguration(typeof(UpstreamIdentityProvider)),
                        new TypeReferenceConfiguration(typeof(UpstreamApplicationIdentityProvider)),
                        new TypeReferenceConfiguration(typeof(UpstreamPolicyInformationService)),
                        new TypeReferenceConfiguration(typeof(UpstreamRoleProviderService)),
                        new TypeReferenceConfiguration(typeof(UpstreamSecurityRepository)),
                        new TypeReferenceConfiguration(typeof(UpstreamSecurityChallengeProvider)),
                        new TypeReferenceConfiguration(typeof(RepositoryEntitySource)),
                        new TypeReferenceConfiguration(typeof(DefaultSynchronizationLogService)),
                        new TypeReferenceConfiguration(typeof(DefaultSynchronizationQueueManager))
                    });
                    break;

                default:
                    throw new NotSupportedException();
            }
            return true;
        }
    }
}
