/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * User: fyfej (Justin Fyfe)
 * Date: 2021-8-5
 */
using SanteDB.Core;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Provenance
    /// </summary>
    public class SecurityProvenanceHandler : ResourceHandlerBase<SecurityProvenance>
    {
        /// <summary>
        /// Capabilities
        /// </summary>
        public override ResourceCapabilityType Capabilities => ResourceCapabilityType.Get | ResourceCapabilityType.Search;

        /// <summary>
        /// Query for security provenance objects
        /// </summary>
        public override IEnumerable<object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {

            var query = QueryExpressionParser.BuildLinqExpression<SecurityProvenance>(queryParameters);
            Guid queryId = Guid.Empty;
            List<String> qvalue = null;
            if (queryParameters.TryGetValue("_queryId", out qvalue))
                queryId = Guid.Parse(qvalue.First());

            // Order by
            ModelSort<SecurityProvenance>[] sortParameters = null;
            if (queryParameters.TryGetValue("_orderBy", out qvalue))
                sortParameters = QueryExpressionParser.BuildSort<SecurityProvenance>(qvalue);
            return ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>().FindProvenance(query, offset, count, out totalCount, queryId, sortParameters);

            
        }

        /// <summary>
        /// Get the specified object
        /// </summary>
        /// <param name="id"></param>
        /// <param name="versionId"></param>
        /// <returns></returns>
        public override object Get(object id, object versionId)
        {
            return ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>().GetProvenance((Guid)id);
        }
    }
}
