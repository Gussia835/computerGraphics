using System.Windows;

namespace color_converter
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonTask1_Click(object sender, RoutedEventArgs e)
        {
            GrayVariantsWindow task1 = new GrayVariantsWindow();
            task1.Show();
            this.Close();
        }

        private void ButtonTask2_Click(object sender, RoutedEventArgs e)
        {
            RGBHystograms task2 = new RGBHystograms();
            task2.Show();
            this.Close();
        }

        private void ButtonTask3_Click(object sender, RoutedEventArgs e)
        {
            RGBtoHSVconverter task3 = new RGBtoHSVconverter();
            task3.Show();
            this.Close();
        }

    }
}