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
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// An operation that can enroll patients into a care pathway
    /// </summary>
    public class CarepathUnEnrollOperation : IApiChildOperation
    {


        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(CarepathUnEnrollOperation));
        private readonly ICarePathwayEnrollmentService m_enrollmentService;
        private readonly IRepositoryService<Patient> m_patientRepository;

        public CarepathUnEnrollOperation(ICarePathwayEnrollmentService enrollmentService, IRepositoryService<Patient> patientRepository)
        {
            this.m_enrollmentService = enrollmentService;
            this.m_patientRepository = patientRepository;
        }

        /// <inheritdoc/>
        public string Name => "carepath-unenroll";

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[]
        {
            typeof(Patient)
        };

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.WriteClinicalData)]
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if(scopingKey is Guid uuid || Guid.TryParse(scopingKey.ToString(), out uuid))
            {
                if(!parameters.TryGet(CdssParameterNames.PATHWAY_SCOPE, out Guid pathwayId))
                {
                    throw new ArgumentNullException(CdssParameterNames.PATHWAY_SCOPE, ErrorMessages.ARGUMENT_NULL);
                }
                var patient = this.m_patientRepository.Get(uuid);
                if(patient == null)
                {
                    throw new KeyNotFoundException($"{scopingType.GetSerializationName()}/{scopingKey}");
                }
                return this.m_enrollmentService.UnEnroll(patient, pathwayId);
            }
            else
            {
                throw new ArgumentOutOfRangeException(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE);
            }
        }
    }
}
