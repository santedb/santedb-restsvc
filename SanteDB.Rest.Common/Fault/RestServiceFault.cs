using Newtonsoft.Json;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SanteDB.Rest.Common.Fault
{
    /// <summary>
    /// REST service fault wrapper
    /// </summary>
    [XmlType(nameof(RestServiceFault), Namespace = "http://santedb.org/fault")]
    [XmlRoot(nameof(RestServiceFault), Namespace = "http://santedb.org/fault")]
    [JsonObject]
    public class RestServiceFault
    {
        private DetectedIssue itm;

        public RestServiceFault()
        {

        }

        /// <summary>
        /// Creates a new rest service fault
        /// </summary>
        /// <param name="ex"></param>
        public RestServiceFault(Exception ex)
        {
            this.Type = ex.GetType().Name;
            this.Message = ex.Message;
#if DEBUG
            this.StackTrace = ex.StackTrace;
#endif
            this.Rules = (ex as DetectedIssueException)?.Issues;
            if(ex.InnerException != null)
                this.CausedBy = new RestServiceFault(ex.InnerException);
        }

        
        /// <summary>
        /// Gets or sets the type of fault
        /// </summary>
        [XmlElement("type"), JsonProperty("$type")]
        public String Type { get; set; }

        /// <summary>
        /// Gets or sets the message
        /// </summary>
        [XmlElement("message"), JsonProperty("message")]
        public String Message { get; set; }

        /// <summary>
        /// Detail of exception
        /// </summary>
        [XmlElement("detail"), JsonProperty("detail")]
        public String StackTrace { get; set; }

        /// <summary>
        /// Gets or sets the caused by
        /// </summary>
        [XmlElement("cause"), JsonProperty("cause")]
        public RestServiceFault CausedBy { get; set; }

        /// <summary>
        /// Gets or sets the rules
        /// </summary>
        [XmlElement("rule"), JsonProperty("rules")]
        public List<DetectedIssue> Rules { get; set; }
    }
}
