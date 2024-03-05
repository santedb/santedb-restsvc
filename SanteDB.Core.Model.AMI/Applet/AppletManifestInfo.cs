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
using Newtonsoft.Json;
using SanteDB.Core.Applets.Model;
using SanteDB.Core.Model.AMI.Security;
using SanteDB.Core.Security;
using System.Xml.Serialization;

namespace SanteDB.Core.Model.AMI.Applet
{
    /// <summary>
    /// Represents a wrapper for the <see cref="Applets.Model.AppletManifest"/> class.
    /// </summary>
    [XmlType(nameof(AppletManifestInfo), Namespace = "http://santedb.org/ami")]
    [XmlRoot(nameof(AppletManifestInfo), Namespace = "http://santedb.org/ami")]
    [JsonObject(nameof(AppletManifestInfo))]
    public class AppletManifestInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppletManifestInfo"/> class.
        /// </summary>
        public AppletManifestInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppletManifestInfo"/> class
        /// with a specific applet manifest instance.
        /// </summary>
        /// <param name="manifest">The applet manifest metadata instance.</param>
        public AppletManifestInfo(AppletPackage manifest)
        {
            this.AppletInfo = manifest.Meta;

            if (manifest.PublicKey != null)
            {
                this.PublisherData = new X509Certificate2Info(new System.Security.Cryptography.X509Certificates.X509Certificate(manifest.PublicKey));
            }
            else if (manifest.Meta.PublicKeyToken != null &&
                X509CertificateUtils.GetPlatformServiceOrDefault().TryGetCertificate(System.Security.Cryptography.X509Certificates.X509FindType.FindByThumbprint, manifest.Meta.PublicKeyToken, out var cert))
            {
                this.PublisherData = new X509Certificate2Info(cert);
            }
        }

        /// <summary>
        /// Gets the applet information name
        /// </summary>
        [XmlElement("applet"), JsonProperty("applet")]
        public AppletInfo AppletInfo { get; set; }

        /// <summary>
        /// Publisher information if available
        /// </summary>
        [XmlElement("publisher"), JsonProperty("publisher")]
        public X509Certificate2Info PublisherData { get; set; }
    }
}