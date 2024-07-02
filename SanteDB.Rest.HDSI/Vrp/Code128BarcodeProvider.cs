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
