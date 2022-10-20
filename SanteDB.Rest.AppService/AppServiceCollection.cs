using Newtonsoft.Json;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Attributes;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace SanteDB.Rest.AppService
{
    [AddDependentSerializers]
    [XmlType(nameof(AppServiceCollection), Namespace = "http://santedb.org/appService")]
    [JsonObject(nameof(AppServiceCollection))]
    [XmlInclude(typeof(Model.ConfigurationViewModel))]
    [XmlInclude(typeof(AppServiceCollection))]
    [XmlInclude(typeof(Model.Menu))]
    [XmlInclude(typeof(Core.Model.Tickles.Tickle))]
    public class AppServiceCollection : RestCollectionBase
    {
        public AppServiceCollection()
        {
        }

        public AppServiceCollection(IEnumerable<object> collectionItems) : base(collectionItems)
        {
        }

        public AppServiceCollection(IEnumerable<object> collectionItems, int offset, int totalCount) : base(collectionItems, offset, totalCount)
        {
        }
    }
}
