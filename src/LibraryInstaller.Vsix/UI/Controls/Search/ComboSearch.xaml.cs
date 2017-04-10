// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.VisualStudio.PlatformUI;

namespace Microsoft.Web.LibraryInstaller.Vsix.Controls.Search
{
    public partial class ComboSearch
    {
        public static readonly DependencyProperty GlyphStyleProperty = DependencyProperty.Register(
            "GlyphStyle", typeof(Style), typeof(ComboSearch), new PropertyMetadata(null));

        public Style GlyphStyle
        {
            get { return (Style)GetValue(GlyphStyleProperty); }
            set { SetValue(GlyphStyleProperty, value); }
        }

        public static readonly DependencyProperty CommitedItemProperty = DependencyProperty.Register(
            "CommitedItem", typeof(ISearchItem), typeof(ComboSearch), new PropertyMetadata(default(ISearchItem)));

        private static readonly DependencyPropertyKey HasResultsPropertyKey = DependencyProperty.RegisterReadOnly(
            "HasResults", typeof(bool), typeof(ComboSearch), new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty HasResultsProperty = HasResultsPropertyKey.DependencyProperty;

        public bool HasResults
        {
            get { return (bool)GetValue(HasResultsProperty); }
            private set { SetValue(HasResultsPropertyKey, value); }
        }

        public ISearchItem CommitedItem
        {
            get { return (ISearchItem)GetValue(CommitedItemProperty); }
            set { SetValue(CommitedItemProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            "SelectedItem", typeof(ISearchItem), typeof(ComboSearch), new PropertyMetadata(default(ISearchItem), SelectedItemChanged));

        private static void SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            IList<SearchItemContainer> items = (IList<SearchItemContainer>)d.GetValue(ContainersProperty);

            foreach (SearchItemContainer item in items)
            {
                item.TemplateChanged();
            }
        }

        public ISearchItem SelectedItem
        {
            get { return (ISearchItem)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource", typeof(IList), typeof(ComboSearch), new PropertyMetadata(default(IList), ItemsChangedCallback));

        public static readonly DependencyProperty ContainersProperty = DependencyProperty.Register(
            "Containers", typeof(IList<SearchItemContainer>), typeof(ComboSearch), new PropertyMetadata(default(IList<SearchItemContainer>)));

        public IList<SearchItemContainer> Containers
        {
            get { return (IList<SearchItemContainer>)GetValue(ContainersProperty); }
            set { SetValue(ContainersProperty, value); }
        }

        private static void ItemsChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            INotifyCollectionChanged oldCollectionChanged = e.OldValue as INotifyCollectionChanged;

            if (oldCollectionChanged != null)
            {
                oldCollectionChanged.CollectionChanged -= ((ComboSearch)d).OnCollectionItemsChanged;
            }

            INotifyCollectionChanged newCollectionChanged = e.NewValue as INotifyCollectionChanged;
            ((ComboSearch)d).OnCollectionItemsChanged(e.NewValue, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            if (newCollectionChanged != null)
            {
                newCollectionChanged.CollectionChanged += ((ComboSearch)d).OnCollectionItemsChanged;
            }
        }

        private void OnCollectionItemsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IEnumerable<ISearchItem> items = ((IEnumerable)sender).OfType<ISearchItem>();
            List<SearchItemContainer> containers = new List<SearchItemContainer>();

            foreach (ISearchItem item in items)
            {
                containers.Add(new SearchItemContainer(this, item));
            }

            Containers = containers;
            HasResults = Containers.Count > 0;

            if (HasResults)
            {
                Options.SelectedIndex = 0;
                Options.ScrollIntoView(Options.Items[0]);
            }
        }

        public IList ItemsSource
        {
            get { return (IList)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(string), typeof(ComboSearch), new PropertyMetadata(default(string), RaiseSearchTextChanged));

        private static void RaiseSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ComboSearch search = d as ComboSearch;
            search?.OnSearchTextChanged();
        }

        private Window _window;

        public event EventHandler SearchTextChanged;

        private void OnSearchTextChanged()
        {
            SearchTextChanged?.Invoke(this, EventArgs.Empty);

            if (ItemsSource != null && ItemsSource.Count > 0)
            {
                Options.SelectedIndex = 0;
                Options.ScrollIntoView(Options.Items[0]);
            }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty ExpandedTemplateProperty = DependencyProperty.Register(
            "ExpandedTemplate", typeof(DataTemplate), typeof(ComboSearch), new PropertyMetadata(default(DataTemplate), TemplateChanged));

        private static void TemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            IList<SearchItemContainer> containers = (IList<SearchItemContainer>)d.GetValue(ContainersProperty);

            if (containers != null)
            {
                foreach (SearchItemContainer container in containers)
                {
                    container.TemplateChanged();
                }
            }
        }

        public DataTemplate ExpandedTemplate
        {
            get { return (DataTemplate)GetValue(ExpandedTemplateProperty); }
            set { SetValue(ExpandedTemplateProperty, value); }
        }

        public static readonly DependencyProperty CollapsedTemplateProperty = DependencyProperty.Register(
            "CollapsedTemplate", typeof(DataTemplate), typeof(ComboSearch), new PropertyMetadata(default(DataTemplate), TemplateChanged));

        private Style DefaultGlyphStyle { get; }

        public DataTemplate CollapsedTemplate
        {
            get { return (DataTemplate)GetValue(CollapsedTemplateProperty); }
            set { SetValue(CollapsedTemplateProperty, value); }
        }

        public ComboSearch()
        {
            InitializeComponent();

            DefaultGlyphStyle = new Style
            {
                TargetType = typeof(Path),
                Setters =
                {
                    new Setter(Shape.FillProperty, new DynamicResourceExtension(CommonControlsColors.TextBoxTextBrushKey))
                },
                Triggers =
                {
                    new DataTrigger
                    {
                        Binding = new Binding
                        {
                            Source = this,
                            Path = new PropertyPath(nameof(IsMouseOver))
                        },
                        Setters =
                        {
                            new Setter(Shape.FillProperty, new DynamicResourceExtension(CommonControlsColors.ComboBoxGlyphPressedBrushKey))
                        }
                    }
                }
            };

            GlyphStyle = DefaultGlyphStyle;
            SearchBox.AddHandler(GotKeyboardFocusEvent, (RoutedEventHandler)SearchBoxGotKeyboardFocus, true);
            SearchBox.AddHandler(MouseDoubleClickEvent, (RoutedEventHandler)SearchBoxGotKeyboardFocus, true);
            SearchBox.AddHandler(PreviewMouseLeftButtonUpEvent, (RoutedEventHandler)SearchBoxGotKeyboardFocus, true);
            SearchBox.AddHandler(LostFocusEvent, (RoutedEventHandler)SearchBoxLostFocus, true);
        }

        private void WindowMoved(object sender, EventArgs e)
        {
            if (!Flyout.IsOpen)
            {
                return;
            }

            double offset = Flyout.HorizontalOffset;
            Flyout.HorizontalOffset = offset + 1;
            Flyout.HorizontalOffset = offset;
        }

        private void SearchBoxLostFocus(object sender, RoutedEventArgs e)
        {
            SearchBox.AddHandler(PreviewMouseLeftButtonUpEvent, (RoutedEventHandler)SearchBoxGotKeyboardFocus, true);
        }

        private void SearchBoxGotKeyboardFocus(object sender, RoutedEventArgs e)
        {
            if (_window == null)
            {
                _window = Window.GetWindow(this);

                if (_window != null)
                {
                    _window.LocationChanged += WindowMoved;
                }
            }

            if (!SearchBox.IsFocused)
            {
                return;
            }

            SearchBox.SelectAll();

            if (e.RoutedEvent == PreviewMouseLeftButtonUpEvent)
            {
                SearchBox.RemoveHandler(PreviewMouseLeftButtonUpEvent, (RoutedEventHandler)SearchBoxGotKeyboardFocus);
                e.Handled = true;
            }
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            ISearchItem selected = SelectedItem;
            if (selected != null && !Options.IsKeyboardFocusWithin)
            {
                Text = selected.CollapsedItemText;
                CommitedItem = selected;
            }
        }

        private void OnGotFocus(object sender, RoutedEventArgs e)
        {
            CommitedItem = null;

            if (IsKeyboardFocused)
            {
                SearchBox.Focus();
            }

            if (ItemsSource != null && ItemsSource.Count > 0 && Options.SelectedIndex == -1)
            {
                Options.SelectedIndex = 0;
                Options.ScrollIntoView(Options.Items[0]);
            }
        }

        private void HandleKeyPress(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Tab:
                case Key.Enter:
                    if (Options.Items.Count > 0)
                    {
                        SelectedItem = (ISearchItem)ItemsSource[0];
                        CommitedItem = SelectedItem;
                        e.Handled = true;
                        SearchBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    }
                    break;
                case Key.Escape:
                    e.Handled = true;
                    SearchBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    break;
                case Key.Down:
                    if (HasResults)
                    {
                        Options.ScrollIntoView(Options.Items[0]);
                        FrameworkElement fe = (FrameworkElement)Options.ItemContainerGenerator.ContainerFromIndex(0);

                        if (fe != null)
                        {
                            fe.Focus();
                        }

                        Options.SelectedIndex = 0;
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void HandleListBoxKeyPress(object sender, KeyEventArgs e)
        {
            int index = SearchBox.CaretIndex;

            switch (e.Key)
            {
                case Key.Tab:
                case Key.Enter:
                    CommitedItem = SelectedItem;
                    e.Handled = true;
                    SearchBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    break;
                case Key.Up:
                    if (Options.SelectedIndex == 0)
                    {
                        SelectedItem = (ISearchItem)ItemsSource[0];
                        LostFocus -= OnLostFocus;
                        SearchBox.Focus();
                        SearchBox.CaretIndex = index;
                        LostFocus += OnLostFocus;
                    }
                    break;
                case Key.Escape:
                    e.Handled = true;
                    SearchBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    break;
                case Key.Down:
                case Key.PageDown:
                case Key.PageUp:
                case Key.Home:
                case Key.End:
                    break;
                default:
                    LostFocus -= OnLostFocus;
                    SearchBox.Focus();
                    SearchBox.CaretIndex = index;
                    LostFocus += OnLostFocus;
                    break;
            }
        }

        private void OnItemCommitGesture(object sender, MouseButtonEventArgs e)
        {
            HitTestResult result = VisualTreeHelper.HitTest((Visual)sender, e.GetPosition((IInputElement)sender));
            TextBlock over = result.VisualHit as TextBlock;

            if (over != null && over.Inlines.OfType<Hyperlink>().Any(x => x.IsMouseOver))
            {
                return;
            }

            object item = Options.ItemContainerGenerator.ItemFromContainer((DependencyObject)sender);
            SelectedItem = ((SearchItemContainer)item).Item;
            CommitedItem = SelectedItem;
            e.Handled = true;
            SearchBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }

        public void ResumeFocusEvents()
        {
            GotFocus += OnGotFocus;
            LostFocus += OnLostFocus;
        }

        public void SuspendFocusEvents()
        {
            GotFocus -= OnGotFocus;
            LostFocus -= OnLostFocus;
        }
    }
}
