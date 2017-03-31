using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using Microsoft.VisualStudio.Telemetry;

namespace LibraryInstaller.Vsix
{
    internal sealed class RestoreSolutionCommand
    {
        private readonly Package _package;
        private static string[] _ignore = { "\\node_modules\\", "\\bower_components\\", "\\jspm_packages\\", "\\lib\\", "\\vendor\\" };

        private RestoreSolutionCommand(Package package, OleMenuCommandService commandService)
        {
            _package = package;

            var cmdId = new CommandID(PackageGuids.guidLibraryInstallerPackageCmdSet, PackageIds.RestoreSolution);
            var cmd = new OleMenuCommand(ExecuteAsync, cmdId);
            commandService.AddCommand(cmd);
        }

        public static RestoreSolutionCommand Instance
        {
            get;
            private set;
        }

        private IServiceProvider ServiceProvider => _package;

        public static void Initialize(Package package, OleMenuCommandService commandService)
        {
            Instance = new RestoreSolutionCommand(package, commandService);
        }

        private async void ExecuteAsync(object sender, EventArgs e)
        {
            var solution = (IVsSolution)ServiceProvider.GetService(typeof(SVsSolution));
            IEnumerable<IVsHierarchy> hierarchies = GetProjectsInSolution(solution, __VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION);
            var configFiles = new List<string>();

            foreach (IVsHierarchy hierarchy in hierarchies)
            {
                Project project = GetDTEProject(hierarchy);

                configFiles.AddRange(FindConfigFiles(project.ProjectItems));
            }

            await LibraryHelpers.RestoreAsync(configFiles);

            Telemetry.TrackUserTask("restoresolution");
        }

        public static IEnumerable<IVsHierarchy> GetProjectsInSolution(IVsSolution solution, __VSENUMPROJFLAGS flags)
        {
            if (solution == null)
                yield break;

            Guid guid = Guid.Empty;
            solution.GetProjectEnum((uint)flags, ref guid, out IEnumHierarchies enumHierarchies);
            if (enumHierarchies == null)
                yield break;

            IVsHierarchy[] hierarchy = new IVsHierarchy[1];
            while (enumHierarchies.Next(1, hierarchy, out uint fetched) == VSConstants.S_OK && fetched == 1)
            {
                if (hierarchy.Length > 0 && hierarchy[0] != null)
                    yield return hierarchy[0];
            }
        }

        public static Project GetDTEProject(IVsHierarchy hierarchy)
        {
            hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object obj);
            return obj as Project;
        }

        private static IEnumerable<string> FindConfigFiles(ProjectItems items, List<string> files = null)
        {
            if (files == null)
                files = new List<string>();

            foreach (ProjectItem item in items)
            {
                if (item.Name.Equals(Constants.ConfigFileName, StringComparison.OrdinalIgnoreCase))
                {
                    files.Add(item.FileNames[1]);
                }

                if (!ShouldIgnore(item.FileNames[1]) && item.ProjectItems != null)
                    FindConfigFiles(item.ProjectItems, files);
            }

            return files;
        }

        public static bool ShouldIgnore(string filePath)
        {
            return _ignore.Any(ign => filePath.IndexOf(ign, StringComparison.OrdinalIgnoreCase) > -1);
        }
    }
}
