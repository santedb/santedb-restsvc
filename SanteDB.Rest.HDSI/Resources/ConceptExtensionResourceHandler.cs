using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Concept Extension resource handler
    /// </summary>
    public class ConceptExtensionResourceHandler : HdsiResourceHandlerBase<ConceptExtension>
    {
        public ConceptExtensionResourceHandler(ILocalizationService localizationService, IRepositoryService<ConceptExtension> repositoryService, IResourceCheckoutService resourceCheckoutService, ISubscriptionExecutor subscriptionExecutor, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, resourceCheckoutService, subscriptionExecutor, freetextSearchService)
        {
        }
    }
}
