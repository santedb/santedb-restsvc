﻿/*
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
using SanteDB.Core.BusinessRules;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Rest.HDSI.Operation
{

    /// <summary>
    /// Result of analysis
    /// </summary>
    [XmlType(nameof(CdssAnalyzeResult), Namespace = "http://santedb.org/cdss")]
    [XmlRoot(nameof(CdssAnalyzeResult), Namespace = "http://santedb.org/cdss")]
    [JsonObject]
    [XmlInclude(typeof(Bundle))]
    [XmlInclude(typeof(Act))]
    [XmlInclude(typeof(SubstanceAdministration))]
    [XmlInclude(typeof(Observation))]
    [XmlInclude(typeof(QuantityObservation))]
    [XmlInclude(typeof(CodedObservation))]
    [XmlInclude(typeof(TextObservation))]
    [XmlInclude(typeof(Procedure))]
    [XmlInclude(typeof(ControlAct))]
    [XmlInclude(typeof(InvoiceElement))]
    [XmlInclude(typeof(FinancialContract))]
    [XmlInclude(typeof(FinancialTransaction))]
    [XmlInclude(typeof(Narrative))]
    [XmlInclude(typeof(CarePlan))]
    [XmlInclude(typeof(PatientEncounter))]
    [XmlInclude(typeof(Account))]
    public class CdssAnalyzeResult
    {

        /// <summary>
        /// The submission (with any modifications)
        /// </summary>
        [XmlElement("submission"), JsonProperty("submission")]
        public IdentifiedData Submission { get; set; }

        /// <summary>
        /// The issues that were detected
        /// </summary>
        [XmlElement("issue"), JsonProperty("issue")]
        public List<DetectedIssue> Issues { get; set; }

    }

    /// <summary>
    /// An implementation of an operation that executes any applicable rules from the CDSS layer and generates
    /// a validation event from raised issues.
    /// </summary>
    public class CdssAnalyzeOperation : IApiChildOperation
    {
        // CDSS descision support service
        private readonly IDecisionSupportService m_decisionSupportService;

        /// <summary>
        /// Decision support service
        /// </summary>
        public CdssAnalyzeOperation(IDecisionSupportService decisionSupportService)
        {
            this.m_decisionSupportService = decisionSupportService;
        }

        /// <inheritdoc/>
        public string Name => "analyze";

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(Bundle), typeof(Act) };

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            // Find the parameter for the submission
            if(!parameters.TryGet("target", out IdentifiedData submission))
            {
                throw new ArgumentNullException("target");
            }

            var issues = new List<DetectedIssue>();
            if (submission is Bundle bundle)
            {
                foreach (var itm in bundle.Item.OfType<Act>())
                {
                    issues.AddRange(this.m_decisionSupportService.Analyze(itm));
                }
            }
            else if(submission is Act act)
            {
                issues.AddRange(this.m_decisionSupportService.Analyze(act));
            }

            return new CdssAnalyzeResult()
            {
                Issues = issues,
                Submission = submission
            };
        }
    }
}
