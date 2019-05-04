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
        internal static readonly string Prefix = " - " + ((System.Reflection.AssemblyProductAttribute)Attribute.GetCustomAttribute(
            System.Reflection.Assembly.GetExecutingAssembly(), typeof(System.Reflection.AssemblyProductAttribute))).Product;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            foreach (var screen in System.Windows.Forms.Screen.AllScreens)
            {
                var window = new MainWindow();
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

            //ヘルプ表示
            if (JikagakiDesktop.Properties.Settings.Default.StartupHelpEnabled)
            {
                if (MessageBox.Show(JikagakiDesktop.Properties.Settings.Default.StartupHelp + "\r\n次回起動時もこのヘルプを表示する場合は「はい」、\r\n次回から表示しない場合は「いいえ」を押してください。", "使い方" + Prefix, MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    JikagakiDesktop.Properties.Settings.Default.StartupHelpEnabled = false;
                    JikagakiDesktop.Properties.Settings.Default.Save();
                }
            }
        }
    }
}
