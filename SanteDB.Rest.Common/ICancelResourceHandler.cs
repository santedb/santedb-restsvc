using SanteDB.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Represents a resource handler that can cancel objects
    /// </summary>
    public interface ICancelResourceHandler : IResourceHandler
    {

        /// <summary>
        /// Cancel the specified object
        /// </summary>
        Object Cancel(object key);
    }
}
