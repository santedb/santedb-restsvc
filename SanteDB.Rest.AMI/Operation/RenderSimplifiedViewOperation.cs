﻿using RestSrvr;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Templates;
using SanteDB.Core.Templates.Definition;
using SanteDB.Core.Templates.View;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Fault;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SanteDB.Rest.AMI.Operation
{
    /// <summary>
    /// Render a simplified view operation
    /// </summary>
    public class RenderSimplifiedViewOperation : IApiChildOperation
    {
        private readonly IDataTemplateManagementService m_dataTemplateService;

        /// <summary>
        /// DI ctor
        /// </summary>
        public RenderSimplifiedViewOperation(IDataTemplateManagementService dataTemplateManagementService)
        {
            this.m_dataTemplateService = dataTemplateManagementService;
        }

        /// <inheritdoc/>
        public string Name => "render";

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class | ChildObjectScopeBinding.Instance;

        /// <inheritdoc/>
        public Type[] ParentTypes => new Type[] { typeof(DataTemplateDefinition) };

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            try
            {
                if (scopingKey is Guid uuid)
                {
                    var dte = this.m_dataTemplateService.Get(uuid);
                    if (dte == null)
                    {
                        throw new KeyNotFoundException(uuid.ToString());
                    }

                    if (parameters.TryGet("view", out String viewStr) && Enum.TryParse<DataTemplateViewType>(viewStr, out var view))
                    {
                        var viewDef = dte.Views?.Find(o => o.ViewType == view);
                        RestOperationContext.Current.OutgoingResponse.ContentType = "text/html";
                        var ms = new MemoryStream();
                        viewDef.Render(ms);
                        ms.Seek(0, SeekOrigin.Begin);
                        return ms;
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException();
                    }
                }
                else if (scopingKey == null)
                {
                    if (parameters.TryGet("view", out String viewDef) && parameters.TryGet("contentType", out string contentTypeStr) && Enum.TryParse<DataTemplateContentChoice>(contentTypeStr, out var contentType))
                    {
                        switch (contentType)
                        {
                            case DataTemplateContentChoice.div:
                                {
                                    return new MemoryStream(Encoding.UTF8.GetBytes(XElement.Parse(viewDef).ToString()));
                                }
                            case DataTemplateContentChoice.svd:
                                {
                                    var sdl = SimplifiedViewDefinition.Parse(viewDef);
                                    RestOperationContext.Current.OutgoingResponse.ContentType = "text/html";
                                    var ms = new MemoryStream();
                                    using (var xw = XmlWriter.Create(ms, new XmlWriterSettings()
                                    {
                                        Indent = true,
                                        CloseOutput = false
                                    }))
                                    {
                                        sdl.Render(xw);
                                    }
                                    ms.Seek(0, SeekOrigin.Begin);
                                    return ms;// Load and parse
                                }
                            case DataTemplateContentChoice.bin:
                                {
                                    return new MemoryStream(Convert.FromBase64String(viewDef));
                                }
                            default:
                                throw new ArgumentOutOfRangeException("contentType");
                        }

                    }
                    else
                    {
                        throw new ArgumentNullException();
                    }
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            catch(InvalidOperationException e) when (e.InnerException is XmlException xe)
            {
                return new RestServiceFault(xe);
            }
            catch(XmlException e)
            {
                return new RestServiceFault(e);
            }
        }
    }
}