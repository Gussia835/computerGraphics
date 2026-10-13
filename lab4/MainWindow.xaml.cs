using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using BezierSpline;

namespace lab4
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

        private void ButtonTask1_Click(object sender, RoutedEventArgs e)
        {
            // FillImage task1 = new FillImage();
            // task1.Show();
            // this.Close();
        }

        private void ButtonTask2_Click(object sender, RoutedEventArgs e)
        {
            // DrawSegment task2 = new DrawSegment();
            // task2.Show();
            // this.Close();
        }

        private void ButtonTask3_Click(object sender, RoutedEventArgs e)
        {
            Bezier task3 = new Bezier();
            task3.Show();
            this.Close();
        }

    }
}
