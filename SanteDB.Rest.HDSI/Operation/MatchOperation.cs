using SanteDB.Core;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Model.Roles;
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
    public class MatchOperation : IApiChildOperation
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
        public Type[] ParentTypes => new Type[]
        {
            typeof(Patient),
            typeof(Entity),
            typeof(Provider),
            typeof(Place),
            typeof(Organization),
            typeof(Material),
            typeof(ManufacturedMaterial)
        };

        /// <summary>
        /// Property name
        /// </summary>
        public string Name => "match";


        /// <summary>
        /// Get the match report for the specified object
        /// </summary>
        public object Invoke(Type scopingType, object scopingKey, ApiOperationParameterCollection parameters)
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

                // key of match configuration
                //if(!parameters.TryGet<String>("configuration", out String configuration))
                //{
                //    throw new InvalidOperationException("Rqeuired parameter 'configuration' missing");
                //}
                return reportFactory.CreateMatchReport(scopingType, source, this.m_matchingService.Match(source, "org.santedb.matcher.example", null));
            }

            return null;
        }

    }
}
