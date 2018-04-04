// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Reflection;

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

        public static void CheckFileOutOfSourceControl(string file)
        {
            if (!File.Exists(file) || DTE.Solution.FindProjectItem(file) == null)
                return;

            if (DTE.SourceControl.IsItemUnderSCC(file) && !DTE.SourceControl.IsItemCheckedOut(file))
                DTE.SourceControl.CheckOutItem(file);

            var info = new FileInfo(file)
            {
                IsReadOnly = false
            };
        }

        public static void AddFileToProject(this Project project, string file, string itemType = null)
        {
            if (IsCapabilityMatch(project, Constants.DotNetCoreWebCapability))
                return;

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
                System.Diagnostics.Debug.Write(ex);
                // TODO: Implement logging
            }
        }

        public static void AddFilesToProject(this Project project, IEnumerable<string> files)
        {
            if (project == null || IsCapabilityMatch(project, Constants.DotNetCoreWebCapability))
                return;

            if (project.IsKind(Constants.WebsiteProject))
            {
                Command command = DTE.Commands.Item("SolutionExplorer.Refresh");

                if (command.IsAvailable)
                    DTE.ExecuteCommand(command.Name);

                return;
            }

            var solutionService = Package.GetGlobalService(typeof(SVsSolution)) as IVsSolution;

            IVsHierarchy hierarchy = null;
            solutionService?.GetProjectOfUniqueName(project.UniqueName, out hierarchy);

            if (hierarchy == null)
                return;

            var ip = (IVsProject)hierarchy;
            VSADDRESULT[] result = new VSADDRESULT[files.Count()];

            ip.AddItem(VSConstants.VSITEMID_ROOT,
                       VSADDITEMOPERATION.VSADDITEMOP_LINKTOFILE,
                       string.Empty,
                       (uint)files.Count(),
                       files.ToArray(),
                       IntPtr.Zero,
                       result);
        }

        public static string GetRootFolder(this Project project)
        {
            if (project == null)
                return null;

            if (project.IsKind(ProjectKinds.vsProjectKindSolutionFolder))
                return Path.GetDirectoryName(DTE.Solution.FullName);

            if (string.IsNullOrEmpty(project.FullName))
                return null;

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
                return File.Exists(project.FullName) ? Path.GetDirectoryName(project.FullName) : null;

            if (Directory.Exists(fullPath))
                return fullPath;

            if (File.Exists(fullPath))
                return Path.GetDirectoryName(fullPath);

            return null;
        }

        public static bool IsKind(this Project project, params string[] kindGuids)
        {
            foreach (string guid in kindGuids)
            {
                if (project.Kind.Equals(guid, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static void SatisfyImportsOnce(this object o)
        {
            _compositionService = _compositionService ?? GetService<SComponentModel, IComponentModel>();

            if (_compositionService != null)
            {
                _compositionService.DefaultCompositionService.SatisfyImportsOnce(o);
            }
        }

        public static bool ProjectContainsManifestFile(Project project)
        {
            string configFilePath = Path.Combine(GetRootFolder(project), Constants.ConfigFileName);

            if (File.Exists(configFilePath))
            {
                return true;
            }

            return false;
        }

        public static bool SolutionContainsManifestFile(IVsSolution solution)
        {
            IEnumerable<IVsHierarchy> hierarchies = GetProjectsInSolution(solution, __VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION);

            foreach (IVsHierarchy hierarchy in hierarchies)
            {
                Project project = GetDTEProject(hierarchy);

                if (project != null && ProjectContainsManifestFile(project))
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<IVsHierarchy> GetProjectsInSolution(IVsSolution solution, __VSENUMPROJFLAGS flags)
        {
            if (solution == null)
                yield break;

            Guid guid = Guid.Empty;
            if (ErrorHandler.Failed(solution.GetProjectEnum((uint)flags, ref guid, out IEnumHierarchies enumHierarchies)) || enumHierarchies == null)
            {
                yield break;
            }

            IVsHierarchy[] hierarchy = new IVsHierarchy[1];
            while (enumHierarchies.Next(1, hierarchy, out uint fetched) == VSConstants.S_OK && fetched == 1)
            {
                if (hierarchy.Length > 0 && hierarchy[0] != null)
                    yield return hierarchy[0];
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
    }
}
