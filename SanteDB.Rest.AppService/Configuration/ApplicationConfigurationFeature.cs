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
 * Date: 2023-3-10
 */
using Newtonsoft.Json.Linq;
using SanteDB.Client.Configuration;
using SanteDB.Core.Configuration;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Rest.AppService.Configuration
{
    /// <summary>
    /// The application service configuration feature
    /// </summary>
    public class ApplicationConfigurationFeature : IClientConfigurationFeature
    {
        /// <summary>
        /// The setting for services
        /// </summary>
        public const string SERVICES_SETTING = "service";
        /// <summary>
        /// The appsetting setting name in the property grid
        /// </summary>
        public const string APPSETTING_SETTING = "setting";
        /// <summary>
        /// The instance name setting int he property grid
        /// </summary>
        public const string INSTANCE_NAME_SETTING = "instance";

        private readonly ApplicationServiceContextConfigurationSection m_configurationSection;

        /// <summary>
        /// DI constructor
        /// </summary>
        public ApplicationConfigurationFeature(IConfigurationManager configurationManager)
        {
            this.m_configurationSection = configurationManager.GetSection<ApplicationServiceContextConfigurationSection>();

        }

        /// <inheritdoc/>
        public int Order => 0;

        /// <inheritdoc/>
        public string Name => "application";

        /// <inheritdoc/>
        public ConfigurationDictionary<string, object> Configuration => this.Refresh();

        /// <inheritdoc/>
        public String ReadPolicy => PermissionPolicyIdentifiers.Login;

        /// <inheritdoc/>
        public String WritePolicy => PermissionPolicyIdentifiers.AccessClientAdministrativeFunction;

        /// <summary>
        /// Refresh the configuration
        /// </summary>
        private ConfigurationDictionary<string, object> Refresh() => new ConfigurationDictionary<string, object>()
            {
                { SERVICES_SETTING, m_configurationSection.ServiceProviders.Select(o=> o.TypeXml).ToArray() },
                { APPSETTING_SETTING, m_configurationSection.AppSettings },
                { INSTANCE_NAME_SETTING, m_configurationSection.InstanceName }
            };

        /// <inheritdoc/>
        public bool Configure(SanteDBConfiguration configuration, IDictionary<string, object> featureConfiguration)
        {
            var section = configuration.GetSection<ApplicationServiceContextConfigurationSection>();
            if (section == null)
            {
                section = new ApplicationServiceContextConfigurationSection()
                {
                    ServiceProviders = new List<TypeReferenceConfiguration>(),
                    AppSettings = new List<AppSettingKeyValuePair>()
                };
                configuration.AddSection(section);
            }

            section.AppSettings = ((IEnumerable)featureConfiguration[APPSETTING_SETTING])?.OfType<JObject>().Select(o => new AppSettingKeyValuePair(o["key"].ToString(), o["value"]?.ToString())).ToList();
            //section.ServiceProviders = ((IEnumerable)featureConfiguration[SERVICES_SETTING])?.OfType<JObject>().Select(o => new TypeReferenceConfiguration(o["type"].ToString())).ToList();
            return true;

        }
    }
}
