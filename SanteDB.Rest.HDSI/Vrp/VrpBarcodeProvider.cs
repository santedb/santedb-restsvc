/*
 * Copyright (C) 2021 - 2026, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Services;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Drawing;
using System.IO;
using System.Text;
using ZXing;

namespace SanteDB.Rest.HDSI.Vrp
{
    /// <summary>
    /// Visual Resource Pointer implementation of barcode service
    /// </summary>
    public class VrpBarcodeProvider : IBarcodeGenerator
    {
        /// <summary>
        /// The name of the VRP barcode algorithm
        /// </summary>
        public const string AlgorithmName = "santedb-vrp";
        private readonly IResourcePointerService m_pointerService;

        /// <summary>
        /// Gets the barcode algorithm 
        /// </summary>
        public string BarcodeAlgorithm => AlgorithmName;

        /// <summary>
        /// DI constructor
        /// </summary>
        public VrpBarcodeProvider(IResourcePointerService pointerService)
        {
            this.m_pointerService = pointerService;
        }

        /// <inheritdoc/>
        public Stream Generate(IHasIdentifiers entity, params string[] identityDomains)
        {
            try
            {
                // Generate the pointer
                var identityToken = this.m_pointerService.GeneratePointer(entity);
                return this.Generate(Encoding.UTF8.GetBytes(identityToken));
            }
            catch (Exception e)
            {
                throw new Exception("Cannot generate QR code for specified identifier list", e);
            }
        }

        /// <inheritdoc/>
        public Stream Generate(byte[] rawData)
        {
            // Now generate the token
            var writer = new ZXing.ImageSharp.BarcodeWriter<Rgba32>()
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new ZXing.Common.EncodingOptions()
                {
                    Height = 300,
                    Width = 300,
                    NoPadding = true,
                    PureBarcode = true
                },
            };

            using (var bmp = writer.Write($"svrp://{Encoding.UTF8.GetString(rawData)}"))
            {
                var retVal = new MemoryStream();
                bmp.Save(retVal, new PngEncoder());
                retVal.Seek(0, SeekOrigin.Begin);
                return retVal;
            }
        }
    }
}
