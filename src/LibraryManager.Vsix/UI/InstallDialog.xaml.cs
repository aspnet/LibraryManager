using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.Imaging;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Vsix.UI.Models;

namespace Microsoft.Web.LibraryManager.Vsix.UI
{
    public partial class InstallDialog
    {
        private readonly IDependencies _deps;
        private readonly string _folder;
        private readonly string _configFileName;

        public InstallDialog(IDependencies dependencies, string configFileName, string folder)
        {
            InitializeComponent();

            _deps = dependencies;
            _folder = folder;
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

        public InstallDialogViewModel ViewModel
        {
            get { return DataContext as InstallDialogViewModel; }
            set { DataContext = value; }
        }

        public Task<CompletionSet> PerformSearch(string searchText, int caretPosition)
        {
            return ViewModel.SelectedProvider.GetCatalog().GetLibraryCompletionSetAsync(searchText, caretPosition);
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
            Icon = WpfUtil.GetIconForImageMoniker(KnownMonikers.JSWebScript, 16, 16);
            Title = Vsix.Name;

            ViewModel = new InstallDialogViewModel(Dispatcher, _configFileName, _deps, _folder, CloseDialog);

            FocusManager.SetFocusedElement(cbName, cbName);
        }

        private void ThemedWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!cbName.IsMouseOver && !cbName.IsMouseOverFlyout)
            {
                TraversalRequest request = new TraversalRequest(FocusNavigationDirection.Next);
                MoveFocus(request);
            }
        }
    }
}
