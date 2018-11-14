// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Microsoft.WebTools.Languages.Json.Editor.Completion;
using Microsoft.WebTools.Languages.Json.Parser.Nodes;

namespace Microsoft.Web.LibraryManager.Vsix
{
    [Export(typeof(IJsonCompletionListProvider))]
    [Name(nameof(PathCompletionProvider))]
    internal class PathCompletionProvider : BaseCompletionProvider
    {
        public override JsonCompletionContextType ContextType
        {
            get { return JsonCompletionContextType.PropertyValue; }
        }

        protected override IEnumerable<JsonCompletionEntry> GetEntries(JsonCompletionContext context)
        {
            MemberNode member = context.ContextNode.FindType<MemberNode>();

            if (member == null || (member.UnquotedNameText != ManifestConstants.Destination && member.UnquotedNameText != ManifestConstants.DefaultDestination)) 
                yield break;

            MemberNode parent = member.FindType<ObjectNode>()?.FindType<MemberNode>();

            if (member.UnquotedNameText == ManifestConstants.Destination && (parent == null || parent.UnquotedNameText != ManifestConstants.Libraries))
            {
                yield break;
            }

            if (member.UnquotedNameText == ManifestConstants.DefaultDestination && parent != null)
            { 
                yield break;
            }

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

            IEnumerable<Tuple<string, string>> completions = GetCompletions(cwd, member.UnquotedValueText, caretPosition, out Span span);
            int start = member.Value.Start;
            ITrackingSpan trackingSpan = context.Snapshot.CreateTrackingSpan(start + 1 + span.Start, span.Length, SpanTrackingMode.EdgeInclusive);

            foreach (Tuple<string, string> item in completions)
            {
                yield return new SimpleCompletionEntry(item.Item1, item.Item2, KnownMonikers.FolderClosed, trackingSpan, context.Session);
            }
        }

        private IEnumerable<Tuple<string, string>> GetCompletions(string cwd, string value, int caretPosition, out Span span)
        {
            span = new Span(0, value.Length);
            var list = new List<Tuple<string, string>>();

            int index = value.Length >= caretPosition - 1 ? value.LastIndexOf('/', Math.Max(caretPosition - 1, 0)) : value.Length;
            string prefix = "";

            if (index > 0)
            {
                prefix = value.Substring(0, index + 1);
                cwd = Path.Combine(cwd, prefix);
                span = new Span(index + 1, value.Length - index - 1);
            }

            var dir = new DirectoryInfo(cwd);

            if (dir.Exists)
            {
                foreach (FileSystemInfo item in dir.EnumerateDirectories())
                {
                    list.Add(Tuple.Create(item.Name + "/", prefix + item.Name + "/"));
                }
            }

            return list;
        }
    }
}
