/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: justin
 * Date: 2018-11-20
 */
using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Rest.Common.Attributes;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using SanteDB.Core.Interop;
using SanteDB.Rest.Common;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Resource handler which can deal with metadata resources
    /// </summary>
    public class AssigningAuthorityResourceHandler : ResourceHandlerBase<AssigningAuthority>
    {

        /// <summary>
        /// Get the capabilities of this handler
        /// </summary>
        public override ResourceCapability Capabilities => ResourceCapability.Get | ResourceCapability.Search;

    }
}