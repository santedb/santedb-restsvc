using System;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Represents a resource handler that can lock or unlock objects
    /// </summary>
    public interface ICheckoutResourceHandler : IApiResourceHandler
    {
        /// <summary>
        /// Check-out a resource.
        /// </summary>
        /// <param name="key">The key of the resource to check-out.</param>
        /// <returns>Returns the locked object</returns>
        Object CheckOut(Object key);

        /// <summary>
        /// Obsoletes a unlock.
        /// </summary>
        /// <param name="key">The key of the resource to check-in.</param>
        /// <returns>Returns the unlock object.</returns>
        Object CheckIn(Object key);
    }
}
