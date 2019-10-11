using Newtonsoft.Json;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Principal;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        }

        /// <summary>
        /// The session is not valid after time
        /// </summary>
        [XmlElement("exp"), JsonProperty("exp")]
        public DateTime NotAfter { get; set; }

        /// <summary>
        /// The session is not valid before the specified time
        /// </summary>
        [XmlElement("nbf") ,JsonProperty("nbf")]
        public DateTime NotBefore { get; set; }

        /// <summary>
        /// The session identifier
        /// </summary>
        [XmlElement("ssessionId"), JsonProperty("sessionId")]
        public byte[] SessionId { get; set; }

        /// <summary>
        /// Gets the device name
        /// </summary>
        [XmlElement("deviceIdentity"), JsonProperty("deviceIdentity")]
        public String Device { get; set; }

        /// <summary>
        /// Gets the application name
        /// </summary>
        [XmlElement("applicationIdentity"), JsonProperty("applicationIdentity")]
        public String Application { get; set; }

        /// <summary>
        /// Gets the user identity
        /// </summary>
        [XmlElement("userIdentity"), JsonProperty("userIdentity")]
        public String User { get; set; }
    }
}
