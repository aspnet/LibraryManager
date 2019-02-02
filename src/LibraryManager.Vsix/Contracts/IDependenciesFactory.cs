using Microsoft.Web.LibraryManager.Contracts;

namespace Microsoft.Web.LibraryManager.Vsix.Contracts
{
    internal interface IDependenciesFactory
    {
        IDependencies FromConfigFile(string configFilePath);
    }
}
