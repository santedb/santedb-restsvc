/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2022-5-30
 */
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
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Json.Formatter;
using SanteDB.Core.Model.Serialization;
using System;
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
        private static Dictionary<Type, RestMessageDispatchFormatter> m_formatters = new Dictionary<Type, RestMessageDispatchFormatter>();

        /// <summary>
        /// Create a formatter for the specified contract type
        /// </summary>
        public static RestMessageDispatchFormatter CreateFormatter(Type contractType)
        {
            RestMessageDispatchFormatter retVal = null;
            if (!m_formatters.TryGetValue(contractType, out retVal))
            {
                lock (m_formatters)
                {
                    if (!m_formatters.ContainsKey(contractType))
                    {
                        var typeFormatter = typeof(RestMessageDispatchFormatter<>).MakeGenericType(contractType);
                        retVal = Activator.CreateInstance(typeFormatter) as RestMessageDispatchFormatter;
                        m_formatters.Add(contractType, retVal);
                    }
                }
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

                this.m_traceSource.TraceInfo("Will generate serializer for {0} ({1} types)...", typeof(TContract).FullName, s_knownTypes.Length);

                foreach (var s in s_knownTypes)
                {
                    this.m_traceSource.TraceInfo("Generating serializer for {0}...", s.Name);
                    try
                    {
                        // Force creation of .NET Serializer
                        XmlModelSerializerFactory.Current.CreateSerializer(s);
                        ModelSerializationBinder.RegisterModelType(s);
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceError("Error generating for {0} : {1}", s.Name, e.ToString());
                        //throw;
                    }
                }
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
                ContentType contentType = null;
                if (!String.IsNullOrEmpty(httpRequest.Headers["Content-Type"]))
                    contentType = new ContentType(httpRequest.Headers["Content-Type"]);

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
                                XmlSerializer serializer = null;
                                using (XmlReader bodyReader = XmlReader.Create(request.Body))
                                {
                                    while (bodyReader.NodeType != XmlNodeType.Element)
                                        bodyReader.Read();

                                    Type eType = s_knownTypes.FirstOrDefault(o => o.GetCustomAttribute<XmlRootAttribute>()?.ElementName == bodyReader.LocalName &&
                                        o.GetCustomAttribute<XmlRootAttribute>()?.Namespace == bodyReader.NamespaceURI);
                                    if (eType == null)
                                        eType = new ModelSerializationBinder().BindToType(null, bodyReader.LocalName); // Try to find by root element

                                    serializer = XmlModelSerializerFactory.Current.CreateSerializer(eType ?? parm.ParameterType);
                                    parameters[pNumber] = serializer.Deserialize(bodyReader);
                                }
                                break;

                            case "application/json+sdb-viewmodel":
                                var viewModel = httpRequest.Headers["X-SanteDB-ViewModel"] ?? httpRequest.QueryString["_viewModel"];

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
                                    parameters[pNumber] = viewModelSerializer.DeSerialize(sr, parm.ParameterType);

                                break;

                            case "application/json":
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

                            default:
                                throw new InvalidOperationException("Invalid request format");
                        }

                        // Set no load
                        switch (parameters[pNumber])
                        {
                            case IResourceCollection irc:
                                irc.AddAnnotationToAll(SanteDBModelConstants.NoDynamicLoadAnnotation);
                                break;

                            case IIdentifiedData ide:
                                ide.AddAnnotation(SanteDBModelConstants.NoDynamicLoadAnnotation);
                                break;
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
                this.m_traceSource.TraceInfo("Serializing {0}", result?.GetType());
                // Outbound control
                var httpRequest = RestOperationContext.Current.IncomingRequest;
                string accepts = httpRequest.Headers["Accept"],
                    contentType = httpRequest.Headers["Content-Type"];

                if (String.IsNullOrEmpty(accepts) && String.IsNullOrEmpty(contentType))
                {
                    accepts = "application/xml";
                }
                var contentTypeMime = (accepts ?? contentType).Split(',').Select(o => new ContentType(o)).First();

                // Result is serializable
                if (result == null)
                {
                    if (response.StatusCode == 200)
                        response.StatusCode = 204;
                }
                else if (result?.GetType().GetCustomAttribute<XmlTypeAttribute>() != null ||
                    result?.GetType().GetCustomAttribute<JsonObjectAttribute>() != null)
                {
                    switch (contentTypeMime.MediaType)
                    {
                        case "application/json+sdb-viewmodel":

                            if (result is IdentifiedData id)
                            {
#if DEBUG
                                this.m_traceSource.TraceInfo("Serializing {0} as view model result", result);
#endif
                                var viewModel = httpRequest.Headers["X-SanteDB-ViewModel"] ?? httpRequest.QueryString["_viewModel"];

                                // Create the view model serializer
                                var viewModelSerializer = new JsonViewModelSerializer();

#if DEBUG
                                this.m_traceSource.TraceInfo("Will load serialization assembly {0}", typeof(ActExtensionViewModelSerializer).Assembly);
#endif
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

#if DEBUG
                                this.m_traceSource.TraceInfo("Using view model {0}", viewModelSerializer.ViewModel?.Name);
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
                                this.m_traceSource.TraceInfo("Serialized body of  {0}", result);
#endif
                                contentType = "application/json+sdb-viewmodel";
                            }
                            else
                            {
                                goto case "application/json"; // HACK: C# doesn't do fallthrough so we have to push it
                            }
                            break;

                        case "application/json":
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
                                    jsz.TypeNameHandling = TypeNameHandling.Auto;
                                    jsz.Converters.Add(new StringEnumConverter());
                                    jsz.Serialize(jsw, result);
                                    jsw.Flush();
                                    sw.Flush();
                                    response.Body = new MemoryStream(ms.ToArray());
                                }

                                // Prepare reply for the WCF pipeline
                                contentType = "application/json";
                                break;
                            }
                        default:
                            {
                                XmlSerializer xsz = XmlModelSerializerFactory.Current.CreateSerializer(result.GetType());
                                MemoryStream ms = new MemoryStream();
                                try
                                {
                                    if (xsz == null)
                                        xsz = XmlModelSerializerFactory.Current.CreateSerializer(result.GetType());
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
                                contentType = "application/xml";
                                ms.Seek(0, SeekOrigin.Begin);
                                response.Body = ms;
                                break;
                            }
                    }
                }
                else if (result is XmlSchema)
                {
                    MemoryStream ms = new MemoryStream();
                    (result as XmlSchema).Write(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    contentType = "text/xml";
                    response.Body = ms;
                }
                else if (result is Stream) // TODO: This is messy, clean it up
                {
                    contentType = "application/octet-stream";
                    response.Body = result as Stream;
                }
                else
                {
                    contentType = "text/plain";
                    response.Body = new MemoryStream(Encoding.UTF8.GetBytes(result.ToString()));
                }

#if DEBUG
                this.m_traceSource.TraceVerbose("Setting response headers");
#endif
                RestOperationContext.Current.OutgoingResponse.ContentType = RestOperationContext.Current.OutgoingResponse.ContentType ?? contentType;
                RestOperationContext.Current.OutgoingResponse.AppendHeader("X-PoweredBy", String.Format("SanteDB {0} ({1})", m_version, m_versionName));
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