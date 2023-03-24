using RestSrvr.Attributes;
using SanteDB.Core;
using SanteDB.Core.Data.Initialization;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
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
    /// Download as a child resource
    /// </summary>
    public class ExportDatasetOperation : IApiChildOperation
    {
        // Parameter name
        public const string QueryParameterName = "query";
        // Include name
        public const string IncludeParameterName = "include";

        /// <summary>
        /// Get the scope binding
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        /// <summary>
        /// Get parent types
        /// </summary>
        public Type[] ParentTypes => new Type[]
        {
            typeof(Place),
            typeof(Concept),
            typeof(ReferenceTerm),
            typeof(Material),
            typeof(ManufacturedMaterial),
            typeof(Organization)
        };

        /// <summary>
        /// Get the name of the operation
        /// </summary>
        public string Name => "$export";

        /// <summary>
        /// Invoke the operation
        /// </summary>
        [UrlParameter(IncludeParameterName, typeof(String), "Include additional resources")]
        [UrlParameter(QueryParameterName, typeof(String), "Query to include in the export")]
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if (!this.ParentTypes.Contains(scopingType))
            {
                throw new InvalidOperationException(String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, scopingType.Name, String.Join(",", this.ParentTypes.Select(o => o.Name))));
            }
            else
            {
                var repositoryType = typeof(IRepositoryService<>).MakeGenericType(scopingType);
                var repositoryService = ApplicationServiceContext.Current.GetService(repositoryType) as IRepositoryService;
                IQueryResultSet results = null;

                if (parameters.TryGet<String>(QueryParameterName, out var query))
                {
                    var queryExpression = QueryExpressionParser.BuildLinqExpression(scopingType, query.ParseQueryString());
                    results = repositoryService.Find(queryExpression);
                }
                else
                {
                    results = repositoryService.Find(QueryExpressionParser.BuildLinqExpression(scopingType, new NameValueCollection()));
                }

                using (DataPersistenceControlContext.Create(LoadMode.SyncLoad))
                {
                    var retVal = new Dataset()
                    {
                        Action = results.OfType<IdentifiedData>().ToArray().Select(o => new DataUpdate()
                        {
                            IgnoreErrors = false,
                            InsertIfNotExists = true,
                            Element = o
                        }).OfType<DataInstallAction>().ToList()
                    };

                    // Process the includes
                    if (parameters.TryGet<String[]>(IncludeParameterName, out var includes))
                    {
                        retVal.Action.InsertRange(0, 
                            includes.SelectMany(inc=>
                            {
                                var resourceParts = inc.Split('?');
                                if(resourceParts.Length != 2)
                                {
                                    throw new ArgumentOutOfRangeException(IncludeParameterName, String.Format(ErrorMessages.ARGUMENT_COUNT_MISMATCH, 2, resourceParts.Length));
                                }

                                var includeHandler = HdsiMessageHandler.ResourceHandler.GetResourceHandler<IHdsiServiceContract>(resourceParts[0]);
                                var ds = this.Invoke(includeHandler.Type, null, new ParameterCollection(new Parameter(QueryParameterName, resourceParts[1]))) as Dataset;
                                return ds.Action;
                            }));
                    }

                    return retVal;
                }
            }

        }
    }
}
