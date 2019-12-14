using Newtonsoft.Json;
using SanteDB.Core.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Diagnostics
{
    /// <summary>
    /// Repreesnts a wire level format of a performance counter
    /// </summary>
    [JsonObject(nameof(DiagnosticsProbeReading))]
    [XmlType(nameof(DiagnosticsProbeReading), Namespace = "http://santedb.org/ami")]
    [XmlRoot(nameof(DiagnosticsProbeReading), Namespace = "http://santedb.org/ami")]
    public class DiagnosticsProbeReading 
    {

        /// <summary>
        /// Creates a new performance counter
        /// </summary>
        public DiagnosticsProbeReading()
        {

        }

        /// <summary>
        /// Creates a new performance counter
        /// </summary>
        public DiagnosticsProbeReading(IDiagnosticsProbe counter)
        {
            this.ReadingDate = DateTime.Now;
            this.ProbeKey = counter.Uuid;
            if (counter is ICompositeDiagnosticsProbe)
                this.Value = (counter as ICompositeDiagnosticsProbe).Value.Select(o => new DiagnosticsProbeReading(o)).ToArray();
            else
                this.Value = counter.Value;
        }

        /// <summary>
        /// Gets the reading type
        /// </summary>
        [XmlElement("probe"), JsonProperty("probe")]
        public Guid ProbeKey { get; set; }

        /// <summary>
        /// Gets or sets the time that the reading was taken
        /// </summary>
        [XmlElement("ts"), JsonProperty("ts")]
        public DateTime ReadingDate { get; set; }


        /// <summary>
        /// Gets the value of the reading
        /// </summary>
        [JsonProperty("value")]
        [XmlElement("valueInt", typeof(int))]
        [XmlElement("valueFloat", typeof(float))]
        [XmlElement("valueList", typeof(DiagnosticsProbeReading[]))]
        [XmlElement("valueBool", typeof(bool))]
        [XmlElement("valueString", typeof(string))]
        [XmlElement("valueDate", typeof(DateTime))]
        [XmlElement("valueTimespan", typeof(TimeSpan))]
        public object Value { get; set; }


    }
}
