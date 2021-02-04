using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
            this.log1task = null;
            this.log1cancel = null;
            this.log2task = null;
            this.log2cancel = null;
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

        private Task log1task;
        private CancellationTokenSource log1cancel;
        private Task log2task;
        private CancellationTokenSource log2cancel;

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            if ( this.log1task == null )
            {
                this.log1cancel = new CancellationTokenSource();
                this.log1task = Task.Factory.StartNew(() => { this.task_outputlog(0, this.log1cancel.Token); });
            } else
            {
                this.log1cancel.Cancel();
                this.log1task.Wait();
                this.log1task = null;
            }
            Log.WriteDEBUG("Exit.");
        }

        private void task_outputlog(int no, CancellationToken cancelt)
        {
            int cnt = 0;
            while ( cancelt.IsCancellationRequested == false )
            {
                Log.WriteINFO($"taskno:{no} count={cnt}");
                cnt++;
                if ( cnt % 15 == 15 )
                {
                    System.Threading.Thread.Sleep(1);
                }
            }
            Log.WriteINFO($"taskno:{no} canceled.");
        }

        private void btntask2_Click(object sender, RoutedEventArgs e)
        {
            if (this.log2task == null)
            {
                this.log2cancel = new CancellationTokenSource();
                this.log2task = Task.Factory.StartNew(() => { this.task_outputlog(1, this.log2cancel.Token); });
            }
            else
            {
                this.log2cancel.Cancel();
                this.log2task.Wait();
                this.log2task = null;
            }
            Log.WriteDEBUG("task2.");
        }
    }
}
