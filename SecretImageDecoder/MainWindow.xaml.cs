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
            ppm.Decode(ppm.EncodedImagePath, out string? message);
            txtMessage.Text = message;
        }

        private void MenuItemDecode_Click(object sender, RoutedEventArgs e)
        {
            ppm.Decode(ppm.EncodedImagePath, out string? message);
            txtMessage.Text = message;
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
                    ppm.EncodedImagePath = openFileDialog.FileName;
                    imgToDecode.Source = ppm.LoadPPMImage();

                    //imgToEncode.Source = LoadPPMImage(imgPath, imgToEncode).MakeBitmap();
                    //imgToDecode.Source = ppm.PreEncodedBitmap.MakeBitmap();
                    btnSelectImage.Visibility = Visibility.Hidden;
                }

        } //END MENUITEMOPEN_CLICK

        private void MenuItemClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

    }
}
