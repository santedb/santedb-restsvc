using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.HDSI.ChildResources
{
    /// <summary>
    /// A child resource which loads all relationships for the child
    /// </summary>
    public class ConceptRelationshipChildResource : IApiChildResourceHandler
    {
        private readonly IRepositoryService<ConceptRelationship> m_relationshipRepository;

        /// <summary>
        /// DI ctor
        /// </summary>
        public ConceptRelationshipChildResource(IRepositoryService<ConceptRelationship> relationshipRepositoryService)
        {
            this.m_relationshipRepository = relationshipRepositoryService;
        }

        /// <inheritdoc/>
        public string Name => "relationship";

        /// <inheritdoc/>
        public Type PropertyType => typeof(ConceptRelationship);

        /// <inheritdoc/>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Search | ResourceCapabilityType.Get | ResourceCapabilityType.Create | ResourceCapabilityType.CreateOrUpdate | ResourceCapabilityType.Update | ResourceCapabilityType.Delete;

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(Concept) };

        /// <inheritdoc/>
        public object Add(Type scopingType, object scopingKey, object item)
        {
            if(scopingType != typeof(Concept))
            {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_OUT_OF_RANGE, typeof(Concept), scopingType));
            }
            else if(!(scopingKey is Guid uuidScope))
            {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_INVALID_TYPE, typeof(Guid), scopingKey?.GetType()));
            }
            else if(!(item is ConceptRelationship relationship))
            {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_INVALID_TYPE, typeof(ConceptRelationship), item?.GetType()));
            }
            else if(relationship.SourceEntityKey != uuidScope && relationship.TargetConceptKey != uuidScope)
            {
                throw new InvalidOperationException(String.Format(ErrorMessages.WOULD_RESULT_INVALID_STATE, $"source or targetConcept must be {scopingKey}"));
            }
            else
            {
                return this.m_relationshipRepository.Save(relationship);
            }
        }

        /// <inheritdoc/>
        public object Get(Type scopingType, object scopingKey, object key)
        {
            if (scopingType != typeof(Concept))
            {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_OUT_OF_RANGE, typeof(Concept), scopingType));
            }
            else if (!(scopingKey is Guid uuidScope))
            {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_INVALID_TYPE, typeof(Guid), scopingKey?.GetType()));
            }
            else if (!(key is Guid uuidKey))
            {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_INVALID_TYPE, typeof(Guid), key?.GetType()));
            }
            else
            {
                return this.m_relationshipRepository.Find(o => (o.SourceEntityKey.Value == uuidScope || o.TargetConceptKey.Value == uuidScope) && o.Key == uuidKey).FirstOrDefault();
            }
        }

        /// <inheritdoc/>
        public IQueryResultSet Query(Type scopingType, object scopingKey, NameValueCollection filter)
        {
            if (scopingType != typeof(Concept))
            {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_OUT_OF_RANGE, typeof(Concept), scopingType));
            }
            else if (!(scopingKey is Guid uuidScope))
            {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_INVALID_TYPE, typeof(Guid), scopingKey?.GetType()));
            }
            else
            {
                var expr = QueryExpressionParser.BuildLinqExpression<ConceptRelationship>(filter).Compile();
                return new MemoryQueryResultSet(this.m_relationshipRepository.Find(o=>o.SourceEntityKey == uuidScope || o.TargetConceptKey == uuidScope).OfType<ConceptRelationship>().Where(expr));
            }
        }

        /// <inheritdoc/>
        public object Remove(Type scopingType, object scopingKey, object key)
        {
            if (scopingType != typeof(Concept))
            {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_OUT_OF_RANGE, typeof(Concept), scopingType));
            }
            else if (!(scopingKey is Guid uuidScope))
            {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_INVALID_TYPE, typeof(Guid), scopingKey?.GetType()));
            }
            else if (!(key is Guid uuidKey))
            {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_INVALID_TYPE, typeof(Guid), key?.GetType()));
            }
            else
            {
                var candidateKey = this.m_relationshipRepository.Find(o => (o.SourceEntityKey.Value == uuidScope || o.TargetConceptKey.Value == uuidScope) && o.Key == uuidKey).FirstOrDefault();
                if(candidateKey == null)
                {
                    throw new KeyNotFoundException(key.ToString());
                }
                return this.m_relationshipRepository.Delete(candidateKey.Key.Value);

            }
        }
    }
}
