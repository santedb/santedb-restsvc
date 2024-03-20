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
using SanteDB.Core.Configuration;
using SanteDB.Core.i18n;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Rest.AppService.Configuration
{
    /// <summary>
    /// Integration mode rest configuration
    /// </summary>
    public class IntegrationModeRestConfigurationFeature : IClientConfigurationFeature
    {

        /// <summary>
        /// Get the mode of configuration
        /// </summary>
        public const string MODE_SETTING = "mode";

        // Integration patterns
        private IEnumerable<IUpstreamIntegrationPattern> m_integrationPatterns;
        private readonly ApplicationServiceContextConfigurationSection m_appConfiguration;

        /// <summary>
        /// DI constructor
        /// </summary>
        public IntegrationModeRestConfigurationFeature(IServiceManager serviceManager, IConfigurationManager configurationManager)
        {
            this.m_integrationPatterns = serviceManager.CreateInjectedOfAll<IUpstreamIntegrationPattern>();
            this.m_appConfiguration = configurationManager.GetSection<ApplicationServiceContextConfigurationSection>();
        }

        /// <summary>
        /// The order of this configuration being applied
        /// </summary>
        public int Order => 0;

        /// <summary>
        /// Get the name
        /// </summary>
        public string Name => "integration";

        /// <summary>
        /// Gets the configuration
        /// </summary>
        public ConfigurationDictionary<string, object> Configuration => new ConfigurationDictionary<string, object>()
        {
            { MODE_SETTING, this.m_integrationPatterns.FirstOrDefault(p=>p.GetServices().All(s=>this.m_appConfiguration.ServiceProviders.Any(c=>c.Type == s)))?.Name }
        };

        /// <summary>
        /// Read cofiguration policy
        /// </summary>
        public string ReadPolicy => PermissionPolicyIdentifiers.AlterSystemConfiguration;

        /// <summary>
        /// Write policy
        /// </summary>
        public string WritePolicy => PermissionPolicyIdentifiers.AlterSystemConfiguration;

        /// <summary>
        /// Configure the section
        /// </summary>
        public bool Configure(SanteDBConfiguration configuration, IDictionary<string, object> featureConfiguration)
        {
            var appSetting = configuration.GetSection<ApplicationServiceContextConfigurationSection>();
            if (featureConfiguration.TryGetValue(MODE_SETTING, out var modeRaw))
            {
                // Remove old services
                var oldMode = this.m_integrationPatterns.FirstOrDefault(o => o.Name == (this.Configuration[MODE_SETTING]?.ToString() ?? OnlineIntegrationPattern.INTEGRATION_PATTERN_NAME))?.GetServices();
                if (oldMode != null)
                {
                    appSetting.ServiceProviders.RemoveAll(s => oldMode.Contains(s.Type));
                }
                var newMode = this.m_integrationPatterns.First(o => o.Name == modeRaw.ToString());
                if (newMode == null)
                {
                    throw new InvalidOperationException(String.Format(ErrorMessages.TYPE_NOT_FOUND, modeRaw));
                }
                else
                {
                    appSetting.ServiceProviders.AddRange(newMode.GetServices().Select(o => new TypeReferenceConfiguration(o)));
                }
                newMode.SetDefaults(configuration);

                appSetting.AddAppSetting("integration-mode", modeRaw.ToString());
            }
            return true;
        }
    }
}
