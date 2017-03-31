using System.Windows;
using System.Windows.Data;
using Microsoft.VisualStudio.PlatformUI;

namespace LibraryInstaller.Vsix.Controls
{
    /// <summary>
    /// Interaction logic for PackageContentsTreeView.xaml
    /// </summary>
    public partial class PackageContentsTreeView
    {
        public PackageContentsTreeView()
        {
            InitializeComponent();
            SetBinding(ImageThemingUtilities.ImageBackgroundColorProperty, new Binding
            {
                Source = Content,
                Path = new PropertyPath("Background"),
                Converter = new BrushToColorConverter()
            });
        }
    }
}
