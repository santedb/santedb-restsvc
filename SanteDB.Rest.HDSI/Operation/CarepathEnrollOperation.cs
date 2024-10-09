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
    /// An operation that can enroll patients into a care pathway
    /// </summary>
    public class CarepathEnrollOperation : IApiChildOperation
    {

        public const string CARE_PATHWAY_PARAMETER = "pathway";

        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(CarepathEnrollOperation));
        private readonly ICarePathwayEnrollmentService m_enrollmentService;
        private readonly IRepositoryService<Patient> m_patientRepository;

        public CarepathEnrollOperation(ICarePathwayEnrollmentService enrollmentService, IRepositoryService<Patient> patientRepository)
        {
            this.m_enrollmentService = enrollmentService;
            this.m_patientRepository = patientRepository;
        }

        /// <inheritdoc/>
        public string Name => "carepath-enroll";

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
            if(scopingKey is Guid uuid || Guid.TryParse(scopingKey.ToString(), out uuid))
            {
                if(!parameters.TryGet(CARE_PATHWAY_PARAMETER, out Guid pathwayId))
                {
                    throw new ArgumentNullException(CARE_PATHWAY_PARAMETER, ErrorMessages.ARGUMENT_NULL);
                }
                var patient = this.m_patientRepository.Get(uuid);
                if(patient == null)
                {
                    throw new KeyNotFoundException($"{scopingType.GetSerializationName()}/{scopingKey}");
                }
                return this.m_enrollmentService.Enroll(patient, pathwayId);
            }
            else
            {
                throw new ArgumentOutOfRangeException(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE);
            }
        }
    }
}
