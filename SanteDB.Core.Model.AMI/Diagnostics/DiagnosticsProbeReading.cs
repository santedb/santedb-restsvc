/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-5
 */
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
