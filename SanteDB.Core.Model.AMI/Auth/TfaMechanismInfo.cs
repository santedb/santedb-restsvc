﻿/*
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE.md)
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
 * Date: 2021-2-9
 */
using System;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace SanteDB.Core.Model.AMI.Auth
{
    /// <summary>
    /// Represents two-factor authentication mechanism information
    /// </summary>
    [XmlType(nameof(TfaMechanismInfo), Namespace = "http://santedb.org/ami")]
    [XmlRoot(nameof(TfaMechanismInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(TfaMechanismInfo))]
    public class TfaMechanismInfo
    {
	    /// <summary>
        /// Gets or sets the challenge text
        /// </summary>
        [XmlElement("challenge")][JsonProperty("challenge")]
        public string Challenge { get; set; }

	    /// <summary>
        /// Gets or sets the identifier
        /// </summary>
        [XmlElement("id")][JsonProperty("id")]
        public Guid Id { get; set; }

	    /// <summary>
        /// Gets or sets the name
        /// </summary>
        [XmlElement("name")][JsonProperty("name")]
        public string Name { get; set; }
    }
}