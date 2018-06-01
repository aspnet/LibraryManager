// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.Web.LibraryManager
{
    /// <summary>
    /// Compares two instances of CacheServiceMetadata
    /// </summary>
    internal class MetadataComparer : IEqualityComparer<CacheServiceMetadata>
    {
        public MetadataComparer()
        {
        }

        public bool Equals(CacheServiceMetadata instance1, CacheServiceMetadata instance2)
        {

            if (instance1 != null && instance2 != null &&
                instance1.DestinationPath == instance2.DestinationPath &&
                instance1.Source == instance2.Source)
            {
                return true;
            }

            return false;
        }

        public int GetHashCode(CacheServiceMetadata obj)
        {
            if (obj == null)
            {
                return 0;
            }

            int destinationCode = obj.DestinationPath == null ? 0 : obj.DestinationPath.GetHashCode();
            int sourceCode = obj.Source == null ? 0 : obj.Source.GetHashCode();

            return destinationCode ^ sourceCode;
        }
    }
}
