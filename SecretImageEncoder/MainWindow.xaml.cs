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

    static string imgPath;

    static string header;
    static string ppmMessage;
    static string dimensions;
    static string maxColor;
    static string pixelPalette;
    static string encodedPixelPalette;

    static int msgBitLen;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
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
            imgPath = openFileDialog.FileName;

            imgToEncode.Source = LoadPPMImage(imgPath, imgToEncode).MakeBitmap();
            btnSelectImage.Visibility = Visibility.Hidden;
        }
    } //END MENUITEMOPEN_CLICK

    private BitmapMaker LoadPPMImage(string path, System.Windows.Controls.Image img)
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
        byte currentByte;
        byte lineFeed = 10;

        // Get Metadata
        // metadata 0 = Header
        // metadata 1 = Message
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

        ppmMessage = metaData[1];
        dimensions = metaData[2];
        maxColor = metaData[3];
        pixelPalette = "";

        while (infile.Position < infile.Length)
        {
            currentByte = (byte)infile.ReadByte();
            if (currentByte % 2 == 1) currentByte--;

            //PROCESS PIXEL DATA
            pixelPalette += (char)currentByte;
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
        BitmapMaker bmpMaker = new BitmapMaker(width, height); int plotX = 0;

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

    }

    private BitmapMaker ShowPpmImageAscii(string path)
    {
        ////PROCESS PIXEL DATA
        //pixelPalette = ""; //for encoding
        //string showPalette = ""; // for display

        //while ( infile.EndOfStream == false)
        //{
        //    byte line = byte.Parse(infile.ReadLine());
        //    if (line % 2 == 1 & line!=lineFeed) line--;
        //    pixelPalette += (char)line;
        //    showPalette += line; 
        //    showPalette += (byte)lineFeed;
        //}

        //infile.Close();

        byte lineFeed = 10;

        //OPEN THE PPM IMAGE
        StreamReader infile = new StreamReader(path);

        //READ HEADER
        header = infile.ReadLine();
        ppmMessage = infile.ReadLine();
        dimensions = infile.ReadLine();
        maxColor = infile.ReadLine();

        //PROCESS PIXEL DATA
        string showPalette = infile.ReadToEnd(); 
        
        infile.Close();           
        
        //PROCESS HEADER DIMENSIONS                
        string[] aryDimensions = dimensions.Split();
        string[] aryPalette = showPalette.Split();
        int width = int.Parse(aryDimensions[0]);
        int height = int.Parse(aryDimensions[1]);

        for (int i = 0; i < aryPalette.Length-1; i++)
        {
            byte b = byte.Parse(aryPalette[i]);
            if (b % 2 == 1) b--;
            pixelPalette += (char)b;

        }

        
        //PROCESS HEADER PALTTE DATA
        Color[] aryColors = new Color[aryPalette.Length / 3];
        for (int paletteIndex = 0; paletteIndex * 3 < aryPalette.Length - 1; paletteIndex++)
        {
            int newIndex = paletteIndex * 3; aryColors[paletteIndex].A = byte.Parse(maxColor);
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

    private void 
        btnEncode_Click(object sender, RoutedEventArgs e)
    {
        
        ErrorBox.Text = null;
        string hiddenMsg = txtMessage.Text;

        if (hiddenMsg.Length > 255)
        {
            txtMessage.Clear();
            ErrorBox.Text = "Message must be under 256 characters";
            return;
        }

        byte[] messageBytes = Encoding.ASCII.GetBytes(hiddenMsg);

        // https://stackoverflow.com/questions/2548018/convert-byte-array-to-bit-array  ?????????
        bool[] msgBits = messageBytes.SelectMany(GetBits).ToArray();

        IEnumerable<bool> GetBits(byte b)
        {
            for (int i = 0; i < 8; i++)
            {
                
                yield return (b & 0x80) != 0;
                b *= 2;
            }
        }

        var pixelData = pixelPalette;

        char[] arPixelPalette = pixelPalette.ToCharArray();

        string messageBitS = "";

        // for the length of the message in bits
        for (int i = 0; i < msgBits.Length; i++)
        {
            var currentByte = (byte)pixelPalette[i];
            // if bit in message equals '1', add 1 to currentByte, making it odd
            if (msgBits[i].Equals(true))
            {
                messageBitS += "1"; // For testing

                currentByte++; // oddify

                //then add to the palette
                arPixelPalette[i] = (char)currentByte;
            }
            // else, msgBit=0 ( byte is already set to 'zero' aka Even )
            else
            {
                messageBitS += "0";
                arPixelPalette[i] = (char)currentByte; // add to palette unchanged
            }

        }
        encodedPixelPalette = "";

        for (int i = 0; i < arPixelPalette.Length; i++)
        {
            encodedPixelPalette += arPixelPalette[i];
        }

        msgBitLen = msgBits.Length;

        var dim = dimensions.Split();
        int width = int.Parse(dim[0]);
        int height = int.Parse(dim[1]);

        //CREATE A BITMAPMAKER TO HOLD IMAGE DATA
        BitmapMaker bmpMaker = new BitmapMaker(width, height);

        int plotX = 0;
        int plotY = 0;
        int colorIndex = 0;

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

        var bmpImage = bmpMaker.MakeBitmap();

        imgEncoded.Source = bmpImage;

        imgEncoded.Visibility = Visibility.Visible;
        txtHiddenEncodedImageLabel.Visibility = Visibility.Visible;


    }
    private void btnSave_Click(object sender, RoutedEventArgs e)
    {
        var savefile = new SaveFileDialog();
        savefile.DefaultExt = ".ppm";
        savefile.Filter = "PPM Files (.ppm)|*.ppm";
        savefile.Title = "Save PPM File";

        if (savefile.ShowDialog() == true)
        {
            //WRITING TO A FILE 
            //CREATE A FILESTREAM OBJECT IN OPEN OR CREATE MODE

            var path = savefile.FileName;
            //FileStream outfile = new FileStream($"C:\\Users\\POBOYINSAMSARA\\Desktop\\Coding\\marioCODE.ppm", FileMode.OpenOrCreate);
            FileStream outfile = new FileStream(path, FileMode.OpenOrCreate);

            string text = "";
            text += "P6" + (char)10;
            text += ppmMessage + msgBitLen.ToString() + (char)10;
            text += dimensions + (char)10;
            text += maxColor + (char)10;
            char[] buffer1 = text.ToCharArray();

            foreach (char c in buffer1)
            {
                outfile.WriteByte((byte)c);
            }

            //foreach (char c in encodedPixelPalette)
            //{
            //    text += c;
            //}

            //char[] buffer = text.ToCharArray();

            //LOOP THROUGH CHAR BUFFER CREATED FROM STRING
            for (int index = 0; index < encodedPixelPalette.Length; index += 1)
            {
                outfile.WriteByte((byte)encodedPixelPalette[index]); //Convert  data value to byte type // Write byte to file
            }//end for

            //text += "P6" + (char)10;
            //text += ppmMessage + msgBitLen.ToString() + (char)10;
            //text += dimensions + (char)10;
            //text += maxColor + (char)10;

            //foreach (char c in encodedPixelPalette)
            //{
            //    text += c;
            //}

            //char[] buffer = text.ToCharArray();

            ////LOOP THROUGH CHAR BUFFER CREATED FROM STRING
            //for (int index = 0; index < buffer.Length; index += 1)
            //{
            //    byte data = (byte)buffer[index]; //Convert  date value to byte type
            //    outfile.WriteByte(data); // Write byte to file
            //}//end for

            //CLOSE FILE
            outfile.Close();
        }
    }

    private void MenuItemDecode_Click(object sender, RoutedEventArgs e)
    {
        FileStream infile = new FileStream(@"C:\Users\POBOYINSAMSARA\Desktop\Coding\marioCODE.ppm", FileMode.Open);
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

        //string mdOne = metaData[1]; //
        //string temp = "";
        //temp = mdOne.Substring(mdOne.Length - 5, 5)
        //string mdOne = metaData[1]; //

        // get last 5 chars from ppmMessage string(metaData[1])
        string temp = metaData[1].Substring(metaData[1].Length - 5, 5); 
        // remove any non-digit chars, leaving the length of the message
        int msgLen = int.Parse(Regex.Replace(temp, @"\D", ""));

        // get first byte of pixelData
        currentByte = (byte)infile.ReadByte();

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

        string msg = ConvertBinaryToText(msgBytes);

        txtDecodedMessage.Text = msg;
        txtDecodedMessage.Visibility = Visibility.Visible;

        string ConvertBinaryToText(List<List<int>> seq)
        {
            return new String(seq.Select(s => (char)s.Aggregate((a, b) => a * 2 + b)).ToArray());
        }

    } //END DECODE
}

