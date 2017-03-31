using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;

namespace LibraryInstaller.Vsix.Json
{
    [Export(typeof(IWpfTextViewCreationListener))]
    [ContentType("JSON")]
    [TextViewRole(PredefinedTextViewRoles.Debuggable)]
    public class TextviewCreationListener : IWpfTextViewCreationListener
    {
        [Import]
        private ITextDocumentFactoryService DocumentService { get; set; }

        public void TextViewCreated(IWpfTextView textView)
        {
            if (!DocumentService.TryGetTextDocument(textView.TextBuffer, out var doc))
                return;

            string fileName = Path.GetFileName(doc.FilePath);

            if (!fileName.Equals(Constants.ConfigFileName, StringComparison.OrdinalIgnoreCase))
                return;

            doc.FileActionOccurred += OnFileSavedAsync;
            textView.Closed += OnViewClosed;
        }

        private async void OnFileSavedAsync(object sender, TextDocumentFileActionEventArgs e)
        {
            if (e.FileActionType == FileActionTypes.ContentSavedToDisk)
            {
                try
                {
                    await LibraryHelpers.RestoreAsync(e.FilePath, CancellationToken.None);
                    Telemetry.TrackOperation("restoresave");
                }
                catch (Exception ex)
                {
                    Logger.LogEvent(ex.ToString(), Contracts.Level.Error);
                    Telemetry.TrackException("configsaved", ex);
                }
            }
        }

        private void OnViewClosed(object sender, EventArgs e)
        {
            var view = (IWpfTextView)sender;

            if (DocumentService.TryGetTextDocument(view.TextBuffer, out var doc))
            {
                doc.FileActionOccurred -= OnFileSavedAsync;
            }
        }
    }
}
