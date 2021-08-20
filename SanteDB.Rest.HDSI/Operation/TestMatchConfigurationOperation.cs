using RestSrvr;
using SanteDB.Core;
using SanteDB.Core.Interop;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using SanteDB.Rest.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SanteDB.Rest.HDSI.Operation
{
    /// <summary>
    /// Test the match configuration REST operation
    /// </summary>
    public class TestMatchConfigurationOperation : IApiChildResourceHandler
    {

        // Config service
        private IRecordMatchingConfigurationService m_configService;

        /// <summary>
        /// Create a new match configuration operation
        /// </summary>
        public TestMatchConfigurationOperation(IRecordMatchingConfigurationService configService)
        {
            this.m_configService = configService;

        }


        /// <summary>
        /// Gets the type to bind to
        /// </summary>
        public Type[] ParentTypes => new Type[] { typeof(IRecordMatchingConfiguration) };

        /// <summary>
        /// Gets the property name
        /// </summary>
        public string Name => "$test";

        /// <summary>
        /// Gets the type that this object interacts with
        /// </summary>
        public Type PropertyType => typeof(object);

        /// <summary>
        /// Gets the capabilities of this property
        /// </summary>
        public ResourceCapabilityType Capabilities => ResourceCapabilityType.Get | ResourceCapabilityType.Search;

        /// <summary>
        /// Test the match configuration is an instance method
        /// </summary>
        public ChildObjectScopeBinding ScopeBinding => ChildObjectScopeBinding.Instance;

        /// <summary>
        /// Add a test? Not supported
        /// </summary>
        public object Add(Type scopingType, object scopingKey, object item)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Test the specified object 
        /// </summary>
        /// <param name="scopingKey">The name of the match configuration</param>
        /// <param name="key">The entity to use as the test object</param>
        public object Get(Type scopingType, object scopingKey, object key)
        {
            // Sub-item key is the object we want to test the match against
            var targetKey = Guid.Parse(key.ToString());

            try
            {
                // Get the target object 
                dynamic target = null;
                var config = this.m_configService.GetConfiguration(scopingKey.ToString());

                if (config.AppliesTo.All(o => typeof(Act).IsAssignableFrom(o)))
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
                    var merger = ApplicationServiceContext.Current.GetService(typeof(IRecordMergingService<>).MakeGenericType(config.AppliesTo.First())) as IRecordMergingService;
                    object retVal = null;
                    switch (RestOperationContext.Current.IncomingRequest.QueryString["_mode"])
                    {
                        case "block":
                            retVal = BundleUtil.CreateBundle(matcher.Block(target, config.Name, merger.GetIgnoredKeys(targetKey)), 0, 0, true);
                            break;
                        default:
                            retVal = (matcher as IMatchReportFactory).CreateMatchReport(target, matcher.Match(target, config.Name, merger.GetIgnoredKeys(targetKey)));
                            break;
                    }
                    return retVal;
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error testing match configuration", e);
            }
        }

        /// <summary>
        /// Test against a series of entities
        /// </summary>
        public IEnumerable<object> Query(Type scopingType, object scopingKey, NameValueCollection filter, int offset, int count, out int totalCount)
        {

            try
            {
                // Get the target object 
                dynamic target = null;
                var config = this.m_configService.GetConfiguration(scopingKey.ToString());

                IEnumerable<dynamic> targets = null;
                if (config.AppliesTo.All(o => typeof(Act).IsAssignableFrom(o)))
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
                    var merger = ApplicationServiceContext.Current.GetService(typeof(IRecordMergingService<>).MakeGenericType(config.AppliesTo.First())) as IRecordMergingService;
                    return targets.Select(o => matcher.Match(o, config.Name, merger.GetIgnoredKeys(o)));
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error testing match configuration", e);
            }
        }

        /// <summary>
        /// Remove a test (not supported)
        /// </summary>
        public object Remove(Type scopingType, object scopingKey, object key)
        {
            throw new NotSupportedException();
        }
    }
}
