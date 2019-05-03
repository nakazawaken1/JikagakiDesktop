using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace JikagakiDesktop
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            {
                var window = new WindowCustom();
                if (screen.Primary)
                {
                    MainWindow = window;
                }
                window.Left = screen.Bounds.Left;
                window.Width = screen.Bounds.Width;
                window.Top = screen.Bounds.Top;
                window.Height = screen.Bounds.Height;
                window.ShowInTaskbar = screen.Primary;
                window.Show();
            }
        }
    }
}
