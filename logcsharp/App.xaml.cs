using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace logcsharp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Log.Setup("test.log", Log.Level.DETAIL, false, 10);
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Log.Terminate();
        }
    }
}
