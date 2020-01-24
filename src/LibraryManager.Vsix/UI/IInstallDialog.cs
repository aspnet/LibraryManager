using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Vsix.UI
{
    /// <summary>
    /// Test contract for add client side libraries dialog.
    /// </summary>
    public interface IInstallDialog
    {
        string Provider { get; set; }

        string Library { get; set; }

        Task ClickInstallAsync();

        void CloseDialog();

        bool IsAnyFileSelected { get; }
    }
}
