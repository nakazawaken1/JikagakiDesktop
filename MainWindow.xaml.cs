using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace JikagakiDesktop
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        readonly static Dictionary<String, Color> ColorMap = new Dictionary<string, Color>()
        {
            { "B", Colors.Black },
            { "W", Colors.White },
            { "R", Colors.Red },
            { "Y", Colors.Yellow },
            { "C", Colors.Cyan },
            { "G", Colors.Green },
        };
        readonly List<MenuItem> ToolMenuItems = new List<MenuItem>();
        readonly List<MenuItem> ColorMenuItems = new List<MenuItem>();
        readonly List<MenuItem> ThicknessMenuItems = new List<MenuItem>();
        Action<Point> StartPoint = null;
        Action<Point> MovePoint = null;
        Action<Point> EndPoint = null;
        Point Start = default(Point);
        InkCanvasEditingMode backup;
        bool drawing = false;

        public MainWindow()
        {
            InitializeComponent();

            foreach (var item in ContextMenu.Items)
            {
                if (!(item is MenuItem)) continue;
                var menu = (MenuItem)item;
                //ツールメニューを集める
                if ("Tool".Equals(menu.Tag))
                {
                    ToolMenuItems.Add(menu);
                }
                //色メニューを集める
                else if ("Color".Equals(menu.Tag))
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

            //初期設定
            OnStateChanged(EventArgs.Empty);
            SetTool(Properties.Settings.Default.StartupTool);
            SetColor(Properties.Settings.Default.StartupColor);
            SetThickness(Properties.Settings.Default.StartupWidth);
        }

        void ApplyAllWindows(Action<MainWindow, bool> action)
        {
            var main = Application.Current.MainWindow;
            foreach (Window window in Application.Current.Windows)
            {
                action((MainWindow)window, window == main);
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            Debug.WriteLine("Window_StateChanged");
            ApplyAllWindows((window, main) =>
            {
                window.WindowState = Application.Current.MainWindow.WindowState;
                window.Title = (window.WindowState == WindowState.Minimized ? "待機中" : "動作中") + App.Prefix;
            });
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            Debug.WriteLine("Window_PreviewKeyDown");
            switch (e.Key)
            {
                case Key.P:
                case Key.L:
                case Key.F:
                case Key.D:
                    SetTool(e.Key.ToString());
                    break;
                case Key.B:
                case Key.W:
                case Key.R:
                case Key.Y:
                case Key.C:
                case Key.G:
                case Key.Space:
                    SetColor(e.Key.ToString());
                    break;
                case Key.D1:
                case Key.D2:
                case Key.D3:
                case Key.D4:
                case Key.D5:
                case Key.D6:
                case Key.D7:
                case Key.D8:
                case Key.D9:
                    SetThickness(Double.Parse(e.Key.ToString().Substring(1)));
                    break;
                case Key.Z:
                    if (Keyboard.Modifiers == ModifierKeys.Control)
                    {
                        CommandUndo(null, EventArgs.Empty);
                    }
                    break;
                case Key.Delete:
                    CommandClear(null, EventArgs.Empty);
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
                case Key.Escape:
                    CommandMinimize(null, EventArgs.Empty);
                    break;
                case Key.F1:
                    CommandHelp(null, EventArgs.Empty);
                    break;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Debug.WriteLine("Window_Closed");
            Application.Current.Shutdown();
        }

        void CommandHelp(object o, EventArgs e)
        {
            Properties.Settings.Default.StartupHelpEnabled = MessageBox.Show(Properties.Settings.Default.StartupHelp + "\r\n次回起動時にこのヘルプを表示させたい場合は「はい」、\r\n表示させたくない場合は「いいえ」を押してください。", "使い方" + App.Prefix, MessageBoxButton.YesNo) != MessageBoxResult.No;
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

        void CommandTool(object o, EventArgs e)
        {
            SetTool(((MenuItem)o).InputGestureText);
        }

        void CommandColor(object o, EventArgs e)
        {
            SetColor(((MenuItem)o).InputGestureText);
        }

        void CommandThickness(object o, EventArgs e)
        {
            SetThickness(Double.Parse(((MenuItem)o).InputGestureText));
        }

        void CommandUndo(object o, EventArgs e)
        {
            if(InkCanvas.Strokes.Count > 0) InkCanvas.Strokes.RemoveAt(InkCanvas.Strokes.Count - 1);
        }

        void CommandSave(object o, EventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog() { Title = "保存先を選択してください。", DefaultExt = ".jdx", Filter = "直書きデスクトップファイル(*.jdx)|*.jdx" };
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
                if (color.HasValue)
                {
                    window.ColorMenuItems.ForEach(i => i.IsChecked = i.InputGestureText == c);
                    window.InkCanvas.DefaultDrawingAttributes.Color = color.Value;
                    window.InkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                    window.InkCanvas.ForceCursor = true;
                }
                else
                {
                    if (window.InkCanvas.EditingMode == InkCanvasEditingMode.Ink)
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

        void SetTool(string tool)
        {
            ApplyAllWindows((window, main) =>
            {
                switch (tool)
                {
                    case "P":
                        window.StartPoint = window.MovePoint = window.EndPoint = null;
                        break;
                    case "L":
                        window.StartPoint = point =>
                        {
                            backup = window.InkCanvas.EditingMode;
                            window.InkCanvas.EditingMode = InkCanvasEditingMode.None;
                            window.Line.Visibility = Visibility.Visible;
                            window.Line.X1 = window.Line.X2 = point.X;
                            window.Line.Y1 = window.Line.Y2 = point.Y;
                            window.Line.Stroke = new SolidColorBrush(window.InkCanvas.DefaultDrawingAttributes.Color);
                            window.Line.StrokeThickness = window.InkCanvas.DefaultDrawingAttributes.Width;
                        };
                        window.MovePoint = point =>
                        {
                            window.Line.X2 = point.X;
                            window.Line.Y2 = point.Y;
                        };
                        window.EndPoint = point =>
                        {
                            window.Line.Visibility = Visibility.Collapsed;
                            window.InkCanvas.Strokes.Add(new Stroke(new StylusPointCollection(new Point[] { Start, point }), window.InkCanvas.DefaultDrawingAttributes.Clone()));
                            window.InkCanvas.EditingMode = backup;
                        };
                        break;
                    case "F":
                        window.StartPoint = point =>
                        {
                            backup = window.InkCanvas.EditingMode;
                            window.InkCanvas.EditingMode = InkCanvasEditingMode.None;
                            window.Rectangle.Visibility = Visibility.Visible;
                            window.Rectangle.Margin = new Thickness(point.X, point.Y, 0, 0);
                            window.Rectangle.Width = window.Rectangle.Height = 0;
                            window.Rectangle.Stroke = new SolidColorBrush(window.InkCanvas.DefaultDrawingAttributes.Color);
                            window.Rectangle.StrokeThickness = window.InkCanvas.DefaultDrawingAttributes.Width;
                        };
                        window.MovePoint = point =>
                        {
                            var left = Math.Min(point.X, Start.X);
                            var right = Math.Max(point.X, Start.X);
                            var top = Math.Min(point.Y, Start.Y);
                            var bottom = Math.Max(point.Y, Start.Y);
                            window.Rectangle.Margin = new Thickness(left, top, 0, 0);
                            window.Rectangle.Width = right - left;
                            window.Rectangle.Height = bottom - top;
                        };
                        window.EndPoint = point =>
                        {
                            window.Rectangle.Visibility = Visibility.Collapsed;
                            window.InkCanvas.Strokes.Add(new RectangleStroke(Start, point, window.InkCanvas.DefaultDrawingAttributes.Clone()));
                            window.InkCanvas.EditingMode = backup;
                        };
                        break;
                    case "D":
                        window.StartPoint = point =>
                        {
                            backup = window.InkCanvas.EditingMode;
                            window.InkCanvas.EditingMode = InkCanvasEditingMode.None;
                            window.Ellipse.Visibility = Visibility.Visible;
                            window.Ellipse.Margin = new Thickness(point.X, point.Y, 0, 0);
                            window.Ellipse.Width = window.Ellipse.Height = 0;
                            window.Ellipse.Stroke = new SolidColorBrush(window.InkCanvas.DefaultDrawingAttributes.Color);
                            window.Ellipse.StrokeThickness = window.InkCanvas.DefaultDrawingAttributes.Width;
                        };
                        window.MovePoint = point =>
                        {
                            var left = Math.Min(point.X, Start.X);
                            var right = Math.Max(point.X, Start.X);
                            var top = Math.Min(point.Y, Start.Y);
                            var bottom = Math.Max(point.Y, Start.Y);
                            window.Ellipse.Margin = new Thickness(left, top, 0, 0);
                            window.Ellipse.Width = right - left;
                            window.Ellipse.Height = bottom - top;
                        };
                        window.EndPoint = point =>
                        {
                            window.Ellipse.Visibility = Visibility.Collapsed;
                            window.InkCanvas.Strokes.Add(new EllipseStroke(Start, point, window.InkCanvas.DefaultDrawingAttributes.Clone()));
                            window.InkCanvas.EditingMode = backup;
                        };
                        break;
                }
                window.ToolMenuItems.ForEach(i => i.IsChecked = i.InputGestureText == tool);
            });
        }

        private void Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("Grid_PreviewMouseLeftButtonDown");
            drawing = true;
            Start = e.GetPosition(this);
            if (StartPoint != null) StartPoint(Start);
        }

        private void Grid_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine("Grid_PreviewMouseLeftButtonUp");
            if (EndPoint != null) EndPoint(e.GetPosition(this));
            drawing = false;
        }

        private void Grid_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Debug.WriteLine("Grid_PreviewMouseMove");
            if (drawing && MovePoint != null) MovePoint(e.GetPosition(this));
        }
    }

    class RectangleStroke : Stroke
    {
        // Constructor
        public RectangleStroke(Point leftTop, Point rightBottom, DrawingAttributes drawingAttributes)
            : base(new StylusPointCollection(new Point[] { leftTop, rightBottom }), drawingAttributes)
        {
        }

        protected override void DrawCore(DrawingContext drawingContext, DrawingAttributes drawingAttributes)
        {
            var brush = new SolidColorBrush(drawingAttributes.Color);
            var pen = new Pen(brush, drawingAttributes.Width);
            var points = (Point[])StylusPoints;
            drawingContext.DrawRectangle(null, pen, new Rect(points[0], points[1]));
        }
    }

    class EllipseStroke : Stroke
    {
        // Constructor
        public EllipseStroke(Point leftTop, Point rightBottom, DrawingAttributes drawingAttributes)
            : base(new StylusPointCollection(new Point[] { leftTop, rightBottom }), drawingAttributes)
        {
        }

        protected override void DrawCore(DrawingContext drawingContext, DrawingAttributes drawingAttributes)
        {
            var brush = new SolidColorBrush(drawingAttributes.Color);
            var pen = new Pen(brush, drawingAttributes.Width);
            var center = new Point((StylusPoints[1].X + StylusPoints[0].X) / 2, (StylusPoints[1].Y + StylusPoints[0].Y) / 2);
            drawingContext.DrawEllipse(null, pen, center, Math.Abs(StylusPoints[1].X - StylusPoints[0].X) / 2, Math.Abs(StylusPoints[1].Y - StylusPoints[0].Y) / 2);
        }
    }
}
