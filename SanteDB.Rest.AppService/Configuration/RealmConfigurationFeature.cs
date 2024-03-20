/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-6-21
 */
using SanteDB.Client.Configuration;
using SanteDB.Client.Configuration.Upstream;
using SanteDB.Core.Configuration;
using SanteDB.Core.i18n;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;

namespace SanteDB.Rest.AppService.Configuration
{
    /// <summary>
    /// Configuration for saving REALM configuration
    /// </summary>
    public class RealmConfigurationFeature : IClientConfigurationFeature
    {
        /// <summary>
        /// The name of the realm address property int he configuration dictionary
        /// </summary>
        public const string REALM_NAME = "address";
        /// <summary>
        /// The name of the port number in the configuration dictionary
        /// </summary>
        public const string PORT_NUMBER = "port";
        /// <summary>
        /// The name of the TLS configuration parameter in the dictionary
        /// </summary>
        public const string USE_TLS = "tls";
        /// <summary>
        /// The name of the device configuraiton property in the dictionary
        /// </summary>
        public const string DEVICE_NAME = "device";
        /// <summary>
        /// Th ename of the client name configuration property in the dictionary
        /// </summary>
        public const string CLIENT_NAME = "client";
        /// <summary>
        /// The name of the client secret configuraiton property in the dictionary
        /// </summary>
        public const string CLIENT_SECRET = "secret";
        /// <summary>
        /// The name of the override client secret configuration property in the dictionary
        /// </summary>
        public const string OVERRIDE_NAME = "override";
        /// <summary>
        /// The name of the joined indicator in the configuration property
        /// </summary>
        public const string IS_JOINED = "joined";

        private readonly IConfigurationManager m_configurationManager;
        private readonly IUpstreamManagementService m_upstreamManager;
        private readonly UpstreamConfigurationSection m_configuration;


        /// <inheritdoc/>
        public String ReadPolicy => PermissionPolicyIdentifiers.Login;

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
