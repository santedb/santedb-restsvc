/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
 */
using SanteDB.Core.Data.Import;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Alien;
using SanteDB.Core.Model.Parameters;
using SanteDB.Rest.Common;
using System;

namespace SanteDB.Rest.AMI.Operation
{
    /// <summary>
    /// Schedule a foreign data import job
    /// </summary>
    public class ForeignDataScheduleJobOperation : IApiChildOperation
    {
        private readonly IForeignDataManagerService m_foreignDataManagerService;

        /// <summary>
        /// DI constructor
        /// </summary>
        public ForeignDataScheduleJobOperation(IForeignDataManagerService foreignDataManagerService)
        {
            this.m_foreignDataManagerService = foreignDataManagerService;
        }

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(IForeignDataSubmission) };

        /// <inheritdoc/>
        public string Name => "schedule";

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if (scopingKey is Guid uuid || Guid.TryParse(scopingKey.ToString(), out uuid))
            {
                return new ForeignDataInfo(this.m_foreignDataManagerService.Schedule(uuid));
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(scopingKey), String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(Guid), scopingKey.GetType()));
            }
        }
    }
}
