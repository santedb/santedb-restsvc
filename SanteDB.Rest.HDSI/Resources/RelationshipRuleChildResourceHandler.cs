using SanteDB.Core.BusinessRules;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
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


        public RelationshipRuleChildResourceHandler(IRelationshipValidationProvider provider)
        {
            _Tracer = new Tracer(nameof(RelationshipRuleChildResourceHandler));
            _Provider = provider;
        }

        public Type PropertyType => typeof(Core.BusinessRules.RelationshipValidationRule);

        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Create | ResourceCapabilityType.Get | ResourceCapabilityType.Delete | ResourceCapabilityType.Search;

        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        public Type[] ParentTypes => new[] { s_EntityRelationshipType };

        public string Name => "_relationshipRule";

        public object Add(Type scopingType, object scopingKey, object item)
        {
            throw new NotImplementedException();
        }

        public object Get(Type scopingType, object scopingKey, object key)
        {
            throw new NotImplementedException();
        }

        public IQueryResultSet Query(Type scopingType, object scopingKey, NameValueCollection filter)
        {
            if (scopingType == s_EntityRelationshipType)
            {
                var relationship = _Provider.GetValidRelationships<EntityRelationship>().Select(rel => new RelationshipValidationRule
                {
                    Key = rel.Key,
                    SourceClassKey = rel.SourceClassKey,
                    RelationshipTypeKey = rel.RelationshipTypeKey,
                    TargetClassKey = rel.TargetClassKey,
                    Description = rel.Description
                });

                return new MemoryQueryResultSet<IRelationshipValidationRule>(relationship);
            }
            else
            {
                throw new NotImplementedException($"Support for {scopingType.FullName} is not available.");
            }
        }

        public object Remove(Type scopingType, object scopingKey, object key)
        {
            throw new NotImplementedException();
        }
    }
}
