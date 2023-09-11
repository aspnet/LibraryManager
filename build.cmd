msbuild /r %~dp0LibraryManager.sln /clp:Verbosity=Minimal;Summary;ForceNoAlign /bl:%~dp0artifacts/Build.binlog %*
