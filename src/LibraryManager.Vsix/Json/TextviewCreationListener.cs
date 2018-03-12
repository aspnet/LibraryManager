// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.LibraryManager.Contracts;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Web.LibraryManager.Vsix.Json
{
    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("JSON")]
    [TextViewRole(PredefinedTextViewRoles.PrimaryDocument)]
    public class TextviewCreationListener : IVsTextViewCreationListener
    {
        private Manifest _manifest;
        private Dependencies _dependencies;

        [Import]
        public ITextDocumentFactoryService DocumentService { get; set; }

        [Import]
        IVsEditorAdaptersFactoryService EditorAdaptersFactoryService { get; set; }

        [Import]
        ICompletionBroker CompletionBroker { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            IWpfTextView textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);
            new CompletionController(textViewAdapter, textView, CompletionBroker);

            if (!DocumentService.TryGetTextDocument(textView.TextBuffer, out var doc))
            {
                return;
            }

            string fileName = Path.GetFileName(doc.FilePath);

            if (!fileName.Equals(Constants.ConfigFileName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _dependencies = Dependencies.FromConfigFile(doc.FilePath);
            _manifest = Manifest.FromFileAsync(doc.FilePath, _dependencies, CancellationToken.None).Result;

            doc.FileActionOccurred += OnFileSaved;
            textView.Closed += OnViewClosed;
        }

        private async Task RemoveFilesAsync(Manifest newManifest)
        {
            IEnumerable<Tuple<string, string>> prevFiles = await GetAllManifestFilesWithVersionsAsync(_manifest).ConfigureAwait(false);
            IEnumerable<Tuple<string, string>> newFiles = await GetAllManifestFilesWithVersionsAsync(newManifest).ConfigureAwait(false);
            IEnumerable<string> filesToRemove = prevFiles.Where(f => !newFiles.Contains(f)).Select(f => f.Item1);

            if (filesToRemove.Any())
            {
                var hostInteraction = _dependencies.GetHostInteractions() as HostInteraction;
                hostInteraction.DeleteFiles(filesToRemove.ToArray());
            }
        }

        /// <summary>
        /// Returns file path of all files in the manifest suffixed with library version number.
        /// For example, if library jQuery 3.3.0 specifies path "lib\js", then file jQuery.js would
        /// get returned as lib\js\jQuery.js\3.3.0
        /// </summary>
        /// <param name="manifest">Library manifest to use</param>
        /// <returns>Version-suffixed relative file paths for all libraries in the manifest</returns>
        private async Task<IEnumerable<Tuple<string, string>>> GetAllManifestFilesWithVersionsAsync(Manifest manifest)
        {
            var files = new List<Tuple<string, string>>();

            foreach (ILibraryInstallationState state in manifest.Libraries.Where(l => l.IsValid(out var errors)))
            {
                IEnumerable<Tuple<string, string>> stateFiles = await GetFilesWithVersionsAsync(state).ConfigureAwait(false);

                foreach (Tuple<string, string> fileVersion in stateFiles)
                {
                    if (!files.Contains(fileVersion))
                    {
                        files.Add(fileVersion);
                    }
                }
            }

            return files;
        }

        private async Task<IEnumerable<Tuple<string, string>>> GetFilesWithVersionsAsync(ILibraryInstallationState state)
        {
            ILibraryCatalog catalog = _dependencies.GetProvider(state.ProviderId)?.GetCatalog();

            if (catalog != null)
            {
                ILibrary library = await catalog?.GetLibraryAsync(state.LibraryId, CancellationToken.None);
                return library?.Files.Select(f => new Tuple<string, string>(Path.Combine(state.DestinationPath, f.Key), library.Version)).ToList();
            }

            return null;
        }

        private void OnFileSaved(object sender, TextDocumentFileActionEventArgs e)
        {
            var textDocument = sender as ITextDocument;

            if (e.FileActionType == FileActionTypes.ContentSavedToDisk && textDocument != null)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        Manifest newManifest = Manifest.FromJson(textDocument.TextBuffer.CurrentSnapshot.GetText(), _dependencies);
                        await RemoveFilesAsync(newManifest).ConfigureAwait(false);

                        _manifest = newManifest;

                        await LibraryHelpers.RestoreAsync(textDocument.FilePath, _manifest, CancellationToken.None).ConfigureAwait(false);
                        Telemetry.TrackOperation("restoresave");
                    }
                    catch (Exception ex)
                    {
                        string textMessage = string.Concat(Environment.NewLine, LibraryManager.Resources.Text.RestoreHasErrors, Environment.NewLine);

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

            if (DocumentService.TryGetTextDocument(view.TextBuffer, out var doc))
            {
                doc.FileActionOccurred -= OnFileSaved;
            }
        }
    }
}
