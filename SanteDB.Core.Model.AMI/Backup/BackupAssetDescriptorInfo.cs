/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 */
using Newtonsoft.Json;
using SanteDB.Core.Data.Backup;
using System;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Backup
{
    /// <summary>
    /// Backup asset descriptor serialization class
    /// </summary>
    [XmlType(nameof(BackupAssetDescriptorInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(BackupDescriptorInfo))]
    public class BackupAssetDescriptorInfo
    {

        /// <summary>
        /// Serialization ctor
        /// </summary>
        public BackupAssetDescriptorInfo()
        {

        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        public BackupAssetDescriptorInfo(IBackupAssetDescriptor descriptor)
        {
            this.Name = descriptor.Name;
            this.AssetClassId = descriptor.AssetClassId;
        }

        /// <summary>
        /// Gets the name of the asset 
        /// </summary>
        [XmlAttribute("name"), JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets the class of the asset
        /// </summary>
        [XmlAttribute("classId"), JsonProperty("classId")]
        public Guid AssetClassId { get; set; }

    }
}