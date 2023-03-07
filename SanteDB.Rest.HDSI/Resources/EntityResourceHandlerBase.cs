﻿/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-9-7
 */
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Entity resource handler base
    /// </summary>
    /// <typeparam name="TData">The type of entity this handler handles</typeparam>
    public class EntityResourceHandlerBase<TData> : HdsiResourceHandlerBase<TData>
        where TData : Entity, new()
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        /// <param name="localizationService"></param>
        /// <param name="freetextSearchService"></param>
        /// <param name="repositoryService"></param>
        public EntityResourceHandlerBase(ILocalizationService localizationService, IRepositoryService<TData> repositoryService, IResourceCheckoutService resourceCheckoutService, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, resourceCheckoutService, freetextSearchService)
        {
        }

        /// <summary>
        /// Handle a free text search
        /// </summary>
        protected override IQueryResultSet HandleFreeTextSearch(IEnumerable<string> terms)
        {
            return this.m_freetextSearch.SearchEntity<TData>(terms.ToArray());
        }
    }
}