﻿/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-6-21
 */
using Newtonsoft.Json;
using System;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Diagnostics
{
    /// <summary>
    /// Environment information
    /// </summary>
    [JsonObject(nameof(DiagnosticEnvironmentInfo)), XmlType(nameof(DiagnosticEnvironmentInfo), Namespace = "http://santedb.org/ami/diagnostics")]
    public class DiagnosticEnvironmentInfo
    {
        /// <summary>
        /// Is platform 64 bit
        /// </summary>
        [JsonProperty("is64bit"), XmlAttribute("is64Bit")]
        public bool Is64Bit { get; set; }

        /// <summary>
        /// OS Version
        /// </summary>
        [JsonProperty("osVersion"), XmlAttribute("osVersion")]
        public String OSVersion { get; set; }

        /// <summary>
        /// CPU count
        /// </summary>
        [JsonProperty("processorCount"), XmlAttribute("processorCount")]
        public int ProcessorCount { get; set; }

        /// <summary>
        /// Used memory
        /// </summary>
        [JsonProperty("usedMem"), XmlElement("mem")]
        public long UsedMemory { get; set; }

        /// <summary>
        /// Version
        /// </summary>
        [JsonProperty("version"), XmlElement("version")]
        public String Version { get; set; }

        /// <summary>
        /// Operating system classification
        /// </summary>
        [JsonProperty("osClass"), XmlElement("osClass")]
        public OperatingSystemID OSType { get; set; }

        /// <summary>
        /// Manufacturer of this device
        /// </summary>
        [JsonProperty("manufacturer"), XmlElement("manufacturer")]
        public string ManufacturerName { get; set; }
        /// <summary>
        /// Gets the machine name
        /// </summary>
        [JsonProperty("machineName"), XmlElement("machineName")]
        public string MachineName { get; set; }
    }
}