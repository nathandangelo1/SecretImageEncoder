using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SecretImageEncoder
{
    public class Ppm_Image 
    {
        private string? _dimensions;
        private int _width;
        private int _height;
        public int Width { get { return _width; } }
        public int Height { get { return _height; } }
        public string? OriginalImagePath { get; set; }
        public string? EncodedImagePath { get; set; }
        public string? Header { get; set; }
        public string? PpmMessage { get; set; }
        /// <summary>
        /// string containing width + ' ' + height
        /// </summary>
        public string? Dimensions
        {
            set
            {
                _dimensions = value;
                // PROCESS HEADER DIMENSIONS
                if(value is not null)
                {
                    try
                    {
                        string[] aryDimensions = _dimensions.Split();
                        _width = int.Parse(aryDimensions[0]);
                        _height = int.Parse(aryDimensions[1]);
                    }
                    catch
                    {
                        _dimensions = null;
                    }
                }
            }
            get
            {
                return _dimensions;
            }
        }
        public string? MaxColor { get; set; }
        public List<byte>? PaletteBinary { get; set; }
        public string[]? PixelPaletteAscii { get; set; }
        public BitmapMaker? PreEncodedBitmap { get; set; }
        public BitmapMaker? EncodedBitmap { get; set; }
        public int? MsgBitCount { get; set; }
        public bool IsEncoded { get; set; }
        
        public Ppm_Image(string path) => OriginalImagePath = path; // expression bodied constructor
        public void Clear()
        {
            OriginalImagePath = null;
            Header = null;
            PpmMessage = null;
            Dimensions = null;
            MaxColor = null;
            PaletteBinary = null;
            PixelPaletteAscii = null;
            PreEncodedBitmap = null;
            EncodedBitmap = null;
            MsgBitCount = null;
            EncodedImagePath = null;
            IsEncoded = false;
        }
        public WriteableBitmap LoadPPMImage()
        {
            string file = "";
            byte lineFeed = 10;
            byte currentByte = default;

            //READING FROM A FILE             
            //CREATE A FILESTREAM OBJECT IN OPEN MODE
            FileStream infile = new FileStream(OriginalImagePath, FileMode.Open);

            byte[] header = new byte[2];
            infile.ReadExactly(header, 0, 2);

            string head = "";
            head += (char)header[0];
            head += (char)header[1];

            if (head == "P3")
            {
                infile.Close();
                // SET IMAGE CONTROL TO DISPLAY THE BITMAP
                return PpmToWriteableBitmapAscii();
            }
            else if (head == "P6")
            {
                infile.Close();
                // SET IMAGE CONTROL TO DISPLAY THE BITMAP
                return PpmToWriteableBitmapBinary();
            }
            else
            {
                throw new Exception("ERROR File must be in P3 or P6 ppm file format.");
            }

        }
        private WriteableBitmap PpmToWriteableBitmapBinary()
        {
            FileStream infile = new FileStream(OriginalImagePath, FileMode.Open);
            byte currentByte;
            byte lineFeed = 10;

            // Get Metadata
            // metadata 0 = Header
            // metadata 1 = ppmMessage
            // metadata 2 = width and height
            // metadata 3 = alpha value(color intensity)
            string[] metaData = new string[4];

            for (int i = 0; i < metaData.Length; i++)
            {
                currentByte = (byte)infile.ReadByte();
                while (currentByte != lineFeed)
                {
                    metaData[i] += (char)currentByte;
                    currentByte = (byte)infile.ReadByte();
                }
            }

            Header = metaData[0];
            PpmMessage = metaData[1];
            Dimensions = metaData[2];
            MaxColor = metaData[3];

            PaletteBinary = new List<byte>();

            // prepping the LSB of message(max256 bytes) to 0 for later encoding
            for (int i = 0; i < 256; i++)
            {
                currentByte = (byte)infile.ReadByte();
                if (currentByte % 2 == 1) currentByte--;

                // PROCESS PIXEL DATA
                PaletteBinary.Add(currentByte);
            }

            // after 256 bytes just add bytes without modification
            while (infile.Position < infile.Length)
            {
                PaletteBinary.Add((byte)infile.ReadByte());
            }

            //// PROCESS HEADER DIMENSIONS                
            //string[] aryDimensions = Dimensions.Split();
            //int width = int.Parse(aryDimensions[0]);
            //int height = int.Parse(aryDimensions[1]);

            // PROCESS HEADER PALETTE DATA
            Color[] aryColors = new Color[PaletteBinary.Count / 3];

            for (int paletteIndex = 0; paletteIndex * 3 < PaletteBinary.Count - 1; paletteIndex++)
            {
                int newIndex = paletteIndex * 3;

                aryColors[paletteIndex].A = byte.Parse(MaxColor);
                aryColors[paletteIndex].R = PaletteBinary[newIndex];
                aryColors[paletteIndex].G = PaletteBinary[++newIndex];
                aryColors[paletteIndex].B = PaletteBinary[++newIndex];
            }// end for
            infile.Close();

            // CREATE A BITMAPMAKER TO HOLD IMAGE DATA
            BitmapMaker bmpMaker = new BitmapMaker(_width, _height);

            int plotX = 0;
            int plotY = 0;
            int colorIndex = 0;

            // LOOPING THORUGH PIXEL DATA TO SET THE PIXELS
            for (int index = 0; index < PaletteBinary.Count; index++)
            {
                Color plotColor = aryColors[colorIndex];
                bmpMaker.SetPixel(plotX, plotY, plotColor);
                plotX++;

                if (plotX == _width)
                {
                    plotX = 0;
                    plotY += 1;
                } // end if
                if (plotY == _height)
                {
                    break;
                }
                colorIndex++;
            }// end for

            PreEncodedBitmap = bmpMaker;

            //return bmpMaker;
            return bmpMaker.MakeBitmap();
        }
        private WriteableBitmap PpmToWriteableBitmapAscii()
        {
            byte lineFeed = 10;

            //OPEN THE PPM IMAGE
            StreamReader infile = new StreamReader(OriginalImagePath);

            //READ HEADER
            Header = infile.ReadLine();
            PpmMessage = infile.ReadLine();
            Dimensions = infile.ReadLine();
            MaxColor = infile.ReadLine();

            var pixelPaletteBuilder = new StringBuilder();
            //PROCESS PIXEL DATA
            pixelPaletteBuilder.Append(infile.ReadToEnd());

            infile.Close();

            //PROCESS HEADER DIMENSIONS                
            //string[] aryDimensions = Dimensions.Split();
            //int width = int.Parse(aryDimensions[0]);
            //int height = int.Parse(aryDimensions[1]);

            string paletteString = pixelPaletteBuilder.ToString();

            PixelPaletteAscii = paletteString.Split(); ///???????????

            for (int i = 0; i < PixelPaletteAscii.Length - 1; i++)
            {
                string s = PixelPaletteAscii[i];
                int.TryParse(s, out int n);
                if (n % 2 == 1) n--;
                PixelPaletteAscii[i] = n.ToString();

            }

            //PROCESS HEADER PALETTE DATA
            Color[] aryColors = new Color[PixelPaletteAscii.Length / 3];
            for (int paletteIndex = 0; paletteIndex * 3 < PixelPaletteAscii.Length - 1; paletteIndex++)
            {
                int newIndex = paletteIndex * 3;

                aryColors[paletteIndex].A = byte.Parse(MaxColor);

                byte r = byte.Parse(PixelPaletteAscii[newIndex]);
                if (r % 2 == 1) r--;
                aryColors[paletteIndex].R = r;
                //aryColors[paletteIndex].R = byte.Parse(aryPalette[++newIndex]);

                byte g = byte.Parse(PixelPaletteAscii[++newIndex]);
                if (g % 2 == 1) g--;
                aryColors[paletteIndex].G = g;
                //aryColors[paletteIndex].G = byte.Parse(aryPalette[++newIndex]);

                byte b = byte.Parse(PixelPaletteAscii[++newIndex]);
                if (b % 2 == 1) b--;
                aryColors[paletteIndex].B = b;
                //aryColors[paletteIndex].B = byte.Parse(aryPalette[++newIndex]);
            }//end for

            //CREATE A BITMAPMAKER TO HOLD IMAGE DATA
            BitmapMaker bmpMaker = new BitmapMaker(_width, _height);
            int plotX = 0;
            int plotY = 0;
            int colorIndex = 0;

            //LOOPING THORUGH PIXEL DATA TO SET THE PIXELS
            for (int index = 0; index < PixelPaletteAscii.Length; index++)
            {
                Color plotColor = aryColors[colorIndex]; bmpMaker.SetPixel(plotX, plotY, plotColor); plotX++;
                if (plotX == _width)
                {
                    plotX = 0;
                    plotY += 1;
                }// end if
                if (plotY == _height)
                {
                    break;
                }
                colorIndex++;
            }//end for

            PreEncodedBitmap = bmpMaker;

            //CREATE NEW BITMAP
            //WriteableBitmap writeable = bmpMaker.MakeBitmap();

            //CREATE NEW BITMAP
            return bmpMaker.MakeBitmap();

        }//end ShowPpmImage
        /// <summary>
        /// Encodes Ppm message in Bitmap
        /// </summary>
        public void EncodeMessage(string hiddenMsg)
        {
            // convert message string to byte[]
            byte[] messageBytes = Encoding.ASCII.GetBytes(hiddenMsg);

            // Convert message bytes to binary in the form of bool array
            bool[] msgBits = messageBytes.SelectMany(GetBits).ToArray();

            IEnumerable<bool> GetBits(byte b)
            {
                for (int i = 0; i < 8; i++)
                {
                    yield return (b & 0x80) != 0;
                    b *= 2;
                }

            }

            if (Header == "P3") IsEncoded = EncodeAscii(msgBits);

            else if (Header == "P6") IsEncoded = EncodeBinary(msgBits);

            bool EncodeAscii(bool[] msgBits)
            {
                var pixelData = PixelPaletteAscii;

                string messageBitS = "";

                // for the length of the message in bits
                for (int i = 0; i < msgBits.Length; i++)
                {
                    byte currentByte = byte.Parse(PixelPaletteAscii[i]);

                    // if bit in message equals '1', add 1 to currentByte, making it odd
                    if (msgBits[i].Equals(true))
                    {
                        //messageBitS += "1"; // For testing

                        currentByte++; // oddify

                        //then add to the palette
                        int n = currentByte;
                        PixelPaletteAscii[i] = n.ToString();
                    }
                    // else, msgBit=0 ( byte is already set to 'zero' aka Even )
                    else
                    {
                        //messageBitS += "0";
                        int n = currentByte;
                        PixelPaletteAscii[i] = n.ToString(); ; // add to palette unchanged
                    }
                }

                MsgBitCount = msgBits.Length;

                //var dim = Dimensions.Split();
                //int width = int.Parse(dim[0]);
                //int height = int.Parse(dim[1]);

                //CREATE A BITMAPMAKER TO HOLD IMAGE DATA
                BitmapMaker bmpMaker = new BitmapMaker(_width, _height);

                int plotX = 0;
                int plotY = 0;
                int colorIndex = 0;

                //PROCESS HEADER PALETTE DATA
                Color[] aryColors = new Color[PixelPaletteAscii.Length / 3];

                for (int paletteIndex = 0; paletteIndex * 3 < PixelPaletteAscii.Length - 1; paletteIndex++)
                {
                    int newIndex = paletteIndex * 3;

                    aryColors[paletteIndex].A = byte.Parse(MaxColor);

                    aryColors[paletteIndex].R = byte.Parse(PixelPaletteAscii[newIndex]);

                    aryColors[paletteIndex].G = byte.Parse(PixelPaletteAscii[++newIndex]);

                    aryColors[paletteIndex].B = byte.Parse(PixelPaletteAscii[++newIndex]);
                }//end for

                //LOOPING THORUGH PIXEL DATA TO SET THE PIXELS
                for (int index = 0; index < PixelPaletteAscii.Length; index++)
                {
                    Color plotColor = aryColors[colorIndex];
                    bmpMaker.SetPixel(plotX, plotY, plotColor);
                    plotX++;

                    if (plotX == _width)
                    {
                        plotX = 0;
                        plotY += 1;
                    }// end if
                    if (plotY == _height)
                    {
                        break;
                    }
                    colorIndex++;
                }//end for

                EncodedBitmap = bmpMaker;

                return true;
            }

            bool EncodeBinary(bool[] msgBits)
            {
                string messageBitS = "";

                // for the length of the message in bits
                for (int i = 0; i < msgBits.Length; i++)
                {
                    var currentByte = PaletteBinary[i];

                    // if bit in message equals '1', add 1 to currentByte, making it odd
                    if (msgBits[i].Equals(true))
                    {
                        currentByte++; // oddify

                        //then add to the palette
                        PaletteBinary[i] = currentByte;
                    }
                    // else, msgBit=0 ( byte is already set to 'zero' aka Even )
                    else
                    {
                        PaletteBinary[i] = currentByte; // add to palette unchanged
                    }
                }

                MsgBitCount = msgBits.Length;

                //var dim = Dimensions.Split();
                //int width = int.Parse(dim[0]);
                //int height = int.Parse(dim[1]);

                //CREATE A BITMAPMAKER TO HOLD IMAGE DATA
                BitmapMaker bmpMaker = new BitmapMaker(_width, _height);

                int plotX = 0;
                int plotY = 0;
                int colorIndex = 0;

                //PROCESS HEADER PALETTE DATA
                Color[] aryColors = new Color[PaletteBinary.Count / 3];

                for (int paletteIndex = 0; paletteIndex * 3 < PaletteBinary.Count - 1; paletteIndex++)
                {
                    int newIndex = paletteIndex * 3;

                    aryColors[paletteIndex].A = byte.Parse(MaxColor);

                    aryColors[paletteIndex].R = PaletteBinary[newIndex];

                    aryColors[paletteIndex].G = PaletteBinary[++newIndex];

                    aryColors[paletteIndex].B = PaletteBinary[++newIndex];
                }//end for

                //LOOPING THORUGH PIXEL DATA TO SET THE PIXELS
                for (int index = 0; index < PaletteBinary.Count; index++)
                {
                    Color plotColor = aryColors[colorIndex];
                    bmpMaker.SetPixel(plotX, plotY, plotColor);
                    plotX++;

                    if (plotX == _width)
                    {
                        plotX = 0;
                        plotY += 1;
                    }// end if
                    if (plotY == _height)
                    {
                        break;
                    }
                    colorIndex++;
                }//end for

                EncodedBitmap = bmpMaker;

                return true;
            }
        }
        public void Save()
        {
            var savefile = new SaveFileDialog();
            savefile.DefaultExt = ".ppm";
            savefile.Filter = "PPM Files (.ppm)|*.ppm";
            savefile.Title = "Save PPM File";

            if (savefile.ShowDialog() == true)
            {
                EncodedImagePath = savefile.FileName;

                if (Header == "P6")
                {
                    SaveP6binary();
                }
                else if (Header == "P3")
                {
                    SaveP3ascii();
                }
            }

            void SaveP3ascii()
            {
                StreamWriter outfile = new StreamWriter(EncodedImagePath);

                string text = "";

                text += Header + (char)10;
                text += PpmMessage + MsgBitCount.ToString() + (char)10;
                text += Dimensions + (char)10;
                text += MaxColor + (char)10;
                char[] buffer1 = text.ToCharArray();

                foreach (char c in buffer1)
                {
                    outfile.Write(c);
                }

                foreach (string s in PixelPaletteAscii)
                {
                    outfile.Write(s + (char)10); //Convert  data value to byte type // Write byte to file
                }//end for

                //CLOSE FILE
                outfile.Close();
            }
            void SaveP6binary()
            {
                FileStream outfile = new FileStream(EncodedImagePath, FileMode.OpenOrCreate);

                string text = "";

                text += Header + (char)10;
                text += PpmMessage + MsgBitCount.ToString() + (char)10;
                text += Dimensions + (char)10;
                text += MaxColor + (char)10;
                char[] buffer1 = text.ToCharArray();

                foreach (char c in buffer1)
                {
                    outfile.WriteByte((byte)c);
                }

                foreach (byte b in PaletteBinary)
                {
                    outfile.WriteByte((byte)b); //Convert  data value to byte type // Write byte to file
                }//end for

                outfile.Close();

            }// END SAVEBINARY

        } // END SAVE
        /// <summary>
        /// returns true if Decode successful
        /// </summary>
        public bool Decode(out string? message)
        {
            byte currentByte;

            if (!IsEncoded)
            {
                message = null;
                return false;
            }

            FileStream infile = new FileStream(EncodedImagePath, FileMode.Open);
            byte lineFeed = 10;
            List<List<int>> msgBytes = new();


            string[] metaData = new string[4];
            for (int i = 0; i < metaData.Length; i++)
            {
                currentByte = (byte)infile.ReadByte();
                while (currentByte != lineFeed)
                {
                    metaData[i] += (char)currentByte;
                    currentByte = (byte)infile.ReadByte();
                }
            }

            // get last 5 chars from ppmMessage string(metaData[1])
            string temp = metaData[1].Substring(metaData[1].Length - 5, 5);
            // remove any non-digit chars, leaving the length of the message
            int.TryParse(Regex.Replace(temp, @"\D", ""), out int msgLen);


            if (metaData[0] == "P6")
            {
                DecodeBinary(ref msgBytes);
            }
            else // P3
            {
                infile.Close();
                DecodeAscii(ref msgBytes);
            }

            message = ConvertBinaryToText(msgBytes);
            return true;

            string ConvertBinaryToText(List<List<int>> seq)
            {
                return new String(seq.Select(s => (char)s.Aggregate((a, b) => a * 2 + b)).ToArray());
            }

            void DecodeAscii(ref List<List<int>> msgBytes)
            {
                //OPEN THE PPM IMAGE
                StreamReader infile2 = new StreamReader(EncodedImagePath);

                //READ HEADER
                _ = infile2.ReadLine();
                _ = infile2.ReadLine();
                _ = infile2.ReadLine();
                _ = infile2.ReadLine();

                var pixelPaletteBuilder = new StringBuilder();

                for (int i = 0; i < msgLen; i++)
                {
                    //PROCESS PIXEL DATA
                    pixelPaletteBuilder.Append(infile2.ReadLine() + "\n");
                }

                infile2.Close();

                string paletteString = pixelPaletteBuilder.ToString();

                string[] palette = paletteString.Split();

                // create list of list for each byte of message
                msgBytes = new List<List<int>>();

                for (int i = 0; i * 8 < (palette.Length - 1); i++)
                {
                    int newIndex = i * 8;
                    //list of bits in a byte
                    List<int> Byte = new();
                    for (int j = 0; j < 8; j++)
                    {
                        string s = palette[newIndex + j];
                        int n = int.Parse(s);
                        // if bit is odd
                        if (n % 2 == 1)
                        {
                            Byte.Add(1);
                        }
                        // else
                        else
                        {
                            Byte.Add(0);
                        }
                    }
                    //add byte to list of bytes in message
                    msgBytes.Add(Byte);

                }
            }// end decode ascii

            void DecodeBinary(ref List<List<int>> msgBytes)
            {
                // get first byte of pixelData
                currentByte = (byte)infile.ReadByte();

                // create list of list for each byte of message
                msgBytes = new List<List<int>>();

                // for each byte in msg 
                for (int i = 0; i < msgLen / 8; i++)
                {
                    //list of bits in a byte
                    List<int> Byte = new();
                    for (int j = 0; j < 8; j++)
                    {
                        // if bit is odd
                        if (currentByte % 2 == 1)
                        {
                            Byte.Add(1);
                        }
                        // else
                        else
                        {
                            Byte.Add(0);
                        }
                        // Get next byte
                        currentByte = (byte)infile.ReadByte();
                    }
                    //add byte to list of bytes in message
                    msgBytes.Add(Byte);
                }
                infile.Close();
            }

        } // END DECODE
    } // END CLASS
} // END NAMESPACE

