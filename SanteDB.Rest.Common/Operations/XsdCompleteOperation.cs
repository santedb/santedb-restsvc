/*
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
 * Date: 2024-12-23
 */
using Newtonsoft.Json;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Xml;
using System.Xml.Serialization;

namespace SanteDB.Rest.Common.Operations
{
    /// <summary>
    /// Performs a schema completion operation based on the XSD path definition in a specified type
    /// </summary>
    public class XsdCompleteOperation : IApiChildOperation
    {

        private readonly Regex m_pathExtract = new Regex("([/@])([^/@]+)", RegexOptions.Compiled);
        private static readonly ModelSerializationBinder m_binder = new ModelSerializationBinder();

        // Documentation
        internal static readonly XmlDocument m_documentation;

        /// <summary>
        /// Initialize documentation
        /// </summary>
        static XsdCompleteOperation()
        {
            var docPath = Path.Combine(Path.GetDirectoryName(typeof(AutoCompleteOperation).Assembly.Location), "SanteDB.Core.Api.xml");
            if (File.Exists(docPath))
            {
                m_documentation = new XmlDocument();
                m_documentation.Load(docPath);
            }
        }

        /// <inheritdoc/>
        public string Name => "xsd-complete";

        /// <inheritdoc/>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        /// <inheritdoc/>
        public Type[] ParentTypes => Type.EmptyTypes;

        /// <inheritdoc/>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {

            if (!parameters.TryGet("path", out string pathExpression)) pathExpression = "/";
            if (parameters.TryGet("type", out string typeStr))
            {
                scopingType = m_binder.BindToType(null, typeStr);
            }
            // Lookup the path and resolve
            IEnumerable<SchemaCompleteInfo> currentScope = new List<SchemaCompleteInfo>()
            {
                new SchemaCompleteInfo() { Type = scopingType, Name = scopingType.GetSerializationName() }
            };

            var pathMatch = this.m_pathExtract.Matches(pathExpression);
            foreach (Match match in pathMatch)
            {
                switch (match.Groups[1].Value)
                {
                    case "/": // element

                        var element = currentScope.FirstOrDefault(o => o.Name == match.Groups[2].Value && !o.IsAttribute);
                        if (element == null)
                        {
                            return currentScope.ToList();
                        }
                        else
                        {
                            currentScope = element.GetCompleteOptions();
                        }
                        break;
                    case "@": // attribute
                        var attribute = currentScope.FirstOrDefault(o => o.Name == match.Groups[2].Value && o.IsAttribute);
                        if (attribute == null)
                        {
                            return currentScope.ToList();
                        }
                        else
                        {
                            return attribute.GetCompleteOptions().ToList();
                        }
                }
            }

            return currentScope.ToList();
        }

        /// <summary>
        /// Schema complete info
        /// </summary>
        private class SchemaCompleteInfo
        {
            /// <summary>
            /// Member info
            /// </summary>
            private readonly MemberInfo m_memberInfo;

            /// <summary>
            /// Serialization ctor
            /// </summary>
            public SchemaCompleteInfo()
            {

            }

            /// <summary>
            /// From attribute
            /// </summary>
            public SchemaCompleteInfo(XmlAttributeAttribute att, PropertyInfo property)
            {
                this.IsAttribute = true;
                this.Name = att.AttributeName;
                this.Type = att.Type ?? property.PropertyType;
                this.IsCollection = typeof(IList).IsAssignableFrom(property.PropertyType);
                this.m_memberInfo = property;

            }

            public SchemaCompleteInfo(XmlElementAttribute ele, PropertyInfo property, bool isChoice)
            {
                this.Name = ele.ElementName;
                this.Namespace = ele.Namespace;
                this.Type = ele.Type ?? property.PropertyType;
                this.IsCollection = typeof(IList).IsAssignableFrom(property.PropertyType);
                this.IsChoice = isChoice;
                this.m_memberInfo = property;
            }

            public SchemaCompleteInfo(XmlEnumAttribute enu, FieldInfo field)
            {
                this.Name = enu?.Name ?? field.Name;
                this.IsValue = true;
                this.Type = typeof(String);
                this.m_memberInfo = field;
            }

            [JsonProperty("name"), XmlIgnore]
            public String Name { get; set; }

            [JsonProperty("attribute"), XmlIgnore]
            public bool IsAttribute { get; set; }

            [JsonIgnore, XmlIgnore]
            public Type Type { get; set; }

            [JsonProperty("collection"), XmlIgnore]
            public bool IsCollection { get; set; }

            [JsonProperty("enum"), XmlIgnore]
            public bool IsValue { get; set; }

            [JsonProperty("choice"), XmlIgnore]
            public bool IsChoice { get; set; }

            [JsonProperty("type"), XmlIgnore]
            public String TypeXml
            {
                get
                {
                    m_binder.BindToName(this.Type.StripGeneric(), out _, out var retVal);
                    return retVal;
                }
                set { }
            }

            /// <summary>
            /// Namespace
            /// </summary>
            [JsonProperty("namespace"), XmlIgnore]
            public String Namespace { get; set; }

            /// <summary>
            /// Get the documentation
            /// </summary>
            [JsonProperty("documentation"), XmlIgnore]
            public String Documentation
            {
                get
                {
                    var documentationTag = this.IsValue ? $"F:{this.m_memberInfo.DeclaringType.FullName}.{this.m_memberInfo.Name}" :
                        this.IsChoice ? $"T:{this.Type.FullName}" :
                        $"P:{this.m_memberInfo.DeclaringType.FullName}.{this.m_memberInfo.Name}";

                    // Output documentation
                    var typeDoc = 
                        AutoCompleteOperation.m_documentation?.SelectSingleNode(String.Format("//*[local-name() = 'member'][@name = '{0}']", documentationTag)) ??
                        m_documentation?.SelectSingleNode(String.Format("//*[local-name() = 'member'][@name = '{0}']", documentationTag));
                    if (typeDoc != null)
                    {
                        var docNode = typeDoc.SelectSingleNode(".//*[local-name() = 'summary']");
                        if (docNode != null)
                        {
                            return docNode.InnerText.Trim();
                        }
                    }
                    return "Documentation could not be found";
                }
            }

            /// <summary>
            /// Get complete options
            /// </summary>
            internal IEnumerable<SchemaCompleteInfo> GetCompleteOptions()
            {
                if (this.Type.IsEnum)
                {
                    foreach (var fi in this.Type.GetFields())
                    {
                        var enu = fi.GetCustomAttribute<XmlEnumAttribute>();
                        yield return new SchemaCompleteInfo(enu, fi);
                    }
                }
                else if(this.Type.StripNullable() == typeof(bool))
                {
                    yield return new SchemaCompleteInfo(new XmlEnumAttribute("false"), typeof(Boolean).GetField(nameof(Boolean.FalseString)));
                    yield return new SchemaCompleteInfo(new XmlEnumAttribute("true"), typeof(Boolean).GetField(nameof(Boolean.TrueString)));
                }
                else
                {
                    foreach (var pi in this.Type.StripGeneric().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        var ele = pi.GetCustomAttributes<XmlElementAttribute>();
                        if (ele.Any())
                        {
                            foreach (var e in ele)
                            {
                                yield return new SchemaCompleteInfo(e, pi, ele.Count() > 1);
                            }
                        }
                        else
                        {
                            var att = pi.GetCustomAttribute<XmlAttributeAttribute>();
                            if (att != null)
                            {
                                yield return new SchemaCompleteInfo(att, pi);
                            }
                        }
                    }
                }
            }
        }
    }
}
