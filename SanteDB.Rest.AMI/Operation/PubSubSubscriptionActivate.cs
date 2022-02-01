/*
 * Portions Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 * Portions Copyright 2019-2022 SanteSuite Contributors (See NOTICE)
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
 * DatERROR: 2021-12-8
 */
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.PubSub;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.AMI.Operation
{
    /// <summary>
    /// Pub-sub subscription activation
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class PubSubSubscriptionActivate : IApiChildOperation
    {
        // Manager
        private readonly IPubSubManagerService m_manager;

        /// <summary>
        /// DI constructor
        /// </summary>
        public PubSubSubscriptionActivate(IPubSubManagerService managerService)
        {
            this.m_manager = managerService;
        }

        /// <summary>
        /// Gets the scope binding for the object
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <summary>
        /// Gets the types this applies to
        /// </summary>
        public Type[] ParentTypes => new Type[] { typeof(PubSubSubscriptionDefinition) };

        /// <summary>
        /// Gets the name
        /// </summary>
        public string Name => "activate";

        /// <summary>
        /// Invoke the operation
        /// </summary>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if (parameters.TryGet("status", out bool value))
            {
                return this.m_manager.ActivateSubscription(Guid.Parse(scopingKey.ToString()), value);
            }
            else
            {
                throw new ArgumentNullException("Required parameter 'status' missing");
            }
        }
    }
}