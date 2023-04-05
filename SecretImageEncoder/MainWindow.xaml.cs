using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;

namespace SecretImageEncoder;

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
            imgToEncode.Source = ppm.LoadPPMImage();
            btnSelectImage.Visibility = Visibility.Hidden;
        }
    } //END MENUITEMOPEN_CLICK

    private void btnEncode_Click(object sender, RoutedEventArgs e)
    {
        if (ppm is null || ppm.OriginalImagePath is null)
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

            ppm.EncodeMessage(hiddenMsg);

            if (ppm.PixelPaletteBinary!=null || ppm.PixelPaletteAscii!=null)
            {
                btnEncode.Visibility = Visibility.Collapsed;
                btnSave.Visibility = Visibility.Visible;
                imgEncoded.Source = ppm.EncodedBitmap.MakeBitmap();

                imgEncoded.Visibility = Visibility.Visible;
                txtHiddenEncodedImageLabel.Visibility = Visibility.Visible;
            }
            else
            {
                return;
            }
        }
    }
    private void btnSave_Click(object sender, RoutedEventArgs e)
    {
        ppm.Save();

        btnTestDecoder.Visibility = Visibility.Visible;
        btnSave.Visibility = Visibility.Collapsed;

    }


    private void MenuItemDecode_Click(object sender, RoutedEventArgs e)
    {
        byte currentByte;

        if (ppm is null || (ppm.PixelPaletteBinary==null && ppm.PixelPaletteAscii==null))
        {
            ErrorBox.Text = "Image must be encoded and saved first";
            ErrorBox.Visibility = Visibility.Visible;
            return;
        }
        else
        {
            if ( ppm.Decode(ppm.EncodedImagePath, out string msg))
            {
                txtDecodedMsgLabel.Visibility = Visibility.Visible;
                txtHiddenEncodedImageLabel.Visibility = Visibility.Visible;
                txtDecodedMessage.Text = msg;
                txtDecodedMessage.Visibility = Visibility.Visible;
                btnTestDecoder.Visibility = Visibility.Collapsed;
                btnReset.Visibility = Visibility.Visible;
            }
        }
    } //END DECODE

    private void MenuItemReset_Click(object sender, RoutedEventArgs e)
    {
        ResetWindow();
    }
    public void ResetWindow()

    {
        ppm.Clear();
        imgToEncode.Source = null;
        imgEncoded.Source = null;
        txtDecodedMessage.Text = "";
        txtMessage.Text = "";
        txtDecodedMessage.Visibility = Visibility.Collapsed;
        txtDecodedMsgLabel.Visibility = Visibility.Collapsed;
        txtHiddenEncodedImageLabel.Visibility = Visibility.Collapsed;
        ErrorBox.Visibility = Visibility.Hidden;

        btnSave.Visibility = Visibility.Collapsed;
        btnTestDecoder.Visibility = Visibility.Collapsed;
        btnReset.Visibility = Visibility.Collapsed;

        btnSelectImage.Visibility = Visibility.Visible;
        btnEncode.Visibility = Visibility.Visible;
    }
    private void MenuItemClose_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }
}

