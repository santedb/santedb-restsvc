using SanteDB.Core;
using SanteDB.Core.Data;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Json.Formatter;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Serialization;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ZXing.OneD;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// Operation which filters any object based on where the object is used
    /// </summary>
    public class KeyUsedInOperation : IApiChildOperation
    {

        // Serialization binder
        private readonly ModelSerializationBinder m_serializationBinder = new ModelSerializationBinder();

        /// <summary>
        /// Cross reference query parameter
        /// </summary>
        public const string XREF_QUERY_PARAMETER_NAME = "xr-query";
        /// <summary>
        /// Resource
        /// </summary>
        public const string XREF_RESOURCE_PARAMETER_NAME = "xr-resource";
        /// <summary>
        /// Selection from cross reference
        /// </summary>
        public const string XREF_SELECT_PARAMETER_NAME = "xr-select";

        /// <inheritdoc/>
        public string Name => "xref-use";

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        /// <inheritdoc/>
        public Type[] ParentTypes => Type.EmptyTypes;

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if(!parameters.TryGet(XREF_QUERY_PARAMETER_NAME, out string xrefQuery))
            {
                throw new ArgumentNullException(XREF_QUERY_PARAMETER_NAME);
            }
            if(!parameters.TryGet(XREF_RESOURCE_PARAMETER_NAME, out string resourceName))
            {
                throw new ArgumentNullException(XREF_RESOURCE_PARAMETER_NAME);
            }
            if(!parameters.TryGet(XREF_SELECT_PARAMETER_NAME, out string selector))
            {
                throw new ArgumentNullException(XREF_SELECT_PARAMETER_NAME);
            }

            var resourceType = this.m_serializationBinder.BindToType(null, resourceName);
            // Get Build the XREF query 
            var xrefLinq = QueryExpressionParser.BuildLinqExpression(resourceType, xrefQuery.ParseQueryString());
            var keySelector = QueryExpressionParser.BuildPropertySelector(resourceType, selector, forceLoad: false, returnNewObjectOnNull: false);

            // Get the repo
            var repoType = typeof(IRepositoryService<>).MakeGenericType(resourceType);
            var repo = ApplicationServiceContext.Current.GetService(repoType) as IRepositoryService; 
            if(repo == null)
            {
                throw new InvalidOperationException(String.Format(ErrorMessages.DEPENDENT_CONFIGURATION_MISSING, repoType));
            }
            var keyValues = repo.Find(xrefLinq).Select<Guid?>(keySelector).Distinct();

            if (keyValues.Any())
            {
                repoType = typeof(IRepositoryService<>).MakeGenericType(scopingType);
                repo = ApplicationServiceContext.Current.GetService(repoType) as IRepositoryService;

                xrefLinq = QueryExpressionParser.BuildLinqExpression(scopingType, String.Join("&", keyValues.Select(k => $"id={k}")).ParseQueryString());
                var results = repo.Find(xrefLinq);
                return new Bundle(results.OfType<IdentifiedData>());
            }
            else
            {
                return new Bundle();
            }
        }
    }
}
