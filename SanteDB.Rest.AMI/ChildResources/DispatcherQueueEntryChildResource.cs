using RestSrvr;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Audit;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Queue;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AMI.ChildResources
{
    /// <summary>
    /// Dispatcher queue entry child resource
    /// </summary>
    public class DispatcherQueueEntryChildResource : IApiChildResourceHandler
    {
        // Queue service
        private readonly IDispatcherQueueManagerService m_queueService;

        // Localization service
        private readonly ILocalizationService m_localizationService;

        // PEP service
        private readonly IPolicyEnforcementService m_policyEnforcementService;

        /// <summary>
        /// DI constructor for persistent queue
        /// </summary>
        public DispatcherQueueEntryChildResource(IDispatcherQueueManagerService queueService, ILocalizationService localization, IPolicyEnforcementService pepService)
        {
            this.m_queueService = queueService;
            this.m_localizationService = localization;
            this.m_policyEnforcementService = pepService;
        }

        /// <summary>
        /// Gets the type of the property
        /// </summary>
        public Type PropertyType => typeof(DispatcherQueueEntry);

        /// <summary>
        /// Get the capabilities of this service
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Get | ResourceCapabilityType.Delete | ResourceCapabilityType.Update | ResourceCapabilityType.Create | ResourceCapabilityType.Search;

        /// <summary>
        /// Gets the scope binding
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <summary>
        /// Get the parent types
        /// </summary>
        public Type[] ParentTypes => new Type[] { typeof(DispatcherQueueInfo) };

        /// <summary>
        /// Get the name of this sub-resource
        /// </summary>
        public string Name => "entry";

        /// <summary>
        /// Adds an entry - or rather - moves it
        /// </summary>
        public object Add(Type scopingType, object scopingKey, object item)
        {
            if (item is DispatcherQueueEntry dqe)
            {
                var retVal = this.m_queueService.Move(dqe, (string)scopingKey);
                AuditUtil.SendAudit(new AuditEventData()
               .WithLocalDevice()
               .WithUser()
               .WithAction(ActionType.Update)
               .WithEventIdentifier(EventIdentifierType.Import)
               .WithOutcome(OutcomeIndicator.Success)
               .WithTimestamp(DateTime.Now)
               .WithEventType("MoveQueueObject")
               .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
               .WithSystemObjects(AuditableObjectRole.Resource, AuditableObjectLifecycle.Archiving, new Uri($"{dqe.SourceQueue}/entry/{dqe.CorrelationId}"))
               .WithSystemObjects(AuditableObjectRole.Resource, AuditableObjectLifecycle.Creation, new Uri($"{scopingKey}/entry/{retVal.CorrelationId}"))
               );
                return retVal;
            }
            else
            {
                throw new ArgumentException(nameof(item), "Expected DispatcherQueueEntry body");
            }
        }

        /// <summary>
        /// Get the specified queue object
        /// </summary>
        public object Get(Type scopingType, object scopingKey, object key)
        {
            return this.m_queueService.GetQueueEntry((string)scopingKey, (string)key);
        }

        /// <summary>
        /// Query for all entries on the specified queue
        /// </summary>
        public IQueryResultSet Query(Type scopingType, object scopingKey, NameValueCollection filter)
        {
            var entries = this.m_queueService.GetQueueEntries((string)scopingKey);
            if (filter.TryGetValue("name", out var values))
            {
                entries = entries.Where(o => o.CorrelationId.Contains(values.First().Replace("*", "")));
            }

            AuditUtil.SendAudit(new AuditEventData()
               .WithLocalDevice()
               .WithUser()
               .WithAction(ActionType.Execute)
               .WithEventIdentifier(EventIdentifierType.Query)
               .WithOutcome(OutcomeIndicator.Success)
               .WithTimestamp(DateTime.Now)
               .WithEventType("QueryQueueObject")
               .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
               .WithSystemObjects(AuditableObjectRole.Resource, AuditableObjectLifecycle.PermanentErasure, entries.Select(o => new Uri($"{scopingKey}/entry/{o.CorrelationId}")).ToArray()));

            return new MemoryQueryResultSet(entries);
        }

        /// <summary>
        /// Remove the specified object
        /// </summary>
        public object Remove(Type scopingType, object scopingKey, object key)
        {
            var data = this.m_queueService.DequeueById((String)scopingKey, (string)key);
            AuditUtil.SendAudit(new AuditEventData()
                .WithLocalDevice()
                .WithUser()
                .WithAction(ActionType.Delete)
                .WithEventIdentifier(EventIdentifierType.ApplicationActivity)
                .WithOutcome(OutcomeIndicator.Success)
                .WithTimestamp(DateTime.Now)
                .WithEventType("PurgeQueueObject")
                .WithHttpInformation(RestOperationContext.Current.IncomingRequest)
                .WithSystemObjects(AuditableObjectRole.Resource, AuditableObjectLifecycle.PermanentErasure, new Uri($"urn:queue:{scopingKey}/event/{key}")));
            return null;
        }
    }
}