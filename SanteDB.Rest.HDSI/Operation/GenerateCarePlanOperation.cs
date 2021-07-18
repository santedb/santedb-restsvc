using SanteDB.Core.i18n;
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

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// Run a care plan resource operation
    /// </summary>
    public class GenerateCarePlanOperation : IApiChildOperation
    {

        // Care plan service
        private ICarePlanService m_carePlanService;
        private IConceptRepositoryService m_conceptRepositoryService;

        /// <summary>
        /// DI constructor for care plan
        /// </summary>
        public GenerateCarePlanOperation(ICarePlanService carePlanService, IConceptRepositoryService conceptRepositoryService)
        {
            this.m_carePlanService = carePlanService;
            this.m_conceptRepositoryService = conceptRepositoryService;
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
        /// Invoke the care plan operation
        /// </summary>
        public object Invoke(Type scopingType, object scopingKey, ApiOperationParameterCollection parameters)
        {
            // Target - of the operation
            if(!parameters.TryGet("targetPatient", out Patient target))
            {
                throw new InvalidOperationException(ErrorMessages.ERR_MISSING_ARGUMENT.Format("targetPatient"));
            }

            // Get parameters for the history of the patient which can provide history
            if(!parameters.TryGet("history", out Bundle history))
            {
                history = new Bundle();
            }

            // Get parameter for desired protocols
            parameters.TryGet("protocol", out Guid protocolId);
            parameters.TryGet("asEncounter", out bool asEncounters);

            // Get care plan service
            var plan = protocolId != Guid.Empty ?
                this.m_carePlanService.CreateCarePlan(target, asEncounters, parameters.Parameters.ToDictionary(o => o.Name, o => (object)o.Value), protocolId) :
                this.m_carePlanService.CreateCarePlan(target, asEncounters, parameters.Parameters.ToDictionary(o => o.Name, o => (object)o.Value));

            // Expand the participation roles form the care planner
            foreach (var p in plan.Relationships.Where(o => o.RelationshipTypeKey == ActRelationshipTypeKeys.HasComponent).Select(o => o.TargetAct))
                p.Participations.ForEach(o => o.ParticipationRoleKey = o.ParticipationRoleKey ?? this.m_conceptRepositoryService.GetConcept(o.ParticipationRole?.Mnemonic).Key);
            return Bundle.CreateBundle(plan);
        }
    }
}
