/*
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
 *
 * User: fyfej
 * Date: 2023-6-21
 */
using SanteDB.Core.Interop;
using SanteDB.Core.Jobs;
using SanteDB.Core.Model.AMI.Jobs;
using SanteDB.Core.Model.Parameters;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;

namespace SanteDB.Rest.AMI.Operation
{
    /// <summary>
    /// Execute job rest operation
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class CancelJobOperation : IApiChildOperation
    {

        // Manager
        private readonly IJobManagerService m_manager;

        /// <summary>
        /// DI constructor
        /// </summary>
        public CancelJobOperation(IJobManagerService managerService)
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
        public Type[] ParentTypes => new Type[] { typeof(JobInfo) };

        /// <summary>
        /// Gets the name
        /// </summary>
        public string Name => "cancel";

        /// <summary>
        /// Invoke the operation
        /// </summary>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if (Guid.TryParse(scopingKey.ToString(), out var value))
            {
                var job = this.m_manager.GetJobInstance(value);
                if (job == null)
                {
                    throw new KeyNotFoundException($"Cannot find job {value}");
                }
                if (job.CanCancel)
                {
                    job.Cancel();
                }
                return null;
            }
            else
            {
                throw new InvalidOperationException("Job ID must be provided");
            }
        }
    }
}
