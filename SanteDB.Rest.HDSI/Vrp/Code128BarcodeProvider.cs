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
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using ZXing;

namespace SanteDB.Rest.HDSI.Vrp
{
    /// <summary>
    /// An implementation of the <see cref="IBarcodeGenerator"/> that will generate Code39 barcodes for data
    /// </summary>
    public class Code128BarcodeProvider : IBarcodeGenerator
    {
        /// <inheritdoc/>
        public string BarcodeAlgorithm => "code128";

        /// <inheritdoc/>
        public Stream Generate(IHasIdentifiers entity, params string[] identityDomains)
        {
            try
            {
                var bcId = entity.Identifiers.Single(o => identityDomains.Contains(o.IdentityDomain.DomainName) && (o.ExpiryDate == null || o.ExpiryDate > DateTimeOffset.Now));

                var barcodeData = bcId.Value;
                if(!String.IsNullOrEmpty(bcId.CheckDigit))
                {
                    barcodeData += $" {bcId.CheckDigit}";
                }
                return this.Generate(Encoding.UTF8.GetBytes(barcodeData));
            }
            catch(Exception e)
            {
                throw new Exception("Cannot generate barcdecode for specified identifier list", e);
            }

        }

        /// <inheritdoc/>
        public Stream Generate(byte[] rawData)
        {
            var writer = new BarcodeWriter<Bitmap>()
            {
                Format = BarcodeFormat.CODE_128,
                Options = new ZXing.Common.EncodingOptions()
                {
                    Width = 300,
                    Height = 50,
                    PureBarcode = false,
                    NoPadding = true
                },
                Renderer = new ZXing.Windows.Compatibility.BitmapRenderer()
            };

            using (var bmp = writer.Write(Encoding.UTF8.GetString(rawData)))
            {
                var retVal = new MemoryStream();
                bmp.Save(retVal, ImageFormat.Png);
                retVal.Seek(0, SeekOrigin.Begin);
                return retVal;
            }

        }
    }
}
