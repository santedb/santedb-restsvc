/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-9-7
 */
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Protocol;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// Run a care plan resource operation
    /// </summary>
    public class GenerateCarePlanOperation : IApiChildOperation
    {
        // Care plan service
        private ICarePlanService m_carePlanService;
        private readonly IClinicalProtocolRepositoryService m_clinicalProtocolRepository;

        // Repo service
        private IConceptRepositoryService m_conceptRepositoryService;

        // Locale service
        private readonly ILocalizationService m_localizationService;

        /// <summary>
        /// DI constructor for care plan
        /// </summary>
        public GenerateCarePlanOperation(ICarePlanService carePlanService, IClinicalProtocolRepositoryService clinicalProtocolRepository, IConceptRepositoryService conceptRepositoryService, ILocalizationService localizationService)
        {
            this.m_carePlanService = carePlanService;
            this.m_clinicalProtocolRepository = clinicalProtocolRepository;
            this.m_conceptRepositoryService = conceptRepositoryService;
            this.m_localizationService = localizationService;
        }

        /// <summary>
        /// Gets the parent types
        /// </summary>
        public Type[] ParentTypes => new Type[] { typeof(CarePlan) };

        /// <summary>
        /// The name of the operation
        /// </summary>
        public string Name => "cdss";

        /// <summary>
        /// The binding of the scope
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        /// <summary>
        /// Invoke the care plan operation
        /// </summary>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            // Target - of the operation
            if (!parameters.TryGet("targetPatient", out Patient target))
            {
                throw new ArgumentNullException("targetPatient", this.m_localizationService.GetString(ErrorMessageStrings.MISSING_ARGUMENT));
            }

            // Get parameters for the history of the patient which can provide history
            if (!parameters.TryGet("history", out Bundle history))
            {
                history = new Bundle();
            }

            // Get parameter for desired protocols
            IClinicalProtocol clinicalProtocol = null;
            if(parameters.TryGet("protocol", out Guid protocolId))
            {
                clinicalProtocol = this.m_clinicalProtocolRepository.GetProtocol(protocolId);
            }
            parameters.TryGet("asEncounter", out bool asEncounters);

            // Get care plan service
            var plan = clinicalProtocol != null ?
                this.m_carePlanService.CreateCarePlan(target, asEncounters, parameters.Parameters.ToDictionary(o => o.Name, o => (object)o.Value), clinicalProtocol) :
                this.m_carePlanService.CreateCarePlan(target, asEncounters, parameters.Parameters.ToDictionary(o => o.Name, o => (object)o.Value));

            // Expand the participation roles form the care planner
            foreach (var p in plan.Relationships.Where(o => o.RelationshipTypeKey == ActRelationshipTypeKeys.HasComponent).Select(o => o.TargetAct))
                p.Participations.ForEach(o => o.ParticipationRoleKey = o.ParticipationRoleKey ?? this.m_conceptRepositoryService.GetConcept(o.ParticipationRole?.Mnemonic).Key);
            return Bundle.CreateBundle(plan);
        }
    }
}