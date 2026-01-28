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
using System;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Logging
{
    /// <summary>
    /// Log file information
    /// </summary>
    [XmlRoot(nameof(LogFileInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(LogFileInfo))]
    public class LogFileInfo : IAmiIdentified
    {
        /// <summary>
        /// Gets or sets the content
        /// </summary>
        [XmlElement("text"), JsonProperty("text")]
        public byte[] Contents { get; set; }

        /// <summary>
        /// Gets or sets the last write time
        /// </summary>
        [XmlElement("modified"), JsonProperty("modified")]
        public DateTime LastWrite { get; set; }

        /// <summary>
        /// The key of the logfile
        /// </summary>
        [XmlElement("name"), JsonProperty("name")]
        public String Name { get; set; }

        /// <summary>
        /// Gets or sets the size of the file
        /// </summary>
        [XmlElement("size"), JsonProperty("size")]
        public long Size { get; set; }

        /// <summary>
        /// Get the requested key
        /// </summary>
        [JsonIgnore, XmlIgnore]
        public object Key
        {
            get => this.Name;
            set {; }
        }

        /// <summary>
        /// Gets the ETag
        /// </summary>
        [JsonIgnore, XmlIgnore]
        public string Tag => null;

        /// <summary>
        /// Get the modified on
        /// </summary>
        [JsonIgnore, XmlIgnore]
        public DateTimeOffset ModifiedOn => this.LastWrite;
    }
}