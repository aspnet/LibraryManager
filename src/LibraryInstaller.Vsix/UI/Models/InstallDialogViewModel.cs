using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Web.LibraryInstaller.Contracts;

namespace Microsoft.Web.LibraryInstaller.Vsix.UI.Models
{
    public class InstallDialogViewModel : BindableBase
    {
        private readonly Action<bool> _closeDialog;
        private readonly string _configFileName;
        private readonly IDependencies _deps;
        private readonly Dispatcher _dispatcher;
        private readonly string _targetPath;
        private IProvider _activeProvider;
        private IReadOnlyList<ILibraryGroup> _availablePackages;
        private ILibraryCatalog _catalog;
        private IReadOnlyList<PackageItem> _displayRoots;
        private bool _isInstalling;
        private string _packageId;
        private ILibrary _selectedPackage;

        public InstallDialogViewModel(Dispatcher dispatcher, string configFileName, IDependencies deps, string targetPath, Action<bool> closeDialog)
        {
            _configFileName = configFileName;
            _targetPath = targetPath;
            _deps = deps;
            _dispatcher = dispatcher;
            _closeDialog = closeDialog;

            List<IProvider> providers = new List<IProvider>();
            foreach (IProvider provider in deps.Providers.OrderBy(x => x.Id))
            {
                ILibraryCatalog catalog = provider.GetCatalog();

                if (catalog == null)
                {
                    continue;
                }

                if (_catalog == null)
                {
                    _activeProvider = provider;
                    _catalog = catalog;
                }

                providers.Add(provider);
            }

            Providers = providers;
            InstallPackageCommand = ActionCommand.Create(InstallPackageAsync, CanInstallPackage, false);
            Task t = LoadPackagesAsync();
        }

        public IReadOnlyList<ILibraryGroup> AvailablePackages
        {
            get { return _availablePackages; }
            set { Set(ref _availablePackages, value); }
        }

        public IReadOnlyList<PackageItem> DisplayRoots
        {
            get { return _displayRoots; }
            set { Set(ref _displayRoots, value); }
        }

        public ICommand InstallPackageCommand { get; }

        public bool IsTreeViewEmpty => SelectedPackage == null;

        public string PackageId
        {
            get { return _packageId; }
            set
            {
                if (Set(ref _packageId, value))
                {
                    SelectedProvider.GetCatalog().GetLibraryAsync(_packageId, CancellationToken.None).ContinueWith(x =>
                    {
                        if (x.IsFaulted || x.IsCanceled)
                        {
                            SelectedPackage = null;
                            return;
                        }

                        SelectedPackage = x.Result;
                    });
                }
            }
        }

        public IReadOnlyList<IProvider> Providers { get; }

        public HashSet<string> SelectedFiles { get; private set; }

        public ILibrary SelectedPackage
        {
            get { return _selectedPackage; }
            set
            {
                if (Set(ref _selectedPackage, value) && value != null)
                {
                    OnPropertyChanged(nameof(IsTreeViewEmpty));
                    bool canUpdateInstallStatusValue = false;
                    HashSet<string> selectedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    Func<bool> canUpdateInstallStatus = () => canUpdateInstallStatusValue;
                    PackageItem root = new PackageItem(this, null, selectedFiles)
                    {
                        CanUpdateInstallStatus = canUpdateInstallStatus,
                        ItemType = PackageItemType.Folder,
                        Name = Path.GetFileName(_targetPath.TrimEnd('/', '\\')),
                        IsChecked = false
                    };

                    PackageItem packageItem = new PackageItem(this, root, selectedFiles)
                    {
                        CanUpdateInstallStatus = canUpdateInstallStatus,
                        Name = value.Name,
                        ItemType = PackageItemType.Folder,
                        IsChecked = false
                    };

                    //The node that children will be added to
                    PackageItem realParent = root;
                    //The node that will be set as the parent of the child nodes
                    PackageItem virtualParent = packageItem;

                    foreach (KeyValuePair<string, bool> file in value.Files)
                    {
                        string[] parts = file.Key.Split('/');
                        PackageItem currentRealParent = realParent;
                        PackageItem currentVirtualParent = virtualParent;

                        for (int i = 0; i < parts.Length; ++i)
                        {
                            bool isFolder = i != parts.Length - 1;

                            if (isFolder)
                            {
                                PackageItem next = currentRealParent.Children.FirstOrDefault(x => x.ItemType == PackageItemType.Folder && string.Equals(x.Name, parts[i]));

                                if (next == null)
                                {
                                    next = new PackageItem(this, currentVirtualParent, selectedFiles)
                                    {
                                        CanUpdateInstallStatus = canUpdateInstallStatus,
                                        Name = parts[i],
                                        ItemType = PackageItemType.Folder,
                                        IsChecked = false
                                    };

                                    List<PackageItem> children = new List<PackageItem>(currentRealParent.Children) { next };

                                    children.Sort((x, y) => x.ItemType == y.ItemType ? StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name) : y.ItemType == PackageItemType.Folder ? 1 : -1);

                                    currentRealParent.Children = children;

                                    if (currentVirtualParent != currentRealParent)
                                    {
                                        currentVirtualParent.Children = children;
                                    }
                                }

                                currentRealParent = next;
                                currentVirtualParent = next;
                            }
                            else
                            {
                                PackageItem next = new PackageItem(this, currentVirtualParent, selectedFiles)
                                {
                                    CanUpdateInstallStatus = canUpdateInstallStatus,
                                    FullPath = file.Key,
                                    Name = parts[i],
                                    ItemType = PackageItemType.File,
                                    IsChecked = file.Value,
                                };

                                if (next.IsChecked ?? false)
                                {
                                    selectedFiles.Add(next.FullPath);
                                }

                                List<PackageItem> children = new List<PackageItem>(currentRealParent.Children) { next };
                                children.Sort((x, y) => x.ItemType == y.ItemType ? StringComparer.OrdinalIgnoreCase.Compare(x.Name, y.Name) : y.ItemType == PackageItemType.Folder ? -1 : 1);

                                currentRealParent.Children = children;

                                if (currentVirtualParent != currentRealParent)
                                {
                                    currentVirtualParent.Children = children;
                                }
                            }
                        }
                    }

                    _dispatcher.Invoke(() =>
                    {
                        canUpdateInstallStatusValue = true;
                        SetNodeOpenStates(root);
                        DisplayRoots = new[] { root };
                        SelectedFiles = selectedFiles;
                        InstallPackageCommand.CanExecute(null);
                    });
                }
            }
        }

        public IProvider SelectedProvider
        {
            get { return _activeProvider; }
            set
            {
                if (Set(ref _activeProvider, value))
                {
                    _catalog = value.GetCatalog();
                    Task t = LoadPackagesAsync();
                }
            }
        }

        private static void SetNodeOpenStates(PackageItem item)
        {
            bool shouldBeOpen = false;

            foreach (PackageItem child in item.Children)
            {
                SetNodeOpenStates(child);
                shouldBeOpen |= child.IsChecked.GetValueOrDefault(true) || child.IsExpanded;
            }

            item.IsExpanded = shouldBeOpen;
        }

        private bool CanInstallPackage()
        {
            return !_isInstalling && SelectedPackage != null;
        }

        private async void InstallPackageAsync()
        {
            try
            {
                ILibrary selectedPackage = SelectedPackage;
                _isInstalling = true;
                InstallPackageCommand.CanExecute(null);
                Manifest manifest = await Manifest.FromFileAsync(_configFileName, _deps, CancellationToken.None).ConfigureAwait(false);
                string targetPath = _targetPath;

                if (!string.IsNullOrEmpty(_configFileName))
                {
                    Uri configContainerUri = new Uri(_configFileName, UriKind.Absolute);
                    Uri targetUri = new Uri(targetPath, UriKind.Absolute);
                    targetPath = configContainerUri.MakeRelativeUri(targetUri).ToString();
                }

                manifest.AddLibrary(new LibraryInstallationState
                {
                    LibraryId = PackageId,
                    ProviderId = selectedPackage.ProviderId,
                    DestinationPath = targetPath,
                    Files = SelectedFiles.ToList()
                });

                await manifest.SaveAsync(_configFileName, CancellationToken.None).ConfigureAwait(false);
                EnvDTE.Project project = VsHelpers.DTE.SelectedItems.Item(1)?.ProjectItem?.ContainingProject;
                project?.AddFileToProject(_configFileName);

                await manifest.RestoreAsync(CancellationToken.None).ConfigureAwait(false);
                _dispatcher.Invoke(() =>
                {
                    _closeDialog(true);
                });
            }
            catch { }
        }

        private async Task LoadPackagesAsync()
        {
            IReadOnlyList<ILibraryGroup> groups = await _catalog.SearchAsync(string.Empty, 50, CancellationToken.None).ConfigureAwait(false);
            AvailablePackages = groups;
        }
    }
}
