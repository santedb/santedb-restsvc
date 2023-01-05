using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Resource handler which has a repository
    /// </summary>
    public interface IApiResourceHandlerRepository : IApiResourceHandler
    {

        /// <summary>
        /// Get the repository which backs this 
        /// </summary>
        IRepositoryService Repository { get; }

    }
}
