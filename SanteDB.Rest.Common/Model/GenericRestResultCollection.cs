using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Rest.Common.Model
{
    /// <summary>
    /// Generic rest result which has a wrapped simple type
    /// </summary>
    [XmlType(nameof(GenericRestResultCollection), Namespace = "http://santedb.org/model")]
    public class GenericRestResultCollection
    {
        /// <summary>
        /// Gets the values in the result collection
        /// </summary>
        [XmlElement("int", typeof(Int32)),
            XmlElement("string", typeof(String)),
            XmlElement("bool", typeof(bool)),
            XmlElement("float", typeof(float)),
            XmlElement("guid", typeof(Guid)),
            JsonProperty("values")]
        public List<object> Values { get; set; }
    }
}