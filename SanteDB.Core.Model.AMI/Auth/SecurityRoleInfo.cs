﻿/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Auth
{
    /// <summary>
    /// Represents additional information for security role
    /// </summary>
    [XmlType(nameof(SecurityRoleInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(SecurityRoleInfo))]
    [XmlRoot(nameof(SecurityRoleInfo), Namespace = "http://santedb.org/ami")]
    public class SecurityRoleInfo : ISecurityEntityInfo<SecurityRole>
    {
        /// <summary>
        /// Create new security role info
        /// </summary>
        public SecurityRoleInfo()
        {
        }

        /// <summary>
        /// Create new security role
        /// </summary>
        public SecurityRoleInfo(SecurityRole role) : this(role, ApplicationServiceContext.Current.GetService<IPolicyInformationService>())
        {

        }

        /// <summary>
        /// Create new security role information from the specified role
        /// </summary>
        public SecurityRoleInfo(SecurityRole role, IPolicyInformationService pipService)
        {
            this.Users = role.Users.Select(o => o.UserName).ToList();
            this.Entity = role;
            this.Policies = pipService.GetPolicies(role).Select(o => new SecurityPolicyInfo(o)).ToList();
        }

        /// <summary>
        /// Gets the type
        /// </summary>
        [JsonProperty("$type")]
        [XmlIgnore]
        public string Type { get => "SecurityRoleInfo"; set { } }

        /// <summary>
        /// Gets or sets the entity that is wrapped by this wrapper
        /// </summary>
        [XmlElement("entity")]
        [JsonProperty("entity")]
        public SecurityRole Entity { get; set; }

        /// <summary>
        /// Gets or sets the policies that are to be applied are already applied to the entity
        /// </summary>
        [XmlElement("policy")]
        [JsonProperty("policy")]
        public List<SecurityPolicyInfo> Policies { get; set; }

        /// <summary>
        /// Gets the users in the specified role
        /// </summary>
        [XmlElement("user")]
        [JsonProperty("user")]
        public List<string> Users { get; set; }


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