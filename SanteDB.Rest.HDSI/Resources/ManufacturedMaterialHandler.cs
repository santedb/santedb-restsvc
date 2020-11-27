/*
 * Copyright (C) 2019 - 2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE.md)
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
 * Date: 2019-11-27
 */
using SanteDB.Core;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Represents a HDSI handler for manufactured materials
    /// </summary>
    public class ManufacturedMaterialHandler : ResourceHandlerBase<ManufacturedMaterial>
	{

        /// <summary>
        /// Create the specified material
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.WriteMaterials)]
        public override Object Create(Object data, bool updateIfExists)
        {
            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Gets the specified manufactured material
        /// </summary>
        /// <returns></returns>
        [Demand(PermissionPolicyIdentifiers.ReadMaterials)]
        public override Object Get(object id, object versionId)
        {
            return base.Get(id, versionId);
        }

        /// <summary>
        /// Obsoletes the specified material
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.DeleteMaterials)]
        public override Object Obsolete(object key)
        {
            return base.Obsolete(key);
        }

        /// <summary>
        /// Query for the specified material
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMaterials)]
        public override IEnumerable<Object> Query(NameValueCollection queryParameters)
        {
            return base.Query(queryParameters);
        }


        /// <summary>
        /// Query for the specified material with restrictions
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMaterials)]
        public override IEnumerable<Object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            var retVal = base.Query(queryParameters, offset, count, out totalCount);

            var erPersistence = ApplicationServiceContext.Current.GetService<IRepositoryService<EntityRelationship>>() ;
            var auth = AuthenticationContext.Current;

            foreach(var o in retVal.OfType<ManufacturedMaterial>())
            {
                int tr = 0;
                if(!o.Relationships.Any(r=>r.RelationshipTypeKey == EntityRelationshipTypeKeys.Instance))
                    o.Relationships.AddRange(erPersistence.Find(q => q.TargetEntityKey == o.Key && q.RelationshipTypeKey == EntityRelationshipTypeKeys.Instance, 0, 100, out tr));
            };

            return retVal;
        }


        /// <summary>
        /// Update the specified material
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.WriteMaterials)]
        public override Object Update(Object data)
        {
            return base.Update(data);
        }

    }
}