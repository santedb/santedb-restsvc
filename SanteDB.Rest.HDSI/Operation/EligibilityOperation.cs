/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2024-12-12
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// Operation that determiners eligibility
    /// </summary>
    public class EligibilityOperation : IApiChildOperation
    {
        private readonly ICarePathwayEnrollmentService m_cpEnrolmentService;
        private readonly IRepositoryService<Patient> m_patientRepository;

        /// <summary>
        /// DI constructor
        /// </summary>
        public EligibilityOperation(ICarePathwayEnrollmentService cpEnrolmentService, IRepositoryService<Patient> patientRepository)
        {
            this.m_cpEnrolmentService = cpEnrolmentService;
            this.m_patientRepository = patientRepository;
        }

        /// <inheritdoc/>
        public string Name => "carepath-eligibilty";

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[]
        {
            typeof(Patient)
        };

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if(scopingKey is Guid uuid)
            {
                // Determine eligibilty on one patient
                using (AuthenticationContext.EnterSystemContext())
                {
                    var target = this.m_patientRepository.Get(uuid);
                    return this.m_cpEnrolmentService.GetEligibleCarePaths(target).ToList();
                }
            }
            else
            {
                throw new InvalidOperationException(ErrorMessages.ARGUMENT_COUNT_MISMATCH);
            }

        }
    }
}
