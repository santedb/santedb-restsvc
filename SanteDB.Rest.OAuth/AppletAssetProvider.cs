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
using SanteDB.Core.Applets;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SanteDB.Rest.OAuth
{
    internal class AppletAssetProvider : IAssetProvider
    {
        readonly ReadonlyAppletCollection _Applets;
        readonly string _LoginAssetPath;

        public AppletAssetProvider(ReadonlyAppletCollection applets)
        {
            _Applets = applets;

            foreach (var applet in _Applets)
            {
                if (null == applet)
                {
                    continue;
                }

                _LoginAssetPath = applet.Settings?.FirstOrDefault(setting => setting.Name == "oauth2.login")?.Value ?? _LoginAssetPath;

                if (null != _LoginAssetPath)
                {
                    break;
                }
            }

            if (string.IsNullOrEmpty(_LoginAssetPath))
            {
                return;
                //throw new ArgumentNullException("Cannot locate an applet or solution with login assets configured. Ensure that one applet has the oauth2.login setting configured.");
            }

            if (!_LoginAssetPath.EndsWith("/"))
            {
                _LoginAssetPath = $"{_LoginAssetPath}/";
            }

        }

        public (Stream content, string mimeType) GetAsset(string id, string locale, IDictionary<string, string> bindingValues)
        {
            if (string.IsNullOrEmpty(id))
            {
                id = "index.html";
            }
            else if (id.StartsWith("/ ")) //Remove a leading slash.
            {
                id = id.Substring(1);
            }

            var assetpath = _LoginAssetPath + id;

            var asset = _Applets.ResolveAsset(assetpath);

            if (null != asset)
            {
                var stream = new MemoryStream(_Applets.RenderAssetContent(asset, locale ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, allowCache: false, bindingParameters: bindingValues));
                return (stream, asset.MimeType);
            }
            else
            {
                throw new FileNotFoundException("Asset not found", assetpath);
            }

        }
    }
}
