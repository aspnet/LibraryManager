// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Vsix.Resources;
using Microsoft.Web.LibraryManager.Vsix.UI.Controls.AutomationPeers;
using Microsoft.Web.LibraryManager.Vsix.UI.Extensions;
using Microsoft.Web.LibraryManager.Vsix.UI.Models;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Controls
{
    public partial class TextBoxWithSearch : INotifyPropertyChanged
    {
        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem), typeof(CompletionEntry), typeof(TextBoxWithSearch), new PropertyMetadata(default(CompletionEntry)));

        public TextBoxWithSearch()
        {
            InitializeComponent();

            DataContextChanged += HandleDataContextChanged;
        }

        private void HandleDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is SearchTextBoxViewModel old)
            {
                old.ExternalTextChange -= NotifyScreenReaderOfTextChanged;
            }

            if (e.NewValue is SearchTextBoxViewModel vm)
            {
                vm.ExternalTextChange += NotifyScreenReaderOfTextChanged;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TextControlTypeAutomationPeer(this);
        }

        private void NotifyScreenReaderOfTextChanged(object sender, EventArgs e)
        {
            _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                UIElementAutomationPeer.FromElement(SearchTextBox).RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
            });
        }

        public bool IsMouseOverFlyout => Options.IsMouseOver;

        public bool HasItems => CompletionEntries.Count > 0;

        public ObservableCollection<CompletionEntry> CompletionEntries { get; } = new ObservableCollection<CompletionEntry>();

        public CompletionEntry SelectedItem
        {
            get { return (CompletionEntry)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Commit(CompletionEntry completion)
        {
            if (completion == null || completion.CompletionItem.InsertionText == null)
            {
                return;
            }

            ViewModel.SearchText = completion.CompletionItem.InsertionText;
            ViewModel.OnExternalTextChange();
            SearchTextBox.CaretIndex = ViewModel.SearchText.IndexOf(completion.CompletionItem.DisplayText, StringComparison.OrdinalIgnoreCase) + completion.CompletionItem.DisplayText.Length;
            Flyout.IsOpen = false;
            SelectedItem = null;
        }

        private void HandleKeyPress(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Tab:
                    if (SelectedItem != null)
                    {
                        CommitSelectionAndMoveFocus();
                    }
                    break;
                case Key.Enter:
                    CommitSelectionAndMoveFocus();
                    e.Handled = true;
                    break;
                case Key.Escape:
                    Flyout.IsOpen = false;
                    SearchTextBox.ScrollToEnd();
                    SelectedItem = null;
                    e.Handled = true;
                    break;
                case Key.Down:
                    if (Options.Items.Count > 0)
                    {
                        Options.ScrollIntoView(Options.Items[0]);
                        var fe = (FrameworkElement)Options.ItemContainerGenerator.ContainerFromIndex(0);
                        fe?.Focus();
                        Options.SelectedIndex = 0;
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void HandleListBoxKeyPress(object sender, KeyEventArgs e)
        {
            int index = SearchTextBox.CaretIndex;

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
                        SelectedItem = CompletionEntries[0];
                        LostFocus -= OnLostFocus;
                        SearchTextBox.Focus();
                        SearchTextBox.CaretIndex = index;
                        LostFocus += OnLostFocus;
                    }
                    break;
                case Key.Escape:
                    Flyout.IsOpen = false;
                    SearchTextBox.ScrollToEnd();
                    SelectedItem = null;
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
                    SearchTextBox.Focus();
                    SearchTextBox.CaretIndex = index;
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
            SearchTextBox.Focus();
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (SelectedItem != null && !Options.IsKeyboardFocusWithin)
            {
                Commit(SelectedItem);
                SearchTextBox.ScrollToEnd();
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextChange textChange = e.Changes.Last();

            // We will invoke completion on text insertion and not deletion.
            // Also, we don't want to invoke completion on dialog load as we pre populate the target
            // location textbox with name of the folder when dialog is initially loaded.
            // In the case of deletion or replacement, if the completion flyout is already open, we
            // should still update the list, as the filtered items have likely changed.
            bool textInserted = textChange.AddedLength > 0 && SearchTextBox.CaretIndex > 0;
            if (textInserted || Flyout.IsOpen)
            {
                _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    // grab these WPF dependant things while we're still on the UI thread
                    int caretIndex = SearchTextBox.CaretIndex;
                    SearchTextBoxViewModel viewModel = ViewModel;

                    string textBeforeGetCompletion = SearchTextBox.Text;

                    // Switch to a background thread to not block the UI thread, as this operation can take
                    // a while for slow network connections
                    await TaskScheduler.Default;
                    CompletionSet completionSet = await viewModel.GetCompletionSetAsync(caretIndex);

                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                    // If the value has changed then this request is out of date.
                    // If focus is elsewhere the work below won't be used anyway
                    if (textBeforeGetCompletion != SearchTextBox.Text || !SearchTextBox.IsFocused)
                    {
                        return;
                    }

                    if (completionSet.Completions == null || !completionSet.Completions.Any())
                    {
                        Flyout.IsOpen = false;
                        completionSet = new CompletionSet
                        {
                            Completions = new[]
                            {
                                new CompletionItem
                                {
                                    DisplayText = Text.NoMatchesFound,
                                    InsertionText = null,
                                }
                            }
                        };
                    }

                    // repopulate the completion list
                    CompletionEntries.Clear();

                    List<CompletionItem> completions = GetSortedCompletionItems(completionSet);

                    foreach (CompletionItem entry in completions)
                    {
                        CompletionEntries.Add(new CompletionEntry(entry, completionSet.Start, completionSet.Length));
                    }

                    if (CompletionEntries != null && CompletionEntries.Count > 0 && Options.SelectedIndex == -1)
                    {
                        CompletionItem selectionCandidate = await ViewModel.GetRecommendedSelectedCompletionAsync(
                            completions: completions,
                            lastSelected: SelectedItem?.CompletionItem);
                        SelectedItem = CompletionEntries.FirstOrDefault(x => x.CompletionItem.InsertionText == selectionCandidate.InsertionText) ?? CompletionEntries[0];
                        Options.ScrollIntoView(SelectedItem);
                    }

                    Flyout.IsOpen = true;
                });
            }
        }

        private List<CompletionItem> GetSortedCompletionItems(CompletionSet completionSet)
        {
            var completions = completionSet.Completions.ToList();

            switch (completionSet.CompletionType)
            {
                case CompletionSortOrder.AsSpecified:
                    break;
                case CompletionSortOrder.Alphabetical:
                    completions.Sort((a, b) => string.Compare(a.DisplayText, b.DisplayText, StringComparison.OrdinalIgnoreCase));
                    break;
                case CompletionSortOrder.Version:
                    // return in descending order, so negate the result of the CompareTo
                    completions.Sort((a, b) => -SemanticVersion.Parse(a.DisplayText).CompareTo(SemanticVersion.Parse(b.DisplayText)));
                    break;
            }

            return completions;
        }

        private void SearchTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!Options.IsKeyboardFocusWithin && !SearchTextBox.IsKeyboardFocusWithin && !Flyout.IsKeyboardFocusWithin)
            {
                Flyout.IsOpen = false;
            }
        }

        protected override void OnAccessKey(AccessKeyEventArgs e)
        {
            SearchTextBox.Focus();
        }

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e?.Key == Key.Escape && Flyout.IsOpen)
            {
                SearchTextBox.Focus();
            }
        }

        private void SearchTextBox_GotKeyboardForcus(object sender, KeyboardFocusChangedEventArgs e)
        {
            // If the library search box is empty, the watermark text will be visible. We'll make sure that narrator reads it.
            if (string.IsNullOrEmpty(SearchTextBox.Text))
            {
                var removeCharacterExtension = new RemoveCharacterExtension(ViewModel.WatermarkText, "<>");
                string watermarkText = (string)removeCharacterExtension.ProvideValue(ServiceProvider.GlobalProvider);

                SearchTextBox.SetValue(AutomationProperties.HelpTextProperty, watermarkText);
            }
            else
            {
                SearchTextBox.ClearValue(AutomationProperties.HelpTextProperty);
            }

            e.Handled = true;
        }

        private SearchTextBoxViewModel ViewModel => DataContext as SearchTextBoxViewModel;
    }
}
