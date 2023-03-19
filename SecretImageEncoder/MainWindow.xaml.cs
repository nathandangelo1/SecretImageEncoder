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

namespace SecretImageEncoder;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{

    static string imgPath;
    static BitmapMaker preparedImage;

    //static WriteableBitmap preparedImage;

    static string header;
    string ppmMessage;
    string dimensions;
    string maxColor;
    //string pixelPalette1;

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
            preparedImage = LoadPPMImage(imgPath, imgToEncode);
            imgToEncode.Source = preparedImage.MakeBitmap();
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

        header = metaData[0];
        ppmMessage = metaData[1];
        dimensions = metaData[2];
        maxColor = metaData[3];
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
            if (aryColors[paletteIndex].A % 2 == 1) aryColors[paletteIndex].A -= 1;

            aryColors[paletteIndex].R = (byte)pixelPalette[newIndex];
            if (aryColors[paletteIndex].R % 2 == 1) aryColors[paletteIndex].R -= 1;

            aryColors[paletteIndex].G = (byte)pixelPalette[++newIndex];
            if (aryColors[paletteIndex].G % 2 == 1) aryColors[paletteIndex].G -= 1;

            aryColors[paletteIndex].B = (byte)pixelPalette[++newIndex];
            if (aryColors[paletteIndex].B % 2 == 1) aryColors[paletteIndex].B -= 1;
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

        ////SET IMAGE CONTROL TO DISPLAY THE BITMAP
        //imgToEncode.Source = wbmImage;

    }

    private BitmapMaker ShowPpmImageAscii(string path)
    {
        //OPEN THE PPM IMAGE
        StreamReader infile = new StreamReader(path);

        //READ HEADER
        header = infile.ReadLine();
        ppmMessage = infile.ReadLine();
        dimensions = infile.ReadLine();
        maxColor = infile.ReadLine();

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
            if (aryColors[paletteIndex].A % 2 == 1) aryColors[paletteIndex].A -= 1;

            aryColors[paletteIndex].R = byte.Parse(aryPalette[newIndex]);
            if (aryColors[paletteIndex].R % 2 == 1) aryColors[paletteIndex].R -= 1;

            aryColors[paletteIndex].G = byte.Parse(aryPalette[++newIndex]);
            if (aryColors[paletteIndex].G % 2 == 1) aryColors[paletteIndex].G -= 1;

            aryColors[paletteIndex].B = byte.Parse(aryPalette[++newIndex]);
            if (aryColors[paletteIndex].B % 2 == 1) aryColors[paletteIndex].B -= 1;
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

    private void btnEncode_Click(object sender, RoutedEventArgs e)
    {
        string hiddenMsg = txtMessage.Text;

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

        var pixelData = preparedImage._pixelData;

        MemoryStream imgStream = new MemoryStream(pixelData);

        //FileStream infile = new FileStream(@"C:\Users\POBOYINSAMSARA\Desktop\Coding\mariotmp", FileMode.Open);
        byte currentByte = (byte)imgStream.ReadByte();
        byte lineFeed = 10;


        byte[] pixelPalette1 = new byte[msgBits.Length];

        string messageBitS = "";

        // for the length of the message in bits
        for (int i = 0; i < msgBits.Length; i++)
        {
            // if bit in message equals '1', add 1 to currentByte, making it odd
            if (msgBits[i].Equals(true))
            {
                messageBitS += "1"; // For testing

                currentByte++; // oddify

                //then add to the palette
                pixelPalette1[i] = currentByte;
            }
            // else, msgBit=0 ( byte is already set to 'zero' aka Even )
            else
            {
                messageBitS += "0";
                pixelPalette1[i] = currentByte; // add to palette unchanged
            }
            currentByte = (byte)imgStream.ReadByte(); // Get next byte
        }

        imgStream.Close();

        for (int i = 0; i < pixelPalette1.Length; i++)
        {
            preparedImage._pixelData[i] = pixelPalette1[i];
        }

        WriteableBitmap bmpImage = (WriteableBitmap)imgToEncode.Source;

        imgEncoded.Source = bmpImage;

        imgEncoded.Visibility = Visibility.Visible;

    }
    private void btnSave_Click(object sender, RoutedEventArgs e)
    {

        var img = preparedImage;
        //WRITING TO A FILE 
        //CREATE A FILESTREAM OBJECT IN OPEN OR CREATE MODE
        FileStream outfile = new FileStream($"C:\\Users\\POBOYINSAMSARA\\Desktop\\Coding\\marioCODE.ppm", FileMode.OpenOrCreate);

        string text = "";

        text += "P6" + (char)10;
        text += ppmMessage + (char)10;
        text += dimensions + (char)10;
        text += maxColor + (char)10;

        foreach (byte b in preparedImage._pixelData)
        {
            text += (char)b;
        }

        char[] buffer = text.ToCharArray();

        //LOOP THROUGH CHAR BUFFER CREATED FROM STRING
        for (int index = 0; index < buffer.Length; index += 1)
        {
            byte data = (byte)buffer[index]; //Convert  date value to byte type
            outfile.WriteByte(data); // Write byte to file
        }//end for

        //CLOSE FILE
        outfile.Close();
    }

    private void MenuItemDecode_Click(object sender, RoutedEventArgs e)
    {
        FileStream infile = new FileStream(@"C:\Users\POBOYINSAMSARA\Desktop\Coding\marioCODE.ppm", FileMode.Open);
        byte currentByte = (byte)infile.ReadByte();
        byte lineFeed = 10;

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

        string dimension = metaData[2];
        string maxColor = metaData[3];

        string msg = "";

        var msgBytes = new List<List<int>>();

        // for
        for (int i = 0; i < 256 / 8; i++)
        {
            List<int> bytes = new();
            for (int j = 0; j < 8; j++)
            {
                // if bit 
                if (currentByte % 2 == 1)
                {
                    bytes.Add(1);
                }
                // else
                else
                {
                    bytes.Add(0);
                }
                currentByte = (byte)infile.ReadByte(); // Get next byte
            }
            msgBytes.Add(bytes);
        }

        infile.Close();

        msg = ConvertBinaryToText(msgBytes);


        string ConvertBinaryToText(List<List<int>> seq)
        {
            return new String(seq.Select(s => (char)s.Aggregate((a, b) => a * 2 + b)).ToArray());
        }


    } //END CLASS
}

