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
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Represents a device entity handler for security devices
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class DeviceEntityResourceHandler : EntityResourceHandlerBase<DeviceEntity>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public DeviceEntityResourceHandler(ILocalizationService localizationService, IRepositoryService<DeviceEntity> repositoryService, IResourceCheckoutService resourceCheckoutService, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, resourceCheckoutService, freetextSearchService)
        {
        }
    }
}