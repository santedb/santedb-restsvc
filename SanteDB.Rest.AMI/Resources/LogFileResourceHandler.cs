/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-6-21
 */
using RestSrvr;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.AMI.Logging;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Log file resource handler
    /// </summary>
    public class LogFileResourceHandler : IApiResourceHandler
    {
        private readonly ILogManagerService m_logManagerService;

        /// <summary>
        /// Gets the resource name
        /// </summary>
        public string ResourceName => "Log";

        /// <summary>
        /// Gets the type of this reosurce handler
        /// </summary>
        public Type Type => typeof(LogFileInfo);

        /// <summary>
        /// Scope of this API handler
        /// </summary>
        public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// DI ctor
        /// </summary>
        public LogFileResourceHandler(ILogManagerService logManagerService)
        {
            this.m_logManagerService = logManagerService;
        }

        /// <summary>
        /// Gets the capabilities
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Search | ResourceCapabilityType.Get;

        /// <inheritdoc/>
        public object Create(object data, bool updateIfExists)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public object Delete(object key)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadServiceLogs)]
        public object Get(object id, object versionId)
        {
            var logFile = this.m_logManagerService.GetLogFile(id.ToString());
            if (logFile == null)
            {
                throw new KeyNotFoundException(id.ToString());
            }

            if (Boolean.TryParse(RestOperationContext.Current.IncomingRequest.QueryString["_download"], out var asAttachment) && asAttachment)
            {
                RestOperationContext.Current.OutgoingResponse.AddHeader("Content-Disposition", $"attachment; filename={logFile.Name}");
                RestOperationContext.Current.OutgoingResponse.ContentType = "text/plain";
            }

            RestOperationContext.Current.OutgoingResponse.ContentType = "text/plain";
            _ = Int32.TryParse(RestOperationContext.Current.IncomingRequest.QueryString["_offset"], out var offset);
            _ = Int32.TryParse(RestOperationContext.Current.IncomingRequest.QueryString["_count"], out var count);
            if (offset > 0 || count > 0)
            {
                using (var fs = logFile.OpenRead())
                {
                    var retVal = new MemoryStream();
                    byte[] buffer = null;
                    if (offset + count > fs.Length)
                    {
                        buffer = new byte[fs.Length - offset];
                    }
                    else
                    {
                        buffer = new byte[count];
                    }
                    fs.Seek(offset, SeekOrigin.Begin);
                    fs.Read(buffer, 0, buffer.Length);
                    retVal.Write(buffer, 0, buffer.Length);
                    retVal.Seek(0, SeekOrigin.Begin);
                    return retVal;
                }
            }
            else
            {
                return logFile.OpenRead();
            }
        }

        /// <inheritdoc/>
        [Demand(PermissionPolicyIdentifiers.ReadServiceLogs)]
        public IQueryResultSet Query(NameValueCollection queryParameters)
        {
            return this.m_logManagerService.GetLogFiles().Select(o => new LogFileInfo()
            {
                Name = o.Name,
                Key = o.Name,
                LastWrite = o.LastWriteTime,
                Size = o.Length
            }).AsResultSet();
        }

        /// <inheritdoc/>
        public object Update(object data)
        {
            throw new NotSupportedException();
        }
    }
}
