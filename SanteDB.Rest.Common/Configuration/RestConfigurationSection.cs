/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
 */
using Newtonsoft.Json;
using SanteDB.Core.Configuration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Rest.Common.Configuration
{
    /// <summary>
    /// Represents the configuration for the AGS
    /// </summary>
    [XmlType(nameof(RestConfigurationSection), Namespace = "http://santedb.org/configuration")]
    [JsonObject]
    public class RestConfigurationSection : IConfigurationSection
    {

        /// <summary>
        /// Construct the AGS configuration
        /// </summary>
        public RestConfigurationSection()
        {
            this.Services = new List<RestServiceConfiguration>();
        }

        /// <summary>
        /// Gets the base address
        /// </summary>
        [XmlElement("baseAddress"), JsonProperty("baseAddress")]
        [DisplayName("Base Address"), Description("When running SanteDB behind a reverse proxy (like NGINX or OpenHIM), this setting controls the external base address of all REST endpoints")]
        public string ExternalHostPort { get; set; }

        /// <summary>
        /// Gets or sets the service configuration
        /// </summary>
        [XmlElement("service"), JsonProperty("service")]
        [DisplayName("REST Services"), Description("A complete list of REST based services which are exposed within the host context on this server")]
        public List<RestServiceConfiguration> Services { get; set; }
    }
}
