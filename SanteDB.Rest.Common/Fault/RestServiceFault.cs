/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
 *
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
 * User: justin
 * Date: 2018-11-20
 */
using Newtonsoft.Json;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            this.Detail = ex.ToString();
#endif

            this.PolicyId = ex.GetType().GetRuntimeProperty("Policy")?.GetValue(ex)?.ToString();

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
        [XmlElement("stack"), JsonProperty("stack")]
        public String StackTrace { get; set; }

        /// <summary>
        /// Policy ID was violated
        /// </summary>
        [XmlElement("policyId"), JsonProperty("policyId")]
        public String PolicyId { get; set; }

        /// <summary>
        /// Detail of exception
        /// </summary>
        [XmlElement("detail"), JsonProperty("detail")]
        public String Detail { get; set; }

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
