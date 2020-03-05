/*
 * Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
 * Copyright (C) 2019 - 2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE.md)
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
 * Date: 2019-11-27
 */
using Newtonsoft.Json;
using SanteDB.Core.Auditing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Security
{
    /// <summary>
    /// A submission of one or more audits
    /// </summary>
    [XmlRoot("AuditSubmission", Namespace = "http://santedb.org/ami")]
    [XmlType(nameof(AuditSubmission), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(AuditSubmission))]
    public class AuditSubmission : IdentifiedData
    {
        /// <summary>
        /// Creates a new instance of audit information
        /// </summary>
        public AuditSubmission()
        {
            this.Audit = new List<AuditData>();
        }

        /// <summary>
        ///  Audit info with data
        /// </summary>
        /// <param name="data"></param>
        public AuditSubmission(AuditData data)
        {
            this.Audit = new List<AuditData>() { data };
        }

        /// <summary>
        /// Gets or sets the audit
        /// </summary>
        [XmlElement("audit"), JsonProperty("audit")]
        public List<AuditData> Audit { get; set; }

        /// <summary>
        /// When was the audit modified
        /// </summary>
        public override DateTimeOffset ModifiedOn
        {
            get
            {
                return this.Audit.Min(o => o.Timestamp);
            }
        }

        /// <summary>
        /// Gets the process identifier of the mobile application
        /// </summary>
        [XmlElement("pid")]
        public int ProcessId { get; set; }

        /// <summary>
        /// Gets or sets the device identifier which sent the audit
        /// </summary>
        [XmlElement("deviceId")]
        public Guid SecurityDeviceId { get; set; }
    }
}