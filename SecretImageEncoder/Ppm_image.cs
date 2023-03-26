using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Documents.Serialization;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Color = System.Windows.Media.Color;

namespace SecretImageEncoder
{
    public class Ppm_image
    {
        private string _imgPath;

        private string _header;
        private string _ppmMessage;
        private string _dimensions;
        private string _maxColor;

        private List<byte> _paletteBinary;
        private string[] _pixelPaletteAscii;

        //static string encodedPixelPalette;

        private string _encodedFilePath;
        private int _msgBitLen;

        public string ImgPath { get; set; }
        public string? Header { get; set; }
        public string? PpmMessage { get; set; }
        public string? Dimensions { get; set; }
        public string? MaxColor { get; set; }
        public List<byte>? PaletteBinary { get; set; }
        public string[]? PixelPaletteAscii { get; set;}

        public BitmapMaker? ImageBitmap { get; set; }
        public BitmapMaker? EncodedImageBitmap { get; set; }
        public int? MsgBitLen { get; set; }
        public string? EncodedFilePath { get; set; }

        public Ppm_image(string path)
        {
            ImgPath = path;
        }
    }
}
