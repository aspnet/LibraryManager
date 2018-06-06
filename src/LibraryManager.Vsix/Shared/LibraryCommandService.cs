// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TaskStatusCenter;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.Web.LibraryManager.Contracts;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Web.LibraryManager.Vsix
{
    [Export(typeof(ILibraryCommandService))]
    internal class LibraryCommandService : ILibraryCommandService, IVsSolutionEvents, IDisposable
    {
        [Import(typeof(ITaskStatusCenterService))]
        internal ITaskStatusCenterService TaskStatusCenterServiceInstance;

        private readonly IVsSolution _solution;
        private readonly uint _solutionCookie;
        private CancellationTokenSource _linkedCancellationTokenSource;
        private CancellationTokenSource _internalCancellationTokenSource;
        private Task _currentOperationTask;
        private object _lockObject = new object(); 

        [ImportingConstructor]
        public LibraryCommandService([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            _solution = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
            _solution.AdviseSolutionEvents(this, out _solutionCookie);
        }

        public bool IsOperationInProgress
        {
            get
            {
                lock (_lockObject)
                {
                    return _currentOperationTask != null && !_currentOperationTask.IsCompleted;
                }
            }
        }

        public async Task RestoreAsync(string configFilePath, CancellationToken cancellationToken = default(CancellationToken))
        {
            Dictionary<string, Manifest> manifests = await GetManifestFromConfigAsync(new[] { configFilePath }, cancellationToken);
            await RestoreAsync(manifests, cancellationToken);
        }

        public async Task RestoreAsync(IEnumerable<string> configFilePaths, CancellationToken cancellationToken = default(CancellationToken))
        {
            Dictionary<string, Manifest> manifests = await GetManifestFromConfigAsync(configFilePaths, cancellationToken);
            await RestoreAsync(manifests, cancellationToken);
        }

        public async Task RestoreAsync(string configFilePath, Manifest manifest, CancellationToken cancellationToken = default(CancellationToken))
        {
            await RestoreAsync(new Dictionary<string, Manifest>() { [configFilePath] = manifest }, cancellationToken);
        }

        public async Task UninstallAsync(string configFilePath, string libraryId, CancellationToken cancellationToken = default(CancellationToken))
        {
            string taskTitle = GetTaskTitle(OperationType.Uninstall, libraryId);
            string errorMessage = string.Format(LibraryManager.Resources.Text.Uninstall_LibraryFailed, libraryId);

            await RunTaskAsync((internalToken) => UninstallLibraryAsync(configFilePath, libraryId, internalToken), taskTitle, errorMessage);
        }

        public async Task CleanAsync(ProjectItem configProjectItem, CancellationToken cancellationToken = default(CancellationToken))
        {

            string taskTitle = GetTaskTitle(OperationType.Clean, string.Empty);

            await RunTaskAsync((internalToken) => CleanLibrariesAsync(configProjectItem, internalToken), taskTitle, LibraryManager.Resources.Text.Clean_OperationFailed);
        }

        private async Task RestoreAsync(Dictionary<string, Manifest> manifests, CancellationToken cancellationToken = default(CancellationToken))
        {
            string taskTitle = GetTaskTitle(OperationType.Restore, string.Empty);
            string errorMessage = LibraryManager.Resources.Text.Restore_OperationFailed;

            await RunTaskAsync((internalToken) => RestoreAsync(manifests, internalToken), taskTitle, errorMessage);
        }

        private async Task RunTaskAsync(Func<CancellationToken, Task> toRun, string taskTitle, string errorMessage)
        {
            if (IsOperationInProgress)
            {
                return;
            }

            try
            {
                ITaskHandler handler = TaskStatusCenterServiceInstance.CreateTaskHandler(taskTitle);
                CancellationToken internalToken = RegisterCancellationToken(handler.UserCancellation);

                lock (_lockObject)
                {
                    _currentOperationTask = toRun(internalToken);
                    handler.RegisterTask(_currentOperationTask);
                }

                await _currentOperationTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogEvent(errorMessage + Environment.NewLine + ex.Message, LogLevel.Operation);
            }
        }

        private async Task<Dictionary<string, Manifest>> GetManifestFromConfigAsync(IEnumerable<string> configFiles, CancellationToken cancellationToken)
        {
            Dictionary<string, Manifest> manifests = new Dictionary<string, Manifest>();

            try
            {
                foreach (string configFilePath in configFiles)
                {
                    Dependencies dependencies = Dependencies.FromConfigFile(configFilePath);
                    Manifest manifest = await Manifest.FromFileAsync(configFilePath, dependencies, cancellationToken).ConfigureAwait(false);
                    manifests.Add(configFilePath, manifest);
                }
            }
            catch (Exception ex)
            {
                Logger.LogEvent(LibraryManager.Resources.Text.Restore_OperationFailed + Environment.NewLine + ex.Message, LogLevel.Operation);
            }

            return manifests;
        }

        private async Task CleanLibrariesAsync(ProjectItem configProjectItem, CancellationToken cancellationToken = default(CancellationToken))
        {
            Logger.LogEventsHeader(OperationType.Clean, string.Empty);

            try
            {
                Stopwatch sw = new Stopwatch();
                string configFileName = configProjectItem.FileNames[1];
                var dependencies = Dependencies.FromConfigFile(configFileName);
                Manifest manifest = await Manifest.FromFileAsync(configFileName, dependencies, CancellationToken.None).ConfigureAwait(false);
                IHostInteraction hostInteraction = dependencies.GetHostInteractions();

                sw.Start();
                IEnumerable<ILibraryOperationResult> results = await manifest.CleanAsync(async (filesPaths) => await hostInteraction.DeleteFilesAsync(filesPaths, cancellationToken), cancellationToken);
                sw.Stop();

                Logger.LogEventsSummary(results, OperationType.Clean, sw.Elapsed);
            }
            catch (OperationCanceledException)
            {
                Logger.LogEvent(LibraryManager.Resources.Text.Clean_OperationCancelled, LogLevel.Task);
            }
        }

        private async Task RestoreAsync(IDictionary<string, Manifest> manifests, CancellationToken cancellationToken)
        {
            Logger.LogEventsHeader(OperationType.Restore, string.Empty);

            Stopwatch sw = new Stopwatch();
            List<ILibraryOperationResult> totalResults = new List<ILibraryOperationResult>();

            try
            {
                sw.Start();

                foreach (KeyValuePair<string, Manifest> manifest in manifests)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Project project = VsHelpers.GetDTEProjectFromConfig(manifest.Key);
                    Logger.LogEvent(string.Format("Restoring packages for project {0}...", project.Name), LogLevel.Operation);

                    IEnumerable<ILibraryOperationResult> results = await RestoreLibrariesAsync(manifest.Value, cancellationToken).ConfigureAwait(false);

                    await AddFilesToProjectAsync(manifest.Key, project, results, cancellationToken);
                    AddErrorsToErrorList(project?.Name, manifest.Key, results);
                    totalResults.AddRange(results);
                }

                sw.Stop();

                Logger.LogEventsSummary(totalResults, OperationType.Restore, sw.Elapsed);
                PostRestoreTelemetryData(totalResults, sw.Elapsed);
            }
            catch (OperationCanceledException)
            {
                Logger.LogEvent(LibraryManager.Resources.Text.Restore_OperationCancelled, LogLevel.Task);
            }
        }

        private async Task<IEnumerable<ILibraryOperationResult>> RestoreLibrariesAsync(Manifest manifest, CancellationToken cancellationToken)
        {
            return await manifest.RestoreAsync(cancellationToken).ConfigureAwait(false);
        }

        private async Task UninstallLibraryAsync(string configFilePath, string libraryId, CancellationToken cancellationToken)
        {
            Logger.LogEventsHeader(OperationType.Uninstall, libraryId);

            try
            {
                Stopwatch sw = new Stopwatch();
                var dependencies = Dependencies.FromConfigFile(configFilePath);
                Manifest manifest = await Manifest.FromFileAsync(configFilePath, dependencies, cancellationToken).ConfigureAwait(false);
                IHostInteraction hostInteraction = dependencies.GetHostInteractions();

                sw.Start();
                ILibraryOperationResult result = await manifest.UninstallAsync(libraryId, async (filesPaths) => await hostInteraction.DeleteFilesAsync(filesPaths, cancellationToken), cancellationToken).ConfigureAwait(false);
                sw.Stop();

                Logger.LogEventsSummary(new List<ILibraryOperationResult> { result }, OperationType.Uninstall, sw.Elapsed);
                Telemetry.TrackUserTask("libraryuninstall");
            }
            catch (OperationCanceledException)
            {
                Logger.LogEvent(string.Format(LibraryManager.Resources.Text.Uninstall_LibraryCancelled, libraryId), LogLevel.Task);
            }
        }

        private string GetTaskTitle(OperationType operation, string libraryId)
        {
            switch (operation)
            {
                case OperationType.Restore:
                    {
                        return LibraryManager.Resources.Text.Restore_OperationStarted;
                    }
                case OperationType.Clean:
                    {
                        return LibraryManager.Resources.Text.Clean_OperationStarted;
                    }
                case OperationType.Uninstall:
                    {
                        return string.Format(LibraryManager.Resources.Text.Uninstall_LibraryStarted, libraryId ?? string.Empty);
                    }
                case OperationType.Upgrade:
                    {
                        return string.Format(LibraryManager.Resources.Text.Update_LibraryStarted, libraryId ?? string.Empty);
                    }
            }

            return string.Empty;
        }

        private void PostRestoreTelemetryData(IEnumerable<ILibraryOperationResult> results, TimeSpan elapsedTime)
        {
            var telResult = new Dictionary<string, double>();
            foreach (ILibraryOperationResult result in results.Where(r => r.Success))
            {
                if (result.InstallationState.ProviderId != null)
                {
                    telResult.TryGetValue(result.InstallationState.ProviderId, out double count);
                    telResult[result.InstallationState.ProviderId] = count + 1;
                }
            }
            telResult.Add("time", elapsedTime.TotalMilliseconds);
            Telemetry.TrackUserTask("restore", TelemetryResult.None, telResult.Select(i => new KeyValuePair<string, object>(i.Key, i.Value)).ToArray());
        }

        private void AddErrorsToErrorList(string projectName, string configFile, IEnumerable<ILibraryOperationResult> results)
        {
            var errorList = new ErrorList(projectName, configFile);
            errorList.HandleErrors(results);
        }

        private async Task AddFilesToProjectAsync(string configFilePath, Project project, IEnumerable<ILibraryOperationResult> results, CancellationToken cancellationToken)
        {
            string cwd = Path.GetDirectoryName(configFilePath);
            var files = new List<string>();

            foreach (ILibraryOperationResult state in results)
            {
                if (state.Success)
                {
                    IEnumerable<string> absoluteFiles = state.InstallationState.Files
                        .Select(file => Path.Combine(cwd, state.InstallationState.DestinationPath, file)
                        .Replace('/', Path.DirectorySeparatorChar));
                    files.AddRange(absoluteFiles.Where(file => !files.Contains(file)));
                }
            }

            if (project != null)
            {
                var logAction = new Action<string, LogLevel>((message, level) => { Logger.LogEvent(message, level); });
                await VsHelpers.AddFilesToProjectAsync(project, files, logAction, cancellationToken);
            }
        }

        private CancellationToken RegisterCancellationToken(CancellationToken userCancellation)
        {
            CancellationTokenSource internalSource = new CancellationTokenSource();
            _internalCancellationTokenSource = internalSource;

            CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(userCancellation);
            _linkedCancellationTokenSource = linkedSource;

            linkedSource.Token.Register(() =>
            {
                internalSource.Cancel();
            });

            return internalSource.Token;
        }

        public void CancelOperation()
        {
            if (_internalCancellationTokenSource != null)
            {
                if (!_internalCancellationTokenSource.IsCancellationRequested)
                {
                    _internalCancellationTokenSource.Cancel();
                }
                _internalCancellationTokenSource.Dispose();
            }

            if (_linkedCancellationTokenSource != null)
            {
                 _linkedCancellationTokenSource.Dispose();
            }

            _currentOperationTask = null;
        }

        public void Dispose()
        {
            _solution.UnadviseSolutionEvents(_solutionCookie);
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            CancelOperation();

            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            CancelOperation();

            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            CancelOperation();

            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }
    }
}
