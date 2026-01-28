/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
 */
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// TFA mechanism resource handler
    /// </summary>
    public class TfaMechanismResourceHandler : ChainedResourceHandlerBase
    {
        private readonly ITfaService m_tfaRelayService;

        /// <summary>
        /// Gets the resource name
        /// </summary>
        public override string ResourceName => "Tfa";

        /// <summary>
        /// Get the for this resource
        /// </summary>
        public override Type Type => typeof(TfaMechanismInfo);

        /// <summary>
        /// Gets the scope of the api
        /// </summary>
        public override Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Get the capabilities
        /// </summary>
        public override ResourceCapabilityType Capabilities => ResourceCapabilityType.Search | ResourceCapabilityType.Get;

        /// <summary>
        /// TFA mechanism resource handler
        /// </summary>
        public TfaMechanismResourceHandler(ITfaService tfaRelay, ILocalizationService localizationService) : base(localizationService)
        {
            this.m_tfaRelayService = tfaRelay;
        }

        /// <inheritdoc/>
        public override object Create(object data, bool updateIfExists)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override object Delete(object key)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override object Get(object id, object versionId)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            IQueryable<TfaMechanismInfo> query = this.m_tfaRelayService.Mechanisms.Select(o => new TfaMechanismInfo(o)).AsQueryable();

            if (queryParameters?.Count > 0)
            {
                query = query.Where(QueryExpressionParser.BuildLinqExpression<TfaMechanismInfo>(queryParameters, null, false));
            }

            return query.AsResultSet();
        }

        /// <inheritdoc/>
        public override object Update(object data)
        {
            throw new NotSupportedException();
        }
    }
}
