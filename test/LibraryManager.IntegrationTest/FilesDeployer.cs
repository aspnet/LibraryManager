using System;
using System.IO;
using System.Reflection;

namespace Microsoft.Web.LibraryManager.IntegrationTest
{
    /// <summary>
    /// Deploys assembly manifest resources.
    /// </summary>
    public static class FilesDeployer
    {
        /// <summary>
        /// Method for deploying a set of resources logically inside a given directory.
        /// </summary>
        /// <param name="owningAssembly">The assembly containing the resources
        /// <param name="deploymentDirectory">The destination directory where the files should be deployed.</param>
        /// <param name="rootDirectory">The root directory of the files to be deployed. Any embedded resource 
        /// whose name starts with this rootDirectory is deployed to the deploymentDirectory. The logical name of the 
        /// embedded resource is used as the relative path from deploymentDirectory.</param>
        public static void DeployDirectory(Assembly owningAssembly, string deploymentDirectory, string rootDirectory)
        {
            string[] resources = owningAssembly.GetManifestResourceNames();

            foreach (var resourceName in resources)
            {
                if (resourceName.StartsWith(rootDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    string filePath = Path.Combine(deploymentDirectory, resourceName);

                    using (var inStream = owningAssembly.GetManifestResourceStream(resourceName))
                    {
                        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                        }
                        using (var outStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {
                            inStream.CopyTo(outStream);
                            outStream.Flush();
                        }
                    }
                }
            }
        }

        public static bool ForceDeleteDirectory(string path)
        {
            bool isDeleted = true;

            try
            {
                DirectoryInfo directory = new DirectoryInfo(path) { Attributes = FileAttributes.Normal };

                FileSystemInfo[] fileSystemInfos = directory.GetFileSystemInfos("*", SearchOption.AllDirectories);
                foreach (FileSystemInfo info in fileSystemInfos)
                {
                    // Unfortunately, Directory.Delete doesn't work if there are readonly items inside it. Thus,
                    //   we need to remove any read-only bits to get the delete to occur.
                    info.Attributes = FileAttributes.Normal;
                }

                directory.Delete(true);
            }
            catch (IOException)
            {
                // Don't fail if the directoy doesn't actually delete.
                isDeleted = false;
            }

            return isDeleted;
        }
    }
}
