/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: justin
 * Date: 2018-11-20
 */
using MARC.HI.EHRS.SVC.Auditing.Data;
using Newtonsoft.Json;
using System;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Security
{
    /// <summary>
    /// Represents a simple wrapper for an audit data instance
    /// </summary>
    [XmlType(nameof(AuditInfo), Namespace = "http://santedb.org/ami")]
    [XmlRoot(nameof(AuditInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject]
    public class AuditInfo : AuditData, IAmiIdentified
    {
        /// <summary>
        /// Get the key for this object
        /// </summary>
        public string Key
        {
            get => this.CorrelationToken.ToString();
            set
            {
                ;
            }
        }

        /// <summary>
        /// Gets the ETag
        /// </summary>
        public string Tag => this.Timestamp.ToString("yyyyMMddHHmmSS");

        /// <summary>
        /// Get the modified on
        /// </summary>
        [JsonIgnore, XmlIgnore]
        public DateTimeOffset ModifiedOn => this.Timestamp;
    }
}
