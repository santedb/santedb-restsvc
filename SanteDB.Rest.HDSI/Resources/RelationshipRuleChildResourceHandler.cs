﻿using SanteDB.Core.BusinessRules;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.HDSI.Resources
{
    public class RelationshipRuleChildResourceHandler : IApiChildResourceHandler
    {
        readonly Tracer _Tracer;
        readonly IRelationshipValidationProvider _Provider;

        static readonly Type s_EntityRelationshipType = typeof(EntityRelationship);
        static readonly Type s_ActRelationshipType = typeof(ActRelationship);
        static readonly Type s_ActParticipationType = typeof(ActParticipation);


        public RelationshipRuleChildResourceHandler(IRelationshipValidationProvider provider)
        {
            _Tracer = new Tracer(nameof(RelationshipRuleChildResourceHandler));
            _Provider = provider;
        }

        public Type PropertyType => typeof(Core.BusinessRules.RelationshipValidationRule);

        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.Get | ResourceCapabilityType.Delete | ResourceCapabilityType.Search;

        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        public Type[] ParentTypes => new[] { s_EntityRelationshipType, s_ActRelationshipType, s_ActParticipationType };

        public string Name => "_relationshipRule";

        public object Add(Type scopingType, object scopingKey, object item)
        {
            if (item is RelationshipValidationRule rr)
            {
                IRelationshipValidationRule rule = null;

                if (scopingType == s_EntityRelationshipType)
                {
                    rule = _Provider.AddValidRelationship<EntityRelationship>(rr.SourceClassKey, rr.TargetClassKey, rr.RelationshipTypeKey, rr.Description);
                }
                else if (scopingType == s_ActParticipationType)
                {
                    rule = _Provider.AddValidRelationship<ActParticipation>(rr.SourceClassKey, rr.TargetClassKey, rr.RelationshipTypeKey, rr.Description);
                }
                else if (scopingType == s_ActRelationshipType)
                {
                    rule = _Provider.AddValidRelationship<ActRelationship>(rr.SourceClassKey, rr.TargetClassKey, rr.RelationshipTypeKey, rr.Description);
                }
                else
                {
                    throw new NotImplementedException($"Support for {scopingType.FullName} is not available.");
                }

                if (null != rule)
                {
                    rr.Key = rule.Key;
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

        public object Get(Type scopingType, object scopingKey, object key)
        {
            IRelationshipValidationRule relationship = null;

            if (scopingType == s_EntityRelationshipType)
            {
                relationship = _Provider.GetRuleByKey<EntityRelationship>((Guid)key);
            }
            else if (scopingType == s_ActParticipationType)
            {
                relationship = _Provider.GetRuleByKey<ActParticipation>((Guid)key);
            }
            else if (scopingType == s_ActRelationshipType)
            {
                relationship = _Provider.GetRuleByKey<ActRelationship>((Guid)key);
            }
            else
            {
                throw new NotImplementedException($"Support for {scopingType.FullName} is not available.");
            }

            if (null == relationship)
            {
                return null;
            }
            else
            {
                return new RelationshipValidationRule
                {
                    Key = relationship.Key,
                    SourceClassKey = relationship.SourceClassKey,
                    RelationshipTypeKey = relationship.RelationshipTypeKey,
                    TargetClassKey = relationship.TargetClassKey,
                    Description = relationship.Description
                };
            }
        }

        public IQueryResultSet Query(Type scopingType, object scopingKey, NameValueCollection filter)
        {
            IEnumerable<IRelationshipValidationRule> relationships = null;

            if (scopingType == s_EntityRelationshipType)
            {
                relationships = _Provider.GetValidRelationships<EntityRelationship>();
            }
            else if (scopingType == s_ActParticipationType)
            {
                relationships = _Provider.GetValidRelationships<ActParticipation>();
            }
            else if (scopingType == s_ActRelationshipType)
            {
                relationships = _Provider.GetValidRelationships<ActRelationship>();
            }
            else
            {
                throw new NotImplementedException($"Support for {scopingType.FullName} is not available.");
            }

            if (null == relationships)
            {
                return new MemoryQueryResultSet<RelationshipValidationRule>();
            }
            else
            {
                return new MemoryQueryResultSet<RelationshipValidationRule>(relationships.Select(rel => new RelationshipValidationRule
                {
                    Key = rel.Key,
                    SourceClassKey = rel.SourceClassKey,
                    RelationshipTypeKey = rel.RelationshipTypeKey,
                    TargetClassKey = rel.TargetClassKey,
                    Description = rel.Description
                }));
            }
        }

        public object Remove(Type scopingType, object scopingKey, object key)
        {
            throw new NotImplementedException();
        }
    }
}