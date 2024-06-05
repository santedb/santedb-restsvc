﻿/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-6-21
 */
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// $tag operation
    /// </summary>
    public class TagOperation : IApiChildOperation
    {
        private readonly ITagPersistenceService m_tagPersistenceService;

        /// <summary>
        /// DI constructor
        /// </summary>
        public TagOperation(ITagPersistenceService tagPersistenceService)
        {
            this.m_tagPersistenceService = tagPersistenceService;
        }

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes => Type.EmptyTypes;

        /// <inheritdoc/>
        public string Name => "tag";

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if (scopingKey is Guid scopeGuid || Guid.TryParse(scopingKey.ToString(), out scopeGuid))
            {
                foreach (var p in parameters.Parameters)
                {
                    this.m_tagPersistenceService.Save(scopeGuid, p.Name, p.Value?.ToString());
                }
                return null;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(scopingKey));
            }
        }
    }
}
