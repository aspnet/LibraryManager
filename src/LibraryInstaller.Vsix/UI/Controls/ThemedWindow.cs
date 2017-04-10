// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using EnvDTE;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.Web.LibraryInstaller.Vsix.Controls
{
    public class ThemedWindow : System.Windows.Window
    {
        public new static readonly DependencyProperty ContentProperty = DependencyProperty.Register(
            "Content", typeof(FrameworkElement), typeof(ThemedWindow), new PropertyMetadata(default(FrameworkElement)));

        private bool _isShowingAsDialog;

        public ThemedWindow()
        {
            this.ShouldBeThemed();

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            Background = Brushes.Transparent;
            AllowsTransparency = true;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ShowInTaskbar = false;

            Grid host = new Grid();
            //Header
            host.RowDefinitions.Add(new RowDefinition
            {
                Height = GridLength.Auto
            });
            //Body
            host.RowDefinitions.Add(new RowDefinition());

            FrameworkElement header = BuildHeaderArea();
            header.SetValue(Grid.RowProperty, 0);
            host.Children.Add(header);

            ContentPresenter contentPresenter = new ContentPresenter();
            contentPresenter.SetValue(Grid.RowProperty, 1);
            contentPresenter.SetBinding(ContentPresenter.ContentProperty, new Binding
            {
                Mode = BindingMode.OneWay,
                RelativeSource = new RelativeSource
                {
                    Mode = RelativeSourceMode.FindAncestor,
                    AncestorType = typeof(ThemedWindow)
                },
                Path = new PropertyPath("Content")
            });
            contentPresenter.Resources = Resources;
            host.Children.Add(contentPresenter);

            host.SetResourceReference(BackgroundProperty, EnvironmentColors.ToolWindowBackgroundBrushKey);

            Border hostContainer = new Border
            {
                Child = host,
                //Margin = new Thickness(1, 1, 5, 5),
                BorderThickness = new Thickness(1)
            };
            hostContainer.SetResourceReference(BorderBrushProperty, EnvironmentColors.MainWindowActiveDefaultBorderBrushKey);
            //hostContainer.Effect = new DropShadowEffect
            //{
            //    Direction = -75,
            //    ShadowDepth = 2,
            //    BlurRadius = 2,
            //    Color = Colors.Azure
            //};

            base.Content = hostContainer;
        }

        public new FrameworkElement Content
        {
            get { return (FrameworkElement)GetValue(ContentProperty); }
            set { SetValue(ContentProperty, value); }
        }

        public new void Show()
        {
            new WindowInteropHelper(this)
            {
                Owner = new IntPtr((Package.GetGlobalService(typeof(SDTE)) as DTE)?.MainWindow.HWnd ?? 0)
            };
            _isShowingAsDialog = false;
            base.Show();
        }

        public new bool? ShowDialog()
        {
            new WindowInteropHelper(this)
            {
                Owner = new IntPtr((Package.GetGlobalService(typeof(SDTE)) as DTE)?.MainWindow.HWnd ?? 0)
            };
            _isShowingAsDialog = true;
            return base.ShowDialog();
        }

        protected virtual void HandleSystemButtonClick()
        {
            if (OnCloseButtonClicked())
            {
                try
                {
                    if (_isShowingAsDialog)
                    {
                        DialogResult = false;
                        return;
                    }
                }
                catch
                {
                }

                Close();
            }
        }

        protected virtual bool OnCloseButtonClicked()
        {
            return true;
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Escape:
                    HandleSystemButtonClick();
                    e.Handled = true;
                    return;
            }

            base.OnPreviewKeyDown(e);
        }

        private FrameworkElement BuildHeaderArea()
        {
            Grid header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition());
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            //Move grip containing the icon and title
            Grid moveGrip = new Grid();
            moveGrip.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto
            });
            moveGrip.ColumnDefinitions.Add(new ColumnDefinition());
            moveGrip.SetResourceReference(Border.BackgroundProperty, EnvironmentColors.ToolWindowBackgroundBrushKey);
            moveGrip.SetValue(Grid.ColumnProperty, 0);
            moveGrip.MouseLeftButtonDown += TitleBarLeftMouseButtonDown;

            Image icon = new Image
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 5, 4, 0)
            };
            icon.SetBinding(Image.SourceProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath("Icon"),
                Mode = BindingMode.OneWay
            });
            moveGrip.Children.Add(icon);

            Label label = new Label
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0)
            };
            label.SetValue(Grid.ColumnProperty, 1);
            label.SetBinding(ContentControl.ContentProperty, new Binding
            {
                Source = this,
                Path = new PropertyPath("Title"),
                Mode = BindingMode.OneWay
            });
            label.SetResourceReference(ForegroundProperty, EnvironmentColors.MainWindowActiveCaptionTextBrushKey);
            moveGrip.Children.Add(label);
            header.Children.Add(moveGrip);

            //Close button
            FrameworkElement closeGlyph;
            Path buttonPath;
            GenerateCloseGlyph(out closeGlyph, out buttonPath);

            Style closeButtonStyle = new Style(typeof(Button));
            closeButtonStyle.Setters.Add(new Setter(HeightProperty, (double)25));
            closeButtonStyle.Setters.Add(new Setter(WidthProperty, (double)25));
            closeButtonStyle.Setters.Add(new Setter(FocusVisualStyleProperty, null));
            closeButtonStyle.Setters.Add(new Setter(ForegroundProperty, new DynamicResourceExtension(EnvironmentColors.MainWindowButtonActiveGlyphBrushKey)));
            closeButtonStyle.Setters.Add(new Setter(BackgroundProperty, null));
            closeButtonStyle.Setters.Add(new Setter(BorderBrushProperty, new DynamicResourceExtension(EnvironmentColors.MainWindowButtonActiveBorderBrushKey)));
            closeButtonStyle.Setters.Add(new Setter(TemplateProperty, FindResource("FlatButton")));

            Trigger closeButtonHoverTrigger = new Trigger
            {
                Property = IsMouseOverProperty,
                Value = true
            };

            closeButtonHoverTrigger.Setters.Add(new Setter(ForegroundProperty, new DynamicResourceExtension(EnvironmentColors.MainWindowButtonHoverActiveGlyphBrushKey)));
            closeButtonHoverTrigger.Setters.Add(new Setter(BackgroundProperty, new DynamicResourceExtension(EnvironmentColors.MainWindowButtonHoverActiveBrushKey)));
            closeButtonHoverTrigger.Setters.Add(new Setter(BorderBrushProperty, new DynamicResourceExtension(EnvironmentColors.MainWindowButtonHoverActiveBorderBrushKey)));
            closeButtonStyle.Triggers.Add(closeButtonHoverTrigger);

            Trigger closeButtonPressTrigger = new Trigger
            {
                Property = ButtonBase.IsPressedProperty,
                Value = true
            };

            closeButtonPressTrigger.Setters.Add(new Setter(ForegroundProperty, new DynamicResourceExtension(EnvironmentColors.MainWindowButtonDownGlyphBrushKey)));
            closeButtonPressTrigger.Setters.Add(new Setter(BackgroundProperty, new DynamicResourceExtension(EnvironmentColors.MainWindowButtonDownBrushKey)));
            closeButtonPressTrigger.Setters.Add(new Setter(BorderBrushProperty, new DynamicResourceExtension(EnvironmentColors.MainWindowButtonDownBorderBrushKey)));
            closeButtonStyle.Triggers.Add(closeButtonPressTrigger);

            Button closeButton = new Button
            {
                Style = closeButtonStyle,
                Name = "closeButton",
                Content = closeGlyph,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };

            closeButton.SetValue(Grid.ColumnProperty, 2);
            header.Children.Add(closeButton);
            buttonPath.SetBinding(Shape.FillProperty, new Binding
            {
                Path = new PropertyPath("Foreground"),
                Source = closeButton,
                Mode = BindingMode.OneWay
            });

            closeButton.Click += CloseButtonClick;

            return header;
        }

        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            HandleSystemButtonClick();
        }

        private void GenerateCloseGlyph(out FrameworkElement container, out Path path)
        {
            Path buttonPath = new Path();
            PathGeometry geometry = new PathGeometry();
            PathFigure figure = new PathFigure
            {
                StartPoint = new Point(0, 0)
            };

            figure.Segments.Add(new LineSegment
            {
                Point = new Point(2, 0)
            });

            figure.Segments.Add(new LineSegment
            {
                Point = new Point(5, 3)
            });

            figure.Segments.Add(new LineSegment
            {
                Point = new Point(8, 0)
            });

            figure.Segments.Add(new LineSegment
            {
                Point = new Point(10, 0)
            });

            figure.Segments.Add(new LineSegment
            {
                Point = new Point(6, 4)
            });

            figure.Segments.Add(new LineSegment
            {
                Point = new Point(10, 8)
            });

            figure.Segments.Add(new LineSegment
            {
                Point = new Point(8, 8)
            });

            figure.Segments.Add(new LineSegment
            {
                Point = new Point(5, 5)
            });

            figure.Segments.Add(new LineSegment
            {
                Point = new Point(2, 8)
            });

            figure.Segments.Add(new LineSegment
            {
                Point = new Point(0, 8)
            });

            figure.Segments.Add(new LineSegment
            {
                Point = new Point(4, 4)
            });

            figure.Segments.Add(new LineSegment
            {
                Point = new Point(0, 0)
            });

            geometry.Figures.Add(figure);
            buttonPath.Data = geometry;

            Canvas closeGlyphCanvas = new Canvas
            {
                Width = 10,
                Height = 8
            };

            closeGlyphCanvas.Children.Add(buttonPath);
            container = closeGlyphCanvas;
            path = buttonPath;
        }

        private void TitleBarLeftMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
    }
}
