using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace JikagakiDesktop
{
    /// <summary>
    /// WindowCustom.xaml の相互作用ロジック
    /// </summary>
    public partial class WindowCustom : Window
    {
        string Prefix = " - " + ((System.Reflection.AssemblyProductAttribute)Attribute.GetCustomAttribute(
            System.Reflection.Assembly.GetExecutingAssembly(), typeof(System.Reflection.AssemblyProductAttribute))).Product;

        static Dictionary<String, Color> ColorMap = new Dictionary<string, Color>()
        {
            { "B", Colors.Black },
            { "W", Colors.White },
            { "R", Colors.Red },
            { "Y", Colors.Yellow },
            { "C", Colors.Cyan },
            { "G", Colors.Green },
        };
        List<MenuItem> ColorMenuItems = new List<MenuItem>();
        List<MenuItem> ThicknessMenuItems = new List<MenuItem>();

        public WindowCustom()
        {
            InitializeComponent();

            foreach (var item in ContextMenu.Items)
            {
                if (!(item is MenuItem)) continue;
                var menu = (MenuItem)item;
                //色メニューを集める
                if ("Color".Equals(menu.Tag))
                {
                    ColorMenuItems.Add(menu);
                }
                foreach (var sub in menu.Items)
                {
                    if (!(sub is MenuItem)) continue;
                    var subMenu = (MenuItem)sub;
                    //太さメニューを集める
                    if ("Thickness".Equals(subMenu.Tag))
                    {
                        ThicknessMenuItems.Add(subMenu);
                    }
                }
            }

            //背景の透明設定
            InkCanvas.Background = new SolidColorBrush(Color.FromArgb(Properties.Settings.Default.Alpha, 0, 0, 0));

            //ショートカットキー
            PreviewKeyDown += new KeyEventHandler(WindowCustom_PreviewKeyDown);

            //最大最小化時処理登録
            StateChanged += new EventHandler(WindowCustom_StateChanged);
            OnStateChanged(EventArgs.Empty);

            //終了
            Closed += (o, e) => Application.Current.Shutdown();

            //初期色
            foreach (var menu in ColorMenuItems)
            {
                menu.IsChecked = menu.InputGestureText == Properties.Settings.Default.StartupColor;
                if (menu.IsChecked) SetColor(menu.InputGestureText);
            }

            //初期太さ
            foreach (var menu in ThicknessMenuItems)
            {
                menu.IsChecked = menu.InputGestureText == Properties.Settings.Default.StartupWidth;
                if (menu.IsChecked) SetThickness(Double.Parse(menu.InputGestureText));
            }

            //ヘルプ表示
            if (Properties.Settings.Default.StartupHelpEnabled)
            {
                if (MessageBox.Show(Properties.Settings.Default.StartupHelp + "\r\n次回起動時もこのヘルプを表示する場合は「はい」、\r\n次回から表示しない場合は「いいえ」を押してください。", "使い方" + Prefix, MessageBoxButton.YesNo) == MessageBoxResult.No)
                {
                    Properties.Settings.Default.StartupHelpEnabled = false;
                    Properties.Settings.Default.Save();
                }
            }
        }

        void ApplyAllWindows(Action<WindowCustom, bool> action)
        {
            var main = Application.Current.MainWindow;
            foreach (Window window in Application.Current.Windows)
            {
                action((WindowCustom)window, window == main);
            }
        }

        void WindowCustom_StateChanged(object sender, EventArgs e)
        {
            ApplyAllWindows((window, main) =>
            {
                window.WindowState = Application.Current.MainWindow.WindowState;
                window.Title = (window.WindowState == WindowState.Minimized ? "待機中" : "動作中") + Prefix;
            });
        }

        void WindowCustom_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
            case Key.B:
            case Key.W:
            case Key.R:
            case Key.Y:
            case Key.C:
            case Key.G:
            case Key.Space:
                    SetColor(e.Key.ToString());
                break;
            case Key.F1:
                CommandHelp(null, EventArgs.Empty);
                break;
            case Key.Delete:
                CommandClear(null, EventArgs.Empty);
                break;
            case Key.Escape:
                CommandMinimize(null, EventArgs.Empty);
                break;
            case Key.S:
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    CommandSave(null, EventArgs.Empty);
                }
                break;
            case Key.O:
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    CommandOpen(null, EventArgs.Empty);
                }
                break;
            }
        }

        void CommandHelp(object o, EventArgs e)
        {
            Properties.Settings.Default.StartupHelpEnabled = MessageBox.Show(Properties.Settings.Default.StartupHelp + "\r\n次回起動時にこのヘルプを表示させたい場合は「はい」、\r\n表示させたくない場合は「いいえ」を押してください。", "使い方" + Prefix, MessageBoxButton.YesNo) != MessageBoxResult.No;
            Properties.Settings.Default.Save();
        }

        void CommandClear(object o, EventArgs e)
        {
            ApplyAllWindows((window, main) =>
            {
                window.InkCanvas.Strokes.Clear();
            });
        }

        void CommandMinimize(object o, EventArgs e)
        {
            ApplyAllWindows((window, main) => window.WindowState = WindowState.Minimized);
        }

        void CommandQuit(object o, EventArgs e)
        {
            Close();
        }

        void CommandColor(object o, EventArgs e)
        {
            SetColor(((MenuItem)o).InputGestureText);
        }

        void CommandThickness(object o, EventArgs e)
        {
            SetThickness(Double.Parse(((MenuItem)o).InputGestureText));
        }

        void CommandSave(object o, EventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog() { Title = "保存先を選択してください。", DefaultExt = ".jdx", Filter="直書きデスクトップファイル(*.jdx)|*.jdx" };
            if (dialog.ShowDialog().Value)
            {
                using (var stream = System.IO.File.OpenWrite(dialog.FileName))
                {
                    InkCanvas.Strokes.Save(stream);
                }
            }
        }

        void CommandOpen(object o, EventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog() { Title = "読み込むファイルを選択してください。", DefaultExt = ".jdx", Filter = "直書きデスクトップファイル(*.jdx)|*.jdx" };
            if (dialog.ShowDialog().Value)
            {
                using (var stream = dialog.OpenFile())
                {
                    InkCanvas.Strokes = new System.Windows.Ink.StrokeCollection(stream);
                }
            }
        }

        void SetColor(string c)
        {
            Color? color = ColorMap.ContainsKey(c) ? ColorMap[c] : (Color?)null;
            ApplyAllWindows((window, main) =>
            {
                if(color.HasValue)
                {
                    window.ColorMenuItems.ForEach(i => i.IsChecked = i.InputGestureText == c);
                    window.InkCanvas.DefaultDrawingAttributes.Color = color.Value;
                    window.InkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    window.InkCanvas.ForceCursor = true;
                }
                else
                {
                    if(window.InkCanvas.EditingMode == InkCanvasEditingMode.Ink)
                    {
                        window.InkCanvas.EditingMode = InkCanvasEditingMode.EraseByPoint;
                        window.ColorMenuItems.ForEach(i => i.IsChecked = i.InputGestureText == "Space");
                        window.InkCanvas.ForceCursor = false;
                    }
                    else
                    {
                        window.InkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                        var text = ColorMap.Where(p => p.Value == window.InkCanvas.DefaultDrawingAttributes.Color).First().Key;
                        window.ColorMenuItems.ForEach(i => i.IsChecked = i.InputGestureText == text);
                        window.InkCanvas.ForceCursor = true;
                    }
                }
            });
        }

        void SetThickness(double thickness)
        {
            ApplyAllWindows((window, main) =>
            {
                window.InkCanvas.DefaultDrawingAttributes.Width = window.InkCanvas.DefaultDrawingAttributes.Height = thickness;
                window.ThicknessMenuItems.ForEach(i => i.IsChecked = Double.Parse(i.InputGestureText) == thickness);
            });
        }
    }
}
