using Newtonsoft.Json;
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Data.Import;
using SanteDB.Core.Model.AMI.Security;
using SanteDB.Core.Model.Interfaces;
using System;
using System.Collections.Generic;
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
    public class ForeignDataInfo : IAmiIdentified, IIdentifiedResource
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
            this.Status = submission.Status;
            this.ForeignDataMap = submission.ForeignDataMapKey;
            this.ModifiedOn = submission.ModifiedOn;
        }
        /// <summary>
        /// Gets or sets the unique identifier for the foriegn data 
        /// </summary>
        [XmlElement("id"), JsonProperty("id")]
        public Guid? Key { get; set; }

        /// <summary>
        /// Gets or sets the name of the foreign data
        /// </summary>
        [XmlElement("name"), JsonProperty("name")]
        public String Name { get; set; }

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
        /// Gets the tag of this object
        /// </summary>
        public string Tag => this.Key?.ToString();

        /// <summary>
        /// Gets the last modified time
        /// </summary>
        [XmlElement("modifiedOn"), JsonProperty("modifiedOn")]
        public DateTimeOffset ModifiedOn { get; set; }

        /// <summary>
        /// Key for the <see cref="IAmiIdentified"/>
        /// </summary>
        [XmlIgnore, JsonIgnore]
        object IAmiIdentified.Key { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
