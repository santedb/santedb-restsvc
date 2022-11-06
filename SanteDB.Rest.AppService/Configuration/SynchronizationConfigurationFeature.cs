using SanteDB.Client.Disconnected.Data.Synchronization.Configuration;
using SanteDB.Core.Configuration;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
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
        private readonly SynchronizationConfigurationSection m_configuration;
        public const string MODE_SETTING = "mode";
        public const string OVERWRITE_SERVER_SETTING = "overwriteServer";
        public const string POLL_SETTING = "pollInterval";
        public const string BIG_BUNDLES_SETTING = "bigBundles";
        public const string ENABLED_SUBSCRIPTIONS_SETTING = "subscription";
        public const string SUBSCRIBED_OBJECTS_SETTING = "objects";
        public const string USE_PATCHES_SETTING = "usePatch";
        public const string FORBID_SYNC_SETTING = "forbidSync";

        /// <summary>
        /// DI ctor
        /// </summary>
        public SynchronizationConfigurationFeature(IConfigurationManager configurationManager)
        {
            this.m_configurationManager = configurationManager;
            this.m_configuration = this.m_configurationManager.GetSection<SynchronizationConfigurationSection>();
        }

        /// <inheritdoc/>
        public int Order => 0;

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
                { SUBSCRIBED_OBJECTS_SETTING, this.m_configuration?.SubscribedObjects?.Select(o => new { typ = o.TypeXml, id = o.Identifier }).ToList()  },
                { USE_PATCHES_SETTING, this.m_configuration?.UsePatches ?? false },
                { FORBID_SYNC_SETTING, this.m_configuration?.ForbidSending?.Select(o => o.TypeXml).ToList() }
            };

        /// <inheritdoc/>
        public bool Configure(SanteDBConfiguration configuration, IDictionary<string, object> featureConfiguration)
        {
            var configSection = configuration.GetSection<SynchronizationConfigurationSection>();
            if (configSection == null)
            {
                configSection = new SynchronizationConfigurationSection()
                {
                    SubscribedObjects = new List<SubscribedObjectConfiguration>(),
                    Subscriptions = new List<String>()
                };
                configuration.AddSection(configSection);
            }

            // Copy subscription settings over
            configSection.OverwriteServer = (bool?)featureConfiguration[OVERWRITE_SERVER_SETTING] ?? configSection.OverwriteServer;
            configSection.BigBundles = (bool?)featureConfiguration[BIG_BUNDLES_SETTING] ?? configSection.BigBundles;
            configSection.Mode = (SynchronizationMode?)featureConfiguration[MODE_SETTING] ?? configSection.Mode;
            configSection.PollIntervalXml = featureConfiguration[POLL_SETTING]?.ToString() ?? configSection.PollIntervalXml;
            configSection.Subscriptions = ((IEnumerable)featureConfiguration[ENABLED_SUBSCRIPTIONS_SETTING])?.OfType<String>().ToList();
            configSection.SubscribedObjects = ((IEnumerable)featureConfiguration[SUBSCRIBED_OBJECTS_SETTING])?.OfType<dynamic>()
                .Select(o => new SubscribedObjectConfiguration()
                {
                    TypeXml = (String)o.typ,
                    Identifier = Guid.Parse((String)o.id)
                }).ToList();

            configSection.ForbidSending = ((IEnumerable)featureConfiguration[FORBID_SYNC_SETTING])?.OfType<String>().Select(o => new ResourceTypeReferenceConfiguration(o)).ToList();
            return true;
        }
    }
}
