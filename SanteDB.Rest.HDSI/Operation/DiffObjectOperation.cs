using SanteDB.Core;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// Perform a difference operation
    /// </summary>
    public class DiffObjectOperation : IApiChildOperation
    {

        // The patch service
        private IPatchService m_patchService;

        /// <summary>
        /// Create new DI object operation
        /// </summary>
        public DiffObjectOperation(IPatchService patchService)
        {
            this.m_patchService = patchService;
        }

        /// <summary>
        /// Scope binding
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <summary>
        /// Get parent typers
        /// </summary>
        public Type[] ParentTypes => AppDomain.CurrentDomain.GetAllTypes().Where(t => typeof(IdentifiedData).IsAssignableFrom(t) && !t.IsAbstract).ToArray();

        /// <summary>
        /// Get the name
        /// </summary>
        public string Name => "diff";

        /// <summary>
        /// Invoke the operation
        /// </summary>
        public object Invoke(Type scopingType, object scopingKey, ApiOperationParameterCollection parameters)
        {
            
            if(parameters.TryGet<String>("other", out string bKeyString) &&
                Guid.TryParse(scopingKey.ToString(), out Guid aKey) && Guid.TryParse(bKeyString, out Guid bKey))
            {
                var repository = ApplicationServiceContext.Current.GetService(typeof(IRepositoryService<>).MakeGenericType(scopingType)) as IRepositoryService;
                if(repository == null)
                {
                    throw new InvalidOperationException($"Cannot load repository for {scopingType.Name}");
                }
                IdentifiedData objectA = repository.Get(aKey), objectB = repository.Get(bKey);
                return this.m_patchService.Diff(objectA, objectB);
            }
            else
            {
                throw new ArgumentException("Both A and B parameters must be specified");
            }

        }
    }
}
