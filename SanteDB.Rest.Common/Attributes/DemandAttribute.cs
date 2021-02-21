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
using System;

namespace SanteDB.Rest.Common.Attributes
{
    /// <summary>
    /// Indicates a demand for a policy in the local execution environment
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class DemandAttribute : Attribute
    {

        /// <summary>
        /// Creates a new demand attribute
        /// </summary>
        public DemandAttribute(String policyId, bool overrideBase = false)
        {
            this.PolicyId = policyId;
            this.Override = overrideBase;
        }

        /// <summary>
        /// Override all other policy identifiers
        /// </summary>
        public bool Override { get; set; }

        /// <summary>
        /// Gets or sets the policy id
        /// </summary>
        public String PolicyId { get; set; }

    }
}
