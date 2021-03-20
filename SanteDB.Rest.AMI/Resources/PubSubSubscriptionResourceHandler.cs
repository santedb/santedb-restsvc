using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Query;
using SanteDB.Core.PubSub;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Publish Subscribe Resource Handler
    /// </summary>
    public class PubSubSubscriptionResourceHandler : IApiResourceHandler
    {

        // The manager for the pub-sub service
        private IPubSubManagerService m_manager;

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(PubSubSubscriptionDefinition));

        /// <summary>
        /// Creates a new pub-sub manager resource hander
        /// </summary>
        public PubSubSubscriptionResourceHandler(IPubSubManagerService manager)
        {
            this.m_manager = manager;
        }

        /// <summary>
        /// Gets the name of the resource
        /// </summary>
        public string ResourceName => "PubSubSubscription";

        /// <summary>
        /// Gets the type this handles
        /// </summary>
        public Type Type => typeof(PubSubSubscriptionDefinition);

        /// <summary>
        /// Gets the scoped service
        /// </summary>
        public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Get the capabilities of this reousrce handler
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.CreateOrUpdate
            | ResourceCapabilityType.Delete | ResourceCapabilityType.Get | ResourceCapabilityType.Search | ResourceCapabilityType.Update;

        /// <summary>
        /// Creates a new object
        /// </summary>
        public object Create(object data, bool updateIfExists)
        {
            if (data is PubSubSubscriptionDefinition definition)
            {
                try
                {
                    // First , find the channel
                    var retVal = this.m_manager.RegisterSubscription(definition.ResourceType, definition.Name, definition.Description, definition.Event, definition.Filter.First(), definition.ChannelKey, definition.SupportContact, definition.NotBefore, definition.NotAfter);
                    if (definition.IsActive)
                        this.m_manager.ActivateSubscription(retVal.Key.Value, true);
                    return retVal;
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error creating subscription - {0}", e);
                    throw new Exception($"Error creating subscription", e);
                }
            }
            else
                throw new ArgumentException("Payload must be of type PubSubSubscriptionDefinition");
        }

        /// <summary>
        /// Gets the specified definition
        /// </summary>
        public object Get(object id, object versionId)
        {
            if (id is Guid uuid)
            {
                try
                {
                    return this.m_manager.GetSubscription(uuid);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error fetching subscription {0} - {1}", id, e);
                    throw new Exception($"Error fetching subscription {uuid}", e);
                }
            }
            else
                throw new ArgumentException($"ID must be a uuid");
        }
        
        /// <summary>
        /// Deletes the specified subscription
        /// </summary>
        public object Obsolete(object key)
        {
            if (key is Guid uuid)
            {
                try
                {
                    return this.m_manager.RemoveSubscription(uuid);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error obsoleting / deleting subscription {0} - {1}", uuid, e);
                    throw new Exception($"Error obsoleting subscription {uuid}", e);
                }
            }
            else
                throw new ArgumentOutOfRangeException($"ID must be a uuid");
        }

        /// <summary>
        /// Find all subscriptions
        /// </summary>
        public IEnumerable<object> Query(NameValueCollection queryParameters)
        {
            return this.Query(queryParameters, 0, 25, out int _);
        }

        /// <summary>
        /// Query for subscriptions
        /// </summary>
        public IEnumerable<object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            try
            {
                var filter = QueryExpressionParser.BuildLinqExpression<PubSubSubscriptionDefinition>(queryParameters);
                return this.m_manager.FindSubscription(filter, offset, count, out totalCount);
            }
            catch(Exception e)
            {
                this.m_tracer.TraceError("Error querying subscriptions - {0}", e);
                throw new Exception($"Could not execute subscription query", e);
            }
        }

        /// <summary>
        /// Update the specified object
        /// </summary>
        public object Update(object data)
        {
            if (data is PubSubSubscriptionDefinition definition)
            {
                try
                {
                    var retVal = this.m_manager.UpdateSubscription(definition.Key.Value, definition.Name, definition.Description, definition.Event, definition.Filter.First(), definition.SupportContact, definition.NotBefore, definition.NotAfter);
                    this.m_manager.ActivateSubscription(retVal.Key.Value, definition.IsActive);
                    return retVal;
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error updating subscription {0} - {1}", definition.Key, e);
                    throw new Exception($"Error updating subscription {definition.Key}", e);
                }
            }
            else
                throw new ArgumentException("Parameter must be of type PubSubSubscription");
        }
    }
}
