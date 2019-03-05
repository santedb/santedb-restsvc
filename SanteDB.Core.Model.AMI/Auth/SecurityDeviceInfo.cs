/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 *
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using Newtonsoft.Json;
using SanteDB.Core.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Auth
{
    /// <summary>
    /// Represents wrapper information for security devices
    /// </summary>
    [XmlType(nameof(SecurityDeviceInfo), Namespace = "http://santedb.org/ami")]
    [XmlRoot(nameof(SecurityDeviceInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(SecurityDeviceInfo))]
    public class SecurityDeviceInfo : ISecurityEntityInfo<SecurityDevice>
    {
        /// <summary>
        /// Default CTOR
        /// </summary>
        public SecurityDeviceInfo()
        {

        }

        /// <summary>
        /// Creates a new device info from the specified object
        /// </summary>
        public SecurityDeviceInfo(SecurityDevice device)
        {
            this.Entity = device;
            this.Policies = device.Policies.Select(o => new SecurityPolicyInfo(o)).ToList();
        }

        /// <summary>
        /// Gets or sets the entity that is wrapped by this wrapper
        /// </summary>
        [XmlElement("entity"), JsonProperty("entity")]
        public SecurityDevice Entity { get; set; }

        /// <summary>
        /// Gets or sets the policies that are to be applied are already applied to the entity
        /// </summary>
        [XmlElement("policy"), JsonProperty("policy")]
        public List<SecurityPolicyInfo> Policies { get; set; }

        /// <summary>
        /// Get the key for the object
        /// </summary>
        [JsonIgnore, XmlIgnore]
        public string Key
        {
            get => this.Entity?.Key?.ToString();
            set => this.Entity.Key = Guid.Parse(value);
        }

        /// <summary>
        /// Get the tag
        /// </summary>
        [JsonIgnore, XmlIgnore]
        public string Tag => this.Entity?.Tag;

        /// <summary>
        /// Get the modified on
        /// </summary>
        [JsonIgnore, XmlIgnore]
        public DateTimeOffset ModifiedOn => this.Entity?.ModifiedOn ?? DateTimeOffset.Now;
    }
}
