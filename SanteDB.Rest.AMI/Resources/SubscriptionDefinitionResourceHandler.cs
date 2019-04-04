using SanteDB.Core.Interop;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Subscription;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents the subscription definition resource handler
    /// </summary>
    public class SubscriptionDefinitionResourceHandler : ResourceHandlerBase<SubscriptionDefinition>
    {
        /// <summary>
        /// Gets the capabilities
        /// </summary>
        public override ResourceCapabilityType Capabilities => ResourceCapabilityType.Search | ResourceCapabilityType.Get ;

    }
}
