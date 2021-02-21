/*
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
using Newtonsoft.Json;
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml.Serialization;
using System.Linq;
using SanteDB.Core.Model.Security;

namespace SanteDB.Rest.Common.Fault
{
    /// <summary>
    /// REST service fault wrapper
    /// </summary>
    [XmlType(nameof(RestServiceFault), Namespace = "http://santedb.org/fault")]
    [XmlRoot(nameof(RestServiceFault), Namespace = "http://santedb.org/fault")]
    [JsonObject(nameof(RestServiceFault))]
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

            if (ex is PolicyViolationException polViolation)
            {
                this.PolicyId = polViolation.PolicyId;
                this.PolicyOutcome = polViolation.PolicyDecision;
            }

            this.Rules = (ex as DetectedIssueException)?.Issues;
            if(ex.InnerException != null)
                this.CausedBy = new RestServiceFault(ex.InnerException);

            if (ex.Data.Count > 0)
                this.Data = ex.Data.Values.OfType<object>().ToList();
        }


        /// <summary>
        /// Gets or sets any additional data
        /// </summary>
        [JsonProperty("data"), XmlIgnore]
        public List<Object> Data { get; set; }

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
        /// Policy ID was violated
        /// </summary>
        [XmlElement("policyOutcome"), JsonProperty("policyOutcome")]
        public PolicyGrantType PolicyOutcome { get; set; }

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

        /// <summary>
        /// Rest service fault as string
        /// </summary>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
