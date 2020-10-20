/*
 * Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
 * Copyright (C) 2019 - 2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE.md)
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
 * Date: 2020-3-18
 */

using System;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace SanteDB.Core.Model.AMI.Auth
{
    /// <summary>
    /// Represents security user information
    /// </summary>
    [XmlType(nameof(SecurityUserChallengeInfo), Namespace = "http://santedb.org/ami")]
    [XmlRoot(nameof(SecurityUserChallengeInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(SecurityUserChallengeInfo))]
    public class SecurityUserChallengeInfo
    {
	    /// <summary>
        /// Gets or sets the key of the challenge
        /// </summary>
        [XmlElement("challenge")][JsonProperty("challenge")]
        public Guid ChallengeKey { get; set; }

	    /// <summary>
        /// The challenge response
        /// </summary>
        [XmlElement("response")][JsonProperty("response")]
        public string ChallengeResponse { get; set; }
    }
}
