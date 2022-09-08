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
 * Date: 2022-5-30
 */
using Newtonsoft.Json;
using SanteDB.Core.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Newtonsoft.Json;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Security.Services;

namespace SanteDB.Core.Model.AMI.Auth
{
    /// <summary>
    /// Represents wrapper information for security devices
    /// </summary>
    [XmlType(nameof(SecurityApplicationInfo), Namespace = "http://santedb.org/ami")]
    [XmlRoot(nameof(SecurityApplicationInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(SecurityApplicationInfo))]
    public class SecurityApplicationInfo : ISecurityEntityInfo<SecurityApplication>
    {
        /// <summary>
        /// Default CTOR
        /// </summary>
        public SecurityApplicationInfo()
        {

        }

        /// <summary>
        /// Creates a new app info from the specified object
        /// </summary>
        public SecurityApplicationInfo(SecurityApplication app, IPolicyInformationService pipService)
        {
            this.Entity = app;
            this.Policies = pipService.GetPolicies(app).Select(o => new SecurityPolicyInfo(o)).ToList();
        }

        /// <summary>
        /// Gets or sets the entity that is wrapped by this wrapper
        /// </summary>
        [XmlElement("entity")]
        [JsonProperty("entity")]
        public SecurityApplication Entity { get; set; }

        /// <summary>
        /// Get the key for the object
        /// </summary>
        [JsonProperty("id"), XmlElement("id")]
        public string Key
        {
            get => this.Entity?.Key?.ToString();
            set => this.Entity.Key = Guid.Parse(value);
        }

        /// <summary>
        /// Get the modified on
        /// </summary>
        [JsonIgnore]
        [XmlIgnore]
        public DateTimeOffset ModifiedOn => this.Entity?.ModifiedOn ?? DateTimeOffset.Now;

        /// <summary>
        /// Gets or sets the policies that are to be applied are already applied to the entity
        /// </summary>
        [XmlElement("policy")]
        [JsonProperty("policy")]
        public List<SecurityPolicyInfo> Policies { get; set; }

        /// <summary>
        /// Get the tag
        /// </summary>
        [JsonIgnore]
        [XmlIgnore]
        public string Tag => this.Entity?.Tag;

        /// <summary>
        /// Gets the object as identified data
        /// </summary>
        public IdentifiedData ToIdentifiedData()
        {
            return this.Entity;
        }
    }
}
