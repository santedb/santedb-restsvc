using System;
using System.Collections.Generic;
using System.Text;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Roles;
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
                var target = this.m_patientRepository.Get(uuid);
                return this.m_cpEnrolmentService.GetEligibleCarePaths(target);
            }
            else
            {
                throw new InvalidOperationException(ErrorMessages.ARGUMENT_COUNT_MISMATCH);
            }

        }
    }
}
