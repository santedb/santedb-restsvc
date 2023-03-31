using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// Get child concepts
    /// </summary>
    public class ConceptSetConceptChildHandler : IApiChildResourceHandler
    {
        // Concept repository service
        private readonly IConceptRepositoryService m_conceptRepository;

        /// <summary>
        /// DI constructor
        /// </summary>
        public ConceptSetConceptChildHandler(IConceptRepositoryService conceptRepository)
        {
            this.m_conceptRepository = conceptRepository;
        }

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(ConceptSet) };

        /// <inheritdoc/>
        public string Name => "Concept";

        /// <inheritdoc/>
        public Type PropertyType => typeof(Concept);

        /// <inheritdoc/>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Search;

        /// <inheritdoc/>
        public object Add(Type scopingType, object scopingKey, object item)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public object Get(Type scopingType, object scopingKey, object key)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public IQueryResultSet Query(Type scopingType, object scopingKey, NameValueCollection filter)
        {
            if (scopingKey is Guid uuid)
            {
                var retVal = this.m_conceptRepository.ExpandConceptSet(uuid);
                var query = QueryExpressionParser.BuildLinqExpression<Concept>(filter);
                return retVal.Where(query);
            }
            else {
                throw new ArgumentOutOfRangeException(String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(Guid), scopingKey.GetType()));
            }
        }

        /// <inheritdoc/>
        public object Remove(Type scopingType, object scopingKey, object key)
        {
            throw new NotImplementedException();
        }
    }
}
