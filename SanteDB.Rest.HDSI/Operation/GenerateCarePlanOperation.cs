/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using SanteDB.Core;
using SanteDB.Core.Cdss;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// Run a care plan resource operation
    /// </summary>
    public class GenerateCarePlanOperation : IApiChildOperation
    {
        // Care plan service
        private IDecisionSupportService m_cdssService;
        private readonly IRepositoryService<CarePlan> m_careplanRepository;
        private readonly ICdssLibraryRepository m_clinicalProtocolRepository;

        // Repo service
        private IConceptRepositoryService m_conceptRepositoryService;

        // Locale service
        private readonly ILocalizationService m_localizationService;
        private readonly IRepositoryService<Patient> m_patientRepository;

        /// <summary>
        /// DI constructor for care plan
        /// </summary>
        public GenerateCarePlanOperation(IDecisionSupportService cdssService,
            ICdssLibraryRepository clinicalProtocolRepository,
            IConceptRepositoryService conceptRepositoryService,
            IRepositoryService<Patient> patientRepository,
            IRepositoryService<CarePlan> careplanRepository,
            ILocalizationService localizationService)
        {
            this.m_cdssService = cdssService;
            this.m_careplanRepository = careplanRepository;
            this.m_clinicalProtocolRepository = clinicalProtocolRepository;
            this.m_conceptRepositoryService = conceptRepositoryService;
            this.m_localizationService = localizationService;
            this.m_patientRepository = patientRepository;
        }

        /// <summary>
        /// Gets the parent types
        /// </summary>
        public Type[] ParentTypes => new Type[] { typeof(Patient) };

        /// <summary>
        /// The name of the operation
        /// </summary>
        public string Name => "generate-careplan";

        /// <summary>
        /// The binding of the scope
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class | ChildObjectScopeBinding.Instance;

        /// <summary>
        /// Invoke the care plan operation
        /// </summary>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            // Target - of the operation
            var hasIdentifiedTarget = scopingKey is Guid targetUuid || Guid.TryParse(scopingKey?.ToString(), out targetUuid);
            Patient target = null;
            if (!hasIdentifiedTarget && !parameters.TryGet("targetPatient", out target))
            {
                throw new ArgumentNullException("targetPatient", this.m_localizationService.GetString(ErrorMessageStrings.MISSING_ARGUMENT));
            }

            if (hasIdentifiedTarget)
            {
                target = this.m_patientRepository.Get(targetUuid);
                if(target == null)
                {
                    throw new KeyNotFoundException(String.Format(ErrorMessages.REFERENCE_NOT_FOUND, $"Patient/${targetUuid}"));
                }
            }
            else if (parameters.TryGet("history", out Bundle history))
            {
                target.Participations = history.Item.OfType<Act>().Select(o => new ActParticipation(ActParticipationKeys.RecordTarget, target) { Act = o }).ToList();
            }

            // Get parameter for desired protocols
            ICdssLibrary libraryToApply = null;
            if (parameters.TryGet("library", out Guid libraryId))
            {
                libraryToApply = this.m_clinicalProtocolRepository.Get(libraryId, null);
            }
            parameters.TryGet("asEncounters", out bool asEncounters);

            var cpParameters = parameters.Parameters.ToDictionary(o => o.Name, p => p.Value);

            CarePlan plan = null;
            if (libraryToApply != null)
            {
                plan = this.m_cdssService.CreateCarePlan(target, asEncounters, cpParameters, libraryToApply);
            }
            else
            {
                plan = this.m_cdssService.CreateCarePlan(target, asEncounters, cpParameters);
            }


            return plan; //.HarmonizeCarePlan(); // Harmonize with stored careplan

        }
    }
}