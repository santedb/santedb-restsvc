using SanteDB.Core.Interop;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.PubSub;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.AMI.Operation
{
    /// <summary>
    /// Pub-sub subscription activation
    /// </summary>
    public class PubSubSubscriptionActivate : IApiChildOperation
    {
        // Manager
        private readonly IPubSubManagerService m_manager;

        /// <summary>
        /// DI constructor
        /// </summary>
        public PubSubSubscriptionActivate(IPubSubManagerService managerService)
        {
            this.m_manager = managerService;
        }

        /// <summary>
        /// Gets the scope binding for the object
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <summary>
        /// Gets the types this applies to
        /// </summary>
        public Type[] ParentTypes => new Type[] { typeof(PubSubSubscriptionDefinition) };

        /// <summary>
        /// Gets the name
        /// </summary>
        public string Name => "activate";

        /// <summary>
        /// Invoke the operation
        /// </summary>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if (parameters.TryGet("status", out bool value))
            {
                return this.m_manager.ActivateSubscription((Guid)scopingKey, value);
            }
            else
            {
                throw new ArgumentNullException("Required parameter 'status' missing");
            }
        }
    }
}