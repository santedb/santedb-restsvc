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
 * Date: 2023-3-10
 */
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// TFA mechanism resource handler
    /// </summary>
    public class TfaMechanismResourceHandler : IApiResourceHandler
    {
        private readonly ITfaService m_tfaRelayService;

        /// <summary>
        /// Gets the resource name
        /// </summary>
        public string ResourceName => "Tfa";

        /// <summary>
        /// Get the for this resource
        /// </summary>
        public Type Type => typeof(TfaMechanismInfo);

        /// <summary>
        /// Gets the scope of the api
        /// </summary>
        public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Get the capabilities
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Search | ResourceCapabilityType.Get;

        /// <summary>
        /// TFA mechanism resource handler
        /// </summary>
        public TfaMechanismResourceHandler(ITfaService tfaRelay)
        {
            this.m_tfaRelayService = tfaRelay;
        }

        /// <inheritdoc/>
        public object Create(object data, bool updateIfExists)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public object Delete(object key)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public object Get(object id, object versionId)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public IQueryResultSet Query(NameValueCollection queryParameters)
        {
            IQueryable<TfaMechanismInfo> query = this.m_tfaRelayService.Mechanisms.Select(o => new TfaMechanismInfo(o)).AsQueryable();

            if (queryParameters?.Count > 0)
            {
                query = query.Where(QueryExpressionParser.BuildLinqExpression<TfaMechanismInfo>(queryParameters, null, false));
            }

            return query.AsResultSet();
        }

        /// <inheritdoc/>
        public object Update(object data)
        {
            throw new NotSupportedException();
        }
    }
}
