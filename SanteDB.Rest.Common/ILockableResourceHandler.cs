using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Represents a resource handler that can lock or unlock objects
    /// </summary>
    public interface ILockableResourceHandler : IResourceHandler
    {
        /// <summary>
        /// Locks a resource.
        /// </summary>
        /// <param name="key">The key of the resource to Locks.</param>
        /// <returns>Returns the locked object</returns>
        Object Lock(Object key);

        /// <summary>
        /// Obsoletes a unlock.
        /// </summary>
        /// <param name="key">The key of the resource to unlock.</param>
        /// <returns>Returns the unlock object.</returns>
        Object Unlock(Object key);
    }
}
