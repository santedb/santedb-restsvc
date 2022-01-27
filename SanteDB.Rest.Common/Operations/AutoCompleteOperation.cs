using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Attributes;
using SanteDB.Core.Model.Parameters;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;

namespace SanteDB.Rest.Common.Operations
{
    /// <summary>
    /// Represents basic auto-completion data
    /// </summary>
    [JsonObject(nameof(AutoCompleteTypeInfo))]
    public class AutoCompleteTypeInfo
    {
        /// <summary>
        /// Creates a new instance of the auto complete type information class
        /// </summary>
        public AutoCompleteTypeInfo(Type hostType)
        {
            this.TypeName = hostType.Name;
            this.Type = hostType;
            this.Properties = new List<AutoCompletePropertyInfo>();
            foreach (var prop in hostType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                var existing = this.Properties.Find(o => o.Name == prop.GetSerializationName());
                if (existing == null)
                {
                    this.Properties.Add(new AutoCompletePropertyInfo(prop));
                }
                else
                {
                    existing.UpdateWith(prop);
                }
            }

            this.Properties.RemoveAll(o => String.IsNullOrEmpty(o.Name));
        }

        /// <summary>
        /// Gets the type of property
        /// </summary>
        [JsonIgnore]
        public Type Type { get; }

        /// <summary>
        /// Gets the type this property info describes
        /// </summary>
        [JsonProperty("type")]
        public String TypeName { get; }

        /// <summary>
        /// Gets the auto-complete information for the specified properties in this class
        /// </summary>
        [JsonProperty("properties")]
        public List<AutoCompletePropertyInfo> Properties { get; }
    }

    /// <summary>
    /// Represents a simple class which is the auto-complete information for a property
    /// </summary>
    [JsonObject(nameof(AutoCompletePropertyInfo))]
    public class AutoCompletePropertyInfo
    {
        /// <summary>
        /// Creates a new instance of the auto complete property information class
        /// </summary>
        public AutoCompletePropertyInfo(PropertyInfo propertyInfo)
        {
            this.UpdateWith(propertyInfo);
        }

        /// <summary>
        /// Update hte property with the specified property information from <paramref name="propertyInfo"/>
        /// </summary>
        public void UpdateWith(PropertyInfo propertyInfo)
        {
            this.Name = propertyInfo.GetSerializationName();

            // Is this just XML formatting?
            if (propertyInfo.Name.EndsWith("Xml"))
            {
                propertyInfo = propertyInfo.DeclaringType.GetProperty(propertyInfo.Name.Replace("Xml", ""));
            }

            if (this.TypeName == nameof(Guid) || String.IsNullOrEmpty(this.TypeName))
            {
                this.SourceProperty = propertyInfo;

                this.TypeName = propertyInfo.PropertyType.StripGeneric().Name;
                this.IsCollection = typeof(ICollection).IsAssignableFrom(propertyInfo.PropertyType);

                // Output documentation
                var typeDoc = AutoCompleteOperation.m_documentation?.SelectSingleNode(String.Format("//*[local-name() = 'member'][@name = 'P:{0}.{1}']", propertyInfo.DeclaringType.FullName, propertyInfo.Name));
                if (typeDoc != null)
                {
                    var docNode = typeDoc.SelectSingleNode(".//*[local-name() = 'summary']");
                    if (docNode != null)
                    {
                        this.Documentation = docNode.InnerText;
                    }
                }
                else
                {
                    this.Documentation = $"Documentation could not be loaded";
                }
            }

            var typeClassifier = propertyInfo.PropertyType.StripGeneric().GetCustomAttribute<ClassifierAttribute>();
            if (this.IsCollection && typeClassifier != null)
            {
                var classifierProperty = propertyInfo.PropertyType.StripGeneric().GetProperty(typeClassifier.ClassifierProperty);
                if (classifierProperty != null)
                {
                    this.ClassifierType = classifierProperty.PropertyType.StripGeneric().Name;

                    var sredir = classifierProperty.GetCustomAttribute<SerializationReferenceAttribute>();
                    if (sredir != null)
                    {
                        classifierProperty = propertyInfo.PropertyType.StripGeneric().GetProperty(sredir.RedirectProperty);
                    }

                    var binding = classifierProperty.GetCustomAttribute<BindingAttribute>();
                    if (binding != null)
                    {
                        this.ClassifierValues = binding.Binding.GetFields().Where(r => r.FieldType == typeof(Guid)).Select(o => o.Name).ToArray();
                    }
                }
            }
            else
            {
                // Values
                var binding = propertyInfo.GetCustomAttribute<BindingAttribute>();
                if (binding != null)
                {
                    this.Values = binding.Binding.GetFields().Where(r => r.FieldType == typeof(Guid)).ToDictionary(o => o.GetValue(null).ToString(), o => o.Name);
                }
            }
        }

        /// <summary>
        /// Gets the source property from which this metadata class is derived
        /// </summary>
        [JsonIgnore]
        public PropertyInfo SourceProperty { get; private set; }

        /// <summary>
        /// Gets the name of the property
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; private set; }

        /// <summary>
        /// Gets the name of the type that this property information can store
        /// </summary>
        [JsonProperty("type")]
        public String TypeName { get; private set; }

        /// <summary>
        /// Documentation
        /// </summary>
        [JsonProperty("documentation")]
        public String Documentation { get; set; }

        /// <summary>
        /// True if this property can store more than one instnace
        /// </summary>
        [JsonProperty("isCollection")]
        public bool IsCollection { get; private set; }

        /// <summary>
        /// Gets the type of classifier property
        /// </summary>
        [JsonProperty("classifierType")]
        public String ClassifierType { get; private set; }

        /// <summary>
        /// Gets the classifer values which are allowed on this type
        /// </summary>
        [JsonProperty("classifierValues")]
        public String[] ClassifierValues { get; private set; }

        /// <summary>
        /// Gets the possible values which are allowed
        /// </summary>
        [JsonProperty("values")]
        public Dictionary<String, String> Values { get; private set; }
    }

    /// <summary>
    /// Operation which emits schema and auto-complete information
    /// </summary>
    public class AutoCompleteOperation : IApiChildOperation
    {
        // Property extractor
        private readonly Regex m_propertyExtractor = new Regex(@"^([\:\$]?\(?\w*)((?:\[[^\]]+?)\]|(?:\$\w+)|(?:@\w+)|(?::\(.*?\))|[\?\.\=]?)(.*)$");

        private readonly Regex m_functionExtractor = new Regex(@"^:\((\w*)(?:\|(.*?)\)?|\)(.*?))?$");

        // Documentation
        internal static readonly XmlDocument m_documentation;

        /// <summary>
        /// Gets the scope binding
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Class;

        /// <summary>
        /// Get the parent types for the specifed operation
        /// </summary>
        public Type[] ParentTypes => Type.EmptyTypes;

        /// <summary>
        /// Gets the name of this operation
        /// </summary>
        public string Name => "schema-complete";

        /// <summary>
        /// Load documentation
        /// </summary>
        static AutoCompleteOperation()
        {
            var docPath = Path.Combine(Path.GetDirectoryName(typeof(AutoCompleteOperation).Assembly.Location), "SanteDB.Core.Model.xml");
            if (File.Exists(docPath))
            {
                m_documentation = new XmlDocument();
                m_documentation.Load(docPath);
            }
        }

        /// <summary>
        /// Invoke the operation on the specifed type instance
        /// </summary>
        public object Invoke(Type scopingType, object scopingKey, ParameterCollection parameters)
        {
            if (parameters.TryGet("expression", out String propertyPath))
            {
                parameters.TryGet("vars", out JObject variables);
                return this.FollowPath(scopingType, propertyPath, variables);
            }
            else
            {
                return new AutoCompleteTypeInfo(scopingType);
            }
        }

        /// <summary>
        /// Follow property path
        /// </summary>
        /// TODO: Clean this up
        private object FollowPath(Type scopingType, string propertyPath, JObject variables)
        {
            // Get rid f the .
            if (propertyPath.StartsWith("."))
            {
                propertyPath = propertyPath.Substring(1);
            }

            var retVal = new AutoCompleteTypeInfo(scopingType);
            var propertyExtract = this.m_propertyExtractor.Match(propertyPath);
            if (propertyExtract.Success)
            {
                var property = retVal.Properties.FirstOrDefault(o => o.Name == propertyExtract.Groups[1].Value);

                // Is the property path full?
                if (property == null)
                {
                    // Is it a variable?
                    if (propertyExtract.Groups[1].Value.StartsWith("$"))
                    {
                        var variable = variables[propertyExtract.Groups[1].Value];
                        if (variable != null)
                        {
                            return this.FollowPath(new ModelSerializationBinder().BindToType(null, variable.Value<String>()), propertyExtract.Groups[3].Value, variables);
                        }
                        else
                        {
                            return variables.Values().Select(o => o.Path.Substring(1)).ToArray();
                        }
                    }
                    else if (propertyExtract.Groups[1].Value.StartsWith(":(")) // function
                    {
                        var functionMatch = this.m_functionExtractor.Match(propertyPath);
                        if (functionMatch.Success && (!String.IsNullOrEmpty(functionMatch.Groups[2].Value) || !String.IsNullOrEmpty(functionMatch.Groups[3].Value)))
                        {
                            return this.FollowPath(typeof(Object),
                                String.IsNullOrEmpty(functionMatch.Groups[2].Value) ? functionMatch.Groups[3].Value : functionMatch.Groups[2].Value,
                                variables);
                        }
                        else
                        {
                            return QueryFilterExtensions.GetExtendedFilters().Select(o => o.Name);
                        }
                    }
                    return retVal;
                }
                else if (!String.IsNullOrEmpty(propertyExtract.Groups[2].Value))
                {
                    if (propertyExtract.Groups[2].Value.StartsWith("@")) // We're casting
                    {
                        var modelType = new ModelSerializationBinder().BindToType(null, propertyExtract.Groups[2].Value.Substring(1));
                        if (modelType != null)
                        {
                            return this.FollowPath(modelType, propertyExtract.Groups[3].Value, variables);
                        }
                        else
                        {
                            return this.FollowPath(property.SourceProperty.PropertyType.StripGeneric(), propertyExtract.Groups[3].Value, variables);
                        }
                    }
                    else
                    {
                        return this.FollowPath(property.SourceProperty.PropertyType.StripGeneric(), propertyExtract.Groups[3].Value, variables);
                    }
                }
                else if (propertyExtract.Groups[3].Value.StartsWith("@"))
                {
                    return AppDomain.CurrentDomain.GetAllTypes()
                           .Where(o => property.SourceProperty.PropertyType.StripGeneric().IsAssignableFrom(o))
                           .Select(o => o.GetCustomAttribute<XmlTypeAttribute>()?.TypeName)
                           .OfType<String>()
                           .ToArray();
                }
                else if (propertyExtract.Groups[3].Value.StartsWith("["))
                {
                    if (property.ClassifierValues?.Any() == true)
                    {
                        return property.ClassifierValues;
                    }
                    else
                    {
                        return property;
                    }
                }
                else
                {
                    return retVal;
                }
            }
            else
            {
                return retVal;
            }
        }
    }
}