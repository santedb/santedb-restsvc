using SanteDB.Core.Diagnostics;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Query;
using SanteDB.Core.PubSub;
using SanteDB.Core.Security;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace SanteDB.Rest.AMI.ChildResources
{
    /// <summary>
    /// Pub/sub child resource for reading log entries for 
    /// </summary>
    public class PubSubLogEntryChildHandler : IApiChildResourceHandler
    {
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(PubSubLogEntryChildHandler));
        private readonly IPubSubManagerService m_pubSubManager;
        private readonly IPubSubLogService m_pubSubLog;

        /// <summary>
        /// DI constructor
        /// </summary>
        public PubSubLogEntryChildHandler(IPubSubManagerService pubSubManagerService, IPubSubLogService pubSubLogManager)
        {
            this.m_pubSubManager = pubSubManagerService;
            this.m_pubSubLog = pubSubLogManager;
        }

        /// <inheritdoc/>
        public string Name => "log";

        /// <inheritdoc/>
        public Type PropertyType => typeof(PubSubDispatchLog);

        /// <inheritdoc/>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Get | ResourceCapabilityType.Search;

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(PubSubSubscriptionDefinition) };

        /// <inheritdoc/>
        public object Add(Type scopingType, object scopingKey, object item)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadPubSubSubscription)]
        public object Get(Type scopingType, object scopingKey, object key)
        {
            if(scopingKey is Guid uuid && key is Guid uuidLog)
            {
                var subscription = this.m_pubSubManager.GetSubscription(uuid);
                if(subscription == null)
                {
                    throw new KeyNotFoundException(String.Format(ErrorMessages.OBJECT_NOT_FOUND, uuid));
                }
                var logEntry = this.m_pubSubLog.GetDispatches(subscription.Name).Where(o => o.Key == uuidLog).FirstOrDefault();
                if(logEntry == null)
                {
                    throw new KeyNotFoundException(String.Format(ErrorMessages.OBJECT_NOT_FOUND, $"{uuid}/{this.Name}/{uuidLog}"));
                }
                return logEntry;
            }
            else
            {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(Guid), scopingKey.GetType()));
            }
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadPubSubSubscription)]
        public IQueryResultSet Query(Type scopingType, object scopingKey, NameValueCollection filter)
        {
            if (scopingKey is Guid uuid)
            {
                var subscription = this.m_pubSubManager.GetSubscription(uuid);
                if (subscription == null)
                {
                    throw new KeyNotFoundException(String.Format(ErrorMessages.OBJECT_NOT_FOUND, uuid));
                }

                var filterExpression = QueryExpressionParser.BuildLinqExpression<PubSubDispatchLog>(filter);
                return this.m_pubSubLog.GetDispatches(subscription.Name).Where(filterExpression);
            }
            else
            {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(Guid), scopingKey.GetType()));
            }
        }

        /// <inheritdoc/>
        public object Remove(Type scopingType, object scopingKey, object key)
        {
            throw new NotSupportedException();
        }
    }
}
