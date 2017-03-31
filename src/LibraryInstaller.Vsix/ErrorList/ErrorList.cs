using LibraryInstaller.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace LibraryInstaller.Vsix
{
    public class ErrorList
    {
        public ErrorList(string projectName, string configFileName)
        {
            ProjectName = projectName;
            ConfigFileName = configFileName;
            Errors = new List<DisplayError>();
        }

        public string ProjectName { get; set; }
        public string ConfigFileName { get; set; }
        public List<DisplayError> Errors { get; }

        public bool HandleErrors(IEnumerable<ILibraryInstallationResult> results)
        {
            foreach (ILibraryInstallationResult result in results)
            {
                if (!result.Success)
                {
                    IEnumerable<DisplayError> displayErrors = result.Errors.Select(error => new DisplayError(error));
                    Errors.AddRange(displayErrors);

                    foreach (IError error in result.Errors)
                    {
                        Logger.LogEvent(error.Message, Level.Operation);
                    }
                }
            }

            PushToErrorList();
            return Errors.Any();
        }

        private void PushToErrorList()
        {
            TableDataSource.Instance.CleanErrors(ConfigFileName);

            if (Errors.Any())
            {
                TableDataSource.Instance.AddErrors(Errors, ProjectName, ConfigFileName);
            }
        }
    }
}
