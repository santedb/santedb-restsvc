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
 * 
 * User: fyfej
 * Date: 2023-6-21
 */
using Newtonsoft.Json;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Claims;
using SanteDB.Core.Security.Principal;
using SanteDB.Core.Security.Services;
using System;
using System.Linq;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Security
{
    /// <summary>
    /// Represents an AMI metadata class built from a session instance
    /// </summary>
    [XmlType(nameof(SecuritySessionInfo), Namespace = "http://santedb.org/ami")]
    [XmlRoot(nameof(SecuritySessionInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(SecuritySessionInfo))]
    public class SecuritySessionInfo
    {
        /// <summary>
        /// Default ctor for serialization
        /// </summary>
        public SecuritySessionInfo()
        {

        }

        /// <summary>
        /// Create a new session info object from the specified session instance
        /// </summary>
        public SecuritySessionInfo(ISession session)
        {
            this.NotAfter = session.NotAfter.DateTime;
            this.NotBefore = session.NotBefore.DateTime;
            this.SessionId = session.Id;
            var identities = ApplicationServiceContext.Current.GetService<ISessionIdentityProviderService>().GetIdentities(session);
            this.Application = identities.OfType<IApplicationIdentity>().FirstOrDefault()?.Name;
            this.Device = identities.OfType<IDeviceIdentity>().FirstOrDefault()?.Name;
            this.User = identities.FirstOrDefault(o => !(o is IDeviceIdentity || o is IApplicationIdentity))?.Name;
            this.RemoteEndpoint = session.Claims?.FirstOrDefault(o => o.Type == SanteDBClaimTypes.RemoteEndpointClaim)?.Value;
        }

        /// <summary>
        /// The session is not valid before the specified time
        /// </summary>
        [XmlElement("nbf")]
        [JsonProperty("nbf")]
        public DateTime NotBefore { get; set; }

        /// <summary>
        /// The session identifier
        /// </summary>
        [XmlElement("ssessionId")]
        [JsonProperty("sessionId")]
        public byte[] SessionId { get; set; }

        /// <summary>
        /// Gets the device name
        /// </summary>
        [XmlElement("deviceIdentity")]
        [JsonProperty("deviceIdentity")]
        public string Device { get; set; }

        /// <summary>
        /// Gets the application name
        /// </summary>
        [XmlElement("applicationIdentity")]
        [JsonProperty("applicationIdentity")]
        public string Application { get; set; }


        /// <summary>
        /// The session is not valid after time
        /// </summary>
        [XmlElement("exp")]
        [JsonProperty("exp")]
        public DateTime NotAfter { get; set; }


        /// <summary>
        /// Gets the user identity
        /// </summary>
        [XmlElement("userIdentity")]
        [JsonProperty("userIdentity")]
        public string User { get; set; }

        /// <summary>
        /// Claims on the session
        /// </summary>
        [XmlElement("remoteEndpoint")]
        [JsonProperty("remoteEndpoint")]
        public String RemoteEndpoint { get; set; }
    }
}
