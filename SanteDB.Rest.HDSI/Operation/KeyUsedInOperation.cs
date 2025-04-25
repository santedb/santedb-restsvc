/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2025-2-15
 */
using SanteDB.Core;
using SanteDB.Core.Data;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Json.Formatter;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Serialization;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZXing.OneD;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// Operation which filters any object based on where the object is used
    /// </summary>
    public class KeyUsedInOperation : IApiChildOperation
    {

        // Serialization binder
        private readonly ModelSerializationBinder m_serializationBinder = new ModelSerializationBinder();

        /// <summary>
        /// Cross reference query parameter
        /// </summary>
        public const string XREF_QUERY_PARAMETER_NAME = "xr-query";
        /// <summary>
        /// Resource
        /// </summary>
        public const string XREF_RESOURCE_PARAMETER_NAME = "xr-resource";
        /// <summary>
        /// Selection from cross reference
        /// </summary>
        public const string XREF_SELECT_PARAMETER_NAME = "xr-select";

        /// <inheritdoc/>
        public string Name => "xref-use";

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        /// <inheritdoc/>
        public Type[] ParentTypes => Type.EmptyTypes;

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if(!parameters.TryGet(XREF_QUERY_PARAMETER_NAME, out string xrefQuery))
            {
                throw new ArgumentNullException(XREF_QUERY_PARAMETER_NAME);
            }
            if(!parameters.TryGet(XREF_RESOURCE_PARAMETER_NAME, out string resourceName))
            {
                throw new ArgumentNullException(XREF_RESOURCE_PARAMETER_NAME);
            }
            if(!parameters.TryGet(XREF_SELECT_PARAMETER_NAME, out string selector))
            {
                throw new ArgumentNullException(XREF_SELECT_PARAMETER_NAME);
            }

            var resourceType = this.m_serializationBinder.BindToType(null, resourceName);
            // Get Build the XREF query 
            var xrefLinq = QueryExpressionParser.BuildLinqExpression(resourceType, xrefQuery.ParseQueryString());
            var keySelector = QueryExpressionParser.BuildPropertySelector(resourceType, selector, forceLoad: false, returnNewObjectOnNull: false, convertReturn: typeof(Guid?));

            // Get the repo
            var repoType = typeof(IRepositoryService<>).MakeGenericType(resourceType);
            var repo = ApplicationServiceContext.Current.GetService(repoType) as IRepositoryService; 
            if(repo == null)
            {
                throw new InvalidOperationException(String.Format(ErrorMessages.DEPENDENT_CONFIGURATION_MISSING, repoType));
            }
            var keyValues = repo.Find(xrefLinq).Select<Guid?>(keySelector).Distinct();

            if (keyValues.Any())
            {
                repoType = typeof(IRepositoryService<>).MakeGenericType(scopingType);
                repo = ApplicationServiceContext.Current.GetService(repoType) as IRepositoryService;

                xrefLinq = QueryExpressionParser.BuildLinqExpression(scopingType, String.Join("&", keyValues.Select(k => $"id={k}")).ParseQueryString());
                var results = repo.Find(xrefLinq);
                return new Bundle(results.OfType<IdentifiedData>());
            }
            else
            {
                return new Bundle();
            }
        }
    }
}
