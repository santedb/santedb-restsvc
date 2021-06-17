using SanteDB.Core.Interop;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// API Child object
    /// </summary>
    public interface IApiChildObject 
    {

        /// <summary>
        /// Gets the resource name that this applies to
        /// </summary>
        Type[] ParentTypes { get; }

        /// <summary>
        /// Gets the name of the associated property
        /// </summary>
        string Name { get; }

    }
}
