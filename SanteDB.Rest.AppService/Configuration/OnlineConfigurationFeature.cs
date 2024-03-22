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
using SanteDB.Rest.AMI.Configuration;
using SanteDB.Rest.BIS.Configuration;
using SanteDB.Rest.HDSI.Configuration;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Rest.AppService.Configuration
{
    /// <summary>
    /// Online configuration feature
    /// </summary>
    public class OnlineConfigurationFeature : IClientConfigurationFeature
    {
        /// <summary>
        /// Get the ordering of this feature
        /// </summary>
        public int Order => 10;

        /// <summary>
        /// Get the name of the configuration section
        /// </summary>
        public string Name => "online";

        /// <summary>
        /// Get the configuratoin
        /// </summary>
        public ConfigurationDictionary<string, object> Configuration => new ConfigurationDictionary<string, object>();

        /// <summary>
        /// Read policy
        /// </summary>
        public string ReadPolicy => PermissionPolicyIdentifiers.Login;

        /// <summary>
        /// Write policy
        /// </summary>
        public string WritePolicy => PermissionPolicyIdentifiers.AlterSystemConfiguration;

        /// <summary>
        /// Configure this feature
        /// </summary>
        public bool Configure(SanteDBConfiguration configuration, IDictionary<string, object> featureConfiguration)
        {
            if (configuration.GetSection<ApplicationServiceContextConfigurationSection>().AppSettings?.Any(p => p.Key == "integration-mode" && p.Value == "online") == true)
            {
                var amiSection = configuration.GetSection<AmiConfigurationSection>();
                var hdsiSection = configuration.GetSection<HdsiConfigurationSection>();
                var bisSection = configuration.GetSection<BisServiceConfigurationSection>();
                if (amiSection == null)
                {
                    amiSection = new AmiConfigurationSection();
                    configuration.AddSection(amiSection);
                }
                if (hdsiSection == null)
                {
                    hdsiSection = new HdsiConfigurationSection();
                    configuration.AddSection(hdsiSection);
                }
                if (bisSection == null)
                {
                    bisSection = new BisServiceConfigurationSection();
                    configuration.AddSection(bisSection);
                }

                bisSection.AutomaticallyForwardRequests = hdsiSection.AutomaticallyForwardRequests = true;
                amiSection.AutomaticallyForwardRequests = false;
            }
            return true;
        }
    }
}
