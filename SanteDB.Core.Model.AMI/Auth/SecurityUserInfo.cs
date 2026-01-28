/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using Newtonsoft.Json;
using SanteDB.Core.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Auth
{
    /// <summary>
    /// Represents security user information
    /// </summary>
    [XmlType(nameof(SecurityUserInfo), Namespace = "http://santedb.org/ami")]
    [XmlRoot(nameof(SecurityUserInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(SecurityUserInfo))]
    public class SecurityUserInfo : ISecurityEntityInfo<SecurityUser>
    {
        /// <summary>
        /// Default ctor
        /// </summary>
        public SecurityUserInfo()
        {

        }

        /// <summary>
        /// Get the security user information
        /// </summary>
        public SecurityUserInfo(SecurityUser user) //: base(user)
        {
            this.Roles = user.Roles.Select(o => o.Name).ToList();
            this.Entity = user;
        }

        /// <summary>
        /// Represents the entity
        /// </summary>
        [XmlElement("entity")]
        [JsonProperty("entity")]
        public SecurityUser Entity { get; set; }

        /// <summary>
        /// When true, indicates that the update is for password setting only
        /// </summary>
        [XmlElement("passwordOnly")]
        [JsonProperty("passwordOnly")]
        public bool PasswordOnly { get; set; }

        /// <summary>
        /// Force an expiration of the password
        /// </summary>
        [XmlElement("expirePassword")]
        [JsonProperty("expirePassword")]
        public bool ExpirePassword { get; set; }

        /// <summary>
        /// Gets or sets the role this user belongs to
        /// </summary>
        [XmlElement("role")]
        [JsonProperty("role")]
        public List<string> Roles { get; set; }

        /// <summary>
        /// Gets the type
        /// </summary>
        [JsonProperty("$type")]
        [XmlIgnore]
        public string Type { get => "SecurityUserInfo"; set { } }


        /// <summary>
        /// Get the key for the object
        /// </summary>
        [JsonProperty("id"), XmlElement("id")]
        public Guid? Key
        {
            get => this.Entity?.Key;
            set => this.Entity.Key = value;
        }

        /// <inheritdoc/>
        [XmlIgnore, JsonIgnore]
        Object IAmiIdentified.Key
        {
            get => this.Key;
            set => this.Key = Guid.Parse(value.ToString());
        }

        /// <summary>
        /// Get the modified on
        /// </summary>
        [JsonIgnore]
        [XmlIgnore]
        public DateTimeOffset ModifiedOn => this.Entity?.ModifiedOn ?? DateTimeOffset.Now;

        /// <summary>
        /// Get polocies for the user
        /// </summary>
        [XmlElement("policy")]
        [JsonProperty("policy")]
        public List<SecurityPolicyInfo> Policies
        {
            get; set;
        }

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
