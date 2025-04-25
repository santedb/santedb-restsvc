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
 * Date: 2024-6-21
 */
using Newtonsoft.Json;
using SanteDB.Core.Data.Backup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Backup
{
    /// <summary>
    /// Represents a serialized representation of the backup information
    /// </summary>
    [XmlType(nameof(BackupDescriptorInfo), Namespace = "http://santedb.org/ami")]
    [XmlRoot(nameof(BackupDescriptorInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(BackupDescriptorInfo))]
    public class BackupDescriptorInfo
    {
        /// <summary>
        /// Default ctor for serialization
        /// </summary>
        public BackupDescriptorInfo()
        {

        }

        /// <summary>
        /// Copy constructor from IBackupDescriptor
        /// </summary>
        public BackupDescriptorInfo(IBackupDescriptor backupDescriptor, BackupMedia backupMedia)
        {
            this.Label = backupDescriptor.Label;
            this.Timestamp = backupDescriptor.Timestamp;
            this.Size = backupDescriptor.Size;
            this.IsEncrypted = backupDescriptor.IsEnrypted;
            this.Media = backupMedia;
            this.Creator = backupDescriptor.CreatedBy;
            this.Assets = backupDescriptor.Assets.Select(o => new BackupAssetDescriptorInfo(o)).ToList();

        }

        /// <summary>
        /// Gets or sets the label
        /// </summary>
        [JsonProperty("label"), XmlAttribute("label")]
        public string Label { get; set; }

        /// <summary>
        /// GEts or sets the timestamp
        /// </summary>
        [JsonProperty("timestamp"), XmlAttribute("timestamp")]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// GEts or sets the creator of the backup
        /// </summary>
        [JsonProperty("createdBy"), XmlAttribute("createdBy")]
        public string Creator { get; set; }

        /// <summary>
        /// Gets or sets the size
        /// </summary>
        [XmlAttribute("size"), JsonProperty("size")]
        public long Size { get; set; }

        /// <summary>
        /// Gets or sets the encryption flag
        /// </summary>
        [XmlAttribute("encrypted"), JsonProperty("encrypted")]
        public bool IsEncrypted { get; set; }

        /// <summary>
        /// Gets or sets the assets
        /// </summary>
        [XmlArray("assets"), XmlArrayItem("info"), JsonProperty("assets")]
        public List<BackupAssetDescriptorInfo> Assets { get; set; }

        /// <summary>
        /// Gets the backup location 
        /// </summary>
        [XmlAttribute("location"), JsonProperty("location")]
        public BackupMedia Media { get; set; }
    }
}
