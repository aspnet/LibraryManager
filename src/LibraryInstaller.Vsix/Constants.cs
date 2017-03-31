using System;

namespace LibraryInstaller.Vsix
{
    public class Constants
    {
        public const string ConfigFileName = "library.json";
        public static string CacheFolder = Environment.ExpandEnvironmentVariables(@"%localappdata%\Microsoft\Library\");
        public const string TelemetryNamespace = "WebTools/LibraryInstaller/";
    }
}
