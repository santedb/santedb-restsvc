﻿/*
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE.md)
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
 * Date: 2021-2-9
 */
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Represents a resource handler which queries places
    /// </summary>
    public class PlaceResourceHandler : ResourceHandlerBase<Place>
	{

        /// <summary>
        /// Create the specified place
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.WritePlacesAndOrgs)]
        public override Object Create(Object data, bool updateIfExists)
        {
            return base.Create(data, updateIfExists);
        }

        /// <summary>
        /// Gets the specified place
        /// </summary>
        /// <returns></returns>
        [Demand(PermissionPolicyIdentifiers.ReadPlacesAndOrgs)]
        public override Object Get(object id, object versionId)
        {
            return base.Get(id, versionId);
        }

        /// <summary>
        /// Obsoletes the specified place
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.DeletePlacesAndOrgs)]
        public override Object Obsolete(object key)
        {
            return base.Obsolete(key);
        }

        /// <summary>
        /// Query for the specified place
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadPlacesAndOrgs)]
        public override IEnumerable<Object> Query(NameValueCollection queryParameters)
        {
            return base.Query(queryParameters);
        }


        /// <summary>
        /// Query for the specified place with restrictions
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadPlacesAndOrgs)]
        public override IEnumerable<Object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            var retVal = base.Query(queryParameters, offset, count, out totalCount);

            // Clean reverse relationships
            List<String> lean = null;
            if (queryParameters.TryGetValue("_lean", out lean) && lean[0] == "true")
                foreach(var r in retVal.OfType<Entity>())
                    r.Relationships.RemoveAll(o => o.SourceEntityKey != r.Key);

            return retVal;
        }


        /// <summary>
        /// Update the specified place
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.WritePlacesAndOrgs)]
        public override Object Update(Object data)
        {
            return base.Update(data);
        }
    }
}