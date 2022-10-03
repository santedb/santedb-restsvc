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
        [Demand(PermissionPolicyIdentifiers.AlterSystemConfiguration)]
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

            if (Int32.TryParse(RestOperationContext.Current.IncomingRequest.QueryString["_offset"], out var offset) &&
                Int32.TryParse(RestOperationContext.Current.IncomingRequest.QueryString["_count"], out var count))
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
                    fs.Read(buffer, offset, buffer.Length);
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
        [Demand(PermissionPolicyIdentifiers.AlterSystemConfiguration)]
        public IQueryResultSet Query(NameValueCollection queryParameters)
        {
            return this.m_logManagerService.GetLogFiles().Select(o => new LogFileInfo()
            {
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
