using RestSrvr;
using RestSrvr.Message;
using SanteDB.Core;
using SanteDB.Core.Security.Services;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SanteDB.Rest.Common.Behavior
{
    /// <summary>
    /// A behavior that controls and enforces the <see cref="DemandAttribute"/>
    /// </summary>
    public class SecurityPolicyEnforcementBehavior : IServiceBehavior, IEndpointBehavior, IOperationBehavior, IOperationPolicy
    {

        // Behavior type
        private Type m_behaviorType = null;
        private readonly IPolicyEnforcementService m_pepService;

        /// <summary>
        /// Creates a new demand policy
        /// </summary>
        public SecurityPolicyEnforcementBehavior()
        {
            this.m_pepService = ApplicationServiceContext.Current.GetService<IPolicyEnforcementService>();
        }

        /// <summary>
        /// Apply the actual policy
        /// </summary>
        public void Apply(EndpointOperation operation, RestRequestMessage request)
        {
            var methInfo = this.m_behaviorType.GetMethod(operation.Description.InvokeMethod.Name, operation.Description.InvokeMethod.GetParameters().Select(p => p.ParameterType).ToArray());
            foreach (var ppe in methInfo.GetCustomAttributes<DemandAttribute>())
                this.m_pepService.Demand(ppe.PolicyId);
        }

        /// <summary>
        /// Apply the endpoint behavior
        /// </summary>
        public void ApplyEndpointBehavior(ServiceEndpoint endpoint, EndpointDispatcher dispatcher)
        {
            foreach (var op in endpoint.Description.Contract.Operations)
                op.AddOperationBehavior(this);
        }

        /// <summary>
        /// Apply the operation policy behavior
        /// </summary>
        public void ApplyOperationBehavior(EndpointOperation operation, OperationDispatcher dispatcher)
        {
            dispatcher.AddOperationPolicy(this);
        }

        /// <summary>
        /// Apply the service behavior
        /// </summary>
        public void ApplyServiceBehavior(RestService service, ServiceDispatcher dispatcher)
        {
            this.m_behaviorType = service.BehaviorType;
            foreach (var itm in service.Endpoints)
                itm.AddEndpointBehavior(this);
        }
    }
}
