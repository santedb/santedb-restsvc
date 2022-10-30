using SanteDB.Client.Disconnected.Data.Synchronization;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Patch;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.AppService
{
    /// <summary>
    /// Application service behavior for queue operations
    /// </summary>
    public partial class AppServiceBehavior
    {

        public void DeleteQueueItem(string queueName, int id)
        {
            throw new NotImplementedException();
        }


        public Patch GetQueueConflict(int id)
        {
            throw new NotImplementedException();
        }

        public Dictionary<string, int> GetQueue()
        {
            throw new NotImplementedException();
        }

        public List<ISynchronizationQueueEntry> GetQueue(string queueName)
        {
            throw new NotImplementedException();
        }

        public IdentifiedData GetQueueData(string queueName, int id)
        {
            throw new NotImplementedException();
        }

        public IdentifiedData ResolveQueueConflict(int id, Patch resolution)
        {
            throw new NotImplementedException();
        }

        public void RetryQueueEntry(int id, ParameterCollection parameters)
        {
            throw new NotImplementedException();
        }
    }
}
