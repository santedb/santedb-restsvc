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