/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
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
 * User: fyfej
 * Date: 2023-6-21
 */
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace SanteDB.Rest.HDSI.Resources
{
    /// <summary>
    /// Represents an <see cref="IApiChildResourceHandler"/> which gathers the validation criteria for a validation rule
    /// </summary>
    public class RelationshipRuleChildResourceHandler : IApiChildResourceHandler
    {
        readonly Tracer _Tracer;
        readonly IRelationshipValidationProvider _Provider;

        static readonly Type s_EntityRelationshipType = typeof(EntityRelationship);
        static readonly Type s_ActRelationshipType = typeof(ActRelationship);
        static readonly Type s_ActParticipationType = typeof(ActParticipation);


        /// <summary>
        /// DI constructor
        /// </summary>
        public RelationshipRuleChildResourceHandler(IRelationshipValidationProvider provider)
        {
            _Tracer = new Tracer(nameof(RelationshipRuleChildResourceHandler));
            _Provider = provider;
        }

        /// <inheritdoc/>
        public Type PropertyType => typeof(Core.Model.DataTypes.RelationshipValidationRule);

        /// <inheritdoc/>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.Get | ResourceCapabilityType.Delete | ResourceCapabilityType.Search;

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        /// <inheritdoc/>
        public Type[] ParentTypes => new[] { s_EntityRelationshipType, s_ActRelationshipType, s_ActParticipationType };

        /// <inheritdoc/>
        public string Name => "_relationshipRule";

        /// <inheritdoc/>
        public object Add(Type scopingType, object scopingKey, object item)
        {
            if (item is RelationshipValidationRule rr)
            {
                if (scopingType == s_EntityRelationshipType)
                {
                    rr = _Provider.AddValidRelationship<EntityRelationship>(rr.SourceClassKey, rr.TargetClassKey, rr.RelationshipTypeKey, rr.Description);
                }
                else if (scopingType == s_ActParticipationType)
                {
                    rr = _Provider.AddValidRelationship<ActParticipation>(rr.SourceClassKey, rr.TargetClassKey, rr.RelationshipTypeKey, rr.Description);
                }
                else if (scopingType == s_ActRelationshipType)
                {
                    rr = _Provider.AddValidRelationship<ActRelationship>(rr.SourceClassKey, rr.TargetClassKey, rr.RelationshipTypeKey, rr.Description);
                }
                else
                {
                    throw new NotImplementedException($"Support for {scopingType.FullName} is not available.");
                }

                if (null != rr)
                {
                    rr.Key = rr.Key;
                    return rr;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                throw new FormatException("Unsupported body type.");
            }
        }

        /// <inheritdoc/>
        public object Get(Type scopingType, object scopingKey, object key)
        {
            return _Provider.GetRuleByKey((Guid)key);
        }

        /// <inheritdoc/>
        public IQueryResultSet Query(Type scopingType, object scopingKey, NameValueCollection filter)
        {
            var query = QueryExpressionParser.BuildLinqExpression<RelationshipValidationRule>(filter);
            if (scopingType == s_EntityRelationshipType)
            {
                return _Provider.GetValidRelationships<EntityRelationship>().Where(query.Compile()).AsResultSet();
            }
            else if (scopingType == s_ActParticipationType)
            {
                return _Provider.GetValidRelationships<ActParticipation>().Where(query.Compile()).AsResultSet();
            }
            else if (scopingType == s_ActRelationshipType)
            {
                return _Provider.GetValidRelationships<ActRelationship>().Where(query.Compile()).AsResultSet();
            }
            else
            {
                throw new NotImplementedException($"Support for {scopingType.FullName} is not available.");
            }

        }

        /// <inheritdoc/>
        public object Remove(Type scopingType, object scopingKey, object key)
        {
            throw new NotImplementedException();
        }
    }
}
