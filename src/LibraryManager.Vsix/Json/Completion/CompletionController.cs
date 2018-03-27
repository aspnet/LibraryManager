// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSON.Core.Parser;
using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.JSON.Editor.Document;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.Web.LibraryManager.Contracts;
using System;
using System.Runtime.InteropServices;

namespace Microsoft.Web.LibraryManager.Vsix
{
    internal class CompletionController : IOleCommandTarget
    {
        private ITextView _textView;
        private IOleCommandTarget _nextCommandTarget;
        private ICompletionBroker _broker;
        private int _delay = 500;
        private DateTime _lastTyped;
        private ICompletionSession _currentSession;

        public CompletionController(IVsTextView adapter, ITextView textView, ICompletionBroker broker)
        {
            _textView = textView;
            _broker = broker;
            ErrorHandler.ThrowOnFailure(adapter.AddCommandFilter(this, out _nextCommandTarget));
        }

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
            {
                char typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);

                if (char.IsLetterOrDigit(typedChar) && _broker.IsCompletionActive(_textView))
                {
                    RetriggerAsync(false);
                }
                else if (typedChar == '/' || typedChar == '\\' && !_broker.IsCompletionActive(_textView))
                {
                    RetriggerAsync(false);
                }
                else if (typedChar == '@')
                {
                    RetriggerAsync(true);
                }
            }
            else if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.BACKSPACE)
            {
                if (_broker.IsCompletionActive(_textView))
                {
                    _broker.DismissAllSessions(_textView);
                }
            }

            ThreadHelper.Generic.BeginInvoke(() =>
            {
                if (_currentSession == null && _broker.IsCompletionActive(_textView))
                {
                    _currentSession = _broker.GetSessions(_textView)[0];
                    _currentSession.Committed += OnCommitted;
                    _currentSession.Dismissed += OnDismissed;
                }
            });

            return _nextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        private void OnDismissed(object sender, EventArgs e)
        {
            _currentSession.Dismissed -= OnDismissed;
            _currentSession.Committed -= OnCommitted;
            _currentSession = null;
        }

        private void OnCommitted(object sender, EventArgs e)
        {
            string text = _currentSession?.SelectedCompletionSet?.SelectionStatus?.Completion?.DisplayText;

            if (text.EndsWith("/") || text.EndsWith("\\"))
            {
                System.Windows.Forms.SendKeys.Send("{LEFT}");
                RetriggerAsync(true);
            }
        }

        private async void RetriggerAsync(bool force)
        {
            _lastTyped = DateTime.Now;
            int delay = force ? 50 : _delay;

            await System.Threading.Tasks.Task.Delay(delay);

            // Prevents retriggering from happening while typing fast
            if (_lastTyped.AddMilliseconds(delay) > DateTime.Now)
            {
                return;
            }

            var doc = JSONEditorDocument.FromTextView(_textView);
            JSONParseItem parseItem = doc?.JSONDocument.ItemBeforePosition(_textView.Caret.Position.BufferPosition);

            if (parseItem == null)
            {
                return;
            }

            JSONMember member = parseItem.FindType<JSONMember>();

            if (member == null || (!member.UnquotedNameText.Equals(ManifestConstants.Library) && !member.UnquotedNameText.Equals(ManifestConstants.Destination) && member.UnquotedValueText?.Length <= 1))
            {
                return;
            }

            JSONObject parent = parseItem.FindType<JSONObject>();

            if (JsonHelpers.TryGetInstallationState(parent, out ILibraryInstallationState state))
            {
                VsHelpers.DTE.ExecuteCommand("Edit.ListMembers");
            }
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return _nextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }
}
