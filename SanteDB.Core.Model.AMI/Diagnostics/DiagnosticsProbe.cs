﻿/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 * Copyright 2019-2019 SanteSuite Contributors (See NOTICE)
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
 * User: Justin Fyfe
 * Date: 2019-12-14
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
    /// Represents a performance counter metadata
    /// </summary>
    [JsonObject(nameof(DiagnosticsProbe))]
    [XmlType(nameof(DiagnosticsProbe), Namespace = "http://santedb.org/ami")]
    [XmlRoot(nameof(DiagnosticsProbe), Namespace = "http://santedb.org/ami")]
    public class DiagnosticsProbe : IdentifiedData
    {

        /// <summary>
        /// Performance counter
        /// </summary>
        public DiagnosticsProbe()
        {

        }

        /// <summary>
        /// Creates a new performance counter
        /// </summary>
        public DiagnosticsProbe(IDiagnosticsProbe counter)
        {
            this.Key = counter.Uuid;
            this.Description = counter.Description;
            this.Name = counter.Name;
            this.ReadingType = counter.Type.AssemblyQualifiedName;
            this.Components = (counter as ICompositeDiagnosticsProbe)?.Value.Select(o => new DiagnosticsProbe(o)).ToList();
        }

        /// <summary>
        /// Gets the last time that the performance counter was modified
        /// </summary>
        public override DateTimeOffset ModifiedOn => DateTime.Now;

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        [XmlElement("name"), JsonProperty("name")]
        public String Name { get; set; }

        /// <summary>
        /// Gets the description
        /// </summary>
        [XmlElement("description"), JsonProperty("description")]
        public String Description { get; set; }

        /// <summary>
        /// The type of reading 
        /// </summary>
        [XmlElement("type"), JsonProperty("type")]
        public String ReadingType { get; set; }

        /// <summary>
        /// Gets or sets the component probes
        /// </summary>
        [XmlElement("component"), JsonProperty("component")]
        public List<DiagnosticsProbe> Components { get; set; }
    }
}