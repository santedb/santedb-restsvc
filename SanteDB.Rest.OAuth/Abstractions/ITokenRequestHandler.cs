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
 * Date: 2023-5-19
 */
using SanteDB.Core.Services;
using System.Collections.Generic;

namespace SanteDB.Rest.OAuth.Abstractions
{
    /// <summary>
    /// A handler that can process a token endpoint request.
    /// </summary>
    public interface ITokenRequestHandler : IServiceImplementation
    {
        /// <summary>
        /// Gets the supported grant types for the Token handler. This value is read during instantiation to "wire up" the handler to the grant types it will process.
        /// </summary>
        IEnumerable<string> SupportedGrantTypes { get; }

        /// <summary>
        /// Handle a request for a token.
        /// </summary>
        /// <param name="context">The context for the request.</param>
        /// <returns>True if the handler was successful. False otherwise.</returns>
        bool HandleRequest(Model.OAuthTokenRequestContext context);
    }
}
