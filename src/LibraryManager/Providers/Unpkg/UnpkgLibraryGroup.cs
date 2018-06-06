using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Providers.Unpkg
{
    internal class UnpkgLibraryGroup : ILibraryGroup
    {
        public UnpkgLibraryGroup(string displayName, string description = null)
        {
            DisplayName = displayName;
            Description = description;
        }
        public string DisplayName { get; }

        public string Description { get; }

        public Task<IEnumerable<string>> GetLibraryIdsAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
