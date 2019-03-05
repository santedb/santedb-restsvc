﻿/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 *
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using SanteDB.Core.Interop;
using SanteDB.Core.Model.DataTypes;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Represents a handler for extension types
    /// </summary>
    public class ExtensionTypeResourceHandler : ResourceHandlerBase<ExtensionType>
    {

        /// <summary>
        /// Get capabilities of this handler
        /// </summary>
        public override ResourceCapability Capabilities => ResourceCapability.Get | ResourceCapability.Search;
    }
}
