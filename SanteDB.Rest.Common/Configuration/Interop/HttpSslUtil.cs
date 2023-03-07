using System;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Rest.Common.Configuration.Interop
{
    /// <summary>
    /// A utility for binding HTTP certificates 
    /// </summary>
    public static class HttpSslUtil
    {

        /// <summary>
        /// Binder dictionary of all registered binders
        /// </summary>
        private static IDictionary<PlatformID, ISslCertificateBinder> s_binderDictionary = AppDomain.CurrentDomain.GetAllTypes().Where(t => typeof(ISslCertificateBinder).IsAssignableFrom(t) && !t.IsInterface).Select(t => Activator.CreateInstance(t) as ISslCertificateBinder).ToDictionary(o => o.Platform, o => o);

        /// <summary>
        /// Get the current platform's certificate binder
        /// </summary>
        /// <returns></returns>
        public static ISslCertificateBinder GetCurrentPlatformCertificateBinder()
        {
            if (s_binderDictionary.TryGetValue(Environment.OSVersion.Platform, out var binder))
            {
                return binder;
            }
            else
            {
                throw new InvalidOperationException("Cannot locate a SSL certificate binding layer");
            }
        }
    }
}
