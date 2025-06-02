/*
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
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Configuration;
using SanteDB.Core.Data.Import;
using SanteDB.Core.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Alien
{
    /// <summary>
    /// A wrapper for <see cref="IForeignDataSubmission"/> to be serialized over the wire
    /// </summary>
    [XmlRoot("ForeignData", Namespace = "http://santedb.org/ami")]
    [XmlType(nameof(ForeignDataInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(ForeignDataInfo))]
    public class ForeignDataInfo : NonVersionedEntityData, IAmiIdentified, IIdentifiedResource
    {

        /// <summary>
        /// Default ctor for serialization
        /// </summary>
        public ForeignDataInfo()
        {

        }

        /// <summary>
        /// Create a new foreign data import meta file from the <paramref name="submission"/>
        /// </summary>
        public ForeignDataInfo(IForeignDataSubmission submission)
        {
            this.Key = submission.Key;
            this.Name = submission.Name;
            this.Description = submission.Description;
            this.Status = submission.Status;
            this.ForeignDataMap = submission.ForeignDataMapKey;
            this.CreatedByKey = submission.CreatedByKey;
            this.ObsoletedByKey = submission.ObsoletedByKey;
            this.ObsoletionTime = submission.ObsoletionTime;
            this.CreationTime = submission.CreationTime;
            this.UpdatedByKey = submission.UpdatedByKey;
            this.UpdatedTime = submission.UpdatedTime;
            this.Issues = submission.Issues.ToList();
            this.Parameters = submission.ParameterValues.Select(o => new AppSettingKeyValuePair(o.Key, o.Value)).ToList();
        }


        /// <summary>
        /// Gets or sets the name of the foreign data
        /// </summary>
        [XmlElement("name"), JsonProperty("name")]
        public String Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the foreign data
        /// </summary>
        [XmlElement("description"), JsonProperty("description")]
        public String Description { get; set; }

        /// <summary>
        /// Gets or sets the status of the foreign data
        /// </summary>
        [XmlElement("status"), JsonProperty("status")]
        public ForeignDataStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the mapping selected
        /// </summary>
        [XmlElement("map"), JsonProperty("map")]
        public Guid ForeignDataMap { get; set; }

        /// <summary>
        /// Gets or sets the issues for the mapping
        /// </summary>
        [XmlElement("issue"), JsonProperty("issue")]
        public List<DetectedIssue> Issues { get; set; }

        /// <summary>
        /// Parameters 
        /// </summary>
        [XmlElement("parameter"), JsonProperty("parameter")]
        public List<AppSettingKeyValuePair> Parameters { get; set; }

        /// <summary>
        /// Gets or sets the key for the AMI identified
        /// </summary>
        object IAmiIdentified.Key { get => this.Key; set => this.Key = (Guid)value; }
    }
}
