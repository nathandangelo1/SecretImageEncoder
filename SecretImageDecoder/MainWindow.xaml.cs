using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SecretImageDecoder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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
                string imgPath = openFileDialog.FileName;
                BitmapMaker preparedImage = LoadPPMImage(imgPath, imgToDecode);
                imgToDecode.Source = preparedImage.MakeBitmap();
                btnSelectImage.Visibility = Visibility.Hidden;
            }
        } //END MENUITEMOPEN_CLICK

        private BitmapMaker LoadPPMImage(string path, Image img)
        {
            string file = "";
            byte lineFeed = 10;
            byte currentByte = default;

            //READING FROM A FILE             
            //CREATE A FILESTREAM OBJECT IN OPEN MODE
            FileStream infile = new FileStream(path, FileMode.Open);

            byte[] header = new byte[2];
            infile.ReadExactly(header, 0, 2);

            string head = "";
            head += (char)header[0];
            head += (char)header[1];

            if (head == "P3")
            {
                infile.Close();
                //SET IMAGE CONTROL TO DISPLAY THE BITMAP
                return ShowPpmImageAscii(path);
            }
            else if (head == "P6")
            {
                infile.Close();
                //SET IMAGE CONTROL TO DISPLAY THE BITMAP
                return ShowPpmImageBinary(path);
            }
            else
            {
                throw new Exception("ERROR File must be in P3 or P6 ppm file format.");
            }
        }

        public BitmapMaker ShowPpmImageBinary(string path)
        {
            FileStream infile = new FileStream(path, FileMode.Open);
            byte currentByte = (byte)infile.ReadByte();
            byte lineFeed = 10;

            // Get Metadata
            // metadata 0 = Header
            // metadata 1 = Message
            // metadata 2 = width and height
            // metadata 3 = alpha value(color intensity)
            string[] metaData = new string[4];
            for (int i = 0; i < metaData.Length; i++)
            {
                while (currentByte != lineFeed)
                {
                    metaData[i] += (char)currentByte;
                    currentByte = (byte)infile.ReadByte();
                }
                currentByte = (byte)infile.ReadByte();
            }

            var header = metaData[0];
            var ppmMessage = metaData[1];
            var dimensions = metaData[2];
            var maxColor = metaData[3];
            string pixelPalette = "";
            pixelPalette += (char)currentByte;
            while (infile.Position < infile.Length)
            {
                //PROCESS PIXEL DATA
                pixelPalette += (char)infile.ReadByte();
            }
            infile.Close();

            //PROCESS HEADER DIMENSIONS                
            string[] aryDimensions = dimensions.Split();
            int width = int.Parse(aryDimensions[0]);
            int height = int.Parse(aryDimensions[1]);

            //PROCESS HEADER PALETTE DATA
            Color[] aryColors = new Color[pixelPalette.Length / 3];

            for (int paletteIndex = 0; paletteIndex * 3 < pixelPalette.Length - 1; paletteIndex++)
            {
                int newIndex = paletteIndex * 3;

                aryColors[paletteIndex].A = byte.Parse(maxColor);

                aryColors[paletteIndex].R = (byte)pixelPalette[newIndex];

                aryColors[paletteIndex].G = (byte)pixelPalette[++newIndex];

                aryColors[paletteIndex].B = (byte)pixelPalette[++newIndex];
            }//end for

            //CREATE A BITMAPMAKER TO HOLD IMAGE DATA
            BitmapMaker bmpMaker = new BitmapMaker(width, height); 
            
            int plotX = 0;
            int plotY = 0;
            int colorIndex = 0;
            //LOOPING THORUGH PIXEL DATA TO SET THE PIXELS
            for (int index = 0; index < pixelPalette.Length; index++)
            {
                Color plotColor = aryColors[colorIndex];
                bmpMaker.SetPixel(plotX, plotY, plotColor);
                plotX++;

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

            return bmpMaker;

            ////SET IMAGE CONTROL TO DISPLAY THE BITMAP
            //imgToEncode.Source = wbmImage;

        }

        private BitmapMaker ShowPpmImageAscii(string path)
        {
            //OPEN THE PPM IMAGE
            StreamReader infile = new StreamReader(path);

            //READ HEADER
            var header = infile.ReadLine();
            var ppmMessage = infile.ReadLine();
            var dimensions = infile.ReadLine();
            var maxColor = infile.ReadLine();

            //PROCESS PIXEL DATA
            string pixelPalette = infile.ReadToEnd();
            infile.Close();

            //PROCESS HEADER DIMENSIONS                
            string[] dimensions1 = dimensions.Split();
            string[] aryPalette = pixelPalette.Split();
            int width = int.Parse(dimensions1[0]);
            int height = int.Parse(dimensions1[1]);

            //PROCESS HEADER PALETTE DATA
            Color[] aryColors = new Color[aryPalette.Length / 3];
            for (int paletteIndex = 0; paletteIndex * 3 < aryPalette.Length - 1; paletteIndex++)
            {
                int newIndex = paletteIndex * 3;

                aryColors[paletteIndex].A = byte.Parse(maxColor);

                aryColors[paletteIndex].R = byte.Parse(aryPalette[newIndex]);

                aryColors[paletteIndex].G = byte.Parse(aryPalette[++newIndex]);

                aryColors[paletteIndex].B = byte.Parse(aryPalette[++newIndex]);
            }//end for

            //CREATE A BITMAPMAKER TO HOLD IMAGE DATA
            BitmapMaker bmpMaker = new BitmapMaker(width, height);

            int plotX = 0;
            int plotY = 0;
            int colorIndex = 0;
            //LOOPING THORUGH PIXEL DATA TO SET THE PIXELS
            for (int index = 0; index < aryPalette.Length; index++)
            {
                Color plotColor = aryColors[colorIndex];
                bmpMaker.SetPixel(plotX, plotY, plotColor);
                plotX++;

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
            return bmpMaker;

        }//end ShowPpmImage

        private void Decode(string path = @"C:\Users\POBOYINSAMSARA\Desktop\Coding\marioCODE.ppm" )
        {
            FileStream infile = new FileStream(path, FileMode.Open);
            byte lineFeed = 10;
            byte currentByte;

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

            string mdOne = metaData[1];
            string temp = "";
            temp = mdOne.Substring(mdOne.Length - 5, 5);
            int msgLen = int.Parse(Regex.Replace(temp, @"\D", ""));


            //get first byte of pixelData
            currentByte = (byte)infile.ReadByte();

            string msg = "";

            // create list for each byte of message
            var msgBytes = new List<List<int>>();

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

            msg = ConvertBinaryToText(msgBytes);


            string ConvertBinaryToText(List<List<int>> seq)
            {
                return new String(seq.Select(s => (char)s.Aggregate((a, b) => a * 2 + b)).ToArray());
            }

            txtMessage.Text = msg;

        } //END DECODE
    }
}
