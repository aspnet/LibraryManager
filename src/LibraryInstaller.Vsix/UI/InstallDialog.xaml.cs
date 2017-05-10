using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using Microsoft.VisualStudio.Imaging;
using Microsoft.Web.LibraryInstaller.Contracts;
using Microsoft.Web.LibraryInstaller.Vsix.Models;

namespace Microsoft.Web.LibraryInstaller.Vsix
{
    public partial class InstallDialog
    {
        private readonly IDependencies _deps;
        private readonly string _folder;
        private string _configFileName;

        public InstallDialog(IDependencies dependencies, string configFileName, string folder)
        {
            InitializeComponent();

            _deps = dependencies;
            _folder = folder;
            _configFileName = configFileName;

            Loaded += OnLoaded;
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
            DialogResult = res;
            Close();
        }

        private void HyperlinkPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void NavigateToHomepage(object sender, RequestNavigateEventArgs e)
        {
            Hyperlink link = sender as Hyperlink;

            if (link != null)
            {
                Process.Start(link.NavigateUri.AbsoluteUri);
            }

            e.Handled = true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Icon = WpfUtil.GetIconForImageMoniker(KnownMonikers.JSWebScript, 16, 16);
            Title = Vsix.Name;

            ViewModel = new InstallDialogViewModel(Dispatcher, _configFileName, _deps, _folder, CloseDialog);

            FocusManager.SetFocusedElement(cbName, cbName);
        }
    }
}
