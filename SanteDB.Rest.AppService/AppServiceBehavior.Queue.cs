﻿using RestSrvr;
using SanteDB.Client.Disconnected.Data.Synchronization;
using SanteDB.Core.i18n;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Patch;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace SanteDB.Rest.AppService
{
    /// <summary>
    /// Application service behavior for queue operations
    /// </summary>
    public partial class AppServiceBehavior
    {
        //Declared in AppServiceBehavior.cs
        //this.m_synchronizationQueueManager
        //this.m_synchronizationLogService

        /// <summary>
        /// The queue name for the dead queue that is used by <see cref="GetQueueConflict(int)"/>, <see cref="ResolveQueueConflict(int, Patch)"/> and <see cref="RetryQueueEntry(int, ParameterCollection)"/>.
        /// </summary>
        private const string DeadletterQueueName = "dead"; //TODO: Is this right or can it be defined on ISynchronizationQueueService somewhere?

        /// <summary>
        /// Uses the <see cref="ILocalizationService"/> to create an error message with the <paramref name="queueName"/>.
        /// </summary>
        /// <param name="queueName">The name of the queue that was not found.</param>
        /// <returns>A localized error message to insert into an Exception.</returns>
        private string ErrorMessage_QueueNotFound(string queueName) => m_localizationService.GetString("error.queue.notfound", new { queueName }); //TODO: Ensure error message is created.
        /// <summary>
        /// Uses the <see cref="ILocalizationService"/> to create an error message with the <paramref name="id"/> of the entry that is not found.
        /// </summary>
        /// <param name="id">The entry id that was not found.</param>
        /// <returns>A localized error message to insert into an Exception.</returns>
        private string ErrorMessage_QueueEntryNotFound(int id) => m_localizationService.GetString("error.queueentry.notfound", new { id });

        /// <summary>
        /// Uses the <see cref="ILocalizationService"/> to create an error message for the missing <see cref="IPatchService"/>.
        /// </summary>
        /// <returns>A localized error message to insert into an Exception.</returns>
        private string ErrorMessage_PatchServiceNull() => m_localizationService.GetString(ErrorMessageStrings.MISSING_SERVICE, new { serviceName = nameof(IPatchService) });

        /// <summary>
        /// Uses the <see cref="IServiceManager"/> to create an instance of <see cref="IDataPersistenceService{TData}"/> for the <paramref name="type"/> of data.
        /// </summary>
        /// <param name="type">The type of data to get a data persistence service for.</param>
        /// <returns>The constructed non-generic version of <see cref="IDataPersistenceService"/>.</returns>
        private IDataPersistenceService GetDataPersistenceService(string type)
            => m_serviceManager.CreateInjected(typeof(IDataPersistenceService<>).MakeGenericType(Type.GetType(type))) as IDataPersistenceService;

        /// <summary>
        /// Gets a queue from the <see cref="m_synchronizationQueueManager"/> by the name of the queue.
        /// </summary>
        /// <param name="queueName">The name of the queue to get from the service.</param>
        /// <returns>The <see cref="ISynchronizationQueue"/> instance.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when there is no queue with the name in <paramref name="queueName"/>.</exception>
        [DebuggerHidden]
        private ISynchronizationQueue GetQueueByName(string queueName)
            => m_synchronizationQueueManager?.Get(queueName) ?? throw new KeyNotFoundException(ErrorMessage_QueueNotFound(queueName));

        /// <inheritdoc />
        [Demand(PermissionPolicyIdentifiers.AccessClientAdministrativeFunction)]
        public void DeleteQueueItem(string queueName, int id)
        {
            GetQueueByName(queueName)?.Delete(id);
        }

        /// <summary>
        /// Internal shared work from <see cref="GetQueueConflict(int)"/> and <see cref="ResolveQueueConflict(int, Patch)"/>. Retrieves the queue, item, suitable <see cref="IDataPersistenceService"/>, and queue version and database version of the internal <see cref="IdentifiedData"/>.
        /// </summary>
        /// <param name="id">The entry identifier to retrieve from the queue.</param>
        /// <returns>A tuple containing both the queue and database version of the identified data, as well as the <see cref="IDataPersistenceService"/> and <see cref="ISynchronizationQueue"/> used in <see cref="ResolveQueueConflict(int, Patch)"/>.</returns>
        /// <exception cref="KeyNotFoundException">Thrown when the queue or entry cannot be found.</exception>
        /// <exception cref="NotSupportedException">Thrown when there is no patch service.</exception>
        private (IdentifiedData queueVersion, IdentifiedData dbVersion, IDataPersistenceService service, ISynchronizationQueue deadLetterQueue) GetQueueConflictInternal(int id)
        {
            ThrowIfNoPatchService();

            var queue = GetQueueByName(DeadletterQueueName);

            var item = queue?.Get(id) ?? throw new KeyNotFoundException(ErrorMessage_QueueEntryNotFound(id));

            var queueversion = item.Data;

            if (null == queueversion?.Key)
            {
                throw new NotSupportedException(m_localizationService.GetString(ErrorMessageStrings.ARGUMENT_NULL));
            }

            var persistenceservice = GetDataPersistenceService(item.Type);

            var dbversion = (persistenceservice.Get(queueversion.Key.Value) as IdentifiedData) ?? throw new KeyNotFoundException(m_localizationService.GetString(ErrorMessageStrings.NOT_FOUND, new { type = item.Type, id = queueversion.Key }));

            return (queueversion, dbversion, persistenceservice, queue);
        }

        /// <inheritdoc />
        [Demand(PermissionPolicyIdentifiers.ReadClinicalData)]
        public Patch GetQueueConflict(int id)
        {
            (var queueversion, var dbversion, _, _) = GetQueueConflictInternal(id);

            return m_patchService?.Diff(dbversion, queueversion);
        }

        /// <inheritdoc />
        [Demand(PermissionPolicyIdentifiers.WriteClinicalData)]
        public IdentifiedData ResolveQueueConflict(int id, Patch resolution)
        {
            (var queueversion, var dbversion, var persistenceservice, var queue) = GetQueueConflictInternal(id);

            var retval = m_patchService.Patch(resolution, dbversion, force: true);

            retval = persistenceservice.Update(retval) as IdentifiedData;

            queue.Delete(id);

            return retval;
        }

        [DebuggerHidden]
        private void ThrowIfNoPatchService()
        {
            if (null == m_patchService)
            {
                throw new NotSupportedException(ErrorMessage_PatchServiceNull());
            }
        }

        /// <inheritdoc />
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public Dictionary<string, int> GetQueue()
        {
            return m_synchronizationQueueManager?.GetAll(SynchronizationPattern.BiDirectional)?.ToDictionary(k => k.Name, v => v.Count()) ?? new Dictionary<string, int>();
        }

        /// <inheritdoc />
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public List<ISynchronizationQueueEntry> GetQueue(string queueName)
        {
            var qs = RestOperationContext.Current.IncomingRequest.QueryString;

            var results = GetQueueByName(queueName)?.Query(qs);

            if (null == results)
            {
                return new List<ISynchronizationQueueEntry>();
            }

            var retVal = results.ApplyResultInstructions(qs, out _, out _)?.OfType<ISynchronizationQueueEntry>()?.ToList();

            return retVal;

        }

        /*
        /// <inheritdoc />
        [Demand(PermissionPolicyIdentifiers.ReadClinicalData)]
        public ISynchronizationQueueEntry GetQueueEntry(string queueName, int id)
        {
            return GetQueueByName(queueName)?.Get(id);
        }
        */

        /// <inheritdoc />
        [Demand(PermissionPolicyIdentifiers.ReadClinicalData)]
        public IdentifiedData GetQueueData(string queueName, int id)
        {
            return GetQueueByName(queueName)?.Get(id).Data;
        }

        

        /// <inheritdoc />
        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public void RetryQueueEntry(int id, ParameterCollection parameters)
        {
            var queue = GetQueueByName(DeadletterQueueName);
            var item = (queue?.Get(id) as ISynchronizationDeadLetterQueueEntry) ?? throw new KeyNotFoundException(ErrorMessage_QueueEntryNotFound(id));

            queue?.Retry(item);
        }
    }
}
