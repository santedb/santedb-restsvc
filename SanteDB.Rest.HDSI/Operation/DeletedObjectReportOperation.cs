using SanteDB.Core;
using SanteDB.Core.Cdss;
using SanteDB.Core.Data.Quality;
using SanteDB.Core.Data.Quality.Configuration;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.HDSI.Operation
{

    /// <summary>
    /// Represents an operation which sends back a list of UUIDs which have been obsoleted in the CDR and should no longer be used
    /// </summary>
    public class DeletedObjectReportOperation : IApiChildOperation
    {
        private readonly IServiceProvider m_serviceProvider;

        /// <summary>
        /// DI Ctor
        /// </summary>
        public DeletedObjectReportOperation(IServiceProvider serviceProvider)
        {
            this.m_serviceProvider = serviceProvider;
        }

        /// <inheritdoc/>
        public string Name => "deletedObjects";

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[]
        {
            typeof(Act),
            typeof(Entity),
            typeof(IdentityDomain),
            typeof(Concept),
            typeof(ConceptSet),
            typeof(CodeSystem),
            typeof(ReferenceTerm),
            typeof(RelationshipValidationRule),
            typeof(TemplateDefinition),
            typeof(ConceptClass),
            typeof(ExtensionType),
            typeof(DataQualityRulesetConfiguration),
            typeof(ICdssLibraryRepositoryMetadata)
        };

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if(!parameters.TryGet("since", out DateTime since))
            {
                throw new ArgumentNullException("since");
            }

            if(scopingType == typeof(DataQualityRulesetConfiguration))
            {
                var svc = this.m_serviceProvider.GetService<IDataQualityConfigurationProviderService>();
                return svc.GetRuleSets(true).Where(o => o.ObsoletionTime != null && o.ObsoletionTime >= since).Select(o => o.Id).ToArray();
            }
            else if(scopingType == typeof(ICdssLibraryRepositoryMetadata))
            {
                var svc = this.m_serviceProvider.GetService<ICdssLibraryRepository>();
                return svc.Find(o=>o.Id != null).OfType<ICdssLibrary>().ToArray().Where(o => o.StorageMetadata.ObsoletionTime != null && o.StorageMetadata.ObsoletionTime >= since).Select(o => o.Id.ToString()).ToArray();
            }
            else
            {
                var svcType = typeof(IRepositoryService<>).MakeGenericType(scopingType);
                var svc = this.m_serviceProvider.GetService(svcType) as IRepositoryService;
                var filterExpr = $"obsoletionTime=!null&obsoletionTime=>={since:o}".ParseQueryString();
                var filter = QueryExpressionParser.BuildLinqExpression(scopingType, filterExpr);
                var selector = QueryExpressionParser.BuildPropertySelector(scopingType, "id");
                var retVal = new Bundle(svc.Find(filter).Select<Guid?>(selector).Select(o => new IdentifiedDataReference()
                {
                    ReferencedType = scopingType,
                    BatchOperation = BatchOperationType.Delete,
                    Key = o
                }));
                if(retVal.Item.Any())
                {
                    return retVal;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
