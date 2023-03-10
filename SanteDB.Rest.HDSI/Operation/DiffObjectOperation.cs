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
using SanteDB.Core;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Linq;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// Perform a difference operation
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class DiffObjectOperation : IApiChildOperation
    {
        // The patch service
        private IPatchService m_patchService;

        /// <summary>
        /// Create new DI object operation
        /// </summary>
        public DiffObjectOperation(IPatchService patchService)
        {
            this.m_patchService = patchService;
        }

        /// <summary>
        /// Scope binding
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <summary>
        /// Get parent typers
        /// </summary>
        public Type[] ParentTypes => AppDomain.CurrentDomain.GetAllTypes().Where(t => typeof(IdentifiedData).IsAssignableFrom(t) && !t.IsAbstract).ToArray();

        /// <summary>
        /// Get the name
        /// </summary>
        public string Name => "diff";

        /// <summary>
        /// Invoke the operation
        /// </summary>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if (parameters.TryGet<String>("other", out string bKeyString) &&
                Guid.TryParse(scopingKey.ToString(), out Guid aKey) && Guid.TryParse(bKeyString, out Guid bKey))
            {
                var repository = ApplicationServiceContext.Current.GetService(typeof(IRepositoryService<>).MakeGenericType(scopingType)) as IRepositoryService;
                if (repository == null)
                {
                    throw new InvalidOperationException($"Cannot load repository for {scopingType.Name}");
                }
                IdentifiedData objectA = repository.Get(aKey), objectB = repository.Get(bKey);
                return this.m_patchService.Diff(objectA, objectB);
            }
            else
            {
                throw new ArgumentException("Both A and B parameters must be specified");
            }
        }
    }
}