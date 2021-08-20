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
using RestSrvr;
using RestSrvr.Message;
using SanteDB.Core;
using SanteDB.Core.Security.Services;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Rest.Common.Security
{
    /// <summary>
    /// Represents a policy behavior for demanding permission
    /// </summary>
    [DisplayName("API Policy Based Access Control")]
    public class OperationDemandPolicyBehavior : IOperationPolicy, IOperationBehavior, IEndpointBehavior
    {

        // The behavior
        private Type m_behaviorType = null;

        /// <summary>
        /// Creates a new demand policy 
        /// </summary>
        public OperationDemandPolicyBehavior(Type behaviorType)
        {
            this.m_behaviorType = behaviorType;
        }
        /// <summary>
        /// Apply the demand
        /// </summary>
        public void Apply(EndpointOperation operation, RestRequestMessage request)
        {
            var methInfo = this.m_behaviorType.GetMethod(operation.Description.InvokeMethod.Name, operation.Description.InvokeMethod.GetParameters().Select(p => p.ParameterType).ToArray());
            foreach (var demand in methInfo.GetCustomAttributes<DemandAttribute>())
                ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>().Demand(demand.PolicyId);
            
        }

        /// <summary>
        /// Apply the endpoint behavior
        /// </summary>
        public void ApplyEndpointBehavior(ServiceEndpoint endpoint, EndpointDispatcher dispatcher)
        {
            foreach (var op in endpoint.Description.Contract.Operations)
                op.AddOperationBehavior(this);
        }

        /// <summary>
        /// Apply the operation behavior
        /// </summary>
        public void ApplyOperationBehavior(EndpointOperation operation, OperationDispatcher dispatcher)
        {
            dispatcher.AddOperationPolicy(this);
        }
    }
}
