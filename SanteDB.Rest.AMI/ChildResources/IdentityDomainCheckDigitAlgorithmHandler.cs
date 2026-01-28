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
using SanteDB.Core.Model.AMI.Types;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace SanteDB.Rest.AMI.ChildResources
{
    /// <summary>
    /// A sub-resource handler which gets check digit handlers
    /// </summary>
    public class IdentityDomainCheckDigitAlgorithmHandler : IApiChildResourceHandler
    {
        private readonly IServiceManager m_serviceManager;

        /// <summary>
        /// DI CTOR
        /// </summary>
        public IdentityDomainCheckDigitAlgorithmHandler(IServiceManager serviceManager)
        {
            this.m_serviceManager = serviceManager;
        }

        /// <inheritdoc/>
        public string Name => "_checkDigit";

        /// <inheritdoc/>
        public Type PropertyType => typeof(AmiTypeDescriptor);

        /// <inheritdoc/>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Search;

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(IdentityDomain) };

        /// <inheritdoc/>
        public object Add(Type scopingType, object scopingKey, object item)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public object Get(Type scopingType, object scopingKey, object key)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public IQueryResultSet Query(Type scopingType, object scopingKey, NameValueCollection filter)
        {
            return this.m_serviceManager.CreateInjectedOfAll<ICheckDigitAlgorithm>().Select(o => new AmiTypeDescriptor(o.GetType(), o.Name)).AsResultSet();
        }

        /// <inheritdoc/>
        public object Remove(Type scopingType, object scopingKey, object key)
        {
            throw new NotSupportedException();
        }
    }
}
