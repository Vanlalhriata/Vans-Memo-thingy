using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.Reflection;
using Microsoft.Shell;

namespace VanToDo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, ISingleInstanceApp
    {
        private void Application_Startup(object xsender, StartupEventArgs e)
        {
        }

        private const string Unique = "My_Unique_Application_String";

        [STAThread]
        public static void Main()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance(Unique))
            {
                var application = new App();

                application.InitializeComponent();
                application.Run();

                // Allow single instance code to perform cleanup operations
                SingleInstance<App>.Cleanup();
            }
        }

        #region ISingleInstanceApp Members

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            // handle command line arguments of second instance
            // ...
            MessageBox.Show("The application is already running. Jiggity check yo self",
                            "Message", MessageBoxButton.OK, MessageBoxImage.Information);

            return true;
        }

        #endregion
    }
}
