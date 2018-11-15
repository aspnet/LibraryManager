// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.Web.LibraryManager.Contracts;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal static class VsHelpers
    {
        private static IComponentModel _compositionService;

        public static DTE2 DTE { get; } = GetService<DTE, DTE2>();

        public static TReturnType GetService<TServiceType, TReturnType>()
        {
            return (TReturnType)ServiceProvider.GlobalProvider.GetService(typeof(TServiceType));
        }

        public static string GetFileInVsix(string relativePath)
        {
            string folder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            return Path.Combine(folder, relativePath);
        }

        public static bool IsConfigFile(this ProjectItem item)
        {
            return item.Name.Equals(Constants.ConfigFileName, StringComparison.OrdinalIgnoreCase);
        }

        public static async Task CheckFileOutOfSourceControlAsync(string file)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (!File.Exists(file) || DTE.Solution.FindProjectItem(file) == null)
            {
                return;
            }

            if (DTE.SourceControl.IsItemUnderSCC(file) && !DTE.SourceControl.IsItemCheckedOut(file))
            {
                DTE.SourceControl.CheckOutItem(file);
            }

            var info = new FileInfo(file)
            {
                IsReadOnly = false
            };
        }

        internal static async Task OpenFileAsync(string configFilePath)
        {
            if (!string.IsNullOrEmpty(configFilePath))
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                DTE?.ItemOperations?.OpenFile(configFilePath);
            }
        }

        internal static async Task<ProjectItem> GetSelectedItemAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            ProjectItem projectItem = null;

            if (DTE?.SelectedItems.Count == 1)
            {
                SelectedItem selectedItem = VsHelpers.DTE.SelectedItems.Item(1);
                projectItem = selectedItem?.ProjectItem;
            }

            return projectItem;
        }

        public static async Task<Project> GetProjectOfSelectedItemAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            Project project = null;

            if (DTE?.SelectedItems.Count == 1)
            {
                SelectedItem selectedItem = DTE.SelectedItems.Item(1);
                project = selectedItem.Project ?? selectedItem.ProjectItem?.ContainingProject;
            }

            return project;
        }

        public static async Task AddFileToProjectAsync(this Project project, string file, string itemType = null)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (IsCapabilityMatch(project, Constants.DotNetCoreWebCapability))
            {
                return;
            }

            try
            {
                if (DTE.Solution.FindProjectItem(file) == null)
                {
                    ProjectItem item = project.ProjectItems.AddFromFile(file);

                    if (string.IsNullOrEmpty(itemType) || project.IsKind(Constants.WebsiteProject))
                    {
                        return;
                    }

                    item.Properties.Item("ItemType").Value = "None";
                }
            }
            catch (Exception ex)
            {
                Logger.LogEvent(ex.ToString(), LogLevel.Error);
                Telemetry.TrackException(nameof(AddFilesToProjectAsync), ex);
                System.Diagnostics.Debug.Write(ex);
            }
        }

        public static async Task AddFilesToProjectAsync(Project project, IEnumerable<string> files, Action<string, LogLevel> logAction, CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (project == null || IsCapabilityMatch(project, Constants.DotNetCoreWebCapability))
            {
                return;
            }

            if (project.IsKind(Constants.WebsiteProject))
            {
                Command command = DTE.Commands.Item("SolutionExplorer.Refresh");

                if (command.IsAvailable)
                {
                    DTE.ExecuteCommand(command.Name);
                }

                return;
            }

            var solutionService = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;

            IVsHierarchy hierarchy = null;
            if (solutionService != null && !ErrorHandler.Failed(solutionService.GetProjectOfUniqueName(project.UniqueName, out hierarchy)))
            {
                if (hierarchy == null)
                {
                    return;
                }

                var vsProject = (IVsProject)hierarchy;

                await AddFilesToHierarchyAsync(hierarchy, files, logAction, cancellationToken);
            }

        }

        public static async Task<string> GetRootFolderAsync(this Project project)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (project == null)
            {
                return null;
            }

            if (project.IsKind(ProjectKinds.vsProjectKindSolutionFolder))
            {
                return Path.GetDirectoryName(DTE.Solution.FullName);
            }

            if (string.IsNullOrEmpty(project.FullName))
            {
                return null;
            }

            string fullPath;

            try
            {
                fullPath = project.Properties.Item("FullPath").Value as string;
            }
            catch (ArgumentException)
            {
                try
                {
                    // MFC projects don't have FullPath, and there seems to be no way to query existence
                    fullPath = project.Properties.Item("ProjectDirectory").Value as string;
                }
                catch (ArgumentException)
                {
                    // Installer projects have a ProjectPath.
                    fullPath = project.Properties.Item("ProjectPath").Value as string;
                }
            }

            if (string.IsNullOrEmpty(fullPath))
            {
                return File.Exists(project.FullName) ? Path.GetDirectoryName(project.FullName) : null;
            }

            if (Directory.Exists(fullPath))
            {
                return fullPath;
            }

            if (File.Exists(fullPath))
            {
                return Path.GetDirectoryName(fullPath);
            }

            return null;
        }

        public static bool IsKind(this Project project, params string[] kindGuids)
        {
            foreach (string guid in kindGuids)
            {
                if (project.Kind.Equals(guid, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static async Task<bool> IsDotNetCoreWebProjectAsync(Project project)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (project == null || IsCapabilityMatch(project, Constants.DotNetCoreWebCapability))
            {
                return true;
            }

            return false;
        }

        public static async Task<bool> DeleteFilesFromProjectAsync(Project project, IEnumerable<string> filePaths, Action<string, LogLevel> logAction, CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            int batchSize = 10;

            try
            {
                IVsHierarchy hierarchy = GetHierarchy(project);
                IVsProjectBuildSystem bldSystem = hierarchy as IVsProjectBuildSystem;
                List<string> filesToRemove = filePaths.ToList();

                while (filesToRemove.Any())
                {
                    List<string> nextBatch = filesToRemove.Take(batchSize).ToList();
                    bool success = await DeleteProjectItemsInBatchAsync(hierarchy, nextBatch, logAction, cancellationToken);

                    if (!success)
                    {
                        return false;
                    }

                    await System.Threading.Tasks.Task.Yield();

                    int countToDelete = Math.Min(filesToRemove.Count(), batchSize);
                    filesToRemove.RemoveRange(0, countToDelete);
                }

                return true;
            }
            catch (Exception ex)
            {
                Telemetry.TrackException(nameof(DeleteFilesFromProjectAsync), ex);
                return false;
            }
        }

        public static void SatisfyImportsOnce(this object o)
        {
            _compositionService = _compositionService ?? GetService<SComponentModel, IComponentModel>();

            if (_compositionService != null)
            {
                _compositionService.DefaultCompositionService.SatisfyImportsOnce(o);
            }
        }

        public static async Task<bool> ProjectContainsManifestFileAsync(Project project)
        {
            string rootPath = await GetRootFolderAsync(project);

            if (!string.IsNullOrEmpty(rootPath))
            {
                string configFilePath = Path.Combine(rootPath, Constants.ConfigFileName);

                if (File.Exists(configFilePath))
                {
                    Telemetry.TrackUserTask("ProjectContainsLibMan", TelemetryResult.None, new[] { new KeyValuePair<string, object>("ProjectGUID", project.Kind) });
                    return true;
                }
            }

            return false;
        }

        public static async Task<bool> SolutionContainsManifestFileAsync(IVsSolution solution)
        {
            IEnumerable<IVsHierarchy> hierarchies = GetProjectsInSolution(solution, __VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION);

            foreach (IVsHierarchy hierarchy in hierarchies)
            {
                Project project = GetDTEProject(hierarchy);

                if (project != null && await ProjectContainsManifestFileAsync(project))
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<IVsHierarchy> GetProjectsInSolution(IVsSolution solution, __VSENUMPROJFLAGS flags)
        {
            if (solution == null)
            {
                yield break;
            }

            Guid guid = Guid.Empty;
            if (ErrorHandler.Failed(solution.GetProjectEnum((uint)flags, ref guid, out IEnumHierarchies enumHierarchies)) || enumHierarchies == null)
            {
                yield break;
            }

            IVsHierarchy[] hierarchy = new IVsHierarchy[1];
            while (ErrorHandler.Succeeded(enumHierarchies.Next(1, hierarchy, out uint fetched)) && fetched == 1)
            {
                if (hierarchy.Length > 0 && hierarchy[0] != null)
                {
                    yield return hierarchy[0];
                }
            }
        }

        public static Project GetDTEProject(IVsHierarchy hierarchy)
        {
            if (ErrorHandler.Succeeded(hierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object obj)))
            {
                return obj as Project;
            }

            return null;
        }

        public static bool IsCapabilityMatch(Project project, string capability)
        {
            IVsHierarchy hierarchy = GetHierarchy(project);

            if (hierarchy != null)
            {
                return hierarchy.IsCapabilityMatch(capability);
            }

            return false;
        }

        public static IVsHierarchy GetHierarchy(Project project)
        {
            IVsSolution solution = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;

            if (ErrorHandler.Succeeded(solution.GetProjectOfUniqueName(project.FullName, out IVsHierarchy hierarchy)))
            {
                return hierarchy;
            }

            return null;
        }

        public static Project GetDTEProjectFromConfig(string file)
        {
            try
            {
                ProjectItem projectItem = DTE.Solution.FindProjectItem(file);
                if (projectItem != null)
                {
                    return projectItem.ContainingProject;
                }
            }
            catch (Exception ex)
            {
                Logger.LogEvent(ex.ToString(), LogLevel.Error);
                Telemetry.TrackException(nameof(GetDTEProjectFromConfig), ex);
                System.Diagnostics.Debug.Write(ex);
            }

            return null;
        }

        private static async Task<bool> AddFilesToHierarchyAsync(IVsHierarchy hierarchy, IEnumerable<string> filePaths, Action<string, LogLevel> logAction, CancellationToken cancellationToken)
        {
            int batchSize = 10;

            List<string> filesToAdd = filePaths.ToList();

            while (filesToAdd.Any())
            {
                List<string> nextBatch = filesToAdd.Take(batchSize).ToList();
                bool success = await AddProjectItemsInBatchAsync(hierarchy, nextBatch, logAction, cancellationToken);

                if (!success)
                {
                    return false;
                }

                await System.Threading.Tasks.Task.Yield();

                int countToDelete = filesToAdd.Count() >= batchSize ? batchSize : filesToAdd.Count();
                filesToAdd.RemoveRange(0, countToDelete);
            }

            return true;
        }

        private static async Task<bool> AddProjectItemsInBatchAsync(IVsHierarchy vsHierarchy, List<string> filePaths, Action<string, LogLevel> logAction, CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsProjectBuildSystem bldSystem = vsHierarchy as IVsProjectBuildSystem;

            try
            {
                if (bldSystem != null)
                {
                    bldSystem.StartBatchEdit();
                }

                cancellationToken.ThrowIfCancellationRequested();

                var vsProject = (IVsProject)vsHierarchy;
                VSADDRESULT[] result = new VSADDRESULT[filePaths.Count()];

                vsProject.AddItem(VSConstants.VSITEMID_ROOT,
                            VSADDITEMOPERATION.VSADDITEMOP_LINKTOFILE,
                            string.Empty,
                            (uint)filePaths.Count(),
                            filePaths.ToArray(),
                            IntPtr.Zero,
                            result);

                foreach (string filePath in filePaths)
                {
                    logAction.Invoke(string.Format(Resources.Text.LibraryAddedToProject, filePath.Replace('\\', '/')), LogLevel.Operation);
                }
            }
            catch(Exception ex)
            {
                Telemetry.TrackException(nameof(AddProjectItemsInBatchAsync), ex);
                return false;
            }
            finally
            {
                if (bldSystem != null)
                {
                    bldSystem.EndBatchEdit();
                }
            }

            return true;
        }

        private static async Task<bool> DeleteProjectItemsInBatchAsync(IVsHierarchy hierarchy, IEnumerable<string> filePaths, Action<string, LogLevel> logAction, CancellationToken cancellationToken)
        {

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsProjectBuildSystem bldSystem = hierarchy as IVsProjectBuildSystem;
            HashSet<ProjectItem> folders = new HashSet<ProjectItem>();

            try
            {
                if (bldSystem != null)
                {
                    bldSystem.StartBatchEdit();
                }

                foreach (string filePath in filePaths)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ProjectItem item = DTE.Solution.FindProjectItem(filePath);

                    if (item != null)
                    {
                        ProjectItem parentFolder = item.Collection.Parent as ProjectItem;
                        folders.Add(parentFolder);
                        item.Delete();
                        logAction.Invoke(string.Format(Resources.Text.LibraryDeletedFromProject, filePath.Replace('\\', '/')), LogLevel.Operation);
                    }
                }

                DeleteEmptyFolders(folders);
            }
            catch(Exception ex)
            {
                Telemetry.TrackException(nameof(DeleteProjectItemsInBatchAsync), ex);
                return false;
            }
            finally
            {
                if (bldSystem != null)
                {
                    bldSystem.EndBatchEdit();
                }
            }

            return true;
        }

        private static void DeleteEmptyFolders(HashSet<ProjectItem> folders)
        {
            foreach (ProjectItem folder in folders)
            {
                if (folder.ProjectItems.Count == 0)
                {
                    folder.Delete();
                }
            }
        }
    }
}
