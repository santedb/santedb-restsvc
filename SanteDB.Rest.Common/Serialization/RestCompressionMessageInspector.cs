﻿/*
 * Copyright (C) 2021 - 2025, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using RestSrvr.Message;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Http.Compression;
using System;
using System.Diagnostics.Tracing;
using System.IO;

namespace SanteDB.Rest.Common.Serialization
{
    /// <summary>
    /// Represents an HDSI message inspector which can inspect messages and perform tertiary functions
    /// not included in WCF (such as compression)
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Design a shim for testing REST context functions
    public class RestCompressionMessageInspector : IMessageInspector
    {
        // Trace source
        private readonly Tracer m_traceSource = Tracer.GetTracer(typeof(RestCompressionMessageInspector));

        /// <summary>
        /// After request is received
        /// </summary>
        public void AfterReceiveRequest(RestRequestMessage request)
        {
            try
            {
                // Handle compressed requests
                var compressionScheme = CompressionUtil.GetCompressionScheme(RestOperationContext.Current.IncomingRequest.Headers["Content-Encoding"]);
                if (compressionScheme != null)
                {
                    request.Body = compressionScheme.CreateDecompressionStream(request.Body);
                }
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error, e.ToString());
            }
        }

        /// <summary>
        /// Before sending the response
        /// </summary>
        public void BeforeSendResponse(RestResponseMessage response)
        {
            try
            {
                string encodings = RestOperationContext.Current.IncomingRequest.Headers.Get("Accept-Encoding");
                string compressionScheme = String.Empty;

                if (!string.IsNullOrEmpty(encodings))
                {
                    encodings = encodings.ToLowerInvariant();

                    if (encodings.Contains("lzma"))
                    {
                        compressionScheme = "lzma";
                    }
                    else if (encodings.Contains("bzip2"))
                    {
                        compressionScheme = "bzip2";
                    }
                    else if (encodings.Contains("gzip"))
                    {
                        compressionScheme = "gzip";
                    }
                    else if (encodings.Contains("deflate"))
                    {
                        compressionScheme = "deflate";
                    }
                    else
                    {
                        response.Headers.Add("X-CompressResponseStream", "no-known-accept");
                    }
                }

                // No reply = no compress :)
                if (response.Body == null)
                {
                    return;
                }

                // Finally compress
                // Compress
                if (!String.IsNullOrEmpty(compressionScheme))
                {
                    try
                    {
                        response.Headers.Add("Content-Encoding", compressionScheme);
                        response.Headers.Add("X-CompressResponseStream", compressionScheme);

                        // Read binary contents of the message
                        var memoryStream = new MemoryStream();
                        using (response.Body)
                        {
                            using (var compressor = CompressionUtil.GetCompressionScheme(compressionScheme).CreateCompressionStream(memoryStream))
                            {
                                response.Body.CopyTo(compressor);
                            }
                            memoryStream.Seek(0, SeekOrigin.Begin);
                            response.Body = memoryStream;
                        }
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceEvent(EventLevel.Error, e.ToString());
                    }
                }
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error, e.ToString());
            }
        }
    }
}