using SanteDB.Client.Configuration;
using SanteDB.Client.Configuration.Upstream;
using SanteDB.Client.Upstream;
using SanteDB.Core.Configuration;
using SanteDB.Core.i18n;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.AppService.Configuration
{
    /// <summary>
    /// Configuration for saving REALM configuration
    /// </summary>
    public class RealmConfigurationFeature : IRestConfigurationFeature
    {
        public const string REALM_NAME = "address";
        public const string PORT_NUMBER = "port";
        public const string USE_TLS = "tls";
        public const string DEVICE_NAME = "device";
        public const string CLIENT_NAME = "client";
        public const string CLIENT_SECRET = "secret";
        public const string OVERRIDE_NAME = "override";

        private readonly IConfigurationManager m_configurationManager;
        private readonly IUpstreamManagementService m_upstreamManager;

        /// <summary>
        /// DI constructor
        /// </summary>
        public RealmConfigurationFeature(IConfigurationManager configurationManager,
            IUpstreamManagementService upstreamManagement)
        {
            this.m_configurationManager = configurationManager;
            this.m_upstreamManager = upstreamManagement;
            var rawConfiguration = configurationManager.GetSection<UpstreamConfigurationSection>();
            this.Configuration = new RestConfigurationDictionary<String, Object>();

            this.Configuration.Add(PORT_NUMBER, rawConfiguration.Realm?.PortNumber ?? 8080);
            this.Configuration.Add(REALM_NAME, rawConfiguration.Realm?.DomainName);
            this.Configuration.Add(USE_TLS, rawConfiguration.Realm?.UseTls ?? false);
            this.Configuration.Add(DEVICE_NAME, rawConfiguration.Credentials.Find(o => o.CredentialType == UpstreamCredentialType.Device)?.CredentialName);
            var applicationCredential = rawConfiguration.Credentials.Find(o => o.CredentialType == UpstreamCredentialType.Application);
            this.Configuration.Add(CLIENT_NAME, applicationCredential?.CredentialName);
            this.Configuration.Add(CLIENT_SECRET, null);
        }

        /// <inheritdoc/>
        public int Order => Int32.MinValue;

        /// <inheritdoc/>
        public string Name => "realm";

        /// <inheritdoc/>
        public RestConfigurationDictionary<String, Object> Configuration { get; }

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
                PortNumber = (int?)featureConfiguration[PORT_NUMBER] ?? 8080,
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

            // Join the realm
            this.m_upstreamManager.Join(new ConfiguredUpstreamRealmSettings(section), overide);
            return true;
        }
    }
}
