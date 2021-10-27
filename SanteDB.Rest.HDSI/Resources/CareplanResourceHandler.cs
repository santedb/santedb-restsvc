/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-5
 */
using RestSrvr;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Represents a care plan resource handler
    /// </summary>
    public class CareplanResourceHandler : IApiResourceHandler
    {
        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(CareplanResourceHandler));

        // Localization service
        private ILocalizationService m_localizationService = ApplicationServiceContext.Current.GetService<ILocalizationService>();

        /// <summary>
        /// Get capabilities statement
        /// </summary>
        public ResourceCapabilityType Capabilities
        {
            get
            {
                return ResourceCapabilityType.Get | ResourceCapabilityType.Search;
            }
        }

        /// <summary>
        /// Gets the scope
        /// </summary>
        public Type Scope => typeof(IHdsiServiceContract);

        /// <summary>
        /// Gets the resource name
        /// </summary>
        public string ResourceName
        {
            get
            {
                return "CarePlan";
            }
        }

        /// <summary>
        /// Gets the type that this produces
        /// </summary>
        public Type Type
        {
            get
            {
                return typeof(CarePlan);
            }
        }

        /// <summary>
        /// Create a care plan
        /// </summary>
        public Object Create(Object data, bool updateIfExists)
        {

            (data as Bundle)?.Reconstitute();
            data = (data as CarePlan)?.Target ?? data as Patient;
            if (data == null)
            {
                this.m_tracer.TraceError("Careplan requires a patient or bundle containing a patient entry");
                throw new InvalidOperationException(this.m_localizationService.GetString("error.rest.hdsi.careplanRequiresPatient"));
            }

            // Get care plan service
            var carePlanner = ApplicationServiceContext.Current.GetService<ICarePlanService>();
            var plan = carePlanner.CreateCarePlan(data as Patient,
                RestOperationContext.Current.IncomingRequest.QueryString["_asEncounters"] == "true",
                RestOperationContext.Current.IncomingRequest.QueryString.ToQuery().ToDictionary(o => o.Key, o => (Object)o.Value));

            // Expand the participation roles form the care planner
            IConceptRepositoryService conceptService = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>();
            foreach (var p in plan.Action)
                p.Participations.ForEach(o => o.ParticipationRoleKey = o.ParticipationRoleKey ?? conceptService.GetConcept(o.ParticipationRole?.Mnemonic).Key);
            return Bundle.CreateBundle(plan);

        }

        /// <summary>
        /// Gets a careplan by identifier
        /// </summary>
        public Object Get(object id, object versionId)
        {
            // TODO: This will become the retrieval of care plan object 
            throw new NotSupportedException(this.m_localizationService.GetString("error.type.NotSupportedException"));
        }

        /// <summary>
        /// Obsolete the care plan
        /// </summary>
        public Object Obsolete(object key)
        {
            throw new NotSupportedException(this.m_localizationService.GetString("error.type.NotSupportedException"));
        }

        /// <summary>
        /// Query for care plan
        /// </summary>
        public IEnumerable<Object> Query(NameValueCollection queryParameters)
        {
            int tr = 0;
            return this.Query(queryParameters, 0, 100, out tr);
        }

        /// <summary>
        /// Query for care plan objects... Constructs a care plan for all patients matching the specified query parameters
        /// </summary>
        /// TODO: Change this to actually query care plans
        public IEnumerable<Object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            var repositoryService = ApplicationServiceContext.Current.GetService<IRepositoryService<Patient>>();
            if (repositoryService == null)
            {
                this.m_tracer.TraceError("Could not find patient repository service");
                throw new InvalidOperationException(this.m_localizationService.GetString("error.type.InvalidOperation.missingPatientRepo"));
            }
                

            // Query
            var carePlanner = ApplicationServiceContext.Current.GetService<ICarePlanService>();

            Expression<Func<Patient, bool>> queryExpr = QueryExpressionParser.BuildLinqExpression<Patient>(queryParameters);
            List<String> queryId = null;
            IEnumerable<Patient> patients = null;
            if (queryParameters.TryGetValue("_queryId", out queryId) && repositoryService is IPersistableQueryRepositoryService<Patient>)
                patients = (repositoryService as IPersistableQueryRepositoryService<Patient>).Find(queryExpr, offset, count, out totalCount, new Guid(queryId[0]));
            else
                patients = repositoryService.Find(queryExpr, offset, count, out totalCount);

            // Create care plan for the patients
            IConceptRepositoryService conceptService = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>();
            return patients.Select(o =>
            {
                var plan = carePlanner.CreateCarePlan(o);
                foreach (var p in plan.Action)
                    p.Participations.ForEach(x => x.ParticipationRoleKey = x.ParticipationRoleKey ?? conceptService.GetConcept(x.ParticipationRole?.Mnemonic).Key);
                return plan;
            });

        }

        /// <summary>
        /// Update care plan 
        /// </summary>
        public Object Update(Object data)
        {
            throw new NotSupportedException(this.m_localizationService.GetString("error.type.NotSupportedException"));
        }
    }
}
