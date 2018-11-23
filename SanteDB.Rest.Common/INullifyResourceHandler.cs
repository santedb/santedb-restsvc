using SanteDB.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Represents a resource handler that can nullify objects
    /// </summary>
    public interface INullifyResourceHandler : IResourceHandler
    {

        /// <summary>
        /// Nullifies the object, which means it was created in error
        /// </summary>
        object Nullify(object key);
    }
}
