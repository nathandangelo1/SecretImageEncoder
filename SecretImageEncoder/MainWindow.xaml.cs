using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
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

namespace SecretImageEncoder;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    Ppm_image ppm;
    //static string imgPath;

    //static string header;
    //static string ppmMessage;
    //static string dimensions;
    //static string maxColor;

    //List<byte> paletteBin;
    //static string[] pixelPalette;

    ////static string encodedPixelPalette;

    //static string encodedFilePath;
    //static int msgBitLen;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
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
            ppm.ImageBitmap = LoadPPMImage();

            //imgToEncode.Source = LoadPPMImage(imgPath, imgToEncode).MakeBitmap();
            imgToEncode.Source = ppm.ImageBitmap.MakeBitmap();
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
        FileStream infile = new FileStream(ppm.ImgPath, FileMode.Open);

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

    public BitmapMaker ShowPpmImageBinary(Ppm_image ppm)
    {
        FileStream infile = new FileStream(ppm.ImgPath, FileMode.Open);
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

    private BitmapMaker ShowPpmImageAscii(Ppm_image ppm)
    {
        byte lineFeed = 10;

        //OPEN THE PPM IMAGE
        StreamReader infile = new StreamReader(ppm.ImgPath);

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
            int n = int.Parse(s);
            if (n % 2 == 1) n--;
            ppm.PixelPaletteAscii[i] = n.ToString();

        }

        //PROCESS HEADER PALTTE DATA
        Color[] aryColors = new Color[ppm.PixelPaletteAscii.Length / 3];
        for (int paletteIndex = 0; paletteIndex * 3 < ppm.PixelPaletteAscii.Length - 1; paletteIndex++)
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

    private void btnEncode_Click(object sender, RoutedEventArgs e)
    {
        if (ppm is null)
        {
            ErrorBox.Visibility = Visibility.Visible;
            ErrorBox.Text = "Must select image before encoding";
            return;
        }
        else
        {
            ErrorBox.Text = null;
            string hiddenMsg = txtMessage.Text;

            if (hiddenMsg.Length > 255 || hiddenMsg == "")
            {
                txtMessage.Clear();
                ErrorBox.Text = "Message must be between 1 to 255 characters";
                ErrorBox.Visibility = Visibility.Visible;
                return;
            }
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

            if (ppm.Header == "P3")
            {
                if (EncodeAscii(msgBits))
                {
                    btnSave.Visibility = Visibility.Visible;
                }
            }
            else if (ppm.Header == "P6")
            {
                if (EncodeBinary(msgBits))
                {
                    btnSave.Visibility = Visibility.Visible;
                }
            }
        }
        bool EncodeAscii(bool[] msgBits)
        {
            var pixelData = ppm.PixelPaletteAscii;

            string messageBitS = "";

            // for the length of the message in bits
            for (int i = 0; i < msgBits.Length; i++)
            {
                byte currentByte = byte.Parse(ppm.PixelPaletteAscii[i]);

                // if bit in message equals '1', add 1 to currentByte, making it odd
                if (msgBits[i].Equals(true))
                {
                    messageBitS += "1"; // For testing

                    currentByte++; // oddify

                    //then add to the palette
                    int n = currentByte;
                    ppm.PixelPaletteAscii[i] = n.ToString();
                }
                // else, msgBit=0 ( byte is already set to 'zero' aka Even )
                else
                {
                    messageBitS += "0";
                    int n = currentByte;
                    ppm.PixelPaletteAscii[i] = n.ToString(); ; // add to palette unchanged
                }

            }

            ppm.MsgBitLen = msgBits.Length;

            var dim = ppm.Dimensions.Split();
            int width = int.Parse(dim[0]);
            int height = int.Parse(dim[1]);

            //CREATE A BITMAPMAKER TO HOLD IMAGE DATA
            BitmapMaker bmpMaker = new BitmapMaker(width, height);

            int plotX = 0;
            int plotY = 0;
            int colorIndex = 0;

            //PROCESS HEADER PALETTE DATA
            Color[] aryColors = new Color[ppm.PixelPaletteAscii.Length / 3];

            for (int paletteIndex = 0; paletteIndex * 3 < ppm.PixelPaletteAscii.Length - 1; paletteIndex++)
            {
                int newIndex = paletteIndex * 3;

                aryColors[paletteIndex].A = byte.Parse(ppm.MaxColor);

                aryColors[paletteIndex].R = byte.Parse(ppm.PixelPaletteAscii[newIndex]);

                aryColors[paletteIndex].G = byte.Parse(ppm.PixelPaletteAscii[++newIndex]);

                aryColors[paletteIndex].B = byte.Parse(ppm.PixelPaletteAscii[++newIndex]);
            }//end for

            //LOOPING THORUGH PIXEL DATA TO SET THE PIXELS
            for (int index = 0; index < ppm.PixelPaletteAscii.Length; index++)
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

            ppm.EncodedImageBitmap = bmpMaker;
            //var bmpImage = ppm.EncodedImageBitmap.MakeBitmap();

            imgEncoded.Source = ppm.EncodedImageBitmap.MakeBitmap();

            imgEncoded.Visibility = Visibility.Visible;
            txtHiddenEncodedImageLabel.Visibility = Visibility.Visible;
            return true;
        }
        bool EncodeBinary(bool[] msgBits)
        {
            string messageBitS = "";

            // for the length of the message in bits
            for (int i = 0; i < msgBits.Length; i++)
            {
                var currentByte = ppm.PaletteBinary[i];
                // if bit in message equals '1', add 1 to currentByte, making it odd
                if (msgBits[i].Equals(true))
                {
                    messageBitS += "1"; // For testing

                    currentByte++; // oddify

                    //then add to the palette
                    ppm.PaletteBinary[i] = currentByte;
                }
                // else, msgBit=0 ( byte is already set to 'zero' aka Even )
                else
                {
                    messageBitS += "0";
                    ppm.PaletteBinary[i] = currentByte; // add to palette unchanged
                }

            }

            ppm.MsgBitLen = msgBits.Length;

            var dim = ppm.Dimensions.Split();
            int width = int.Parse(dim[0]);
            int height = int.Parse(dim[1]);

            //CREATE A BITMAPMAKER TO HOLD IMAGE DATA
            BitmapMaker bmpMaker = new BitmapMaker(width, height);

            int plotX = 0;
            int plotY = 0;
            int colorIndex = 0;

            //PROCESS HEADER PALETTE DATA
            Color[] aryColors = new Color[ppm.PaletteBinary.Count / 3];

            for (int paletteIndex = 0; paletteIndex * 3 < ppm.PaletteBinary.Count - 1; paletteIndex++)
            {
                int newIndex = paletteIndex * 3;

                aryColors[paletteIndex].A = byte.Parse(ppm.MaxColor);

                aryColors[paletteIndex].R = ppm.PaletteBinary[newIndex];

                aryColors[paletteIndex].G = ppm.PaletteBinary[++newIndex];

                aryColors[paletteIndex].B = ppm.PaletteBinary[++newIndex];
            }//end for

            //LOOPING THORUGH PIXEL DATA TO SET THE PIXELS
            for (int index = 0; index < ppm.PaletteBinary.Count; index++)
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

            ppm.EncodedImageBitmap = bmpMaker;
            //var bmpImage = ppm.EncodedImageBitmap.MakeBitmap();
            imgEncoded.Source = ppm.EncodedImageBitmap.MakeBitmap();

            imgEncoded.Visibility = Visibility.Visible;
            txtHiddenEncodedImageLabel.Visibility = Visibility.Visible;
            return true;
        }
    }
    private void btnSave_Click(object sender, RoutedEventArgs e)
    {
        var savefile = new SaveFileDialog();
        savefile.DefaultExt = ".ppm";
        savefile.Filter = "PPM Files (.ppm)|*.ppm";
        savefile.Title = "Save PPM File";

        if (savefile.ShowDialog() == true)
        {
            ppm.EncodedFilePath = savefile.FileName;

            if (ppm.Header == "P6")
            {
                SaveP6binary();
            }
            else if (ppm.Header == "P3")
            {
                SaveP3ascii();
            }
        }



        void SaveP3ascii()
        {

            StreamWriter outfile = new StreamWriter(ppm.EncodedFilePath);

            string text = "";

            text += ppm.Header + (char)10;
            text += ppm.PpmMessage + ppm.MsgBitLen.ToString() + (char)10;
            text += ppm.Dimensions + (char)10;
            text += ppm.MaxColor + (char)10;
            char[] buffer1 = text.ToCharArray();

            foreach (char c in buffer1)
            {
                outfile.Write(c);
            }

            foreach (string s in ppm.PixelPaletteAscii)
            {
                outfile.Write(s + (char)10); //Convert  data value to byte type // Write byte to file
            }//end for

            //CLOSE FILE
            outfile.Close();
        }
        void SaveP6binary()
        {


            FileStream outfile = new FileStream(ppm.EncodedFilePath, FileMode.OpenOrCreate);

            string text = "";

            text += ppm.Header + (char)10;
            text += ppm.PpmMessage + ppm.MsgBitLen.ToString() + (char)10;
            text += ppm.Dimensions + (char)10;
            text += ppm.MaxColor + (char)10;
            char[] buffer1 = text.ToCharArray();

            foreach (char c in buffer1)
            {
                outfile.WriteByte((byte)c);
            }

            foreach (byte b in ppm.PaletteBinary)
            {
                outfile.WriteByte((byte)b); //Convert  data value to byte type // Write byte to file
            }//end for
            outfile.Close();
        }
    }

    private void MenuItemDecode_Click(object sender, RoutedEventArgs e)
    {
        byte currentByte;

        if (ppm.EncodedFilePath is null)
        {
            ErrorBox.Text = "Image must be encoded and saved first";
            ErrorBox.Visibility = Visibility.Visible;
            return;
        }
        else
        {
            FileStream infile = new FileStream(ppm.EncodedFilePath, FileMode.Open);
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

            txtHiddenEncodedImageLabel.Visibility = Visibility.Visible;
            txtDecodedMessage.Text = msg;
            txtDecodedMessage.Visibility = Visibility.Visible;

            string ConvertBinaryToText(List<List<int>> seq)
            {
                return new String(seq.Select(s => (char)s.Aggregate((a, b) => a * 2 + b)).ToArray());
            }

            void DecodeAscii(ref List<List<int>> msgBytes)
            {

                //OPEN THE PPM IMAGE
                StreamReader infile2 = new StreamReader(ppm.EncodedFilePath);

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

    //static void Decode()
    //{
    //    byte currentByte;

    //    FileStream infile = new FileStream(encodedFilePath, FileMode.Open);
    //    byte lineFeed = 10;
    //    List<List<int>> msgBytes = new();


    //    string[] metaData = new string[4];
    //    for (int i = 0; i < metaData.Length; i++)
    //    {
    //        currentByte = (byte)infile.ReadByte();
    //        while (currentByte != lineFeed)
    //        {
    //            metaData[i] += (char)currentByte;
    //            currentByte = (byte)infile.ReadByte();
    //        }
    //    }


    //    // get last 5 chars from ppmMessage string(metaData[1])
    //    string temp = metaData[1].Substring(metaData[1].Length - 5, 5);
    //    // remove any non-digit chars, leaving the length of the message
    //    int msgLen = int.Parse(Regex.Replace(temp, @"\D", ""));


    //    if (metaData[0] == "P6")
    //    {

    //        // get first byte of pixelData
    //        currentByte = (byte)infile.ReadByte();

    //        // create list of list for each byte of message
    //        msgBytes = new List<List<int>>();

    //        // for each byte in msg 
    //        for (int i = 0; i < msgLen / 8; i++)
    //        {
    //            //list of bits in a byte
    //            List<int> Byte = new();
    //            for (int j = 0; j < 8; j++)
    //            {
    //                // if bit is odd
    //                if (currentByte % 2 == 1)
    //                {
    //                    Byte.Add(1);
    //                }
    //                // else
    //                else
    //                {
    //                    Byte.Add(0);
    //                }
    //                // Get next byte
    //                currentByte = (byte)infile.ReadByte();
    //            }
    //            //add byte to list of bytes in message
    //            msgBytes.Add(Byte);
    //        }
    //        infile.Close();
    //    }
    //    else //if (metaData[0] == "P3")
    //    {
    //        //byte lineFeed = 10;
    //        infile.Close();
    //        //OPEN THE PPM IMAGE
    //        StreamReader infile2 = new StreamReader(encodedFilePath);

    //        //READ HEADER
    //        header = infile2.ReadLine();
    //        ppmMessage = infile2.ReadLine();
    //        dimensions = infile2.ReadLine();
    //        maxColor = infile2.ReadLine();

    //        var pixelPaletteBuilder = new StringBuilder();

    //        for (int i = 0; i < msgLen; i++)
    //        {
    //            //PROCESS PIXEL DATA
    //            pixelPaletteBuilder.Append(infile2.ReadLine() + "\n");
    //        }

    //        infile.Close();

    //        string paletteString = pixelPaletteBuilder.ToString();

    //        string[] palette = paletteString.Split();

    //        // create list of list for each byte of message
    //        msgBytes = new List<List<int>>();

    //        //pixelPalette = paletteString.Split(); ///???????????

    //        for (int i = 0; i * 8 < (palette.Length - 1); i++)
    //        {
    //            int newIndex = i * 8;
    //            //list of bits in a byte
    //            List<int> Byte = new();
    //            for (int j = 0; j < 8; j++)
    //            {
    //                string s = palette[newIndex + j];
    //                int n = int.Parse(s);
    //                // if bit is odd
    //                if (n % 2 == 1)
    //                {
    //                    Byte.Add(1);
    //                }
    //                // else
    //                else
    //                {
    //                    Byte.Add(0);
    //                }


    //            }
    //            //add byte to list of bytes in message
    //            msgBytes.Add(Byte);


    //        }

    //    }
    //    infile.Close();

    //    string msg = ConvertBinaryToText(msgBytes);

    //    txtDecodedMessage.Text = msg;
    //    txtDecodedMessage.Visibility = Visibility.Visible;

    //    string ConvertBinaryToText(List<List<int>> seq)
    //    {
    //        return new String(seq.Select(s => (char)s.Aggregate((a, b) => a * 2 + b)).ToArray());
    //    }
    //}
}

