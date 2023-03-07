using SanteDB.Core.Interop;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// $tag operation
    /// </summary>
    public class TagOperation : IApiChildOperation
    {
        private readonly ITagPersistenceService m_tagPersistenceService;

        /// <summary>
        /// DI constructor
        /// </summary>
        public TagOperation(ITagPersistenceService tagPersistenceService)
        {
            this.m_tagPersistenceService = tagPersistenceService;
        }

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes => Type.EmptyTypes;

        /// <inheritdoc/>
        public string Name => "tag";

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if(scopingKey is Guid scopeGuid || Guid.TryParse(scopingKey.ToString(), out scopeGuid))
            {
                foreach (var p in parameters.Parameters) {
                    this.m_tagPersistenceService.Save(scopeGuid, p.Name, p.Value?.ToString());
                }
                return null;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(scopingKey));
            }
        }
    }
}
