using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using LibraryInstaller.Contracts;
using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.Web.Editor.SuggestedActions;

namespace LibraryInstaller.Vsix
{
    internal class UpdateSuggestedAction : SuggestedActionBase
    {
        private static readonly Guid _guid = new Guid("b3b43e69-7d0a-4acf-99ea-015526f76d84");
        private SuggestedActionProvider _provider;
        private string _updatedLibraryId;
        private bool _disabled;

        public UpdateSuggestedAction(SuggestedActionProvider provider, string libraryId, string displayText, bool disabled = false)
            : base(provider.TextBuffer, provider.TextView, displayText, _guid)
        {
            _provider = provider;
            _updatedLibraryId = libraryId;
            _disabled = disabled;

            if (!disabled)
            {
                IconMoniker = KnownMonikers.StatusReady;
            }
        }

        public override void Invoke(CancellationToken cancellationToken)
        {
            if (_disabled)
                return;

            try
            {
                var dependencies = Dependencies.FromConfigFile(_provider.ConfigFilePath);
                IProvider provider = dependencies.GetProvider(_provider.InstallationState.ProviderId);
                ILibraryCatalog catalog = provider?.GetCatalog();

                if (catalog == null)
                {
                    return;
                }

                JSONMember member = _provider.LibraryObject.Children.OfType<JSONMember>().FirstOrDefault(m => m.UnquotedNameText == "id");

                if (member != null)
                {
                    using (ITextEdit edit = TextBuffer.CreateEdit())
                    {
                        edit.Replace(new Span(member.Value.Start, member.Value.Length), "\"" + _updatedLibraryId + "\"");
                        edit.Apply();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogEvent(ex.ToString(), LogLevel.Error);
            }
        }
    }
}
