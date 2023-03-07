using Newtonsoft.Json;
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Data.Import;
using SanteDB.Core.Model.AMI.Security;
using SanteDB.Core.Model.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        [XmlElement("issue"), JsonProperty("issue") ]
        public List<DetectedIssue> Issues { get; set; }

        /// <summary>
        /// Gets or sets the key for the AMI identified
        /// </summary>
        object IAmiIdentified.Key { get => this.Key; set => this.Key = (Guid)value; }
    }
}
