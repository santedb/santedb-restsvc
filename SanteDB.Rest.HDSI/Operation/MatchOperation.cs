using SanteDB.Core;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Serialization;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// Represents a match operation 
    /// </summary>
    public class MatchOperation : IRestAssociatedPropertyProvider
    {

        // Matching service
        private IRecordMatchingService m_matchingService;

        /// <summary>
        /// Matching service
        /// </summary>
        public MatchOperation(IRecordMatchingService matchingService)
        {
            this.m_matchingService = matchingService;
        }

        /// <summary>
        /// Gets all the types that this is exposed on
        /// </summary>
        public Type[] Types => ModelSerializationBinder.GetRegisteredTypes().ToArray();

        /// <summary>
        /// Property name
        /// </summary>
        public string PropertyName => "$match";

        /// <summary>
        /// POST to match which is a call to matcher
        /// </summary>
        public object Add(Type sopingType, object scopingKey, object item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Get the match report for the specified object
        /// </summary>
        public object Get(Type scopingType, object scopingKey, object key)
        {
            if (!(scopingKey is Guid uuid) && !Guid.TryParse(scopingKey.ToString(), out uuid))
            {
                throw new ArgumentException(nameof(scopingKey), "Must be UUID");
            }


            if(this.m_matchingService is IMatchReportFactory reportFactory)
            {
                var repoService = ApplicationServiceContext.Current.GetService(typeof(IRepositoryService<>).MakeGenericType(scopingType)) as IRepositoryService;
                var source = repoService.Get(uuid);
                if(source == null)
                {
                    throw new KeyNotFoundException($"{uuid} not found");
                }

                this.m_matchingService.Match(source, key?.ToString(), null);
            }

            return null;
        }

        public IEnumerable<object> Query(Type sopingType, object scopingKey, NameValueCollection filter, int offset, int count, out int totalCount)
        {
            throw new NotImplementedException();
        }

        public object Remove(Type sopingType, object scopingKey, object key)
        {
            throw new NotImplementedException();
        }
    }
}
