/*
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
 * Date: 2022-5-30
 */
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using System;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a resource handler base type that is always bound to AMI.
    /// </summary>
    /// <typeparam name="TData">The data which the resource handler is bound to</typeparam>
    public class ResourceHandlerBase<TData> : Common.ResourceHandlerBase<TData> where TData : class, IIdentifiedResource, new()
    {
        /// <summary>
        /// DI Constructor
        /// </summary>
        public ResourceHandlerBase(ILocalizationService localizationService, IRepositoryService<TData> repositoryService, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, freetextSearchService)
        {
        }

        /// <summary>
        /// Gets the resource capabilities for the object
        /// </summary>
        public override ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.Update | ResourceCapabilityType.Get | ResourceCapabilityType.Search;

        /// <summary>
        /// Gets the scope
        /// </summary>
        public override Type Scope => typeof(IAmiServiceContract);
    }
}