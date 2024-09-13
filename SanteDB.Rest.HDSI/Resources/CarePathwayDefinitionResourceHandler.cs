using SanteDB.Core.Interop;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Care pathway definition resource handler
    /// </summary>
    public class CarePathwayDefinitionResourceHandler : HdsiResourceHandlerBase<CarePathwayDefinition>
    {
        /// <summary>
        /// DI constructor
        /// </summary>
        public CarePathwayDefinitionResourceHandler(ILocalizationService localizationService, IRepositoryService<CarePathwayDefinition> repositoryService, IResourceCheckoutService resourceCheckoutService, ISubscriptionExecutor subscriptionExecutor, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, resourceCheckoutService, subscriptionExecutor, freetextSearchService)
        {
        }

        /// <inheritdoc/>
        public override ResourceCapabilityType Capabilities => ResourceCapabilityType.Get | ResourceCapabilityType.Search | ResourceCapabilityType.Create | ResourceCapabilityType.CreateOrUpdate | ResourceCapabilityType.Update | ResourceCapabilityType.Delete;


        /// <inheritdoc />
        [Demand(PermissionPolicyIdentifiers.UnrestrictedMetadata)]
        public override object Create(object data, bool updateIfExists)
        {
            return base.Create(data, updateIfExists);
        }

        /// <inheritdoc />
        [Demand(PermissionPolicyIdentifiers.UnrestrictedMetadata)]
        public override object Delete(object key)
        {
            return base.Delete(key);
        }

        /// <inheritdoc />
        [Demand(PermissionPolicyIdentifiers.UnrestrictedMetadata)]
        public override object Update(object data)
        {
            return base.Update(data);
        }

        /// <inheritdoc />
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override object Get(object id, object versionId)
        {
            return base.Get(id, versionId);
        }

        /// <inheritdoc />
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            return base.Query(queryParameters);
        }
    }
}
