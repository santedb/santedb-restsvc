using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Attributes;
using SanteDB.Rest.HDSI.Resources;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Protocol resource handler allows for the management of the metadata related to clinical protocols
    /// </summary>
    public class ProtocolResourceHandler : HdsiResourceHandlerBase<Protocol>
    {
        /// <summary>
        /// DI ctor
        /// </summary>
        public ProtocolResourceHandler(ILocalizationService localizationService, IRepositoryService<Protocol> repositoryService, IResourceCheckoutService checkoutService, ISubscriptionExecutor subscriptionExecutor, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, checkoutService, subscriptionExecutor, freetextSearchService)
        {
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.WriteClinicalData)]
        public override object Create(object data, bool updateIfExists)
        {
            return base.Create(data, updateIfExists);
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.DeleteClinicalData)]
        public override object Delete(object key)
        {
            return base.Delete(key);
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.WriteClinicalData)]
        public override object Update(object data)
        {
            return base.Update(data);
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override object Get(object id, object versionId)
        {
            return base.Get(id, versionId);
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            return base.Query(queryParameters);
        }

    }
}
