// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.Utilities;
using Microsoft.Web.LibraryManager.Vsix.Contracts;
using Microsoft.WebTools.Languages.Json.Editor.Completion;
using Microsoft.WebTools.Languages.Json.Parser.Nodes;

namespace Microsoft.Web.LibraryManager.Vsix.Json.Completion
{
    [Export(typeof(IJsonCompletionListProvider))]
    [Name(nameof(ProviderCompletionProvider))]
    internal class ProviderCompletionProvider : BaseCompletionProvider
    {
        private static readonly ImageMoniker LibraryIcon = KnownMonikers.Method;

        private readonly IDependenciesFactory _dependenciesFactory;

        [ImportingConstructor]
        internal ProviderCompletionProvider(IDependenciesFactory dependenciesFactory)
        {
            _dependenciesFactory = dependenciesFactory;
        }

        public override JsonCompletionContextType ContextType
        {
            get { return JsonCompletionContextType.PropertyValue; }
        }

        protected override IEnumerable<JsonCompletionEntry> GetEntries(JsonCompletionContext context)
        {
            var member = context.ContextNode as MemberNode;

            if (member == null || (member.UnquotedNameText != ManifestConstants.Provider && member.UnquotedNameText != ManifestConstants.DefaultProvider))
                yield break;

            var dependencies = _dependenciesFactory.FromConfigFile(ConfigFilePath);
            IEnumerable<string> providerIds = dependencies.Providers?.Select(p => p.Id);

            if (providerIds == null || !providerIds.Any())
                yield break;

            foreach (string id in providerIds)
            {
                yield return new SimpleCompletionEntry(id, LibraryIcon, context.Session);
            }
        }
    }
}
