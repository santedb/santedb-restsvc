/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 */
using Newtonsoft.Json;
using SanteDB.Core.Security.Services;
using System;
using System.Xml.Serialization;

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
        /// Get the mechanism
        /// </summary>
        public TfaMechanismInfo()
        {

        }

        /// <summary>
        /// Create a mechanism info from the specified <paramref name="tfaMechanism"/>
        /// </summary>
        public TfaMechanismInfo(ITfaMechanism tfaMechanism)
        {
            this.Id = tfaMechanism.Id;
            this.Name = tfaMechanism.Name;
            this.Classification = tfaMechanism.Classification;
            this.RequiresSetup = true;
            this.HelpText = tfaMechanism.SetupHelpText;
        }

        /// <summary>
        /// Flag indicating the the TFA mechanism requires setup
        /// </summary>
        [XmlElement("setup"), JsonProperty("setup")]
        public bool RequiresSetup { get; set; }

        /// <summary>
        /// Gets the classification of the TFA mechanism
        /// </summary>
        [XmlElement("class"), JsonProperty("class")]
        public TfaMechanismClassification Classification { get; set; }

        /// <summary>
        /// Gets or sets the identifier
        /// </summary>
        [XmlElement("id")]
        [JsonProperty("id")]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the name
        /// </summary>
        [XmlElement("name")]
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets the help text for users to setup the shared secret
        /// </summary>
        [XmlElement("helpText"), JsonProperty("helpText")]
        public string HelpText { get; set; }
    }
}