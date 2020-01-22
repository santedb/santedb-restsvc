using RestSrvr;
using SanteDB.Core;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Rest.AMI;
using SanteDB.Rest.Common;
using SanteDB.Rest.Common.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Rest.AMI.Resources
{
    /// <summary>
    /// Represents a resource handler which serves out match metadata
    /// </summary>
    public class MatchConfigurationResourceHandler : IApiResourceHandler, IAssociativeResourceHandler
    {
        /// <summary>
        /// Gets the resource name
        /// </summary>
        public string ResourceName => "MatchConfiguration";

        /// <summary>
        /// Gets the type that this returns
        /// </summary>
        public Type Type => typeof(IRecordMatchingConfiguration);

        /// <summary>
        /// Gets the scope
        /// </summary>
        public Type Scope => typeof(IAmiServiceContract);

        /// <summary>
        /// Gets the capabilities of this service
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Search | ResourceCapabilityType.Get;

        /// <summary>
        /// Add an associative entity
        /// </summary>
        public object AddAssociatedEntity(object scopingEntityKey, string propertyName, object scopedItem)
        {
            throw new NotSupportedException("Currently not supported");
        }

        /// <summary>
        /// Create a match configuration
        /// </summary>
        public object Create(object data, bool updateIfExists)
        {
            throw new NotSupportedException("Currently not supported");
        }

        /// <summary>
        /// Get the specified match configuration identifier
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.UnrestrictedMetadata)]
        public object Get(object id, object versionId)
        {
            var service = ApplicationServiceContext.Current.GetService<IRecordMatchingConfigurationService>();
            if (service == null)
                throw new InvalidOperationException("Matching configuration manager is not enabled");
            return service.GetConfiguration(id.ToString());
        }

        /// <summary>
        /// Get an associated entity
        /// </summary>
        [Demand(PermissionPolicyIdentifiers.ReadMetadata), Demand(PermissionPolicyIdentifiers.ReadClinicalData)]
        public object GetAssociatedEntity(object scopingEntity, string propertyName, object subItemKey)
        {
            switch(propertyName)
            {
                case "Act":
                case "Entity":
                    // Sub-item key is the object we want to test the match against
                    var targetKey = Guid.Parse(subItemKey.ToString());

                    try
                    {
                        // Get the target object 
                        dynamic target = null;
                        if (propertyName == "Act")
                            target = ApplicationServiceContext.Current.GetService<IRepositoryService<Act>>().Get(targetKey);
                        else
                            target = ApplicationServiceContext.Current.GetService<IRepositoryService<Entity>>().Get(targetKey);

                        if (target == null)
                            throw new KeyNotFoundException($"Target of match {targetKey} could not be found");

                        // Get the matcher
                        var matcher = ApplicationServiceContext.Current.GetService<IRecordMatchingService>();
                        if (matcher == null)
                            throw new InvalidOperationException("Matcher is not configured on this service");
                        else
                        {
                            object retVal = null;
                            switch (RestOperationContext.Current.IncomingRequest.QueryString["_mode"])
                            {
                                case "block":
                                    retVal = BundleUtil.CreateBundle(matcher.Block(target, scopingEntity.ToString()), 0, 0, true);
                                    break;
                                default:
                                    retVal = (matcher as IMatchReportFactory).CreateMatchReport(target, matcher.Match(target, scopingEntity.ToString()));
                                    break;
                            }
                            return retVal;
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Error executing {scopingEntity} against {subItemKey}", e);
                    }
                default:
                    throw new KeyNotFoundException($"{propertyName} is not valid on this object");
            }
        }

        /// <summary>
        /// Delete a match configuration
        /// </summary>
        public object Obsolete(object key)
        {
            throw new NotSupportedException("Not supported yet");
        }

        /// <summary>
        /// Query for match configurations
        /// </summary>
        public IEnumerable<object> Query(NameValueCollection queryParameters)
        {
            return this.Query(queryParameters, 0, 100, out int t);
        }

        /// <summary>
        /// Query for match configurations
        /// </summary>
        public IEnumerable<object> Query(NameValueCollection queryParameters, int offset, int count, out int totalCount)
        {
            var service = ApplicationServiceContext.Current.GetService<IRecordMatchingConfigurationService>();
            if (service == null)
                throw new InvalidOperationException("Matching configuration service is not enabled");

            totalCount = service.Configurations.Count();
            if (queryParameters.TryGetValue("name", out List<String> values))
                return service.Configurations
                    .Where(o => o == values.First())
                    .Skip(offset)
                    .Take(count)
                    .Select(o => service.GetConfiguration(o))
                    .OfType<Object>();
            else
                return service.Configurations
                    .Skip(offset)
                    .Take(count)
                    .Select(o => service.GetConfiguration(o))
                    .OfType<Object>();
        }

        /// <summary>
        /// Query for associated entities on a particular sub-path
        /// </summary>
        public IEnumerable<object> QueryAssociatedEntities(object scopingEntityKey, string propertyName, NameValueCollection filter, int offset, int count, out int totalCount)
        {
            switch (propertyName)
            {
                case "Act":
                case "Entity":
                    try
                    {
                        // Get the target object 
                        IEnumerable<dynamic> targets = null;
                        if (propertyName == "Act")
                        {
                            var query = QueryExpressionParser.BuildLinqExpression<Act>(filter);
                            targets = ApplicationServiceContext.Current.GetService<IRepositoryService<Act>>().Find(query, offset, count, out totalCount, null);
                        }
                        else
                        {
                            var query = QueryExpressionParser.BuildLinqExpression<Entity>(filter);
                            targets = ApplicationServiceContext.Current.GetService<IRepositoryService<Entity>>().Find(query, offset, count, out totalCount, null);
                        }


                        // Get the matcher
                        var matcher = ApplicationServiceContext.Current.GetService<IRecordMatchingService>();
                        if (matcher == null)
                            throw new InvalidOperationException("Matcher is not configured on this service");
                        else
                        {
                            return targets.Select(o=>matcher.Match(o, scopingEntityKey.ToString()));
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Error executing {scopingEntityKey} against {filter.ToString()}", e);
                    }
                default:
                    throw new KeyNotFoundException($"{propertyName} is not valid on this object");
            }
        }

        /// <summary>
        /// Remove an associated entity
        /// </summary>
        public object RemoveAssociatedEntity(object scopingEntityKey, string propertyName, object subItemKey)
        {
            throw new NotSupportedException("Not supported");
        }

        /// <summary>
        /// Update a match configuration
        /// </summary>
        public object Update(object data)
        {
            throw new NotSupportedException("Not currently supported");
        }
    }
}
