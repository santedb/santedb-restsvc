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
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Roles;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Resource handler for patients
    /// </summary>
    public class PatientResourceHandler : ResourceHandlerBase<Patient>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="localizationService"></param>
        public PatientResourceHandler(ILocalizationService localizationService) : base(localizationService)
        {

        }
        /// <summary>
        /// Create the specified patient
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.WriteClinicalData)]
        public override Object Create(Object data, bool updateIfExists)
        {
            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Gets the specified patient
        /// </summary>
        /// <returns></returns>
        [Demand(PermissionPolicyIdentifiers.ReadClinicalData)]
        public override Object Get(object id, object versionId)
        {
            return base.Get(id, versionId);
        }

        /// <summary>
        /// Obsoletes the specified patient
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.DeleteClinicalData)]
        public override Object Obsolete(object key)
        {
            return base.Obsolete(key);
        }

        /// <summary>
        /// Query for the specified patient
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.QueryClinicalData)]
        public override IEnumerable<Object> Query(NameValueCollection queryParameters)
        {
            return base.Query(queryParameters);
        }


        /// <summary>
        /// Query for the specified patient with restrictions
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.QueryClinicalData)]
        public override IEnumerable<Object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            return base.Query(queryParameters, offset, count, out totalCount);
        }


        /// <summary>
        /// Update the specified patient
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.WriteClinicalData)]
        public override Object Update(Object data)
        {
            return base.Update(data);
        }
    }
}