using SanteDB.Core.BusinessRules;
using SanteDB.Core.Data.Quality;
using SanteDB.Core.Exceptions;
using SanteDB.Core.i18n;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using SanteDB.Core.Security.Services;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
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
        private readonly IRepositoryService<EntityRelationship> m_entityRelationshipRepository;
        private readonly ISecurityRepositoryService m_securityRepository;
        private readonly IPolicyEnforcementService m_pepService;

        /// <summary>
        /// DI constructor
        /// </summary>
        public ContainerResourceHandler(ILocalizationService localizationService, IRepositoryService<Container> repositoryService, IRepositoryService<EntityRelationship> entityRelationshipRepository, IPolicyEnforcementService pepService, ISecurityRepositoryService securityRepositoryService, IResourceCheckoutService resourceCheckoutService, ISubscriptionExecutor subscriptionExecutor = null, IFreetextSearchService freetextSearchService = null) : base(localizationService, repositoryService, resourceCheckoutService, subscriptionExecutor, freetextSearchService)
        {
            this.m_entityRelationshipRepository = entityRelationshipRepository;
            this.m_securityRepository = securityRepositoryService;
            this.m_pepService = pepService;
        }

        /// <summary>
        /// Demand the altering of a container - either a manager of the facility to which the container belongs, or AlterPlacesAndOrgs
        /// </summary>
        private void DemandAlterContainer(Container cont)
        {

            var containedFacilityRel = cont.Relationships?.FirstOrDefault(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.LocatedEntity) ??
                this.m_entityRelationshipRepository.Find(o => o.TargetEntityKey == cont.Key && o.RelationshipTypeKey == EntityRelationshipTypeKeys.LocatedEntity).FirstOrDefault();

            if(containedFacilityRel == null )
            {
                throw new DetectedIssueException(Core.BusinessRules.DetectedIssuePriorityType.Error, "santedb.container.locatedEntity", String.Format(ErrorMessages.DEPENDENT_PROPERTY_NULL, nameof(EntityRelationshipTypeKeys.LocatedEntity)), DetectedIssueKeys.InvalidDataIssue, null);
            }

            // TODO: Allow for the classification of a facility administrative user
            var thisUser = this.m_securityRepository.GetCdrEntity(AuthenticationContext.Current.Principal);
            if (thisUser?.LoadProperty(o => o.Relationships).Any(r => r.RelationshipTypeKey == EntityRelationshipTypeKeys.MaintainedEntity && r.TargetEntityKey == containedFacilityRel.SourceEntityKey) != true)
            {
                this.m_pepService.Demand(PermissionPolicyIdentifiers.WritePlacesAndOrgs);
            }


        }

        /// <inheritdoc/>
        public override object Create(object data, bool updateIfExists)
        {
            if (data is Container cont)
            {
                this.DemandAlterContainer(cont);
                return base.Create(cont, updateIfExists);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(data), String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(Container), data.GetType()));
            }
        }

        /// <inheritdoc/>
        public override object Update(object data)
        {
            if (data is Container cont)
            {
                this.DemandAlterContainer(cont);
                return base.Update(cont);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(data), String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(Container), data.GetType()));
            }
        }

        /// <inheritdoc/>
        public override object Delete(object key)
        {
            if (key is Guid uuid)
            {
                var cont = base.Get(key, null) as Container;
                this.DemandAlterContainer(cont);
                return base.Delete(uuid);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(key), String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(Guid), key.GetType()));
            }
        }
    }
}
