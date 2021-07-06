using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Shell;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.Web.LibraryManager.Vsix.Resources;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Theming
{
    [ContentProperty(nameof(Body))]
    public class ThemedWindow : DialogWindow
    {
        public static readonly DependencyProperty BodyProperty = DependencyProperty.Register(nameof(Body), typeof(object), typeof(ThemedWindow), new PropertyMetadata(null, OnBodyChanged));
        public static readonly DependencyProperty SupportsCloseGestureProperty = DependencyProperty.Register(nameof(SupportsCloseGesture), typeof(bool), typeof(ThemedWindow), new PropertyMetadata(true, OnSupportsCloseGestureChanged));

        private static void OnSupportsCloseGestureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is ThemedWindow win) || !(e.NewValue is bool newValue))
            {
                return;
            }

            win._closeButton.Visibility = newValue ? Visibility.Visible : Visibility.Hidden;
        }

        private readonly DockPanel _dockPanel;
        private readonly Button _closeButton;
        private FrameworkElement _body;

        public event EventHandler DialogDismissed;

        public ThemedWindow()
        {
            AllowsTransparency = true;
            WindowStyle = WindowStyle.None;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ShowInTaskbar = false;
            ResizeMode = ResizeMode.CanResize;
            PreviewKeyDown += HandlePreviewKeyDown;

            //WindowChrome, needed to resize when transparent without a gripper
            WindowChrome chrome = new WindowChrome()
            {
                ResizeBorderThickness = new Thickness(4),
                CaptionHeight = 0,
                UseAeroCaptionButtons = false
            };
            SetValue(WindowChrome.WindowChromeProperty, chrome);

            //Close button
            _closeButton = new Button();
            _closeButton.SetValue(AutomationProperties.NameProperty, Text.CloseButtonText);
            _closeButton.Click += OnCloseWindow;
            _closeButton.ToolTip = Text.CloseButtonText;
            _closeButton.SetValue(Grid.ColumnProperty, 1);

            // set up header
            Grid titlebar = new Grid();
            titlebar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            titlebar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            titlebar.SetValue(DockPanel.DockProperty, Dock.Top);
            titlebar.SetResourceReference(Border.BackgroundProperty, EnvironmentColors.MainWindowActiveCaptionBrushKey);
            titlebar.MouseDown += WindowGripMouseDown;

            Label titleText = new Label();
            titleText.SetBinding(ContentControl.ContentProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath("Title"),
                Mode = BindingMode.OneWay
            });
            titleText.SetValue(Grid.ColumnProperty, 0);

            titlebar.Children.Add(titleText);
            titlebar.Children.Add(_closeButton);

            _dockPanel = new DockPanel()
            {
                LastChildFill = true,
            };
            _dockPanel.SetResourceReference(Border.BorderBrushProperty, EnvironmentColors.MainWindowActiveDefaultBorderBrushKey);
            _dockPanel.SetResourceReference(Border.BackgroundProperty, EnvironmentColors.StartPageTabBackgroundBrushKey);

            _dockPanel.Children.Add(titlebar);

            Content = _dockPanel;

            // merge resource dictionaries before applying styles
            this.ShouldBeThemed();
            _closeButton.Style = (Style)Resources["WindowCloseButtonStyle"];
            titleText.Style = (Style)Resources["WindowTitleStyle"];
        }

        private void HandlePreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None && IsCloseButtonEnabled)
            {
                e.Handled = true;
                DialogDismissed?.Invoke(this, EventArgs.Empty);
                Close();
            }
        }

        public object Body
        {
            get => GetValue(BodyProperty);
            set => SetValue(BodyProperty, value);
        }

        public bool SupportsCloseGesture
        {
            get => (bool)GetValue(SupportsCloseGestureProperty);
            set => SetValue(SupportsCloseGestureProperty, value);
        }

        private void OnCloseWindow(object sender, RoutedEventArgs e)
        {
            DialogDismissed?.Invoke(this, EventArgs.Empty);
            Close();
        }

        private static void OnBodyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is ThemedWindow win))
            {
                return;
            }

            if (!(e.NewValue is FrameworkElement element))
            {
                element = new ContentPresenter
                {
                    Content = e.NewValue
                };
            }

            element.SetValue(Grid.RowProperty, 1);

            if (!(win._body is null))
            {
                win._dockPanel.Children.Remove(win._body);
            }

            element.ShouldBeThemed();
            win._dockPanel.Children.Add(element);
            win._body = element;
        }

        private void WindowGripMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
                e.Handled = true;
            }
        }
    }
}
