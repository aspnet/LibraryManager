// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix.Json
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("JSON")]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    internal class TextviewCreationListener : IVsTextViewCreationListener
    {
        private Manifest _manifest;
        private Dependencies _dependencies;
        private Project _project;
        private ErrorList _errorList;
        private string _manifestPath;

        [Import]
        public ITextDocumentFactoryService DocumentService { get; set; }

        [Import]
        IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        ICompletionBroker CompletionBroker { get; set; }

        [Import]
        ILibraryCommandService libraryCommandService { get; set; }

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

            _dependencies = Dependencies.FromConfigFile(doc.FilePath);
            _manifest = Manifest.FromFileAsync(doc.FilePath, _dependencies, CancellationToken.None).Result;
            _manifestPath = doc.FilePath;
            _project = VsHelpers.GetDTEProjectFromConfig(_manifestPath);

            doc.FileActionOccurred += OnFileSaved;
            textView.Closed += OnViewClosed;

            Task.Run(async () =>
            {
                IEnumerable<ILibraryOperationResult> results = await LibrariesValidator.GetManifestErrorsAsync(_manifest, _dependencies, CancellationToken.None).ConfigureAwait(false);
                if (!results.All(r => r.Success))
                {
                    AddErrorsToList(results);
                }
            });
        }

        private void OnFileSaved(object sender, TextDocumentFileActionEventArgs e)
        {
            if (libraryCommandService.IsOperationInProgress)
            {
                Logger.LogEvent(Resources.Text.OperationInProgress, LogLevel.Operation);
            }

            var textDocument = sender as ITextDocument;

            if (e.FileActionType == FileActionTypes.ContentSavedToDisk && textDocument != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        Manifest newManifest = Manifest.FromJson(textDocument.TextBuffer.CurrentSnapshot.GetText(), _dependencies);
                        IEnumerable<ILibraryOperationResult> results = await LibrariesValidator.GetManifestErrorsAsync(newManifest, _dependencies, CancellationToken.None).ConfigureAwait(false);

                        if (!results.All(r => r.Success))
                        {
                            AddErrorsToList(results);
                        }
                        else
                        {
                            if (await _manifest.RemoveUnwantedFilesAsync(newManifest, CancellationToken.None).ConfigureAwait(false))
                            {
                                _manifest = newManifest;

                                await libraryCommandService.RestoreAsync(textDocument.FilePath, _manifest, CancellationToken.None).ConfigureAwait(false);
                                Telemetry.TrackOperation("restoresave");
                            }
                            else
                            {
                                string textMessage = string.Concat(Environment.NewLine, LibraryManager.Resources.Text.Restore_OperationHasErrors, Environment.NewLine);
                                Logger.LogEvent(textMessage, LogLevel.Task);
                            }
                        }
                    }
                    catch (OperationCanceledException ex)
                    {
                        string textMessage = string.Concat(Environment.NewLine, LibraryManager.Resources.Text.Restore_OperationCancelled, Environment.NewLine);

                        Logger.LogEvent(textMessage, LogLevel.Task);
                        Logger.LogEvent(ex.ToString(), LogLevel.Error);

                        Telemetry.TrackException("restoresavecancelled", ex);
                    }
                    catch (Exception ex)
                    {
                        // TO DO: Restore to previous state
                        // and add a warning to the Error List

                        string textMessage = string.Concat(Environment.NewLine, LibraryManager.Resources.Text.Restore_OperationHasErrors, Environment.NewLine);

                        Logger.LogEvent(textMessage, LogLevel.Task);
                        Logger.LogEvent(ex.ToString(), LogLevel.Error);
                        Telemetry.TrackException("restoresavefailed", ex);
                    }
                });
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

        private void AddErrorsToList(IEnumerable<ILibraryOperationResult> errors)
        {
            if (_errorList == null)
            {
                _errorList = new ErrorList(_project?.Name, _manifestPath);
            }

            _errorList.HandleErrors(errors);
        }
    }
}
