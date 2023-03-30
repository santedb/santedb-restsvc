using SanteDB.Core.Interop;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// Expand a concept set into a dropdown
    /// </summary>
    public class ExpandConceptSetOperation : IApiChildOperation
    {
        // Concept repository service
        private readonly IConceptRepositoryService m_conceptRepository;

        // DI CTOR
        public ExpandConceptSetOperation(IConceptRepositoryService conceptRepository)
        {
            this.m_conceptRepository = conceptRepository;
        }

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance | ChildObjectScopeBinding.Class;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(ConceptSet) };

        /// <inheritdoc/>
        public string Name => "expand";

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {

            if(scopingKey is Guid uuid)
            {
                return new Bundle(this.m_conceptRepository.ExpandConceptSet(uuid));
            }
            else if(parameters.TryGet("mnemonic", out String mnemonic))
            {
                return new Bundle(this.m_conceptRepository.ExpandConceptSet(mnemonic));
            }
            else
            {
                throw new ArgumentNullException("mnemonic");
            }
        }
    }
}
