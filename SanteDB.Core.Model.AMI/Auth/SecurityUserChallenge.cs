using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

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
        [XmlElement("challenge"), JsonProperty("challenge")]
        public Guid ChallengeKey { get; set; }

        /// <summary>
        /// The challenge response
        /// </summary>
        [XmlElement("response"), JsonProperty("response")]
        public String ChallengeResponse { get; set; }

    }
}
