using Newtonsoft.Json;
using SanteDB.Core.Data.Backup;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
