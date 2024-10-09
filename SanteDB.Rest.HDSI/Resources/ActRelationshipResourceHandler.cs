using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Implementation of a resource handler for the <see cref="ActRelationship"/>
    /// </summary>
    public class ActRelationshipResourceHandler : HdsiResourceHandlerBase<ActRelationship>
    {
        /// <inheritdoc/>
        public ActRelationshipResourceHandler(ILocalizationService localizationService, IRepositoryService<ActRelationship> repositoryService, IResourceCheckoutService resourceCheckoutService, ISubscriptionExecutor subscriptionExecutor, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, resourceCheckoutService, subscriptionExecutor, freetextSearchService)
        {
        }

        /// <inheritdoc/>
        public override string ResourceName => typeof(ActRelationship).GetSerializationName();

    }
}
