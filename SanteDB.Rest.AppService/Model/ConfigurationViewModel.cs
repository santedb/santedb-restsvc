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
using SanteDB.Client.Configuration;
using SanteDB.Client.Configuration.Upstream;
using SanteDB.Core;
using SanteDB.Core.Applets.Configuration;
using SanteDB.Core.Configuration;
using SanteDB.Core.Configuration.Data;
using SanteDB.Core.Configuration.Http;
using SanteDB.Core.Security.Configuration;
using SanteDB.Core.Services;
using SanteDB.Rest.AppService.Configuration;
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
    [JsonObject(SerializationTypeName, MemberSerialization = MemberSerialization.OptIn, ItemTypeNameHandling = TypeNameHandling.None )]
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
        [JsonProperty("_isConfigured")]
        public bool IsConfigured { get; }

        /// <summary>
        /// When true the system should automatically restart
        /// </summary>
        [JsonProperty("_autoRestart")]
        public bool AutoRestart { get; set; }

        /// <summary>
        /// Default ctor for serialization
        /// </summary>
        public ConfigurationViewModel()
        {
            this.Configuration = new Dictionary<String, Dictionary<String, Object>>();
        }

        /// <summary>
        /// Configuation
        /// </summary>
        public ConfigurationViewModel(IEnumerable<IClientConfigurationFeature> features)
        {
            this.IsConfigured = !(ApplicationServiceContext.Current.GetService<IConfigurationManager>() is InitialConfigurationManager);
            this.Configuration = features.OrderBy(o=>o.Order).ToDictionary(f => f.Name, f => f.Configuration.ToDictionary(o=>o.Key, o=>o.Value));
        }

        /// <summary>
        /// Gets or sets the configuration dictionary
        /// </summary>
        [JsonProperty("values", TypeNameHandling = TypeNameHandling.None, NullValueHandling = NullValueHandling.Include), XmlIgnore]
        public Dictionary<String, Dictionary<String, Object>> Configuration { get; set; }

    }
}