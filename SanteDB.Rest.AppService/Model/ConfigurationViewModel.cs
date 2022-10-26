/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */
using Newtonsoft.Json;
using SanteDB.Client.Configuration.Upstream;
using SanteDB.Core.Applets.Configuration;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Data;
using SanteDB.Core.Configuration.Http;
using SanteDB.Core.Security.Configuration;
using SanteDB.Rest.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace SanteDB.Rest.AppService.Model
{
    /// <summary>
    /// Configuration view model
    /// </summary>
    [JsonObject(SerializationTypeName)]
    [XmlRoot(SerializationTypeName, Namespace ="http://santedb.org/appService")]
    [XmlType(SerializationTypeName, Namespace = "http://santedb.org/appService")]
    public class ConfigurationViewModel
    {
        private const string SerializationTypeName = "Configuration";

        /// <summary>
        /// Get the type
        /// </summary>
        [JsonProperty("$type")]
        public String Type { get { return SerializationTypeName; } }

        /// <summary>
        /// Return true if configured
        /// </summary>
        [JsonProperty("isConfigured")]
        public bool IsConfigured { get => this.Upstream != null; }

        /// <summary>
        /// When true the system should automatically restart
        /// </summary>
        [JsonProperty("autoRestart")]
        public bool AutoRestart { get; set; }

        /// <summary>
        /// Configuation
        /// </summary>
        /// <param name="config"></param>
        public ConfigurationViewModel(SanteDBConfiguration config)
        {
            if (config == null) return;
            this.Security = config.GetSection<SecurityConfigurationSection>().ForDisclosure();
            this.Data = config.GetSection<DataConfigurationSection>();
            this.Applet = config.GetSection<AppletConfigurationSection>();
            this.Application = config.GetSection<ApplicationServiceContextConfigurationSection>();
            this.Log = config.GetSection<DiagnosticsConfigurationSection>();
            this.RestClient = config.GetSection<RestClientConfigurationSection>();
            this.RestServer = config.GetSection<RestConfigurationSection>();
            this.Upstream = config.GetSection<UpstreamConfigurationSection>();
            //this.Synchronization = config.GetSection<SynchronizationConfigurationSection>();
            this.OtherSections = config.Sections.Where(o => !typeof(ConfigurationViewModel).GetRuntimeProperties().Any(p => p.PropertyType.IsAssignableFrom(o.GetType()))).ToList();
        }

        /// <summary>
        /// Security section
        /// </summary>
        [JsonProperty("security")]
        public SecurityConfigurationSection Security { get; set; }

        /// <summary>
        /// Realm name
        /// </summary>
        [JsonProperty("realmName")]
        public String RealmName { get; set; }

        /// <summary>
        /// Data config
        /// </summary>
        [JsonProperty("data")]
        public DataConfigurationSection Data { get; set; }

        /// <summary>
        /// Gets or sets applet
        /// </summary>
        [JsonProperty("applet")]
        public AppletConfigurationSection Applet { get; set; }

        /// <summary>
        /// Gets or sets application
        /// </summary>
        [JsonProperty("application")]
        public ApplicationServiceContextConfigurationSection Application { get; set; }

        /// <summary>
        /// Log
        /// </summary>
        [JsonProperty("log")]
        public DiagnosticsConfigurationSection Log { get; set; }

        /// <summary>
        /// Gets or sets the network
        /// </summary>
        [JsonProperty("restClient")]
        public RestClientConfigurationSection RestClient { get; set; }

        ///// <summary>
        ///// Synchronization
        ///// </summary>
        //[JsonProperty("sync")]
        //public SynchronizationConfigurationSection Synchronization { get; set; }

        /// <summary>
        /// Upstream configuration section
        /// </summary>
        [JsonProperty("upstream")]
        public UpstreamConfigurationSection Upstream { get; set; }

        /// <summary>
        /// Synchronization
        /// </summary>
        [JsonProperty("server")]
        public RestConfigurationSection RestServer { get; set; }

        /// <summary>
        /// Represents other sections
        /// </summary>
        [JsonProperty("others")]
        public List<object> OtherSections { get; set; }
    }
}