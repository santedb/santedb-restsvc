using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.HDSI.ChildResources
{
    /// <summary>
    /// Child resource handler for enroling / removing a patient to/from care pathways
    /// </summary>
    public class PatientCarepathChildResource : IApiChildResourceHandler
    {
        private readonly ICarePathwayEnrollmentService m_enrolmentService;
        private readonly IRepositoryService<Patient> m_patientRepository;

        /// <summary>
        /// DI constructor
        /// </summary>
        public PatientCarepathChildResource(IRepositoryService<Patient> patientRepository, ICarePathwayEnrollmentService carePathwayEnrollmentService)
        {
            this.m_enrolmentService = carePathwayEnrollmentService;
            this.m_patientRepository = patientRepository;
        }

        /// <inheritdoc/>
        public string Name => "carepaths";

        /// <inheritdoc/>
        public Type PropertyType => typeof(CarePathwayDefinition);

        /// <inheritdoc/>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.CreateOrUpdate | ResourceCapabilityType.Delete | ResourceCapabilityType.Get | ResourceCapabilityType.Search | ResourceCapabilityType.Update;

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(Patient) };

        /// <inheritdoc/>
        public object Add(Type scopingType, object scopingKey, object item)
        {
            // Enroll the patient
            if (scopingType != typeof(Patient))
            {
                throw new InvalidOperationException(ErrorMessages.ARGUMENT_INVALID_TYPE);
            }

            if(!(scopingKey is Guid patientId))
            {
                throw new ArgumentOutOfRangeException("scope", String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(Guid), scopingKey.GetType()));
            }
            if (!(item is CarePathwayDefinition carePathwayDefinition))
            {
                throw new ArgumentOutOfRangeException("body", String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(Guid), scopingKey.GetType()));
            }

            var patient = this.m_patientRepository.Get(patientId);
            if (patient == null)
            {
                throw new KeyNotFoundException($"Patient/{patientId}");
            }
            return this.m_enrolmentService.Enroll(patient, carePathwayDefinition);
        }

        /// <inheritdoc/>
        public object Get(Type scopingType, object scopingKey, object key)
        {
            if (scopingType != typeof(Patient))
            {
                throw new InvalidOperationException(ErrorMessages.ARGUMENT_INVALID_TYPE);
            }
            else if (scopingKey is Guid patientId && key is Guid carePlanId)
            {
                var patient = this.m_patientRepository.Get(patientId);
                if (patient == null)
                {
                    throw new KeyNotFoundException($"Patient/{patientId}");
                }
                
                if(!this.m_enrolmentService.TryGetEnrollment(patient, carePlanId, out var carePlan))
                {
                    return null;
                }
                return carePlan;
            }
            else
            {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(Guid), scopingKey.GetType()));
            }
        }

        /// <inheritdoc/>
        public IQueryResultSet Query(Type scopingType, object scopingKey, NameValueCollection filter)
        {
            if (scopingType != typeof(Patient))
            {
                throw new InvalidOperationException(ErrorMessages.ARGUMENT_INVALID_TYPE);
            }
            else if (scopingKey is Guid uuid)
            {
                var patient = this.m_patientRepository.Get(uuid);
                if(patient == null)
                {
                    throw new KeyNotFoundException($"Patient/{uuid}");
                }
                return new MemoryQueryResultSet(this.m_enrolmentService.GetEnrolledCarePaths(patient));
            }
            else { 
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(Guid), scopingKey.GetType()));
            }
        }

        /// <inheritdoc/>
        public object Remove(Type scopingType, object scopingKey, object key)
        {
            if (scopingType != typeof(Patient))
            {
                throw new InvalidOperationException(ErrorMessages.ARGUMENT_INVALID_TYPE);
            }
            else if (scopingKey is Guid patientId && key is Guid carePathId)
            {
                var patient = this.m_patientRepository.Get(patientId);
                if (patient == null)
                {
                    throw new KeyNotFoundException($"Patient/{patientId}");
                }

                return this.m_enrolmentService.UnEnroll(patient, carePathId);
            }
            else
            {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(Guid), scopingKey.GetType()));
            }
        }
    }
}
