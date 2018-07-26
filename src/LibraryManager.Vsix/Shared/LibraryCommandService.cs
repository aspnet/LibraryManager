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
using Microsoft.VisualStudio.TaskStatusCenter;
using Microsoft.Web.LibraryManager.Contracts;
using Task = System.Threading.Tasks.Task;

namespace Microsoft.Web.LibraryManager.Vsix
{
    [Export(typeof(ILibraryCommandService))]
    internal class LibraryCommandService : ILibraryCommandService
    {
        [Import(typeof(ITaskStatusCenterService))]
        internal ITaskStatusCenterService TaskStatusCenterServiceInstance;

        private CancellationTokenSource _linkedCancellationTokenSource;
        private CancellationTokenSource _internalCancellationTokenSource;
        private Task _currentOperationTask;
        private DefaultSolutionEvents _solutionEvents;
        private object _lockObject = new object(); 

        [ImportingConstructor]
        public LibraryCommandService()
        {
            _solutionEvents = new DefaultSolutionEvents();
            _solutionEvents.BeforeCloseSolution += OnBeforeCloseSolution;
            _solutionEvents.BeforeCloseProject += OnBeforeCloseProject;
            _solutionEvents.BeforeUnloadProject += OnBeforeUnloadProject;
        }

        private void OnBeforeUnloadProject(object sender, ParamEventArgs e)
        {
            CancelOperation();
        }

        private void OnBeforeCloseProject(object sender, ParamEventArgs e)
        {
            CancelOperation();
        }

        private void OnBeforeCloseSolution(object sender, ParamEventArgs e)
        {
            CancelOperation();
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

        public async Task RestoreAsync(string configFilePath, CancellationToken cancellationToken)
        {
            Dictionary<string, Manifest> manifests = await GetManifestFromConfigAsync(new[] { configFilePath }, cancellationToken).ConfigureAwait(false);

            if (manifests.Count > 0)
            {
                await RestoreAsync(manifests, cancellationToken);
            }
        }

        public async Task RestoreAsync(IEnumerable<string> configFilePaths, CancellationToken cancellationToken)
        {
            Dictionary<string, Manifest> manifests = await GetManifestFromConfigAsync(configFilePaths, cancellationToken).ConfigureAwait(false);
            await RestoreAsync(manifests, cancellationToken);
        }

        public async Task RestoreAsync(string configFilePath, Manifest manifest, CancellationToken cancellationToken)
        {
            await RestoreAsync(new Dictionary<string, Manifest>() { [configFilePath] = manifest }, cancellationToken);
        }

        public async Task UninstallAsync(string configFilePath, string libraryId, CancellationToken cancellationToken)
        {
            string taskTitle = GetTaskTitle(OperationType.Uninstall, libraryId);
            string errorMessage = string.Format(LibraryManager.Resources.Text.Uninstall_LibraryFailed, libraryId);

            await RunTaskAsync((internalToken) => UninstallLibraryAsync(configFilePath, libraryId, internalToken), taskTitle, errorMessage);
        }

        public async Task CleanAsync(ProjectItem configProjectItem, CancellationToken cancellationToken)
        {
            string taskTitle = GetTaskTitle(OperationType.Clean, string.Empty);

            await RunTaskAsync((internalToken) => CleanLibrariesAsync(configProjectItem, internalToken), taskTitle, LibraryManager.Resources.Text.Clean_OperationFailed);
        }

        private async Task RestoreAsync(Dictionary<string, Manifest> manifests, CancellationToken cancellationToken)
        {
            string taskTitle = GetTaskTitle(OperationType.Restore, string.Empty);
            string errorMessage = LibraryManager.Resources.Text.Restore_OperationFailed;

            await RunTaskAsync((internalToken) => RestoreInternalAsync(manifests, internalToken), taskTitle, errorMessage);
        }

        private async Task RunTaskAsync(Func<CancellationToken, Task> getTaskToRun, string taskTitle, string errorMessage)
        {
            if (IsOperationInProgress)
            {
                return;
            }

            try
            {
                ITaskHandler handler = await TaskStatusCenterServiceInstance.CreateTaskHandlerAsync(taskTitle);
                CancellationToken internalToken = RegisterCancellationToken(handler.UserCancellation);

                lock (_lockObject)
                {
                    _currentOperationTask = getTaskToRun(internalToken);
                    handler.RegisterTask(_currentOperationTask);
                }

                await _currentOperationTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.LogEvent(errorMessage + Environment.NewLine + ex.Message, LogLevel.Operation);
                Telemetry.TrackException(nameof(RunTaskAsync), ex);
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
                Telemetry.TrackException(nameof(GetManifestFromConfigAsync), ex);

                return null;
            }

            return manifests;
        }

        private async Task CleanLibrariesAsync(ProjectItem configProjectItem, CancellationToken cancellationToken)
        {
            Logger.LogEventsHeader(OperationType.Clean, string.Empty);

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                string configFileName = configProjectItem.FileNames[1];
                var dependencies = Dependencies.FromConfigFile(configFileName);
                Project project = VsHelpers.GetDTEProjectFromConfig(configFileName);

                Manifest manifest = await Manifest.FromFileAsync(configFileName, dependencies, CancellationToken.None).ConfigureAwait(false);
                IEnumerable<ILibraryOperationResult> results = new List<ILibraryOperationResult>();

                if (manifest != null)
                {
                    IEnumerable<ILibraryOperationResult> validationResults = await LibrariesValidator.GetManifestErrorsAsync(manifest, dependencies, cancellationToken).ConfigureAwait(false);

                    if (!validationResults.All(r => r.Success))
                    {
                        sw.Stop();
                        AddErrorsToErrorList(project?.Name, configFileName, validationResults);
                        Logger.LogErrorsSummary(validationResults, OperationType.Clean);
                        Telemetry.LogErrors($"FailValidation_{OperationType.Clean}", validationResults);
                    }
                    else
                    {
                        IHostInteraction hostInteraction = dependencies.GetHostInteractions();
                        results = await manifest.CleanAsync(async (filesPaths) => await hostInteraction.DeleteFilesAsync(filesPaths, cancellationToken), cancellationToken);

                        sw.Stop();
                        AddErrorsToErrorList(project?.Name, configFileName, results);
                        Logger.LogEventsSummary(results, OperationType.Clean, sw.Elapsed);
                        Telemetry.LogEventsSummary(results, OperationType.Clean, sw.Elapsed);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                Logger.LogEvent(LibraryManager.Resources.Text.Clean_OperationCancelled, LogLevel.Task);
                Telemetry.TrackException($@"{OperationType.Clean}Cancelled", ex);
            }
        }

        private async Task RestoreInternalAsync(IDictionary<string, Manifest> manifests, CancellationToken cancellationToken)
        {
            Logger.LogEventsHeader(OperationType.Restore, string.Empty);

            try
            {
                Stopwatch swTotal = new Stopwatch();
                swTotal.Start();

                foreach (KeyValuePair<string, Manifest> manifest in manifests)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    Stopwatch swLocal = new Stopwatch();
                    swLocal.Start();
                    IDependencies dependencies = Dependencies.FromConfigFile(manifest.Key);
                    Project project = VsHelpers.GetDTEProjectFromConfig(manifest.Key);

                    Logger.LogEvent(string.Format(LibraryManager.Resources.Text.Restore_LibrariesForProject, project?.Name), LogLevel.Operation);

                    IEnumerable<ILibraryOperationResult> validationResults = await LibrariesValidator.GetManifestErrorsAsync(manifest.Value, dependencies, cancellationToken).ConfigureAwait(false);
                    if (!validationResults.All(r => r.Success))
                    {
                        swLocal.Stop();
                        AddErrorsToErrorList(project?.Name, manifest.Key, validationResults);
                        Logger.LogErrorsSummary(validationResults, OperationType.Restore, false);
                        Telemetry.LogErrors($"FailValidation_{OperationType.Restore}", validationResults);
                    }
                    else
                    {
                        IEnumerable<ILibraryOperationResult> results = await RestoreLibrariesAsync(manifest.Value, cancellationToken).ConfigureAwait(false);
                        await AddFilesToProjectAsync(manifest.Key, project, results.Where(r =>r.Success && !r.UpToDate), cancellationToken).ConfigureAwait(false);

                        swLocal.Stop();
                        AddErrorsToErrorList(project?.Name, manifest.Key, results);
                        Logger.LogEventsSummary(results, OperationType.Restore, swLocal.Elapsed, false);
                        Telemetry.LogEventsSummary(results, OperationType.Restore, swLocal.Elapsed);
                    }
                }

                swTotal.Stop();
                Logger.LogEventsFooter(OperationType.Restore, swTotal.Elapsed);
            }
            catch (OperationCanceledException ex)
            {
                Logger.LogEvent(LibraryManager.Resources.Text.Restore_OperationCancelled, LogLevel.Task);
                Telemetry.TrackException($@"{OperationType.Restore}Cancelled", ex);
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
                sw.Start();

                var dependencies = Dependencies.FromConfigFile(configFilePath);
                Manifest manifest = await Manifest.FromFileAsync(configFilePath, dependencies, cancellationToken).ConfigureAwait(false);
                ILibraryOperationResult result = null;

                if (manifest == null)
                {
                    result = LibraryOperationResult.FromError(PredefinedErrors.ManifestMalformed());
                }
                else
                {
                    IHostInteraction hostInteraction = dependencies.GetHostInteractions();
                    result = await manifest.UninstallAsync(libraryId, async (filesPaths) => await hostInteraction.DeleteFilesAsync(filesPaths, cancellationToken), cancellationToken).ConfigureAwait(false);
                }

                sw.Stop();

                if (result.Errors.Any())
                {
                    Logger.LogErrorsSummary(new List<ILibraryOperationResult> { result }, OperationType.Uninstall);
                }
                else
                {
                    Logger.LogEventsSummary(new List<ILibraryOperationResult> { result }, OperationType.Uninstall, sw.Elapsed);
                }

                Telemetry.LogEventsSummary(new List<ILibraryOperationResult> { result }, OperationType.Uninstall, sw.Elapsed);
            }
            catch (OperationCanceledException ex)
            {
                Logger.LogEvent(string.Format(LibraryManager.Resources.Text.Uninstall_LibraryCancelled, libraryId), LogLevel.Task);
                Telemetry.TrackException($@"{OperationType.Uninstall}Cancelled", ex);
            }
        }

        private string GetTaskTitle(OperationType operation, string libraryId)
        {
            switch (operation)
            {
                case OperationType.Restore:
                    return LibraryManager.Resources.Text.Restore_OperationStarted;

                case OperationType.Clean:
                    return LibraryManager.Resources.Text.Clean_OperationStarted;

                case OperationType.Uninstall:
                    return string.Format(LibraryManager.Resources.Text.Uninstall_LibraryStarted, libraryId ?? string.Empty);

                case OperationType.Upgrade:
                    return string.Format(LibraryManager.Resources.Text.Update_LibraryStarted, libraryId ?? string.Empty);
            }

            return string.Empty;
        }

        private void AddErrorsToErrorList(string projectName, string configFile, IEnumerable<ILibraryOperationResult> results)
        {
            var errorList = new ErrorList(projectName, configFile);
            errorList.HandleErrors(results);
        }

        private async Task AddFilesToProjectAsync(string configFilePath, Project project, IEnumerable<ILibraryOperationResult> results, CancellationToken cancellationToken)
        {
            string workingDirectory = Path.GetDirectoryName(configFilePath);
            var files = new List<string>();

            if (project != null)
            {
                foreach (ILibraryOperationResult state in results)
                {
                    if (state.Success && !state.UpToDate && state.InstallationState.Files != null)
                    {
                        IEnumerable<string> absoluteFiles = state.InstallationState.Files
                            .Select(file => Path.Combine(workingDirectory, state.InstallationState.DestinationPath, file)
                            .Replace('/', Path.DirectorySeparatorChar));
                        files.AddRange(absoluteFiles);
                    }
                }

                if (files.Count > 0)
                {
                    var logAction = new Action<string, LogLevel>((message, level) => { Logger.LogEvent(message, level); });
                    await VsHelpers.AddFilesToProjectAsync(project, files, logAction, cancellationToken);
                }
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
            _solutionEvents.Dispose();
        }
    }
}
