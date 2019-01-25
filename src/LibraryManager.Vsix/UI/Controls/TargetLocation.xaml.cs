using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Vsix.UI.Models;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Controls
{
    public partial class TargetLocation : INotifyPropertyChanged
    {
        public static readonly DependencyProperty CaretIndexProperty = DependencyProperty.Register(
            nameof(CaretIndex), typeof(int), typeof(TargetLocation), new PropertyMetadata(default(int)));

        public static readonly DependencyProperty SearchServiceProperty = DependencyProperty.Register(
            nameof(SearchService), typeof(Func<string, int, Task<CompletionSet>>), typeof(TargetLocation), new PropertyMetadata(default(Func<string, int, Task<CompletionSet>>)));

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem), typeof(Completion), typeof(TargetLocation), new PropertyMetadata(default(Completion)));

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(TargetLocation), new PropertyMetadata(default(string)));

        private string _text;
        private string _lastSuggestedTargetLocation;
        private string _baseFolder;
        private BindLibraryNameToTargetLocation _libraryNameChange;

        public TargetLocation()
        {
            InitializeComponent();

            // Pre populate textBox with folder name
            TargetLocationSearchTextBox.Text = InstallationFolder.DestinationFolder;
            _baseFolder = InstallationFolder.DestinationFolder;
            _lastSuggestedTargetLocation = InstallationFolder.DestinationFolder;

            this.Loaded += TargetLocation_Loaded;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void TargetLocation_Loaded(object sender, RoutedEventArgs e)
        {
            InstallDialogViewModel viewModel = ((InstallDialog)Window.GetWindow(this)).ViewModel;
            _libraryNameChange = viewModel.LibraryNameChange;
            _libraryNameChange.PropertyChanged += this.LibraryNameChanged;

            Window window = Window.GetWindow(TargetLocationSearchTextBox);

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

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TargetLocationAutomationPeer(this);
        }

        private void RepositionPopup(object sender, EventArgs e)
        {
            double offset = Flyout.HorizontalOffset;

            Flyout.HorizontalOffset = offset + 1;
            Flyout.HorizontalOffset = offset;
        }

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

        internal Completion SelectedItem
        {
            get { return (Completion)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        public string Text
        {
            get
            {
                _text = (string)GetValue(TextProperty);

                InstallationFolder.DestinationFolder = _text;
                return _text;
            }
            set
            {
                SetValue(TextProperty, value);
                InstallationFolder.DestinationFolder = value;
            }
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
            TargetLocationSearchTextBox.CaretIndex = Text.IndexOf(completion.CompletionItem.DisplayText, StringComparison.OrdinalIgnoreCase) + completion.CompletionItem.DisplayText.Length;
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
                        TargetLocationSearchTextBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
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
                    TargetLocationSearchTextBox.ScrollToEnd();
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
            int index = TargetLocationSearchTextBox.CaretIndex;

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
                        TargetLocationSearchTextBox.Focus();
                        TargetLocationSearchTextBox.CaretIndex = index;
                        LostFocus += OnLostFocus;
                    }
                    break;
                case Key.Escape:
                    Flyout.IsOpen = false;
                    TargetLocationSearchTextBox.ScrollToEnd();
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
                    TargetLocationSearchTextBox.Focus();
                    TargetLocationSearchTextBox.CaretIndex = index;
                    LostFocus += OnLostFocus;
                    break;
            }
        }

        private void OnItemCommitGesture(object sender, MouseButtonEventArgs e)
        {
            Commit(SelectedItem);
            e.Handled = true;
        }

        private void CommitSelectionAndMoveFocus()
        {
            Commit(SelectedItem);
            TargetLocationSearchTextBox.Focus();
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null && !Options.IsKeyboardFocusWithin)
            {
                Commit(SelectedItem);
                TargetLocationSearchTextBox.ScrollToEnd();
            }
        }

        private void PositionCompletions(int index)
        {
            Rect r = TargetLocationSearchTextBox.GetRectFromCharacterIndex(index);
            Flyout.HorizontalOffset = r.Left - 7;
            Options.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Flyout.Width = Options.DesiredSize.Width;
        }

        private void TargetLocationSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextChange textChange = e.Changes.Last();

            // We will invoke completion on text insertion and not deletion.
            // Also, we don't want to invoke completion on dialog load as we pre populate the target
            // location textbox with name of the folder when dialog is initially loaded.
            if (textChange.AddedLength > 0 && TargetLocationSearchTextBox.CaretIndex > 0)
            {
                VisualStudio.Shell.ThreadHelper.JoinableTaskFactory.Run(async () =>
                {
                    CompletionSet completionSet = await SearchService?.Invoke(Text, TargetLocationSearchTextBox.CaretIndex);

                    if (completionSet.Equals(null) || !completionSet.Completions.Any())
                    {
                        Flyout.IsOpen = false;
                        return;
                    }

                    Items.Clear();

                    foreach (CompletionItem entry in completionSet.Completions)
                    {
                        Items.Add(new Completion(entry, completionSet.Start, completionSet.Length));
                    }

                    PositionCompletions(completionSet.Length);

                    if (Items != null && Items.Count > 0 && Options.SelectedIndex == -1)
                    {
                        string lastSelected = SelectedItem?.CompletionItem.InsertionText;
                        SelectedItem = Items.FirstOrDefault(x => x.CompletionItem.InsertionText == lastSelected) ?? Items[0];
                        Options.ScrollIntoView(SelectedItem);
                    }

                    Flyout.IsOpen = true;
                });
            }

            InstallationFolder.DestinationFolder = TargetLocationSearchTextBox.Text;
        }

        private void TargetLocation_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!Options.IsKeyboardFocusWithin && !TargetLocationSearchTextBox.IsKeyboardFocusWithin && !Flyout.IsKeyboardFocusWithin)
            {
                Flyout.IsOpen = false;
            }
        }

        private void LibraryNameChanged(object sender, PropertyChangedEventArgs e)
        {
            string targetLibrary = _libraryNameChange.LibraryName;

            if (!string.IsNullOrEmpty(targetLibrary))
            {
                this.Dispatcher.Invoke(() =>
                {
                    if (TargetLocationSearchTextBox.Text.Equals(_lastSuggestedTargetLocation))
                    {
                        if (targetLibrary.Length > 0 && targetLibrary[targetLibrary.Length - 1] == '/')
                        {
                            targetLibrary = targetLibrary.Substring(0, targetLibrary.Length - 1);
                        }

                        TargetLocationSearchTextBox.Text = _baseFolder + targetLibrary + '/';
                        InstallationFolder.DestinationFolder = TargetLocationSearchTextBox.Text;
                        _lastSuggestedTargetLocation = TargetLocationSearchTextBox.Text;
                    }
                });
            }
        }

        protected override void OnAccessKey(AccessKeyEventArgs e)
        {
            TargetLocationSearchTextBox.Focus();
        }
    }
}
