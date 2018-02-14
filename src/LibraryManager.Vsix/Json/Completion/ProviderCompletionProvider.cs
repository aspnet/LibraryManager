// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.JSON.Core.Parser.TreeItems;
using Microsoft.JSON.Editor.Completion;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Utilities;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Microsoft.Web.LibraryManager.Vsix
{
    [Export(typeof(IJSONCompletionListProvider))]
    [Name(nameof(ProviderCompletionProvider))]
    internal class ProviderCompletionProvider : BaseCompletionProvider
    {
        private static readonly ImageMoniker _libraryIcon = KnownMonikers.Method;

        public override JSONCompletionContextType ContextType
        {
            get { return JSONCompletionContextType.PropertyValue; }
        }

        protected override IEnumerable<JSONCompletionEntry> GetEntries(JSONCompletionContext context)
        {
            var member = context.ContextItem as JSONMember;

            if (member == null || (member.UnquotedNameText != "provider" && member.UnquotedNameText != "defaultProvider"))
                yield break;

            var dependencies = Dependencies.FromConfigFile(ConfigFilePath);
            IEnumerable<string> providerIds = dependencies.Providers?.Select(p => p.Id);

            if (providerIds == null || !providerIds.Any())
                yield break;

            foreach (string id in providerIds)
            {
                yield return new SimpleCompletionEntry(id, _libraryIcon, context.Session);
            }

            Telemetry.TrackUserTask("completionprovider");
        }
    }
}