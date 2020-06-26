using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// API Resource handler which includes support for extended methods
    /// </summary>
    public interface IApiResourceHandlerEx : IApiResourceHandler
    {

        /// <summary>
        /// Touches a resource (updates its modified on without changing or creating a new version)
        /// </summary>
        Object Touch(Object key);

    }
}
