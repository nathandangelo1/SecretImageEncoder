using Microsoft.Win32;
using SecretImageEncoder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SecretImageDecoder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Ppm_Image ppm;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnDecode_Click(object sender, RoutedEventArgs e)
        {
            Decode();
        }
        private void MenuItemDecode_Click(object sender, RoutedEventArgs e)
        {
            Decode();
        }

        private void MenuItemSelect_Click(object sender, RoutedEventArgs e)
        {
                ErrorBox.Text = "";
                ErrorBox.Visibility = Visibility.Hidden;

                // Create Open Dialog
                OpenFileDialog openFileDialog = new();

                // Setup Params
                openFileDialog.DefaultExt = ".ppm";
                openFileDialog.Filter = "Image Files (.ppm)|*.ppm";

                // Show file dialog
                bool? result = openFileDialog.ShowDialog();

                // Process dialog results
                if (result == true)
                {
                    ppm = new(openFileDialog.FileName);
                    //imgPath = openFileDialog.FileName;
                    ppm.PreEncodedBitmap = LoadPPMImage();

                //imgToEncode.Source = LoadPPMImage(imgPath, imgToEncode).MakeBitmap();
                imgToDecode.Source = ppm.PreEncodedBitmap.MakeBitmap();
                    btnSelectImage.Visibility = Visibility.Hidden;
                }

        } //END MENUITEMOPEN_CLICK

        private BitmapMaker LoadPPMImage()
        {

            string file = "";
            byte lineFeed = 10;
            byte currentByte = default;

            //READING FROM A FILE             
            //CREATE A FILESTREAM OBJECT IN OPEN MODE
            FileStream infile = new FileStream(ppm.OriginalImagePath, FileMode.Open);

            byte[] header = new byte[2];
            infile.ReadExactly(header, 0, 2);

            string head = "";
            head += (char)header[0];
            head += (char)header[1];

            if (head == "P3")
            {
                infile.Close();
                // SET IMAGE CONTROL TO DISPLAY THE BITMAP
                return ShowPpmImageAscii(ppm);
            }
            else if (head == "P6")
            {
                infile.Close();
                // SET IMAGE CONTROL TO DISPLAY THE BITMAP
                return ShowPpmImageBinary(ppm);
            }
            else
            {
                throw new Exception("ERROR File must be in P3 or P6 ppm file format.");
            }
        }

        public BitmapMaker ShowPpmImageBinary(Ppm_Image ppm)
        {
            FileStream infile = new FileStream(ppm.OriginalImagePath, FileMode.Open);
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

            ppm.Header = metaData[0];
            ppm.PpmMessage = metaData[1];
            ppm.Dimensions = metaData[2];
            ppm.MaxColor = metaData[3];

            //pixelPalette = "";

            ppm.PaletteBinary = new List<byte>();
            ////////////////

            // prepping the LSB of message(max256 bytes) to 0 for later encoding
            for (int i = 0; i < 256; i++)
            {
                currentByte = (byte)infile.ReadByte();
                if (currentByte % 2 == 1) currentByte--;

                // PROCESS PIXEL DATA
                ppm.PaletteBinary.Add(currentByte);
            }

            // after 256 bytes just add bytes without modification
            while (infile.Position < infile.Length)
            {
                ppm.PaletteBinary.Add((byte)infile.ReadByte());
            }

            // PROCESS HEADER DIMENSIONS                
            string[] aryDimensions = ppm.Dimensions.Split();
            int width = int.Parse(aryDimensions[0]);
            int height = int.Parse(aryDimensions[1]);

            // PROCESS HEADER PALETTE DATA
            Color[] aryColors = new Color[ppm.PaletteBinary.Count / 3];

            for (int paletteIndex = 0; paletteIndex * 3 < ppm.PaletteBinary.Count - 1; paletteIndex++)
            {
                int newIndex = paletteIndex * 3;

                aryColors[paletteIndex].A = byte.Parse(ppm.MaxColor);
                aryColors[paletteIndex].R = ppm.PaletteBinary[newIndex];
                aryColors[paletteIndex].G = ppm.PaletteBinary[++newIndex];
                aryColors[paletteIndex].B = ppm.PaletteBinary[++newIndex];
            }// end for
            infile.Close();

            // CREATE A BITMAPMAKER TO HOLD IMAGE DATA
            BitmapMaker bmpMaker = new BitmapMaker(width, height);

            int plotX = 0;
            int plotY = 0;
            int colorIndex = 0;

            // LOOPING THORUGH PIXEL DATA TO SET THE PIXELS
            for (int index = 0; index < ppm.PaletteBinary.Count; index++)
            {
                Color plotColor = aryColors[colorIndex];
                bmpMaker.SetPixel(plotX, plotY, plotColor);
                plotX++;

                if (plotX == width)
                {
                    plotX = 0;
                    plotY += 1;
                } // end if
                if (plotY == height)
                {
                    break;
                }
                colorIndex++;
            }// end for

            return bmpMaker;

        }

        private BitmapMaker ShowPpmImageAscii(Ppm_Image ppm)
        {
            byte lineFeed = 10;

            //OPEN THE PPM IMAGE
            StreamReader infile = new StreamReader(ppm.OriginalImagePath);

            //READ HEADER
            ppm.Header = infile.ReadLine();
            ppm.PpmMessage = infile.ReadLine();
            ppm.Dimensions = infile.ReadLine();
            ppm.MaxColor = infile.ReadLine();

            var pixelPaletteBuilder = new StringBuilder();
            //PROCESS PIXEL DATA
            pixelPaletteBuilder.Append(infile.ReadToEnd());

            infile.Close();

            //PROCESS HEADER DIMENSIONS                
            string[] aryDimensions = ppm.Dimensions.Split();
            int width = int.Parse(aryDimensions[0]);
            int height = int.Parse(aryDimensions[1]);

            string paletteString = pixelPaletteBuilder.ToString();

            ppm.PixelPaletteAscii = paletteString.Split(); ///???????????

            for (int i = 0; i < ppm.PixelPaletteAscii.Length - 1; i++)
            {
                string s = ppm.PixelPaletteAscii[i];
                int.TryParse(s, out int n);
                if (n % 2 == 1) n--;
                ppm.PixelPaletteAscii[i] = n.ToString();

            }

            int rem = ppm.PixelPaletteAscii.Length % 3;
            int result = ppm.PixelPaletteAscii.Length - rem;
            //if (rem >= (3 / 2))
            //    result += 3;

            //PROCESS HEADER PALETTE DATA
            Color[] aryColors = new Color[result / 3];
            for (int paletteIndex = 0; paletteIndex * 3 < result; paletteIndex++)
            {
                int newIndex = paletteIndex * 3;

                aryColors[paletteIndex].A = byte.Parse(ppm.MaxColor);

                byte r = byte.Parse(ppm.PixelPaletteAscii[newIndex]);
                if (r % 2 == 1) r--;
                aryColors[paletteIndex].R = r;
                //aryColors[paletteIndex].R = byte.Parse(aryPalette[++newIndex]);

                byte g = byte.Parse(ppm.PixelPaletteAscii[++newIndex]);
                if (g % 2 == 1) g--;
                aryColors[paletteIndex].G = g;
                //aryColors[paletteIndex].G = byte.Parse(aryPalette[++newIndex]);

                byte b = byte.Parse(ppm.PixelPaletteAscii[++newIndex]);
                if (b % 2 == 1) b--;
                aryColors[paletteIndex].B = b;
                //aryColors[paletteIndex].B = byte.Parse(aryPalette[++newIndex]);
            }//end for

            //CREATE A BITMAPMAKER TO HOLD IMAGE DATA
            BitmapMaker bmpMaker = new BitmapMaker(width, height);
            int plotX = 0;
            int plotY = 0;
            int colorIndex = 0;

            //LOOPING THORUGH PIXEL DATA TO SET THE PIXELS
            for (int index = 0; index < ppm.PixelPaletteAscii.Length; index++)
            {
                Color plotColor = aryColors[colorIndex]; bmpMaker.SetPixel(plotX, plotY, plotColor); plotX++;
                if (plotX == width)
                {
                    plotX = 0;
                    plotY += 1;
                }// end if
                if (plotY == height)
                {
                    break;
                }
                colorIndex++;
            }//end for

            //CREATE NEW BITMAP
            WriteableBitmap wbmImage = bmpMaker.MakeBitmap();

            //CREATE NEW BITMAP
            return bmpMaker;

        }//end ShowPpmImage


        private void Decode()
        {
            byte currentByte;

            if (ppm is null || ppm.OriginalImagePath == null)
            {
                ErrorBox.Text = "Select image";
                ErrorBox.Visibility = Visibility.Visible;
                return;
            }
            else
            {
                FileStream infile = new FileStream(ppm.OriginalImagePath, FileMode.Open);
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
                if (!int.TryParse(Regex.Replace(temp, @"\D", ""), out int msgLen))
                {
                    ErrorBox.Text = "Message is empty or in incorrect format";
                    ErrorBox.Visibility = Visibility.Visible;
                    return;
                }

                if (metaData[0] == "P6")
                {
                    DecodeBinary(ref msgBytes);
                }
                else //if (metaData[0] == "P3")
                {
                    infile.Close();
                    DecodeAscii(ref msgBytes);
                }

                string msg = ConvertBinaryToText(msgBytes);

                txtMessage.Text = msg;

                string ConvertBinaryToText(List<List<int>> seq)
                {
                    return new String(seq.Select(s => (char)s.Aggregate((a, b) => a * 2 + b)).ToArray());
                }

                void DecodeAscii(ref List<List<int>> msgBytes)
                {

                    //OPEN THE PPM IMAGE
                    StreamReader infile2 = new StreamReader(ppm.OriginalImagePath);

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

                    //pixelPalette = paletteString.Split(); ///???????????

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
                }

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
            }
        } //END DECODE

    }
}
