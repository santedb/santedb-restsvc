using SanteDB.Core.BusinessRules;
using SanteDB.Core.Data.Quality;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Represents a resource handler that interacts with containers
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Find a manner to test REST classes
    public class ContainerResourceHandler : EntityResourceHandlerBase<Container>
    {

        /// <summary>
        /// DI constructor
        /// </summary>
        public ContainerResourceHandler(ILocalizationService localizationService, IRepositoryService<Container> repositoryService, IResourceCheckoutService resourceCheckoutService, ISubscriptionExecutor subscriptionExecutor = null, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, resourceCheckoutService, subscriptionExecutor, freetextSearchService)
        {
        }

        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public override object Create(object data, bool updateIfExists)
        {
            return base.Create(data, updateIfExists);
        }

        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public override object Update(object data)
        {
            return base.Update(data);
        }

        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public override object Delete(object key)
        {
            return base.Delete(key);
        }

        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public override IQueryResultSet Query(NameValueCollection queryParameters)
        {
            return base.Query(queryParameters);
        }

        [Demand(PermissionPolicyIdentifiers.LoginAsService)]
        public override object Get(object id, object versionId)
        {
            return base.Get(id, versionId);
        }
    }
}
