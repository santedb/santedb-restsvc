/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-5-19
 */
using SanteDB.Core;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Specialized;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Provenance
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class SecurityProvenanceHandler : ResourceHandlerBase<SecurityProvenance>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public SecurityProvenanceHandler(ILocalizationService localizationService, IRepositoryService<SecurityProvenance> repositoryService, ISubscriptionExecutor subscriptionExecutor = null, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, subscriptionExecutor, freetextSearchService)
        {
        }

        /// <summary>
        /// Capabilities
        /// </summary>
        public override ResourceCapabilityType Capabilities => ResourceCapabilityType.Get | ResourceCapabilityType.Search;

        /// <summary>
        /// Query for security provenance objects
        /// </summary>
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            var query = QueryExpressionParser.BuildLinqExpression<SecurityProvenance>(queryParameters);
            return ApplicationServiceContext.Current.GetService<ISecurityRepositoryService>().FindProvenance(query);
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