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
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Exceptions;
using SanteDB.Core.Http;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Rest.Common.Fault
{
    /// <summary>
    /// REST service fault wrapper
    /// </summary>
    [XmlType(nameof(RestServiceFault), Namespace = "http://santedb.org/fault")]
    [XmlRoot(nameof(RestServiceFault), Namespace = "http://santedb.org/fault")]
    [JsonObject(nameof(RestServiceFault))]
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // Model class provides no functions
    public class RestServiceFault : IHasToDisplay
    {
        /// <summary>
        /// Default service fault ctor
        /// </summary>
        public RestServiceFault()
        {
        }

        /// <summary>
        /// Creates a new rest service fault
        /// </summary>
        /// <param name="ex"></param>
        public RestServiceFault(Exception ex)
        {
            if (ex is RestClientException<RestServiceFault> rce)
            {
                this.Type = rce.Result.Type;
                this.CausedBy = rce.Result.CausedBy;
                this.Detail = rce.Result.Detail;
                this.Message = rce.Result.Message;
                this.PolicyId = rce.Result.PolicyId;
                this.PolicyOutcome = rce.Result.PolicyOutcome;
                this.Rules = rce.Result.Rules;
                this.StackTrace = rce.Result.StackTrace;
            }
            else if (ex is RestClientException<Object> rco && rco.Result is RestServiceFault rcf)
            {
                this.Type = rcf.Type;
                this.CausedBy = rcf.CausedBy;
                this.Detail = rcf.Detail;
                this.Message = rcf.Message;
                this.PolicyId = rcf.PolicyId;
                this.PolicyOutcome = rcf.PolicyOutcome;
                this.Rules = rcf.Rules;
                this.StackTrace = rcf.StackTrace;
            }
            else
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
                    this.PolicyName = polViolation.PolicyName;
                    this.PolicyOutcome = polViolation.PolicyDecision;
                }

                if (ex is DetectedIssueException dte)
                {
                    this.Rules = new List<DetectedIssue>(dte.Issues);
                }

                if (ex.InnerException != null)
                {
                    this.CausedBy = new RestServiceFault(ex.InnerException);
                }

                if (ex.Data.Count > 0)
                {
                    this.Data = ex.Data.OfType<object>().Select(o => o.ToString()).ToList();
                }
                else
                {
                    this.Data = new List<string>();
                }
            }
        }

        /// <summary>
        /// Gets or sets any additional data
        /// </summary>
        [JsonProperty("data"), XmlElement("data")]
        public List<String> Data { get; set; }

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
        /// Policy name that was violated
        /// </summary>
        [XmlElement("policyName"), JsonProperty("policyName")]
        public String PolicyName { get; set; }

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

        /// <inheritdoc/>
        public string ToDisplay()
        {
            var stringBuilder = new StringBuilder("\r\n-- START SERVER ERRORS --\r\n");
            var o = this;
            int i = 0;
            while (o != null)
            {
                stringBuilder.AppendFormat("\t{0}: {1}\r\n", i++, o.Message);
                o = o.CausedBy;
            }
            stringBuilder.Append("-- END SERVER ERRORS --\r\n");
            return stringBuilder.ToString();
        }
    }
}