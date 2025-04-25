/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Portions Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you
 * may not use this file except in compliance with the License. You may
 * obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 *
 */
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
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
                if (this.m_conceptRepositoryService.IsMember(ConceptSetKeys.EntityClass, (rule.SourceClassKey ?? rule.TargetClassKey).Value))
                {
                    rule = this.m_ruleProvider.AddValidRelationship<EntityRelationship>(rule.SourceClassKey, rule.TargetClassKey, rule.RelationshipTypeKey, rule.Description);
                }
                else if (this.m_conceptRepositoryService.IsMember(ConceptSetKeys.ActClass, (rule.SourceClassKey ?? rule.TargetClassKey).Value))
                {
                    rule = this.m_ruleProvider.AddValidRelationship<ActRelationship>(rule.SourceClassKey, rule.TargetClassKey, rule.RelationshipTypeKey, rule.Description);
                }
                else if (this.m_conceptRepositoryService.IsMember(ConceptSetKeys.ActParticipationType, (rule.SourceClassKey ?? rule.TargetClassKey).Value))
                {
                    rule = this.m_ruleProvider.AddValidRelationship<ActParticipation>(rule.SourceClassKey, rule.TargetClassKey, rule.RelationshipTypeKey, rule.Description);
                }
                else
                {
                    throw new ArgumentOutOfRangeException(ErrorMessages.INVALID_CLASS_CODE);
                }
                return rule;
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
                return this.m_ruleProvider.RemoveRuleByKey(uuid);
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
                return this.m_ruleProvider.GetRuleByKey(uuid);
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
            return this.m_ruleProvider.QueryRelationships(filter);

        }

        /// <inheritdoc/>
        public object Update(object data)
        {
            throw new NotSupportedException();
        }
    }
}
