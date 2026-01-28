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
using SanteDB.Core.Cdss;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// Represents an API operation that re-computes a carepathway and updates the dates/instructions on the care pathway
    /// </summary>
    public class CarepathRecomputeOperation : IApiChildOperation
    {

        private readonly IRepositoryService<Patient> m_patientRepository;
        private readonly ICarePathwayEnrollmentService m_carePathwayService;
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(CarepathRecomputeOperation));

        /// <summary>
        /// DI ctor
        /// </summary>
        public CarepathRecomputeOperation(IRepositoryService<Patient> patientRepository, ICarePathwayEnrollmentService carePathwayEnrollmentService)
        {
            this.m_patientRepository = patientRepository;
            this.m_carePathwayService = carePathwayEnrollmentService;
        }

        /// <inheritdoc/>
        public string Name => "carepath-recompute";

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(Patient) };

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if(scopingKey is Guid uuid || Guid.TryParse(scopingKey.ToString(), out uuid))
            {

                if(!parameters.TryGet(CdssParameterNames.PATHWAY_SCOPE, out Guid pathwayId))
                {
                    throw new ArgumentNullException(CdssParameterNames.PATHWAY_SCOPE, String.Format(ErrorMessages.MISSING_VALUE, CdssParameterNames.PATHWAY_SCOPE));
                }

                var patient = this.m_patientRepository.Get(uuid);
                if(patient == null)
                {
                    throw new KeyNotFoundException($"Patient/{uuid}");
                }

                return this.m_carePathwayService.RecomputeOrEnroll(patient, pathwayId);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(scopingKey), String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(Guid), scopingKey.GetType()));
            }
        }
    }
}
