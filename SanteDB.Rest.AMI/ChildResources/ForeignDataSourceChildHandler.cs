/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you
 * may not use this file except in compliance with the License. You may
 * obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 *
 * User: fyfej
 * Date: 2023-3-10
 */
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
#pragma warning disable CS0612

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
            if (scopingKey is Guid uuid)
            {
                var foreignData = this.m_foreignDataManager.Get(uuid);
                if (foreignData == null)
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
#pragma warning restore