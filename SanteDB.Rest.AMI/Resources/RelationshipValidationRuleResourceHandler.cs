using SanteDB.Core.BusinessRules;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Specialized;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Resource handler for validation rules
    /// </summary>
    public class RelationshipValidationRuleResourceHandler : IApiResourceHandler
    {
        private readonly IRelationshipValidationProvider m_ruleProvider;
        private readonly IConceptRepositoryService m_conceptRepositoryService;

        /// <summary>
        /// DI constructor
        /// </summary>
        public RelationshipValidationRuleResourceHandler(IRelationshipValidationProvider provider, IConceptRepositoryService conceptRepository)
        {
            this.m_ruleProvider = provider;
            this.m_conceptRepositoryService = conceptRepository;
        }

        /// <inheritdoc/>
        public string ResourceName => "RelationshipValidationRule";

        /// <inheritdoc/>
        public Type Type => typeof(RelationshipValidationRule);

        /// <inheritdoc/>
        public Type Scope => typeof(IAmiServiceContract);

        /// <inheritdoc/>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.Get | ResourceCapabilityType.Delete | ResourceCapabilityType.Search;

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.AlterSystemConfiguration)]
        public object Create(object data, bool updateIfExists)
        {

            if (data is RelationshipValidationRule rule)
            {
                IRelationshipValidationRule createdRule = null;
                if (this.m_conceptRepositoryService.IsMember(ConceptSetKeys.EntityClass, (rule.SourceClassKey ?? rule.TargetClassKey).Value))
                {
                    createdRule = this.m_ruleProvider.AddValidRelationship<EntityRelationship>(rule.SourceClassKey, rule.TargetClassKey, rule.RelationshipTypeKey, rule.Description);
                }
                else if (this.m_conceptRepositoryService.IsMember(ConceptSetKeys.ActClass, (rule.SourceClassKey ?? rule.TargetClassKey).Value))
                {
                    createdRule = this.m_ruleProvider.AddValidRelationship<ActRelationship>(rule.SourceClassKey, rule.TargetClassKey, rule.RelationshipTypeKey, rule.Description);
                }
                else if (this.m_conceptRepositoryService.IsMember(ConceptSetKeys.ActParticipationType, (rule.SourceClassKey ?? rule.TargetClassKey).Value))
                {
                    createdRule = this.m_ruleProvider.AddValidRelationship<ActParticipation>(rule.SourceClassKey, rule.TargetClassKey, rule.RelationshipTypeKey, rule.Description);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(ErrorMessages.INVALID_CLASS_CODE);
                }
                return new RelationshipValidationRule(createdRule);
            }

            else
            {
                throw new ArgumentOutOfRangeException(nameof(data), String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(RelationshipValidationRule), data.GetType()));
            }
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.AlterSystemConfiguration)]
        public object Delete(object key)
        {
            if (key is Guid uuid)
            {
                return new RelationshipValidationRule(this.m_ruleProvider.RemoveRuleByKey(uuid));
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(key));
            }
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public object Get(object id, object versionId)
        {
            if (id is Guid uuid)
            {
                return new RelationshipValidationRule(this.m_ruleProvider.GetRuleByKey(uuid));
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata)]
        public IQueryResultSet Query(NameValueCollection queryParameters)
        {
            var filter = QueryExpressionParser.BuildLinqExpression<RelationshipValidationRule>(queryParameters);
            var newFilter = new ExpressionParameterRewriter<RelationshipValidationRule, IRelationshipValidationRule, bool>(filter).Convert();
            return new TransformQueryResultSet<IRelationshipValidationRule, RelationshipValidationRule>(this.m_ruleProvider.QueryRelationships(newFilter), (o) => new RelationshipValidationRule(o));

        }

        /// <inheritdoc/>
        public object Update(object data)
        {
            throw new NotSupportedException();
        }
    }
}
