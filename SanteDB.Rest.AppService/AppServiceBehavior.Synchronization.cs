using SanteDB.Client.Disconnected.Data.Synchronization;
using SanteDB.Core.Model.Parameters;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.AppService
{
    /// <summary>
    /// Application service behavior for synchronization log management
    /// </summary>
    public partial class AppServiceBehavior
    {

        public List<ISynchronizationLogEntry> GetSynchronizationLogs()
        {
            throw new NotImplementedException();
        }
        public void ResetSynchronizationStatus(ParameterCollection parameters)
        {
            throw new NotImplementedException();
        }


        public void SynchronizeNow(ParameterCollection parameters)
        {
            throw new NotImplementedException();
        }

    }
}
