using SanteDB.Core.Interop;
using SanteDB.Core.Model.Parameters;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.AppService.Operation
{
    /// <summary>
    /// Operation to disable a service
    /// </summary>
    public class DisableServiceOperation : IApiChildOperation
    {
        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        /// <summary>
        /// Get the parent types
        /// </summary>
        public Type[] ParentTypes => typeof()

        public string Name => throw new NotImplementedException();

        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            throw new NotImplementedException();
        }
    }
}
