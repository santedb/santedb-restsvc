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
using SanteDB.Core.Security.Claims;
using System.Security.Principal;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Security
{
    /// <summary>
    /// Submission request
    /// </summary>
    [XmlType(nameof(SubmissionRequest), Namespace = "http://santedb.org/ami")]
    [XmlRoot(nameof(SubmissionRequest), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(SubmissionRequest))]
    public class SubmissionRequest
    {

        /// <summary>
        /// Serialization ctor
        /// </summary>
        public SubmissionRequest()
        {

        }

        /// <summary>
        /// Create submission request
        /// </summary>
        /// <param name="cmcRequest">The request for the certificate</param>
        /// <param name="createdBy">The creation time</param>
        public SubmissionRequest(byte[] cmcRequest, IPrincipal createdBy)
        {
            this.CmcRequest = cmcRequest;
            if (createdBy is IClaimsPrincipal cp)
            {
                this.AdminAddress = cp.GetClaimValue(SanteDBClaimTypes.Email);
                this.AdminContactName = cp.Identity.Name;
            }
        }

        /// <summary>
        /// Gets or sets the admin address
        /// </summary>
        [XmlElement("address")]
        [JsonProperty("address")]
        public string AdminAddress { get; set; }

        /// <summary>
        /// Gets or sets the contact name
        /// </summary>
        [XmlElement("contact")]
        [JsonProperty("contact")]
        public string AdminContactName { get; set; }

        /// <summary>
        /// Gets or sets the cmc request
        /// </summary>
        [XmlElement("cmc")]
        [JsonProperty("cmc")]
        public byte[] CmcRequest { get; set; }
    }
}