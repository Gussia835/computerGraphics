using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace Raster
{
    public partial class MainWindow : Window
    {


        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonTask1_Click(object sender, RoutedEventArgs e)
        {
            FillImage task1 = new FillImage();
            task1.Show();
            this.Close();
        }

        private void ButtonTask2_Click(object sender, RoutedEventArgs e)
        {
            //RGBHystograms task2 = new RGBHystograms();
            //task2.Show();
            //this.Close();
        }

        private void ButtonTask3_Click(object sender, RoutedEventArgs e)
        {
            TriangleRasterizationWindow task3 = new TriangleRasterizationWindow();
            task3.Show();
            this.Close();
        }

    }


}