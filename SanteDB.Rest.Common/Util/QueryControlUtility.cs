/*
 * Copyright (C) 2021 - 2024, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 */
using SanteDB.Core.Model.Query;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Linq.Expressions;

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
            Expression sortExpr = null;
            bool sortAsc = true;
            if (query.TryGetValue(QueryControlParameterNames.HttpOrderByParameterName, out var queryList) && me is IOrderableQueryResultSet orderable)
            {
                foreach (var itm in queryList)
                {
                    var sortParts = itm.Split(':');
                    sortExpr = QueryExpressionParser.BuildPropertySelector(me.ElementType, sortParts[0], false, typeof(Object));
                    if (sortParts.Length == 1 || sortParts[1].Equals("ASC", StringComparison.OrdinalIgnoreCase))
                    {
                        me = orderable.OrderBy(sortExpr);
                    }
                    else
                    {
                        me = orderable.OrderByDescending(sortExpr);
                        sortAsc = false;
                    }
                }
            }
            // Next state
            if (query.TryGetValue(QueryControlParameterNames.HttpQueryStateParameterName, out queryList) && Guid.TryParse(queryList.First(), out Guid queryId) && queryId != Guid.Empty)
            {
                me = me.AsStateful(queryId);
                // HACK: AsStateful() uses an IN() function so we need to resort

            }

            // Include total count?
            if (query.TryGetValue(QueryControlParameterNames.HttpIncludeTotalParameterName, out queryList) && Boolean.TryParse(queryList.First(), out bool includeTotal) && includeTotal)
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
