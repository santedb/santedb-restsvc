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
 */
using Newtonsoft.Json.Linq;
using SanteDB.BI.Configuration;
using SanteDB.Client.Configuration;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Http;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AppService.Configuration
{
    /// <summary>
    /// BI configuration feature
    /// </summary>
    public class BiConfigurationFeature : IClientConfigurationFeature
    {
        /// <summary>
        /// Maximum result set size
        /// </summary>
        public const string MAX_RESULT_SIZE = "maxResults";
        /// <summary>
        /// Automatically register datamarts
        /// </summary>
        public const string AUTO_REGISTER_DM = "autoMarts";

        /// <inheritdoc/>
        public int Order => Int32.MaxValue;

        /// <inheritdoc/>
        public string Name => "bi";

        private readonly BiConfigurationSection m_configuration;

        /// <summary>
        /// Rest constructor
        /// </summary>
        public BiConfigurationFeature(IConfigurationManager configurationManager)
        {
            this.m_configuration = configurationManager.GetSection<BiConfigurationSection>();
        }

        /// <inheritdoc/>
        public ConfigurationDictionary<string, object> Configuration => this.GetConfiguration();

        /// <summary>
        /// Regenerate or refresh the configuration
        /// </summary>
        private ConfigurationDictionary<string, object> GetConfiguration() => new ConfigurationDictionary<string, object>()
        {
            { MAX_RESULT_SIZE, this.m_configuration?.MaxBiResultSetSize ?? 10_000},
            { AUTO_REGISTER_DM, this.m_configuration?.AutoRegisterDatamarts?.ToArray() }
        };

        /// <inheritdoc/>
        public string ReadPolicy => PermissionPolicyIdentifiers.AlterSystemConfiguration;

        /// <inheritdoc/>
        public string WritePolicy => PermissionPolicyIdentifiers.AlterSystemConfiguration;

        /// <inheritdoc/>
        public bool Configure(SanteDBConfiguration configuration, IDictionary<string, object> featureConfiguration)
        {
            var section = configuration.GetSection<BiConfigurationSection>();
            if (section == null)
            {
                section = new BiConfigurationSection();
                configuration.AddSection(section);
            }

            if (featureConfiguration.TryGetValue(MAX_RESULT_SIZE, out var maxResultRaw) && (maxResultRaw is Int32 maxResult || Int32.TryParse(maxResultRaw.ToString(), out maxResult)))
            {
                section.MaxBiResultSetSize = maxResult;
            }

            if(featureConfiguration.TryGetValue(AUTO_REGISTER_DM, out var autoRegisterRaw) && autoRegisterRaw is JArray autoRegister)
            {
                section.AutoRegisterDatamarts = autoRegister.Select(o => o.Value<String>()).ToList();
            }

            return true;
        }
    }
}
