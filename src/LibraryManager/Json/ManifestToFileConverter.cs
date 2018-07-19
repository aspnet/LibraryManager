using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Json
{
    internal class ManifestToFileConverter
    {
        public Manifest ConvertToManifest(ManifestOnDisk manifestOnDisk, IDependencies dependencies)
        {
            if (manifestOnDisk == null)
            {
                return null;
            }

            var manifest = new Manifest(dependencies)
            {
                Version = manifestOnDisk.Version,
                DefaultDestination = manifestOnDisk.DefaultDestination,
                DefaultProvider = manifestOnDisk.DefaultProvider,
            };

            var libraryStateConverter = new LibraryStateToFileConverter(manifest.DefaultProvider, manifest.DefaultDestination);

            if (manifestOnDisk.Libraries != null)
            {
                foreach (LibraryInstallationStateOnDisk lod in manifestOnDisk.Libraries)
                {
                    manifest.AddLibrary(libraryStateConverter.ConvertToLibraryInstallationState(lod));
                }
            }

            return manifest;
        }

        public ManifestOnDisk ConvertToManifestOnDisk(Manifest manifest)
        {
            if (manifest == null)
            {
                return null;
            }

            var manifestOnDisk = new ManifestOnDisk()
            {
                Version = manifest.Version,
                DefaultDestination = manifest.DefaultDestination,
                DefaultProvider = manifest.DefaultProvider,
            };

            var libraries = new List<LibraryInstallationStateOnDisk>();
            var libraryStateConverter = new LibraryStateToFileConverter(manifest.DefaultProvider, manifest.DefaultDestination);


            foreach (ILibraryInstallationState state in manifest.Libraries)
            {
                libraries.Add(libraryStateConverter.ConvertToLibraryInstallationStateOnDisk(state));
            }

            manifestOnDisk.Libraries = libraries;

            return manifestOnDisk;
        }
    }
}
