// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Telemetry;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.LibraryManager.Contracts;
using Microsoft.Web.LibraryManager.Vsix.Contracts;
using Microsoft.Web.LibraryManager.Vsix.ErrorList;
using Microsoft.Web.LibraryManager.Vsix.Json.Completion;
using Microsoft.Web.LibraryManager.Vsix.Shared;

namespace Microsoft.Web.LibraryManager.Vsix.Json
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("JSON")]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    internal class TextviewCreationListener : IVsTextViewCreationListener
    {
        private ErrorListPropagator _errorList;

        private readonly object _manifestPropertyKey = "LibManManifest";
        private readonly object _manifestProjectPropertyKey = "LibManProject";

        [Import]
        public ITextDocumentFactoryService DocumentService { get; set; }

        [Import]
        IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        ICompletionBroker CompletionBroker { get; set; }

        [Import]
        ILibraryCommandService LibraryCommandService { get; set; }

        [Import]
        private IDependenciesFactory DependenciesFactory { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

            if (!DocumentService.TryGetTextDocument(textView.TextBuffer, out ITextDocument doc))
            {
                return;
            }

            string fileName = Path.GetFileName(doc.FilePath);

            if (!fileName.Equals(Constants.ConfigFileName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            new CompletionController(textViewAdapter, textView, CompletionBroker);

            IDependencies dependencies = DependenciesFactory.FromConfigFile(doc.FilePath);
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
                                  // Justification: Manifest is free-threaded, don't need to use JTF here
            Manifest manifest = Manifest.FromFileAsync(doc.FilePath, dependencies, CancellationToken.None).Result;
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
            Project project = VsHelpers.GetDTEProjectFromConfig(doc.FilePath);

            // Save these for later reference.  Must be document-specific to avoid cross-contamination.
            // See https://github.com/aspnet/LibraryManager/issues/800
            doc.TextBuffer.Properties[_manifestPropertyKey] = manifest;
            doc.TextBuffer.Properties[_manifestProjectPropertyKey] = project;

            doc.FileActionOccurred += OnFileSaved;
            textView.Closed += OnViewClosed;

            _ = Task.Run(async () =>
            {
                IEnumerable<OperationResult<LibraryInstallationGoalState>> results = await LibrariesValidator.GetManifestErrorsAsync(manifest, dependencies, CancellationToken.None).ConfigureAwait(false);
                if (!results.All(r => r.Success))
                {
                    AddErrorsToList(results, project.Name, doc.FilePath);
                    Telemetry.LogErrors("Fail-ManifestFileOpenWithErrors", results);
                }
            });
        }

        private void OnFileSaved(object sender, TextDocumentFileActionEventArgs e)
        {
            if (LibraryCommandService.IsOperationInProgress)
            {
                Logger.LogEvent(Resources.Text.OperationInProgress, LogLevel.Operation);
            }

            var textDocument = sender as ITextDocument;

            if (e.FileActionType == FileActionTypes.ContentSavedToDisk && textDocument != null)
            {
                _ = Task.Run(async () => await DoRestoreOnSaveAsync());

                async Task DoRestoreOnSaveAsync()
                {
                    try
                    {
                        IDependencies dependencies = DependenciesFactory.FromConfigFile(textDocument.FilePath);
                        var newManifest = Manifest.FromJson(textDocument.TextBuffer.CurrentSnapshot.GetText(), dependencies);
                        IEnumerable<OperationResult<LibraryInstallationGoalState>> results = await LibrariesValidator.GetManifestErrorsAsync(newManifest, dependencies, CancellationToken.None).ConfigureAwait(false);

                        if (!results.All(r => r.Success))
                        {
                            string projectName = (textDocument.TextBuffer.Properties[_manifestProjectPropertyKey] as Project)?.Name ?? string.Empty;
                            AddErrorsToList(results, projectName, textDocument.FilePath);
                            Logger.LogErrorsSummary(results, OperationType.Restore);
                            Telemetry.LogErrors("Fail-ManifestFileSaveWithErrors", results);
                        }
                        else
                        {
                            Manifest oldManifest = textDocument.TextBuffer.Properties[_manifestPropertyKey] as Manifest;
                            if (oldManifest == null || await oldManifest.RemoveUnwantedFilesAsync(newManifest, CancellationToken.None).ConfigureAwait(false))
                            {
                                textDocument.TextBuffer.Properties[_manifestPropertyKey] = newManifest;

                                await LibraryCommandService.RestoreAsync(textDocument.FilePath, newManifest, CancellationToken.None).ConfigureAwait(false);
                                Telemetry.TrackUserTask("Invoke-RestoreOnSave");
                            }
                            else
                            {
                                string textMessage = string.Concat(Environment.NewLine, LibraryManager.Resources.Text.Restore_OperationHasErrors, Environment.NewLine);
                                Logger.LogErrorsSummary(new[] { textMessage }, OperationType.Restore);
                                Telemetry.TrackUserTask("Fail-RemovedUnwantedFiles", TelemetryResult.Failure);
                            }
                        }
                    }
                    catch (OperationCanceledException ex)
                    {
                        string textMessage = string.Concat(Environment.NewLine, LibraryManager.Resources.Text.Restore_OperationCancelled, Environment.NewLine);

                        Logger.LogEvent(textMessage, LogLevel.Task);
                        Logger.LogEvent(ex.ToString(), LogLevel.Error);

                        Telemetry.TrackException("RestoreOnSaveCancelled", ex);
                    }
                    catch (Exception ex)
                    {
                        // TO DO: Restore to previous state
                        // and add a warning to the Error List

                        string textMessage = string.Concat(Environment.NewLine, LibraryManager.Resources.Text.Restore_OperationHasErrors, Environment.NewLine);

                        Logger.LogEvent(textMessage, LogLevel.Task);
                        Logger.LogEvent(ex.ToString(), LogLevel.Error);
                        Telemetry.TrackException("RestoreOnSaveFailed", ex);
                    }
                };
            }
        }

        private void OnViewClosed(object sender, EventArgs e)
        {
            var view = (IWpfTextView)sender;

            if (DocumentService.TryGetTextDocument(view.TextBuffer, out ITextDocument doc))
            {
                doc.FileActionOccurred -= OnFileSaved;
                view.Closed -= OnViewClosed;
            }

            _errorList?.ClearErrors();
        }

        private void AddErrorsToList(IEnumerable<OperationResult<LibraryInstallationGoalState>> errors, string projectName, string manifestPath)
        {
            _errorList = new ErrorListPropagator(projectName, manifestPath);
            _errorList.HandleErrors(errors);
        }
    }
}
