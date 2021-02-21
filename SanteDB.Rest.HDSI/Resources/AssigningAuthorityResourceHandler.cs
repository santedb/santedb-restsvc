/*
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE.md)
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
 * Date: 2021-2-9
 */
using SanteDB.Core.Interop;
using SanteDB.Core.Model.DataTypes;

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
        public override ResourceCapabilityType Capabilities => ResourceCapabilityType.Get | ResourceCapabilityType.Search;

    }
}