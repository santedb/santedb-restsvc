/*
 * Copyright (C) 2021 - 2023, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2023-3-10
 */
using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace SanteDB.Rest.Common.Configuration.Interop
{
    /// <summary>
    /// A tool which can bind certificates to an IP address
    /// </summary>
    public interface ISslCertificateBinder
    {

        /// <summary>
        /// Gets the platform on which this binder operates
        /// </summary>
        PlatformID Platform { get; }

        /// <summary>
        /// Unbind the certificate to the specified <paramref name="ipAddress"/>
        /// </summary>
        /// <param name="ipAddress">The IP address for the binding</param>
        /// <param name="port">The port where the certificate should be bound</param>
        /// <param name="hash">The certificate to bind</param>
        /// <param name="storeLocation">The location of the certificate store</param>
        /// <param name="storeName">The name of the certificate store</param>
        void UnbindCertificate(IPAddress ipAddress, int port, byte[] hash, StoreName storeName, StoreLocation storeLocation);

        /// <summary>
        /// Bind the specificed <paramref name="hash"/> to <paramref name="ipAddress"/>
        /// </summary>
        /// <param name="ipAddress">The IP address where the binding should be set</param>
        /// <param name="port">The port of the binding</param>
        /// <param name="hash">The certificate to bind</param>
        /// <param name="storeLocation">The location of the certificate store</param>
        /// <param name="negoatiateClientCert">True if client certs are negotiated</param>
        /// <param name="storeName">The name of the certificate store</param>
        void BindCertificate(IPAddress ipAddress, int port, byte[] hash, bool negoatiateClientCert, StoreName storeName, StoreLocation storeLocation);
    }
}
