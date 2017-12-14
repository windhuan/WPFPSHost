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
using System.ComponentModel;
using System.Windows.Threading;
using System.Management.Automation;
using System.Threading;

namespace WPFPSHost
{
    internal class ConsoleControl : FlowDocumentScrollViewer,IDisposable
    {
        protected Paragraph _OutputParagraph;
        private InlineUIContainer _InputTextBoxContainer;
        private InlineUIContainer _InputPasswordBoxContainer;
        private TextBox _InputTextBox = null;
        private PasswordBox _InputPasswordBox = null;
        private ScrollViewer _scrollViewer;
        private Action<string> aInputTextCallBack;
        private Action<System.Security.SecureString> aInputPassowrdCallBack;

        private bool _IsInputLock = false;
        private ManualResetEvent _InputLock = new ManualResetEvent(true);
        private string InputResultTemp = string.Empty;
        private System.Security.SecureString InputPasswordResultTemp = null;
        private bool _IsShowTextBox = false;
        private bool _IsShowPasswordBox = false;

        public bool IsInputLock
        {
            get
            {
                return _IsInputLock;
            }
        }

        static ConsoleControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ConsoleControl), new FrameworkPropertyMetadata(typeof(ConsoleControl)));
        }

        public ConsoleControl()
        {
            this.PreviewMouseRightButtonUp += ConsoleControl_PreviewMouseRightButtonUp;
            this.PreviewMouseLeftButtonDown += ConsoleControl_PreviewMouseLeftButtonDown;
        }

        public override void EndInit()
        {
            base.EndInit();
            InitInputTextBox();
            InitInputPasswordBox();
            InitDocument();
        }

        private void ConsoleControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((e.Source is FlowDocument || e.Source is Paragraph))
            {
                if (_IsShowTextBox)
                {
                    _InputTextBox.Focus();
                    e.Handled = true;
                }

                if (_IsShowPasswordBox)
                {
                    _InputPasswordBox.Focus();
                    e.Handled = true;
                }
            }
        }

        private void ConsoleControl_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.Source is FlowDocument || e.Source is Paragraph)
            {
                if (_IsShowTextBox)
                {
                    _InputTextBox.Focus();
                    this.Selection.Select(_OutputParagraph.ContentEnd, _OutputParagraph.ContentEnd);
                    _InputTextBox.Select(_InputTextBox.Text.Length, 0);
                    this._scrollViewer.ScrollToBottom();
                    e.Handled = true;
                }

                if (_IsShowPasswordBox)
                {
                    _InputPasswordBox.Focus();
                    this.Selection.Select(_OutputParagraph.ContentEnd, _OutputParagraph.ContentEnd);
                    this._scrollViewer.ScrollToBottom();
                    e.Handled = true;
                }
            }
        }

        private void InitInputTextBox()
        {
            _InputTextBox = new TextBox()
            {
                IsEnabled = true,
                Focusable = true,
                AcceptsTab = true,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Hidden,
                TextWrapping = System.Windows.TextWrapping.Wrap,
                Padding = new Thickness(0.0),
                Margin=new Thickness(0)
            };
            aInputTextCallBack = (p) =>
            {
                InputResultTemp = p;
                _InputLock.Set();
            };
            _InputTextBox.PreviewKeyDown += new KeyEventHandler(InputTextBox_PreviewKeyDown);
            _InputTextBoxContainer = new InlineUIContainer(_InputTextBox) { BaselineAlignment = BaselineAlignment.Top };
            _InputTextBox.Loaded += InputTextBox_Loaded;
        }

        private void InitInputPasswordBox()
        {
            _InputPasswordBox = new PasswordBox()
            {
                IsEnabled = true,
                Focusable = true,
                Padding = new Thickness(0.0),
                Margin = new Thickness(0)
            };
            aInputPassowrdCallBack = (p) =>
            {
                InputPasswordResultTemp = p;
                _InputLock.Set();
            };
            _InputPasswordBox.PreviewKeyDown += new KeyEventHandler(InputPasswordBox_PreviewKeyDown);
            _InputPasswordBoxContainer = new InlineUIContainer(_InputPasswordBox) { BaselineAlignment = BaselineAlignment.Top };
            _InputPasswordBox.Loaded += InputTextBox_Loaded;
        }

        private void InputTextBox_Loaded(object sender, RoutedEventArgs e)
        {
            (sender as FrameworkElement).Focus();
        }

        private void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (aInputTextCallBack != null)
                {
                    aInputTextCallBack(_InputTextBox.Text);
                }
                _OutputParagraph.Inlines.Remove(_InputTextBoxContainer);
                this.WriteLine(this.Document.Foreground, this.Document.Background, _InputTextBox.Text);
                _InputTextBox.Text = string.Empty;
                e.Handled = true;
            }
        }

        private void InputPasswordBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (aInputPassowrdCallBack != null)
                {
                    aInputPassowrdCallBack(_InputPasswordBox.SecurePassword);
                }
                _OutputParagraph.Inlines.Remove(_InputPasswordBoxContainer);
                this.WriteLine(this.Document.Foreground, this.Document.Background,"".PadLeft(_InputPasswordBox.Password.Length,'*'));
                _InputPasswordBox.Password = string.Empty;
                e.Handled = true;
            }
        }

        private void InitDocument()
        {
            _OutputParagraph = new Paragraph { ClearFloaters = WrapDirection.Both };
            _OutputParagraph.Padding = new Thickness(0,0,0,20);
            _OutputParagraph.Margin = new Thickness(0);
            Document.Blocks.Add(_OutputParagraph);
            Document.PagePadding = new Thickness(0.0);
            Document.TextAlignment = TextAlignment.Left;
        }

        public void Write(Brush foreground, Brush background, string text)
        {
            Write(foreground, background, text, _OutputParagraph);
        }

        public void Write(Brush foreground, Brush background, string text, Block target)
         {
             Action InsertRun = () =>
             {
                 if (background == null)
                 {
                     background = this.Document.Background;
                 }

                 if (foreground == null)
                 {
                     foreground = this.Document.Foreground;
                 }

                 new Run(text, target.ContentEnd)
                 {
                     Background = background,
                     Foreground = foreground
                 };
                 if (_scrollViewer != null)
                 {
                     _scrollViewer.ScrollToBottom();
                 }
             };
             if (this.Dispatcher.CheckAccess())
             {
                 InsertRun();
             }
             else
             {
                 this.Dispatcher.Invoke(DispatcherPriority.Send, (Action)delegate
                 {
                     InsertRun();
                 });
             }
         }

        public void Write(ConsoleColor foreground, ConsoleColor? background, string text)
        {
            Write(this.BrushFromConsoleColor(foreground),this.BrushFromConsoleColor(background),text);
        }

        public void Write(ConsoleColor foreground, ConsoleColor? background, string text, Block target)
        {
            Write(this.BrushFromConsoleColor(foreground), this.BrushFromConsoleColor(background), text, target);
        }

        public void WriteLine(Brush foreground, Brush background, string text)
        {
            Write(foreground, background,  text + Environment.NewLine);
        }

        public void WriteLine(Brush foreground, Brush background, string text, Block target)
        {
            Write(foreground, background,  text + Environment.NewLine, target);
        }

        public void WriteLine(ConsoleColor foreground, ConsoleColor? background, string text)
        {
            Write(foreground, background, text + Environment.NewLine);
        }

        public void WriteLine(ConsoleColor foreground, ConsoleColor? background, string text, Block target)
        {
            Write(foreground, background, text + Environment.NewLine, target);
        }

        public string ReadLine()
        {
            _InputLock.WaitOne();
            _InputLock.Reset();
            InputResultTemp = string.Empty;
            _IsInputLock = true;
            _IsShowTextBox = true;
            if (this.Dispatcher.CheckAccess())
            {
                _OutputParagraph.Inlines.Add(_InputTextBoxContainer);
            }
            else
            {
                this.Dispatcher.Invoke(DispatcherPriority.Send, (Action)delegate
                {
                    _OutputParagraph.Inlines.Add(_InputTextBoxContainer);
                    
                });
            }
            _InputLock.WaitOne();
            _InputLock.Set();
            _IsInputLock = false;
            _IsShowTextBox = false;
            return InputResultTemp;
        }

        public System.Security.SecureString ReadPassword()
        {
            _InputLock.WaitOne();
            _InputLock.Reset();
            InputPasswordResultTemp = null;
            _IsInputLock = true;
            _IsShowPasswordBox = true;
            if (this.Dispatcher.CheckAccess())
            {
                _OutputParagraph.Inlines.Add(_InputPasswordBoxContainer);
            }
            else
            {
                this.Dispatcher.Invoke(DispatcherPriority.Send, (Action)delegate
                {
                    _OutputParagraph.Inlines.Add(_InputPasswordBoxContainer);

                });
            }
            _InputLock.WaitOne();
            _InputLock.Set();
            _IsInputLock = false;
            _IsShowPasswordBox = false;
            return InputPasswordResultTemp;
        }

        public void ReleaseInputLock()
        {
            Action action = () =>
            {
                if (_IsShowPasswordBox)
                {
                    _OutputParagraph.Inlines.Remove(_InputPasswordBoxContainer);
                    _InputPasswordBox.Password = string.Empty;
                }
                if (_IsShowTextBox)
                {
                    _OutputParagraph.Inlines.Remove(_InputTextBoxContainer);
                    _InputTextBox.Text = string.Empty;
                }
            };

            if (this.Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                this.Dispatcher.Invoke(DispatcherPriority.Send, (Action)(() =>
                {
                    action();
                }));
            }
            _InputLock.Set();
        }

        public void Clear()
        {
            if (this.Dispatcher.CheckAccess())
            {
                Document.Blocks.Clear();
                InitDocument();
            }
            else
            {
                this.Dispatcher.Invoke(DispatcherPriority.Send, (Action)(() =>
                {
                    Document.Blocks.Clear();
                    InitDocument();
                }));
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _scrollViewer = GetTemplateChild("PART_ContentHost") as ScrollViewer;
        }

        private Brush BrushFromConsoleColor(ConsoleColor? color)
        {
            
            switch (color)
            {
                case ConsoleColor.Black:
                    return Brushes.Black;
                case ConsoleColor.Blue:
                    return Brushes.Blue;
                case ConsoleColor.Cyan:
                    return Brushes.Cyan;
                case ConsoleColor.DarkBlue:
                    return Brushes.DarkBlue;
                case ConsoleColor.DarkCyan:
                    return Brushes.DarkCyan;
                case ConsoleColor.DarkGray:
                    return Brushes.DarkGray;
                case ConsoleColor.DarkGreen:
                    return Brushes.DarkGreen;
                case ConsoleColor.DarkMagenta:
                    return Brushes.DarkMagenta;
                case ConsoleColor.DarkRed:
                    return Brushes.DarkRed;
                case ConsoleColor.DarkYellow:
                    return Brushes.LightYellow;
                case ConsoleColor.Gray:
                    return Brushes.Gray;
                case ConsoleColor.Green:
                    return Brushes.Green;
                case ConsoleColor.Magenta:
                    return Brushes.Magenta;
                case ConsoleColor.Red:
                    return Brushes.Red;
                case ConsoleColor.White:
                    return Brushes.White;
                case ConsoleColor.Yellow:
                    return Brushes.Yellow;
                default: // for NULL values
                    return null;
            }
        }

        public void Dispose()
        {
            _InputLock.Set();
        }
    }
}


