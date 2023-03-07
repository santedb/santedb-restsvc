using SanteDB.Core.Model.Query;
using System;
using System.Collections.Specialized;
using System.Linq;

namespace SanteDB.Rest.Common
{
    /// <summary>
    /// Utility classes which implement the control against <see cref="IQueryResultSet"/>
    /// </summary>
    public static class QueryControlUtility
    {


        /// <summary>
        /// Apply result instructions
        /// </summary>
        public static IQueryResultSet ApplyResultInstructions(this IQueryResultSet me, NameValueCollection query, out int offset, out int totalCount)
        {
            if (null == me)
            {
                offset = 0;
                totalCount = 0;
                return null;
            }

            // Next sort
            if (query.TryGetValue(QueryControlParameterNames.HttpOrderByParameterName, out var queryList) && me is IOrderableQueryResultSet orderable)
            {
                foreach (var itm in queryList)
                {
                    var sortParts = itm.Split(':');
                    var sortExpr = QueryExpressionParser.BuildPropertySelector(me.ElementType, sortParts[0], false, typeof(Object));
                    if (sortParts.Length == 1 || sortParts[1].Equals("ASC", StringComparison.OrdinalIgnoreCase))
                    {
                        me = orderable.OrderBy(sortExpr);
                    }
                    else
                    {
                        me = orderable.OrderByDescending(sortExpr);
                    }
                }
            }
            // Next state
            if (query.TryGetValue(QueryControlParameterNames.HttpQueryStateParameterName, out queryList) && Guid.TryParse(queryList.First(), out Guid queryId) && queryId != Guid.Empty)
            {
                me = me.AsStateful(queryId);
            }

            // Include total count?
            if (query.TryGetValue(QueryControlParameterNames.HttpIncludeTotalParameterName, out queryList) && Boolean.TryParse(queryList.First(), out bool includeTotal) == true)
            {
                totalCount = me.Count();
            }
            else
            {
                totalCount = 0;
                includeTotal = false;
            }

            // Next offset
            if (query.TryGetValue(QueryControlParameterNames.HttpOffsetParameterName, out queryList) && Int32.TryParse(queryList.First(), out offset))
            {
                me = me.Skip(offset);
            }
            else
            {
                offset = 0;
            }

            if (!query.TryGetValue(QueryControlParameterNames.HttpCountParameterName, out queryList) || !Int32.TryParse(queryList.First(), out int count))
            {
                count = 100;
            }

            if (count == 0) // HACK: no need to re-query the user was just looking for a quick count
            {
                return new MemoryQueryResultSet(new object[0]);
            }
            else
            {
                return me.Take(count);
            }
        }
    }
}
