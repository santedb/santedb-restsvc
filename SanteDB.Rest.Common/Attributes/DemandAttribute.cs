/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using System;

namespace SanteDB.Rest.Common.Attributes
{
    /// <summary>
    /// Indicates a demand for a policy in the local execution environment
    /// </summary>
    public class DemandAttribute : Attribute
    {

        /// <summary>
        /// Creates a new demand attribute
        /// </summary>
        public DemandAttribute(String policyId)
        {
            this.PolicyId = policyId;
        }

        /// <summary>
        /// Gets or sets the policy id
        /// </summary>
        public String PolicyId { get; set; }

    }
}
