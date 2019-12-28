using Newtonsoft.Json;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Jobs
{
    /// <summary>
    /// Represents a job parameter
    /// </summary>
    [XmlType(nameof(JobParameter), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(JobParameter))]
    public class JobParameter
    {
        /// <summary>
        /// Gets the key 
        /// </summary>
        [XmlAttribute("key"), JsonProperty("key")]
        public string Key { get; set; }

        /// <summary>
        /// Gets the value 
        /// </summary>
        [XmlElement("string", typeof(string)), XmlElement("int", typeof(int)), XmlElement("bool", typeof(bool)), JsonProperty("value")]
        public object Value { get; set; }

        /// <summary>
        /// Gets the type
        /// </summary>
        [XmlAttribute("type"), JsonProperty("type")]
        public string Type { get; set; }
    }
}