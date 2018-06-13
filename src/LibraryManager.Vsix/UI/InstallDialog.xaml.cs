using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.Text;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Vsix.UI.Models;

namespace Microsoft.Web.LibraryManager.Vsix.UI
{
    internal partial class InstallDialog : DialogWindow
    {
        private readonly IDependencies _deps;
        private readonly string _fullPath;
        private readonly string _configFileName;
        private readonly ILibraryCommandService _libraryCommandService;

        public InstallDialog(IDependencies dependencies, ILibraryCommandService libraryCommandService, string configFileName, string fullPath, string rootFolder)
        {
            InstallationFolder.DestinationFolder = fullPath.Replace(rootFolder, "").Replace('\\', '/');

            InitializeComponent();

            _libraryCommandService = libraryCommandService;
            _deps = dependencies;
            _fullPath = fullPath;
            _configFileName = configFileName;

            LostKeyboardFocus += InstallDialog_LostKeyboardFocus;
            Loaded += OnLoaded;
        }

        private void InstallDialog_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (!IsKeyboardFocusWithin && !(e.NewFocus is ListBoxItem))
            {
                TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Next);
                MoveFocus(request);
            }
        }

        internal InstallDialogViewModel ViewModel
        {
            get { return DataContext as InstallDialogViewModel; }
            set { DataContext = value; }
        }

        public Task<CompletionSet> PerformSearch(string searchText, int caretPosition)
        {
            try
            {
                return ViewModel.SelectedProvider.GetCatalog().GetLibraryCompletionSetAsync(searchText, caretPosition);
            }
            catch (InvalidLibraryException ex)
            {
                // Make the warning visible with ex.Message
                return Task.FromResult<CompletionSet>(default(CompletionSet));
            }
        }

        public Task<CompletionSet> TargetLocationSearch(string searchText, int caretPosition)
        {
            // Target location text box is pre populated with name of the folder from where the - Add Client-Side Library command was invoked.
            // If the user clears the field at any point, we should make sure the Install button is disabled till valid folder name is provided.
            if (String.IsNullOrEmpty(searchText))
            {
                InstallButton.IsEnabled = false;
            }
            else
            {
                InstallButton.IsEnabled = true;
            }
        
            Dependencies dependencies = Dependencies.FromConfigFile(_configFileName);
            string cwd = dependencies?.GetHostInteractions().WorkingDirectory;

            IEnumerable<Tuple<string, string>> completions = GetCompletions(cwd, searchText, caretPosition, out Span textSpan);

            CompletionSet completionSet = new CompletionSet
            {
                Start = 0,
                Length = searchText.Length
            };

            List<CompletionItem> completionItems = new List<CompletionItem>();

            foreach (Tuple<string, string> completion in completions)
            {
                CompletionItem completionItem = new CompletionItem
                {
                    DisplayText = completion.Item1,
                    InsertionText = completion.Item2,
                };

                completionItems.Add(completionItem);
            }

            completionSet.Completions = completionItems;

            return Task.FromResult(completionSet);
        }

        private IEnumerable<Tuple<string, string>> GetCompletions(string cwd, string value, int caretPosition, out Span span)
        {
            span = new Span(0, value.Length);
            List<Tuple<string, string>> completions = new List<Tuple<string, string>>();
            int index = 0;

            if (value.Contains("/"))
            {
                index = value.Length >= caretPosition - 1 ? value.LastIndexOf('/', Math.Max(caretPosition - 1, 0)) : value.Length;
            }
       
            string prefix = "";

            if (index > 0)
            {
                prefix = value.Substring(0, index + 1);
                cwd = Path.Combine(cwd, prefix);
                span = new Span(index + 1, value.Length - index - 1);
            }

            DirectoryInfo directoryInfo = new DirectoryInfo(cwd);

            if (directoryInfo.Exists)
            {
                foreach (FileSystemInfo item in directoryInfo.EnumerateDirectories())
                {
                    completions.Add(Tuple.Create(item.Name + "/", prefix + item.Name + "/"));
                }
            }

            return completions;
        }

        private void CloseDialog(bool res)
        {
            try
            {
                DialogResult = res;
            }
            catch { }
            Close();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {           
            ViewModel = new InstallDialogViewModel(Dispatcher, _libraryCommandService, _configFileName, _deps, _fullPath, CloseDialog);

            FocusManager.SetFocusedElement(LibrarySearchBox, LibrarySearchBox);
        }

        private void ThemedWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!LibrarySearchBox.IsMouseOver && !LibrarySearchBox.IsMouseOverFlyout)
            {
                TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Next);
                MoveFocus(request);
            }
        }

        private void IncludeAllLibraryFilesRb_Checked(object sender, RoutedEventArgs e)
        {
            LibraryFilesToInstallTreeView.IsEnabled = false;
        }

        private void ChooseSpecificFilesRb_Checked(object sender, RoutedEventArgs e)
        {
            LibraryFilesToInstallTreeView.IsEnabled = true;
        }

        private void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // User chose to reset provider. So we need to reset all controls to initial state.
            if (!ViewModel.IsTreeViewEmpty)
            {
                IncludeAllLibraryFilesRb.IsChecked = true;
                LibrarySearchBox.Text = String.Empty;
                ViewModel.IsTreeViewEmpty = true;
                ViewModel.PackageId = null;
                ViewModel.AnyFileSelected = false;
            }
            else
            {
                LibrarySearchBox.RefreshSearch();
            }
        }

        private void InstallButton_Clicked(object sender, RoutedEventArgs e)
        {
            CloseDialog(true);
        }
    }
}
