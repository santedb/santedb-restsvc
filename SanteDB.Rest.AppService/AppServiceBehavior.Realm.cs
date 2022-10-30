using SanteDB.Core.Model.Parameters;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.AppService
{
    /// <summary>
    /// Application service behavior for realm functions
    /// </summary>
    public partial class AppServiceBehavior
    {

        /// <inheritdoc/>
        public ParameterCollection JoinRealm(ParameterCollection parameters)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public ParameterCollection UnJoinRealm(ParameterCollection parameters)
        {
            throw new NotImplementedException();
        }

        public ParameterCollection PerformUpdate(ParameterCollection parameters)
        {
            throw new NotImplementedException();
        }
    }
}
