// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.JSON.Editor.Completion;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;

namespace Microsoft.Web.LibraryInstaller.Vsix
{
    [Export(typeof(IJSONCompletionListProvider))]
    [Name(nameof(PathCompletionProvider))]
    internal class PathCompletionProvider : BaseCompletionProvider
    {
        public override JSONCompletionContextType ContextType
        {
            get { return JSONCompletionContextType.PropertyValue; }
        }

        protected override IEnumerable<JSONCompletionEntry> GetEntries(JSONCompletionContext context)
        {
            JSONMember member = context.ContextItem.FindType<JSONMember>();

            if (member == null || member.UnquotedNameText != "path")
                yield break;

            JSONMember parent = member.FindType<JSONObject>()?.FindType<JSONMember>();

            if (parent == null || parent.UnquotedNameText != "packages")
                yield break;

            int caretPosition = context.Session.TextView.Caret.Position.BufferPosition - member.Value.Start - 1;

            if (caretPosition > member.UnquotedValueText.Length)
            {
                yield break;
            }

            var dependencies = Dependencies.FromConfigFile(ConfigFilePath);
            string cwd = dependencies?.GetHostInteractions().WorkingDirectory;

            if (string.IsNullOrEmpty(cwd))
            {
                yield break;
            }

            IEnumerable<Tuple<string, string>> completions = GetCompletions(cwd, member.UnquotedValueText, caretPosition);

            foreach (Tuple<string, string> item in completions)
            {
                yield return new SimpleCompletionEntry(item.Item1, item.Item2, null, KnownMonikers.FolderClosed, context.Session);
            }

            Telemetry.TrackUserTask("completionpath");
        }

        private IEnumerable<Tuple<string, string>> GetCompletions(string cwd, string value, int caretPosition)
        {
            int index = value.Length >= caretPosition - 1 ? value.LastIndexOf('/', Math.Max(caretPosition - 1, 0)) : value.Length;
            string prefix = "";

            if (index > 0)
            {
                prefix = value.Substring(0, index + 1);
                cwd = Path.Combine(cwd, prefix);
            }

            var dir = new DirectoryInfo(cwd);

            if (dir.Exists)
            {
                foreach (FileSystemInfo item in dir.EnumerateDirectories())
                {
                    yield return Tuple.Create(item.Name + "/", prefix + item.Name + "/");
                }
            }
        }
    }
}