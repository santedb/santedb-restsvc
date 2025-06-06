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
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RestSrvr;
using RestSrvr.Attributes;
using RestSrvr.Message;
using SanteDB.Core;
using SanteDB.Core.Applets.Services;
using SanteDB.Core.Applets.ViewModel.Description;
using SanteDB.Core.Applets.ViewModel.Json;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Http;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Json.Formatter;
using SanteDB.Core.Model.Serialization;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace SanteDB.Rest.Common.Serialization
{
    /// <summary>
    /// Represents the non-generic rest message dispatch formatter
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Design a shim for testing REST context functions
    public abstract class RestMessageDispatchFormatter : IDispatchMessageFormatter
    {
        // Formatters
        private static ConcurrentDictionary<Type, RestMessageDispatchFormatter> m_formatters = new ConcurrentDictionary<Type, RestMessageDispatchFormatter>();

        /// <summary>
        /// Create a formatter for the specified contract type
        /// </summary>
        public static RestMessageDispatchFormatter CreateFormatter(Type contractType)
        {
            RestMessageDispatchFormatter retVal = null;
            if (!m_formatters.TryGetValue(contractType, out retVal) || retVal == null)
            {
                var typeFormatter = typeof(RestMessageDispatchFormatter<>).MakeGenericType(contractType);
                retVal = Activator.CreateInstance(typeFormatter) as RestMessageDispatchFormatter;
                m_formatters.TryAdd(contractType, retVal);
            }
            return retVal;
        }

        /// <summary>
        /// Implemented below
        /// </summary>
        public abstract void DeserializeRequest(EndpointOperation operation, RestRequestMessage request, object[] parameters);

        /// <summary>
        /// Implemented below
        /// </summary>
        public abstract void SerializeResponse(RestResponseMessage response, object[] parameters, object result);
    }

    /// <summary>
    /// Represents a dispatch message formatter which uses the JSON.NET serialization
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] // TODO: Design a shim for testing REST context functions
    public class RestMessageDispatchFormatter<TContract> : RestMessageDispatchFormatter
    {
        private String m_version = Assembly.GetEntryAssembly()?.GetName().Version.ToString();
        private String m_versionName = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unnamed";
        private readonly string[] m_noBodyVerbs = { "GET", "HEAD" };

        // Trace source
        private readonly Tracer m_traceSource = Tracer.GetTracer(typeof(RestMessageDispatchFormatter));

        // Known types
        private static Type[] s_knownTypes;

        // Default view model
        private static ViewModelDescription m_defaultViewModel = null;

        /// <summary>
        /// Create new dispatch message formatter
        /// </summary>
        public RestMessageDispatchFormatter()
        {
            try
            {
                m_defaultViewModel = ViewModelDescription.Load(typeof(RestMessageDispatchFormatter<>).Assembly.GetManifestResourceStream("SanteDB.Rest.Common.Resources.ViewModel.xml"));

                try
                {
                    s_knownTypes = typeof(TContract).GetCustomAttributes<ServiceKnownResourceAttribute>().Select(t => t.Type).ToArray();
                }
                catch (Exception e)
                {
                    this.m_traceSource.TraceError("Error scanning for known types on contract {0} - {1}", typeof(TContract), e);
                    throw;
                }

                //if (ApplicationServiceContext.Current.HostType == SanteDBHostType.Server) // This can take a while on .NET framework where the Xml serializers are compiled - 
                //                                                                          // however in an environment like a server it is useful to do this
                //{
                //    this.m_traceSource.TraceVerbose("Will generate serializer for {0} ({1} types)...", typeof(TContract).FullName, s_knownTypes.Length);

                //    foreach (var s in s_knownTypes)
                //    {
                //        this.m_traceSource.TraceVerbose("Generating serializer for {0}...", s.Name);
                //        try
                //        {
                //            // Force creation of .NET Serializer
                //            XmlModelSerializerFactory.Current.CreateSerializer(s);
                //            ModelSerializationBinder.RegisterModelType(s);
                //        }
                //        catch (Exception e)
                //        {
                //            this.m_traceSource.TraceError("Error generating for {0} : {1}", s.Name, e.ToString());
                //            //throw;
                //        }
                //    }
                //}
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceError("Error generating REST message dispatch formatter: {0}", e);
            }
            this.m_traceSource.TraceInfo("Finished creating REST message formatter");
        }

        /// <summary>
        /// Deserialize the request
        /// </summary>
        public override void DeserializeRequest(EndpointOperation operation, RestRequestMessage request, object[] parameters)
        {
            try
            {
#if DEBUG
                this.m_traceSource.TraceEvent(EventLevel.Informational, "Received request from: {0}", RestOperationContext.Current.IncomingRequest.RemoteEndPoint);
#endif

                var httpRequest = RestOperationContext.Current.IncomingRequest;
                if (this.m_noBodyVerbs.Contains(httpRequest.HttpMethod))
                {
                    return; // no body
                }

                ContentType contentType = null;
                if (!String.IsNullOrEmpty(httpRequest.Headers["Content-Type"]))
                {
                    contentType = new ContentType(httpRequest.Headers["Content-Type"]);
                }

                for (int pNumber = 0; pNumber < parameters.Length; pNumber++)
                {
                    var parm = operation.Description.InvokeMethod.GetParameters()[pNumber];
                    // Simple parameter
                    if (parameters[pNumber] != null)
                    {
                        continue; // dispatcher already populated
                    }
                    else
                    {
                        switch (contentType.MediaType)
                        {
                            case "application/xml":
                            case "application/xml+sdb-patch":
                            case SanteDBExtendedMimeTypes.XmlPatch:
                            case SanteDBExtendedMimeTypes.XmlRimModel:
                                XmlSerializer serializer = null;
                                using (XmlReader bodyReader = XmlReader.Create(request.Body))
                                {
                                    while (bodyReader.NodeType != XmlNodeType.Element)
                                    {
                                        bodyReader.Read();
                                    }

                                    Type eType = s_knownTypes.FirstOrDefault(o => o.GetCustomAttribute<XmlRootAttribute>()?.ElementName == bodyReader.LocalName &&
                                        o.GetCustomAttribute<XmlRootAttribute>()?.Namespace == bodyReader.NamespaceURI);
                                    if (eType == null)
                                    {
                                        eType = new ModelSerializationBinder().BindToType(null, bodyReader.LocalName); // Try to find by root element
                                    }

                                    serializer = XmlModelSerializerFactory.Current.CreateSerializer(eType ?? parm.ParameterType);
                                    parameters[pNumber] = serializer.Deserialize(bodyReader);
                                }
                                break;

                            case "application/json+sdb-viewmodel":
                            case SanteDBExtendedMimeTypes.JsonViewModel:
                                var viewModel = httpRequest.Headers[ExtendedHttpHeaderNames.ViewModelHeaderName] ?? httpRequest.QueryString[QueryControlParameterNames.HttpViewModelParameterName];

                                // Create the view model serializer
                                var viewModelSerializer = new JsonViewModelSerializer();
                                viewModelSerializer.LoadSerializerAssembly(typeof(ActExtensionViewModelSerializer).Assembly);

                                if (!String.IsNullOrEmpty(viewModel))
                                {
                                    var viewModelDescription = ApplicationServiceContext.Current.GetService<IAppletManagerService>()?.Applets.GetViewModelDescription(viewModel);
                                    viewModelSerializer.ViewModel = viewModelDescription;
                                }
                                else
                                {
                                    viewModelSerializer.ViewModel = m_defaultViewModel;
                                }

                                using (var sr = new StreamReader(request.Body))
                                {
                                    parameters[pNumber] = viewModelSerializer.DeSerialize(sr, parm.ParameterType);
                                }

                                break;

                            case "application/json":
                            case "application/json+sdb-patch":
                            case SanteDBExtendedMimeTypes.JsonPatch:
                            case SanteDBExtendedMimeTypes.JsonRimModel:
                                using (var sr = new StreamReader(request.Body))
                                {
                                    JsonSerializer jsz = new JsonSerializer()
                                    {
                                        SerializationBinder = new ModelSerializationBinder(),
                                        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                                        TypeNameHandling = TypeNameHandling.All
                                    };
                                    jsz.Converters.Add(new StringEnumConverter());
                                    var dserType = parm.ParameterType;
                                    parameters[pNumber] = jsz.Deserialize(sr, dserType);
                                }
                                break;

                            case "application/octet-stream":
                                parameters[pNumber] = request.Body;
                                break;

                            case "text/plain":
                                using (var sr = new StreamReader(request.Body, Encoding.GetEncoding(contentType.CharSet ?? "utf-8")))
                                {
                                    parameters[pNumber] = sr.ReadToEnd();
                                }
                                break;
                            case "application/x-www-form-urlencoded":
                                NameValueCollection nvc = new NameValueCollection();
                                using (var sr = new StreamReader(request.Body))
                                {
                                    var ptext = sr.ReadToEnd();
                                    var parms = ptext.Split('&');
                                    foreach (var p in parms)
                                    {
                                        var parmData = p.Split('=');
                                        parmData[1] += new string('=', parmData.Length - 2);
                                        nvc.Add(WebUtility.UrlDecode(parmData[0]), WebUtility.UrlDecode(parmData[1]));
                                    }
                                }
                                parameters[pNumber] = nvc;
                                break;

                            case "multipart/form-data":
                                var multipartReader = new MultipartReader(contentType.Boundary, request.Body);
                                var parmValue = new List<MultiPartFormData>();
                                while (true)
                                {
                                    var nextSection = multipartReader.ReadNextSectionAsync().Result;
                                    if (nextSection == null)
                                    {
                                        break;
                                    }

                                    var contentDisposition = nextSection.GetContentDispositionHeader();
                                    if (String.IsNullOrEmpty(contentDisposition.FileName.Value))
                                    {
                                        using (var sr = new StreamReader(nextSection.Body))
                                        {
                                            parmValue.Add(new MultiPartFormData(contentDisposition.Name.Value, sr.ReadToEnd()));
                                        }
                                    }
                                    else
                                    {
                                        using (var buffer = new MemoryStream())
                                        {
                                            nextSection.Body.CopyTo(buffer);
                                            parmValue.Add(new MultiPartFormData(contentDisposition.Name.Value, buffer.ToArray(), nextSection.ContentType, contentDisposition.FileName.Value, false));
                                        }
                                    }
                                }
                                parameters[pNumber] = parmValue;

                                break;

                            default:
                                throw new InvalidOperationException("Invalid request format");
                        }

                        // Set no load
                        switch (parameters[pNumber])
                        {
                            case IResourceCollection irc:
                                irc.AddAnnotationToAll(SanteDBModelConstants.NoDynamicLoadAnnotation);
                                break;

                            case IAnnotatedResource ide:
                                ide.AddAnnotation(SanteDBModelConstants.NoDynamicLoadAnnotation);
                                break;
                        }

                        // If the parameters is an IdentifiedData instance - we want to wipe out collections that are empty
                        // since the serializer populates all lists with no items - meanwhile the persistence interprets this 
                        // as clearing data from the collection.
                        if (parameters[pNumber] is IdentifiedData identifiedData)
                        {
                            identifiedData.NullifyEmptyCollections();
                        }

                    }
                }
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error, e.ToString());
                throw;
            }
        }

        /// <summary>
        /// Serialize the reply
        /// </summary>
        public override void SerializeResponse(RestResponseMessage response, object[] parameters, object result)
        {
            try
            {
                this.m_traceSource.TraceVerbose("Serializing {0}", result?.GetType());
                // Outbound control
                var httpRequest = RestOperationContext.Current.IncomingRequest;
                ContentType contentTypeMime = null;

                if (!String.IsNullOrEmpty(RestOperationContext.Current.OutgoingResponse.ContentType)) // Declared content type
                {
                    contentTypeMime = new ContentType(RestOperationContext.Current.OutgoingResponse.ContentType);
                }
                // Does the user not want an echo on success
                else if(httpRequest.Headers.TryGetValue(ExtendedHttpHeaderNames.NoResponse, out var noResponseRaw) && 
                    Boolean.TryParse(noResponseRaw[0], out var noResponseVal) && 
                    noResponseVal &&
                    (int)response.StatusCode > 200 && 
                    (int)response.StatusCode < 400)
                {
                    result = null; // client instructed us not to send back data
                }
                else // let client decide
                {
                    string accepts = httpRequest.Headers["Accept"],
                        contentType = httpRequest.Headers["Content-Type"];

                    if (String.IsNullOrEmpty(accepts) && String.IsNullOrEmpty(contentType))
                    {
                        accepts = "application/json";
                    }
                    contentTypeMime = (accepts ?? contentType).Split(',').Select(o => new ContentType(o)).First();
                }

                // Result is serializable
                if (result == null)
                {
                    if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Created)
                    {
                        response.StatusCode = HttpStatusCode.NoContent;
                    }
                }
                else
                {
                    switch (result)
                    {
                        case Stream str:
                            {
                                // Did the returner explicitly set the content type mime
                                if (RestOperationContext.Current.OutgoingResponse.ContentType == null)
                                {
                                    contentTypeMime = new ContentType("application/octet-stream");
                                }
                                response.Body = str;
                                break;
                            }
                        case XmlSchema xs:
                            {
                                MemoryStream ms = new MemoryStream();
                                xs.Write(ms);
                                ms.Seek(0, SeekOrigin.Begin);
                                contentTypeMime = new ContentType("text/xml");
                                response.Body = ms;
                                break;
                            }
                        default:
                            switch (contentTypeMime.MediaType)
                            {
                                case "application/json+sdb-viewmodel":
                                case SanteDBExtendedMimeTypes.JsonViewModel:


                                    if (result is IdentifiedData id)
                                    {
#if DEBUG
                                        this.m_traceSource.TraceVerbose("Serializing {0} as view model result", result);
#endif
                                        var viewModel = httpRequest.Headers[ExtendedHttpHeaderNames.ViewModelHeaderName] ?? httpRequest.QueryString[QueryControlParameterNames.HttpViewModelParameterName];

                                        // Create the view model serializer
                                        var viewModelSerializer = new JsonViewModelSerializer();

#if DEBUG
                                        this.m_traceSource.TraceVerbose("Will load serialization assembly {0}", typeof(ActExtensionViewModelSerializer).Assembly);
#endif
                                        viewModelSerializer.LoadSerializerAssembly(typeof(ActExtensionViewModelSerializer).Assembly);

                                        if (!String.IsNullOrEmpty(viewModel))
                                        {
                                            var viewModelDescription = ApplicationServiceContext.Current.GetService<IAppletManagerService>()?.Applets.GetViewModelDescription(viewModel);
                                            viewModelSerializer.ViewModel = viewModelDescription ?? viewModelSerializer.ViewModel;
                                        }
                                        else
                                        {
                                            viewModelSerializer.ViewModel = m_defaultViewModel;
                                        }

#if DEBUG
                                        this.m_traceSource.TraceVerbose("Using view model {0}", viewModelSerializer.ViewModel?.Name);
#endif
                                        using (var tms = new MemoryStream())
                                        using (StreamWriter sw = new StreamWriter(tms, new UTF8Encoding(false)))
                                        using (JsonWriter jsw = new JsonTextWriter(sw))
                                        {
                                            viewModelSerializer.Serialize(jsw, id);
                                            jsw.Flush();
                                            sw.Flush();
                                            response.Body = new MemoryStream(tms.ToArray());
                                        }

#if DEBUG
                                        this.m_traceSource.TraceVerbose("Serialized body of  {0}", result);
#endif
                                        //contentTypeMime = new ContentType("application/json+sdb-viewmodel");
                                        contentTypeMime = new ContentType(SanteDBExtendedMimeTypes.JsonViewModel);
                                    }
                                    else
                                    {
                                        goto case "application/json"; // HACK: C# doesn't do fallthrough so we have to push it
                                    }
                                    break;

                                case "application/json":
                                case SanteDBExtendedMimeTypes.JsonRimModel:
                                    {
                                        // Prepare the serializer
                                        JsonSerializer jsz = new JsonSerializer();
                                        jsz.Converters.Add(new StringEnumConverter());

                                        // Write json data
                                        using (MemoryStream ms = new MemoryStream())
                                        using (StreamWriter sw = new StreamWriter(ms, new UTF8Encoding(false)))
                                        using (JsonWriter jsw = new JsonTextWriter(sw))
                                        {
                                            jsz.DateFormatHandling = DateFormatHandling.IsoDateFormat;
                                            jsz.NullValueHandling = NullValueHandling.Ignore;
                                            jsz.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

                                            if (result is IDictionary)
                                            {
                                                jsz.TypeNameHandling = TypeNameHandling.None;
                                            }
                                            else
                                            {
                                                jsz.TypeNameHandling = result.GetType().GetCustomAttribute<JsonObjectAttribute>()?.ItemTypeNameHandling ?? TypeNameHandling.Auto;
                                            }
                                            jsz.Converters.Add(new StringEnumConverter());
                                            jsz.Serialize(jsw, result);
                                            jsw.Flush();
                                            sw.Flush();
                                            response.Body = new MemoryStream(ms.ToArray());
                                        }

                                        // Prepare reply for the WCF pipeline
                                        contentTypeMime = new ContentType("application/json");
                                        break;
                                    }
                                case "application/xml":
                                case SanteDBExtendedMimeTypes.XmlRimModel:
                                    {
                                        XmlSerializer xsz = XmlModelSerializerFactory.Current.CreateSerializer(result.GetType());
                                        MemoryStream ms = new MemoryStream();
                                        try
                                        {
                                            if (xsz == null)
                                            {
                                                xsz = XmlModelSerializerFactory.Current.CreateSerializer(result.GetType());
                                            }

                                            xsz.Serialize(ms, result);
                                        }
                                        // No longer needed
                                        //catch (InvalidOperationException e) when (e.Message.Contains("XML document") && result is Bundle bundle)
                                        //{
                                        //    this.m_traceSource.TraceWarning("Will create a new serializer because of XML error {0}", e.Message);
                                        //    xsz = XmlModelSerializerFactory.Current.CreateSerializer(typeof(Bundle), bundle.Item?.Select(o => o.GetType()).Distinct().ToArray());
                                        //    ms.Seek(0, SeekOrigin.Begin);
                                        //    xsz.Serialize(ms, result);
                                        //}
                                        catch (Exception e)
                                        {
                                            this.m_traceSource.TraceError("Error serializing response: {0}", e);
                                            throw new Exception($"Could not serialize response message {result}", e);
                                        }
                                        contentTypeMime = new ContentType("application/xml");
                                        ms.Seek(0, SeekOrigin.Begin);
                                        response.Body = ms;
                                        break;
                                    }
                                default:
                                    contentTypeMime = new ContentType("text/plain");
                                    response.Body = new MemoryStream(Encoding.UTF8.GetBytes(result.ToString()));
                                    break;
                            }
                            break;
                    }
                }


#if DEBUG
                this.m_traceSource.TraceVerbose("Setting response headers");
#endif
                RestOperationContext.Current.OutgoingResponse.ContentType = contentTypeMime?.ToString();
                RestOperationContext.Current.OutgoingResponse.AppendHeader("X-GeneratedOn", DateTime.Now.ToString("o"));
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error, e.ToString());
                throw new Exception("Error serializing response for operation", e);
            }
        }
    }
}