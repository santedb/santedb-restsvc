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
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;

namespace SanteDB.Rest.AppService.Configuration
{
    /// <summary>
    /// Solution configuration feature
    /// </summary>
    public class SolutionConfigurationFeature : IClientConfigurationFeature
    {

        /// <summary>
        /// Solution name configuration setting
        /// </summary>
        public const string SOLUTION_NAME_CONFIG = "solution";
        /// <summary>
        /// Auto update configuration
        /// </summary>
        public const string AUTO_UPDATE_CONFG = "autoUpdate";
        private readonly ClientConfigurationSection m_configuration;

        /// <summary>
        /// DI constructor
        /// </summary>
        public SolutionConfigurationFeature(IConfigurationManager configurationManager)
        {
            this.m_configuration = configurationManager.GetSection<ClientConfigurationSection>();
        }

        /// <summary>
        /// Gets the order in the configuration
        /// </summary>
        public int Order => 0;

        /// <inheritdoc/>
        public string Name => "applet";

        /// <inheritdoc/>
        public ConfigurationDictionary<string, object> Configuration => this.GetConfiguration();

        /// <summary>
        /// Get configuration
        /// </summary>
        private ConfigurationDictionary<string, object> GetConfiguration() => new ConfigurationDictionary<string, object>()
            {
                { SOLUTION_NAME_CONFIG, this.m_configuration?.UiSolution },
                { AUTO_UPDATE_CONFG, this.m_configuration?.AutoUpdateApplets }
            };


        /// <inheritdoc/>
        public String ReadPolicy => PermissionPolicyIdentifiers.AlterSystemConfiguration;

        /// <inheritdoc/>
        public String WritePolicy => PermissionPolicyIdentifiers.AlterSystemConfiguration;

        /// <inheritdoc/>
        public bool Configure(SanteDBConfiguration configuration, IDictionary<string, object> featureConfiguration)
        {
            var section = configuration.GetSection<ClientConfigurationSection>();
            if (section == null)
            {
                section = new ClientConfigurationSection();
                configuration.AddSection(section);
            }
            section.AutoUpdateApplets = (bool?)featureConfiguration[AUTO_UPDATE_CONFG] ?? true;
            section.UiSolution = featureConfiguration[SOLUTION_NAME_CONFIG]?.ToString();
            return true;
        }
    }
}
