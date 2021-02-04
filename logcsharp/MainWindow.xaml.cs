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

namespace logcsharp
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

        private void test1button_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteERROR("Click!!");

        }

        private void cbTest2_Checked(object sender, RoutedEventArgs e)
        {
            Log.WriteWARNING("Checked!");
        }

        private void cbTest2_Unchecked(object sender, RoutedEventArgs e)
        {
            Log.WriteINFO("Unchecked");
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Log.WriteDEBUG("Exit.");
        }
    }
}
