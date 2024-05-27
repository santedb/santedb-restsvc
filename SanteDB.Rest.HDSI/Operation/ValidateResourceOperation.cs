using SanteDB.Core.Data.Quality;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// Represents an operation where a resource can be validated
    /// </summary>
    public class ValidateResourceOperation : IApiChildOperation
    {

        /// <summary>
        /// Validate resource
        /// </summary>
        public ValidateResourceOperation(IServiceProvider serviceProvider)
        {
            this.ParentTypes = typeof(IdentifiedData).Assembly.GetTypes().Where(t => t.GetSerializationName() != null).ToArray();
            this.m_serviceProvider = serviceProvider;
        }

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class | ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes { get; }

        private readonly IServiceProvider m_serviceProvider;

        /// <inheritdoc/>
        public string Name => "validate";

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if(scopingKey == null)
            {
                if(parameters.TryGet<IdentifiedData>("target", out var value))
                {
                    return value.Validate().ToArray();
                }
                else
                {
                    throw new ArgumentNullException("target", String.Format(ErrorMessages.MISSING_VALUE, "target"));
                }
            }
            else if(scopingKey is Guid scopedUuid || Guid.TryParse(scopingKey.ToString(), out scopedUuid))
            {
                var pType = typeof(IDataPersistenceService<>).MakeGenericType(scopingType);
                var pInstance = this.m_serviceProvider.GetService(pType) as IDataPersistenceService;
                var instance = pInstance.Get(scopedUuid) as IdentifiedData;
                if(instance == null)
                {
                    throw new KeyNotFoundException($"{scopingType.GetSerializationName()}/{scopingKey}");
                }
                return instance.Validate().ToArray();
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(scopingKey));
            }

        }
    }
}
