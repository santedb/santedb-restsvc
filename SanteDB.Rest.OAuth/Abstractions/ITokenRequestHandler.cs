﻿using SanteDB.Core.Services;
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
