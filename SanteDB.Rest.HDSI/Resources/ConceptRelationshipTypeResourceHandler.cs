using SanteDB.Core.Interop;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Concept relationship type 
    /// </summary>
    public class ConceptRelationshipTypeResourceHandler : HdsiResourceHandlerBase<ConceptRelationshipType>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public ConceptRelationshipTypeResourceHandler(ILocalizationService localizationService, IRepositoryService<ConceptRelationshipType> repositoryService, IResourceCheckoutService resourceCheckoutService, ISubscriptionExecutor subscriptionExecutor = null, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, resourceCheckoutService, subscriptionExecutor, freetextSearchService)
        {
        }

        /// <summary>
        /// Get capabilities
        /// </summary>
        public override ResourceCapabilityType Capabilities => ResourceCapabilityType.Get | ResourceCapabilityType.Search;
    }
}
