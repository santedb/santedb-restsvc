using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Names of cookies used by the rest layer
    /// </summary>
    public static class ExtendedCookieNames
    {

        /// <summary>
        /// The name of the session cookie
        /// </summary>
        public const string SessionCookieName = "_sbs";

        /// <summary>
        /// The name of the refresh cookie
        /// </summary>
        public const string RefreshCookieName = "_sbr";
    }
}
