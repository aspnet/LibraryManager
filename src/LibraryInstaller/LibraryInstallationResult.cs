using LibraryInstaller.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace LibraryInstaller
{
    internal class LibraryInstallationResult : ILibraryInstallationResult
    {
        public LibraryInstallationResult(ILibraryInstallationState installationState)
        {
            Errors = new List<IError>();
            InstallationState = installationState;
        }

        public LibraryInstallationResult(ILibraryInstallationState installationState, params IError[] error)
        {
            var list = new List<IError>();
            list.AddRange(error);
            Errors = list;
            InstallationState = installationState;
        }

        public bool Cancelled
        {
            get;
            set;
        }

        public bool Success
        {
            get { return !Cancelled && !Errors.Any(); }
        }

        public IList<IError> Errors
        {
            get;
            set;
        }

        public ILibraryInstallationState InstallationState
        {
            get;
            set;
        }

        public static LibraryInstallationResult FromSuccess(ILibraryInstallationState installationState)
        {
            return new LibraryInstallationResult(installationState);
        }

        public static LibraryInstallationResult FromCancelled(ILibraryInstallationState installationState)
        {
            return new LibraryInstallationResult(installationState)
            {
                Cancelled = true
            };
        }
    }
}
