using SanteDB.Core.Interop;
using SanteDB.Core.Model.Query;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Allows a programmatic way of providing associated properties on other objects
    /// </summary>
    public interface IApiChildResourceHandler : IApiChildObject
    {
       
        /// <summary>
        /// Gets the type of data this associative property is expecting
        /// </summary>
        Type PropertyType { get; }


        /// <summary>
        /// The capabilities of the sub-resource
        /// </summary>
        ResourceCapabilityType Capabilities { get; }

        /// <summary>
        /// Get the value of the associated property with no context (exmaple: GET /hdsi/resource/property)
        /// </summary>
        IEnumerable<object> Query(Type scopingType, Object scopingKey, NameValueCollection filter, int offset, int count, out int totalCount);

        /// <summary>
        /// Get the value of the associated property with context (exmaple: GET /hdsi/resource/property/key)
        /// </summary>
        object Get(Type scopingType, Object scopingKey, object key);

        /// <summary>
        /// Remove an object from the associated property
        /// </summary>
        object Remove(Type scopingType, Object scopingKey, object key);

        /// <summary>
        /// Add a value to the associated property
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        object Add(Type scopingType, Object scopingKey, object item);
    }
}
