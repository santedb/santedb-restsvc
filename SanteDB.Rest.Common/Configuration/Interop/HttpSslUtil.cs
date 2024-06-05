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
 * 
 * User: fyfej
 * Date: 2023-6-21
 */
using System;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Rest.Common.Configuration.Interop
{
    /// <summary>
    /// A utility for binding HTTP certificates 
    /// </summary>
    public static class HttpSslUtil
    {

        /// <summary>
        /// Binder dictionary of all registered binders
        /// </summary>
        private static IDictionary<PlatformID, ISslCertificateBinder> s_binderDictionary = AppDomain.CurrentDomain.GetAllTypes().Where(t => typeof(ISslCertificateBinder).IsAssignableFrom(t) && !t.IsInterface).Select(t => Activator.CreateInstance(t) as ISslCertificateBinder).ToDictionary(o => o.Platform, o => o);

        /// <summary>
        /// Get the current platform's certificate binder
        /// </summary>
        /// <returns></returns>
        public static ISslCertificateBinder GetCurrentPlatformCertificateBinder()
        {
            if (s_binderDictionary.TryGetValue(Environment.OSVersion.Platform, out var binder))
            {
                return binder;
            }
            else
            {
                throw new InvalidOperationException("Cannot locate a SSL certificate binding layer");
            }
        }
    }
}
