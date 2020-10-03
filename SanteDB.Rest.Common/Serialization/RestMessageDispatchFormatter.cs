/*
 * Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
 * Copyright (C) 2019 - 2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE.md)
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
 * Date: 2019-12-24
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
using SanteDB.Core.Interfaces;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Json.Formatter;
using SanteDB.Core.Model.Serialization;
using SanteDB.Core.Security;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Net;
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
    public class RestMessageDispatchFormatter<TContract> : RestMessageDispatchFormatter
    {

        private String m_version = Assembly.GetEntryAssembly()?.GetName().Version.ToString();
        private String m_versionName = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "Unnamed";

        // Trace source
        private Tracer m_traceSource = Tracer.GetTracer(typeof(RestMessageDispatchFormatter));

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
                catch(Exception e)
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
                string contentType = httpRequest.Headers["Content-Type"];

                for (int pNumber = 0; pNumber < parameters.Length; pNumber++)
                {
                    var parm = operation.Description.InvokeMethod.GetParameters()[pNumber];

                    // Simple parameter
                    if (parameters[pNumber] != null)
                    {
                        continue; // dispatcher already populated
                    }
                    // Use XML Serializer
                    else if (contentType?.StartsWith("application/xml") == true)
                    {
                        XmlSerializer serializer = null;
                        using (XmlReader bodyReader = XmlReader.Create(request.Body))
                        {
                            while (bodyReader.NodeType != XmlNodeType.Element)
                                bodyReader.Read();

                            Type eType = s_knownTypes.FirstOrDefault(o => o.GetCustomAttribute<XmlRootAttribute>()?.ElementName == bodyReader.LocalName &&
                                o.GetCustomAttribute<XmlRootAttribute>()?.Namespace == bodyReader.NamespaceURI);
                            if (eType == null)
                                eType = new ModelSerializationBinder().BindToType(null, bodyReader.LocalName); // Try to find by root element

                            serializer = XmlModelSerializerFactory.Current.CreateSerializer(eType);
                            parameters[pNumber] = serializer.Deserialize(bodyReader);
                        }

                    }
                    else if (contentType?.StartsWith("application/json+sdb-viewmodel") == true && typeof(IdentifiedData).IsAssignableFrom(parm.ParameterType))
                    {
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
                    }
                    else if (contentType?.StartsWith("application/json") == true)
                    {
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
                    }
                    else if (contentType == "application/octet-stream")
                    {
                        parameters[pNumber] = request.Body;
                    }
                    else if (contentType == "application/x-www-form-urlencoded")
                    {
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
                    }
                    else if (contentType != null)// TODO: Binaries
                        throw new InvalidOperationException("Invalid request format");
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

                // Result is serializable
                if (result == null)
                {
                    if (response.StatusCode == 200)
                        response.StatusCode = 204;
                }
                else if (result?.GetType().GetCustomAttribute<XmlTypeAttribute>() != null ||
                    result?.GetType().GetCustomAttribute<JsonObjectAttribute>() != null)
                {
                    // The request was in JSON or the accept is JSON
                    if (accepts?.StartsWith("application/json+sdb-viewmodel") == true &&
                        typeof(IdentifiedData).IsAssignableFrom(result?.GetType()))
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
                        using (StreamWriter sw = new StreamWriter(tms, Encoding.UTF8))
                        using (JsonWriter jsw = new JsonTextWriter(sw))
                        {
                            viewModelSerializer.Serialize(jsw, result as IdentifiedData);
                            jsw.Flush();
                            sw.Flush();
                            response.Body = new MemoryStream(tms.ToArray());
                        }

#if DEBUG
                        this.m_traceSource.TraceInfo("Serialized body of  {0}", result);
#endif
                        contentType = "application/json+sdb-viewmodel";
                    }
                    else if (accepts?.StartsWith("application/json") == true ||
                        contentType?.StartsWith("application/json") == true)
                    {
                        // Prepare the serializer
                        JsonSerializer jsz = new JsonSerializer();
                        jsz.Converters.Add(new StringEnumConverter());

                        // Write json data
                        using (MemoryStream ms = new MemoryStream())
                        using (StreamWriter sw = new StreamWriter(ms, Encoding.UTF8))
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
                    }
                    // The request was in XML and/or the accept is JSON
                    else
                    {
                        XmlSerializer xsz = XmlModelSerializerFactory.Current.CreateSerializer(result.GetType());
                        MemoryStream ms = new MemoryStream();
                        xsz.Serialize(ms, result);
                        contentType = "application/xml";
                        ms.Seek(0, SeekOrigin.Begin);
                        response.Body = ms;
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
                AuthenticationContext.Current = null;
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error, e.ToString());
                throw new Exception("Error serializing response for operation", e);
            }
        }
    }
}
