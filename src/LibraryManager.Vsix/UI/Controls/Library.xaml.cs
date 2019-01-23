using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Controls
{
    public partial class Library : INotifyPropertyChanged
    {
        public static readonly DependencyProperty CaretIndexProperty = DependencyProperty.Register(
            nameof(CaretIndex), typeof(int), typeof(Library), new PropertyMetadata(default(int)));

        public static readonly DependencyProperty SearchServiceProperty = DependencyProperty.Register(
            nameof(SearchService), typeof(Func<string, int, Task<CompletionSet>>), typeof(Library), new PropertyMetadata(default(Func<string, int, Task<CompletionSet>>)));

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem), typeof(Completion), typeof(Library), new PropertyMetadata(default(Completion)));

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(Library), new PropertyMetadata(default(string)));

        public Library()
        {
            InitializeComponent();

            this.Loaded += LibrarySearchBox_Loaded;
        }

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new LibraryAutomationPeer(this);
        }

        private void LibrarySearchBox_Loaded(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(LibrarySearchBox);

            // Simple hack to make the popup dock to the textbox, so that the popup will be repositioned whenever
            // the dialog is dragged or resized.
            // In the below section, we will bump up the HorizontalOffset property of the popup whenever the dialog window
            // location is changed or window is resized so that the popup gets repositioned.
            if (window != null)
            {
                window.LocationChanged += RepositionPopup;
                window.SizeChanged += RepositionPopup;
            }
        }

        private void RepositionPopup(object sender, EventArgs e)
        {
            double offset = Flyout.HorizontalOffset;

            Flyout.HorizontalOffset = offset + 1;
            Flyout.HorizontalOffset = offset;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public int CaretIndex
        {
            get { return (int)GetValue(CaretIndexProperty); }
            set { SetValue(CaretIndexProperty, value); }
        }

        public bool IsMouseOverFlyout => Options.IsMouseOver;

        public bool IsTextEntryEmpty => string.IsNullOrEmpty(Text);

        public bool HasItems => Items.Count > 0;

        public ObservableCollection<Completion> Items { get; } = new ObservableCollection<Completion>();

        public Func<string, int, Task<CompletionSet>> SearchService
        {
            get { return (Func<string, int, Task<CompletionSet>>)GetValue(SearchServiceProperty); }
            set { SetValue(SearchServiceProperty, value); }
        }

        public Completion SelectedItem
        {
            get { return (Completion)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Commit(Completion completion)
        {
            if (completion == null)
            {
                return;
            }

            Text = completion.CompletionItem.InsertionText;
            LibrarySearchBox.CaretIndex = Text.IndexOf(completion.CompletionItem.DisplayText, StringComparison.OrdinalIgnoreCase) + completion.CompletionItem.DisplayText.Length;
            Flyout.IsOpen = false;
            SelectedItem = null;
        }

        private void HandleKeyPress(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Tab:
                    // SelectedItem could be null if the key press came from keyboard navigation and not commit operation.
                    // In this case we will just move the focus to next control.
                    if (SelectedItem == null)
                    {
                        LibrarySearchBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    }
                    else
                    {
                        CommitSelectionAndMoveFocus();
                    }

                    e.Handled = true;
                    break;
                case Key.Enter:
                    CommitSelectionAndMoveFocus();
                    e.Handled = true;
                    break;
                case Key.Escape:
                    Flyout.IsOpen = false;
                    LibrarySearchBox.ScrollToEnd();
                    e.Handled = true;
                    break;
                case Key.Down:
                    if (Options.Items.Count > 0)
                    {
                        Options.ScrollIntoView(Options.Items[0]);
                        FrameworkElement fe = (FrameworkElement)Options.ItemContainerGenerator.ContainerFromIndex(0);
                        fe?.Focus();
                        Options.SelectedIndex = 0;
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void HandleListBoxKeyPress(object sender, KeyEventArgs e)
        {
            int index = LibrarySearchBox.CaretIndex;

            switch (e.Key)
            {
                case Key.Tab:
                case Key.Enter:
                    CommitSelectionAndMoveFocus();
                    e.Handled = true;
                    break;
                case Key.Up:
                    if (Options.SelectedIndex == 0)
                    {
                        SelectedItem = Items[0];
                        LostFocus -= OnLostFocus;
                        LibrarySearchBox.Focus();
                        LibrarySearchBox.CaretIndex = index;
                        LostFocus += OnLostFocus;
                    }
                    break;
                case Key.Escape:
                    Flyout.IsOpen = false;
                    LibrarySearchBox.ScrollToEnd();
                    e.Handled = true;
                    break;
                case Key.Down:
                case Key.PageDown:
                case Key.PageUp:
                case Key.Home:
                case Key.End:
                    break;
                default:
                    LostFocus -= OnLostFocus;
                    LibrarySearchBox.Focus();
                    LibrarySearchBox.CaretIndex = index;
                    LostFocus += OnLostFocus;
                    break;
            }
        }

        private void CommitSelectionAndMoveFocus()
        {
            Commit(SelectedItem);
            LibrarySearchBox.Focus();
        }

        private void OnItemCommitGesture(object sender, MouseButtonEventArgs e)
        {
            Commit(SelectedItem);
            e.Handled = true;
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null && !Options.IsKeyboardFocusWithin)
            {
                Commit(SelectedItem);
                LibrarySearchBox.ScrollToEnd();
            }
        }

        private void PositionCompletions(int index)
        {
            Rect r = LibrarySearchBox.GetRectFromCharacterIndex(index);
            Flyout.HorizontalOffset = r.Left - 7;
            Options.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Flyout.Width = Options.DesiredSize.Width;
        }

        private IEnumerable<CompletionItem> FilterOutUnmatchedItems(IEnumerable<CompletionItem> items, string versionSuffix)
        {
            return items.Where(x => x.DisplayText.Contains(versionSuffix));
        }

        private void LibrarySearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            OnPropertyChanged(nameof(IsTextEntryEmpty));

            TextChange textChange = e.Changes.Last();

            // We will invoke completion on text insertion and not deletion.
            if (textChange.AddedLength > 0 && !string.IsNullOrEmpty(Text))
            {
                VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    CompletionSet completionSet = await SearchService?.Invoke(Text, LibrarySearchBox.CaretIndex);

                    if (completionSet.Equals(null) || !completionSet.Completions.Any())
                    {
                        Flyout.IsOpen = false;
                        return;
                    }

                    int atIndex = Text.IndexOf('@');

                    if (atIndex >= 0)
                    {
                        completionSet.Completions = FilterOutUnmatchedItems(completionSet.Completions, Text.Substring(atIndex + 1));
                    }

                    Items.Clear();

                    foreach (CompletionItem entry in completionSet.Completions)
                    {
                        Items.Add(new Completion(entry, completionSet.Start, completionSet.Length));
                    }

                    PositionCompletions(completionSet.Length);

                    if (Items != null && Items.Count > 0 && Options.SelectedIndex == -1)
                    {
                        if (atIndex >= 0)
                        {
                            SelectedItem = Items.FirstOrDefault(x => x.CompletionItem.DisplayText.StartsWith(Text.Substring(atIndex + 1))) ?? Items[0];
                        }
                        else
                        {
                            string lastSelected = SelectedItem?.CompletionItem.InsertionText;
                            SelectedItem = Items.FirstOrDefault(x => x.CompletionItem.InsertionText == lastSelected) ?? Items[0];
                        }

                        Options.ScrollIntoView(SelectedItem);

                        Flyout.IsOpen = true;
                    }
                });
            }
        }

        private void Library_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!Options.IsKeyboardFocusWithin && !LibrarySearchBox.IsKeyboardFocusWithin && !Flyout.IsKeyboardFocusWithin)
            {
                Flyout.IsOpen = false;
            }
        }
    }
}
