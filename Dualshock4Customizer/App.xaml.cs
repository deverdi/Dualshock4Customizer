using System;
using System.Windows;
using System.Diagnostics;

namespace Dualshock4Customizer
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            try
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                string errorMsg = string.Format("Program baslatma hatasi:\n\n{0}\n\nDetay:\n{1}", 
                    ex.Message, ex.StackTrace);
                MessageBox.Show(errorMsg, "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                Debug.WriteLine("FATAL ERROR: " + ex.ToString());
                Shutdown();
            }
        }
    }
}
