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
