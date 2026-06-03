using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Narratives are structured documents in SanteDB
    /// </summary>
    public class NarrativeResourceHandler : HdsiResourceHandlerBase<Narrative>
    {
        /// <summary>
        /// DI Constructor
        /// </summary>
        public NarrativeResourceHandler(ILocalizationService localizationService, IRepositoryService<Narrative> repositoryService, IResourceCheckoutService resourceCheckoutService, ISubscriptionExecutor subscriptionExecutor, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, resourceCheckoutService, subscriptionExecutor, freetextSearchService)
        {
        }


        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.WriteClinicalData)]
        public override object Create(object data, bool updateIfExists)
        {
            return base.Create(data, updateIfExists);
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.WriteClinicalData)]
        public override object Update(object data)
        {
            return base.Update(data);
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.DeleteClinicalData)]
        public override object Delete(object key)
        {
            return base.Delete(key);
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadClinicalData)]
        public override object Get(object id, object versionId)
        {
            return base.Get(id, versionId);
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.QueryClinicalData)]
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            return base.Query(queryParameters);
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.QueryClinicalData)]
        public override IQueryResultSet QueryChildObjects(object scopingEntityKey, string propertyName, NameValueCollection filter)
        {
            return base.QueryChildObjects(scopingEntityKey, propertyName, filter);
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.WriteClinicalData)]
        public override object AddChildObject(object scopingEntityKey, string propertyName, object scopedItem)
        {
            return base.AddChildObject(scopingEntityKey, propertyName, scopedItem);
        }


        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.DeleteClinicalData)]
        public override object RemoveChildObject(object scopingEntityKey, string propertyName, object subItemKey)
        {
            return base.RemoveChildObject(scopingEntityKey, propertyName, subItemKey);
        }


    }
}
