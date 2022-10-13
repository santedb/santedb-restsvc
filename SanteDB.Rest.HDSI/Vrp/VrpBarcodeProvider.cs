using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using ZXing;
using ZXing.QrCode;

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
        public Stream Generate(IHasIdentifiers entity)
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
            var writer = new BarcodeWriter<Bitmap>()
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new ZXing.Common.EncodingOptions()
                {
                    Height = 380,
                    Width = 380,
                    PureBarcode = true
                }
            };

            using (var bmp = writer.Write($"svrp://{rawData.HexEncode()}"))
            {
                var retVal = new MemoryStream();
                bmp.Save(retVal, ImageFormat.Png);
                retVal.Seek(0, SeekOrigin.Begin);
                return retVal;
            }
        }
    }
}
