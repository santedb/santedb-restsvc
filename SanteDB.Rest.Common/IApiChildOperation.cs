using Newtonsoft.Json;
using SanteDB.Core.Interop;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Rest.Common
{

    /// <summary>
    /// Gets the operation invokation 
    /// </summary>
    [XmlType(nameof(ApiOperationParameterCollection), Namespace = "http://santedb.org/operation")]
    [XmlRoot(nameof(ApiOperationParameterCollection), Namespace = "http://santedb.org/operation")]
    [JsonObject(nameof(ApiOperationParameterCollection))]
    public class ApiOperationParameterCollection 
    {

        /// <summary>
        /// Gets or sets the parameters
        /// </summary>
        [XmlElement("parameter"), JsonProperty("parameter")]
        public List<ApiOperationParameter> Parameters { get; set; }

        /// <summary>
        /// Try to get the specified <paramref name="parameterName"/> from the parameter list
        /// </summary>
        /// <typeparam name="TValue">The type of value</typeparam>
        /// <param name="parameterName">The name of the parameter</param>
        /// <param name="value">The value of the parameter</param>
        /// <returns></returns>
        public bool TryGet<TValue>(String parameterName, out TValue value)
        {
            var p = this.Parameters?.Find(o => o.Name == parameterName);
            value = (TValue)p?.Value;
            return p != null;
        }
    }

    /// <summary>
    /// REST service fault wrapper
    /// </summary>
    [XmlType(nameof(ApiOperationParameter), Namespace = "http://santedb.org/operation")]
    [JsonObject(nameof(ApiOperationParameter))]
    public class ApiOperationParameter
    {

        /// <summary>
        /// Gets or sets the name of the operation
        /// </summary>
        [XmlElement("name"), JsonProperty("name")]
        public String Name { get; set; }

        /// <summary>
        /// Gets or sets the value of the parameter
        /// </summary>
        [XmlElement("value"), JsonProperty("value")]
        public Object Value { get; set; }

    }

    /// <summary>
    /// Operation that can be invoked on the REST API
    /// </summary>
    public interface IApiChildOperation : IApiChildObject
    {

        /// <summary>
        /// Invoke the specified operation
        /// </summary>
        /// <param name="scopingKey">The key of the scoping object</param>
        /// <param name="scopingType">The type of scope object</param>
        /// <param name="parameters">Parameters to the call</param>
        /// <returns>The called method result</returns>
        object Invoke(Type scopingType, Object scopingKey, ApiOperationParameterCollection parameters);

    }
}
