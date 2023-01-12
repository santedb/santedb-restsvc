using RestSrvr;
using SanteDB.Core.Data.Import;
using SanteDB.Core.Http;
using SanteDB.Core.i18n;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security.Audit;
using SanteDB.Core.Security.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace SanteDB.Rest.AMI.ChildResources
{
    /// <summary>
    /// Foreign data source file resource
    /// </summary>
    public class ForeignDataSourceChildHandler : IApiChildResourceHandler
    {
        private readonly IForeignDataManagerService m_foreignDataManager;
        private readonly IAuditService m_auditBuilder;

        /// <summary>
        /// DI constructor
        /// </summary>
        public ForeignDataSourceChildHandler(IForeignDataManagerService foreignDataManagerService, IAuditService auditBuilder)
        {
            this.m_foreignDataManager = foreignDataManagerService;
            this.m_auditBuilder = auditBuilder;
        }

        /// <inheritdoc/>
        public Type PropertyType => typeof(Stream);

        /// <inheritdoc/>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Get;

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(IForeignDataSubmission) };

        /// <inheritdoc/>
        public string Name => "_file";

        /// <inheritdoc/>
        public object Add(Type scopingType, object scopingKey, object item)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public object Get(Type scopingType, object scopingKey, object key)
        {
            if(scopingKey is Guid uuid)
            {
                var foreignData = this.m_foreignDataManager.Get(uuid);
                if(foreignData == null)
                {
                    throw new KeyNotFoundException(uuid.ToString());
                }
                else
                {
                    var audit = this.m_auditBuilder.Audit().ForEventDataAction(EventTypeCodes.Export,
                        Core.Model.Audit.ActionType.Read,
                        Core.Model.Audit.AuditableObjectLifecycle.Access,
                        Core.Model.Audit.EventIdentifierType.Export,
                        Core.Model.Audit.OutcomeIndicator.Success,
                        $"alien/{scopingKey}/{key}.csv",
                        foreignData);

                    try
                    {
                        RestOperationContext.Current.OutgoingResponse.ContentType = DefaultContentTypeMapper.GetContentType(foreignData.Name);
                        switch (key.ToString().ToLowerInvariant())
                        {
                            case "source":
                                RestOperationContext.Current.OutgoingResponse.Headers.Add("Content-Disposition", $"attachment; filename={foreignData.Name}");
                                return foreignData.GetSourceStream();
                            case "reject":
                                RestOperationContext.Current.OutgoingResponse.Headers.Add("Content-Disposition", $"attachment; filename={Path.GetFileNameWithoutExtension(foreignData.Name)}-reject{Path.GetExtension(foreignData.Name)}");
                                return foreignData.GetRejectStream();
                            default:
                                throw new KeyNotFoundException(key.ToString());
                        }
                    }
                    catch
                    {
                        audit.WithOutcome(Core.Model.Audit.OutcomeIndicator.SeriousFail);
                        throw;
                    }
                    finally
                    {
                        audit.Send();
                    }
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(scopingKey), String.Format(ErrorMessages.ARGUMENT_INCOMPATIBLE_TYPE, typeof(Guid), scopingKey.GetType()));
            }
        }

        /// <inheritdoc/>
        public IQueryResultSet Query(Type scopingType, object scopingKey, NameValueCollection filter)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public object Remove(Type scopingType, object scopingKey, object key)
        {
            throw new NotSupportedException();
        }
    }
}
