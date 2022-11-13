using SanteDB.Client.Configuration;
using SanteDB.Client.Configuration.Upstream;
using SanteDB.Client.Upstream;
using SanteDB.Core.Configuration;
using SanteDB.Core.i18n;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.AppService.Configuration
{
    /// <summary>
    /// Configuration for saving REALM configuration
    /// </summary>
    public class RealmConfigurationFeature : IClientConfigurationFeature
    {
        public const string REALM_NAME = "address";
        public const string PORT_NUMBER = "port";
        public const string USE_TLS = "tls";
        public const string DEVICE_NAME = "device";
        public const string CLIENT_NAME = "client";
        public const string CLIENT_SECRET = "secret";
        public const string OVERRIDE_NAME = "override";
        public const string IS_JOINED = "joined";

        private readonly IConfigurationManager m_configurationManager;
        private readonly IUpstreamManagementService m_upstreamManager;
        private readonly UpstreamConfigurationSection m_configuration;


        /// <inheritdoc/>
        public String ReadPolicy => PermissionPolicyIdentifiers.AlterSystemConfiguration;

        /// <inheritdoc/>
        public String WritePolicy => PermissionPolicyIdentifiers.AlterSystemConfiguration;


        /// <summary>
        /// DI constructor
        /// </summary>
        public RealmConfigurationFeature(IConfigurationManager configurationManager,
            IUpstreamManagementService upstreamManagement)
        {
            this.m_configurationManager = configurationManager;
            this.m_upstreamManager = upstreamManagement;
            this.m_configuration = configurationManager.GetSection<UpstreamConfigurationSection>();
        }


        /// <summary>
        /// Refresh the configuration
        /// </summary>
        private ConfigurationDictionary<String, Object> Refresh()
        {
            var config = new ConfigurationDictionary<String, Object>();
            config.Add(IS_JOINED, this.m_upstreamManager.GetSettings() != null);
            config.Add(PORT_NUMBER, this.m_configuration.Realm?.PortNumber ?? 8080);
            config.Add(REALM_NAME, this.m_configuration.Realm?.DomainName);
            config.Add(USE_TLS, this.m_configuration.Realm?.UseTls ?? false);
            config.Add(DEVICE_NAME, this.m_configuration.Credentials.Find(o => o.CredentialType == UpstreamCredentialType.Device)?.CredentialName);
            var applicationCredential = this.m_configuration.Credentials.Find(o => o.CredentialType == UpstreamCredentialType.Application);
            config.Add(CLIENT_NAME, applicationCredential?.CredentialName);
            config.Add(CLIENT_SECRET, null);
            return config;
        }

        /// <inheritdoc/>
        /// This has to be configured first in the configuration file
        public int Order => Int32.MinValue;

        /// <inheritdoc/>
        public string Name => "realm";

        /// <inheritdoc/>
        public ConfigurationDictionary<String, Object> Configuration => this.Refresh();

        /// <inheritdoc/>
        public bool Configure(SanteDBConfiguration configuration, IDictionary<String, Object> featureConfiguration)
        {
            var section = configuration.GetSection<UpstreamConfigurationSection>();
            bool overide = featureConfiguration.TryGetValue(OVERRIDE_NAME, out object overr) && true.Equals(overr);
            if (section == null)
            {
                section = new UpstreamConfigurationSection();
                configuration.AddSection(section);
            }
            section.Realm = new UpstreamRealmConfiguration()
            {
                DomainName = featureConfiguration[REALM_NAME]?.ToString(),
                PortNumber = (int)((long?)featureConfiguration[PORT_NUMBER] ?? 8080),
                UseTls = (bool?)featureConfiguration[USE_TLS] ?? false
            };

            var deviceConfig = section.Credentials.Find(o => o.CredentialType == UpstreamCredentialType.Device);
            if (deviceConfig == null)
            {
                deviceConfig = new UpstreamCredentialConfiguration();
                deviceConfig.CredentialType = UpstreamCredentialType.Device;
                section.Credentials.Add(deviceConfig);
            }
            else if (!String.IsNullOrEmpty(deviceConfig.CredentialName) // there is a current device
                && !deviceConfig.CredentialName.Equals(featureConfiguration[DEVICE_NAME]?.ToString()) // the name has changed
                && !(this.m_configurationManager is InitialConfigurationManager)
                && !overide) // Not inital string {
            {
                throw new InvalidOperationException(ErrorMessages.DEVICE_NAME_CONFIGURATION_CHANGED);
            }
            deviceConfig.CredentialName = featureConfiguration[DEVICE_NAME]?.ToString() ?? deviceConfig.CredentialName;

            var applicationConfig = section.Credentials.Find(o => o.CredentialType == UpstreamCredentialType.Application);
            if (applicationConfig == null)
            {
                applicationConfig = new UpstreamCredentialConfiguration();
                applicationConfig.CredentialType = UpstreamCredentialType.Application;
                section.Credentials.Add(applicationConfig);
            }
            applicationConfig.CredentialName = featureConfiguration[CLIENT_NAME]?.ToString() ?? applicationConfig.CredentialName;
            applicationConfig.CredentialSecret = featureConfiguration[CLIENT_SECRET]?.ToString() ?? applicationConfig.CredentialSecret;

            return true;
        }
    }
}
