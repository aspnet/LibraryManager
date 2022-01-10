# Change log

## 2.1.161
Commit: abc97ecc7db29a48992d14f1585cc092acbf66aa
- Packages updated on NuGet
- Included in Visual Studio 17.1
- Worked around restore issues during VS Live Unit Testing (#644)
- Fixed issues with cross-polluting projects if multiple libman.json files were open in VS (#638)
- Fixed Unpkg provider to show prerelease versions of libraries (#349)
- Several small improvements to the Add New Client-Side Library dialog (#226, #397, #393, #508, #518, #531)
- Some small cleanup of CLI commands (#369, #127)
- Some resource string tweaks (#130, #212) and misc bug fixes (#222, #396)

## 2.1.113
Commit: 422d40002e1fad0412a4bea45850227cf8eefa45
- Packages updated on NuGet
- Included in Visual Studio 16.8
- Added caching for metadata from Cdnjs and jsDelivr providers
- Fixed detection of latest version for Cdnjs provider
- Implemented detection of pre-release and GitHub versions for jsDelivr provider
- Improve CLI error message for unrecognized provider names
  - Thanks @RobJohnston!
- Implement theming support in the VS dialog
- Various engineering and refactoring changes.

## 2.1.76
Commit: 3cafd794c1068ee7d8bcade05ddf087f503075a3
- Packages updated on NuGet
- Fix an issue in 2.1.50 where runtime depenencies were not included in the Microsoft.Web.LibraryManager.Build package for .NET Core
- Engineering work to move to new Azure Pipelines for builds

## 2.1.50
Commit: 25aae5a24a0fb2b8b916e33f4269b9690725f02b
- Packages updated on NuGet
- Moved download cache to %LocalAppData% to avoid synchronizing with roaming Windows user profiles
- Add support for file glob patterns
- Make downloads happen in parallel (up to 10 at once)
- Remove 30-day expiration for cached content files (assume library contents are immutable)
- Made some improvements to file copy speed
- Add support for @latest tag for JSDelivr and unpkg providers
- Read settings from environment variables
- Add support for setting proxy via HTTPS_PROXY
- Don't add '@' to the suggested install path for scoped packages
- Lots of miscellaneous refactoring and test fixes

## 2.0.96
Commit: 65c9be8001a08748466e30ccdcc8fd8ac2168834
- Packages updated on NuGet
- Fixed issue where mismatching versions between VS and the Microsoft.Web.LibraryManager.Build package would cause API conflicts.
- Enable roll-forward-on-major-version for CLI tool (to run on machines with netcore3.0 but not netcore2.1)

## 2.0.87
Commit: bb515bf382e7e7a4d67eb62a425468230cf36906
- Included in Visual Studio 16.4
- Addresses a UI hang in the wizard waiting for network requests

## 2.0.83
Commit: bc8a4b23ecda98996464bc3c23de9a41b0015dc3
- Included in Visual Studio 16.3
- Improves completion for scoped NPM packages in the UI wizard
- Automatically append latest version when completing package names for Unpkg and JsDelivr (like how Cdnjs has done)

## 2.0.76
Commit: b7a1f9d9b081c2a9fd5442ccbb65a71c1b83ccd5
- Packages updated on NuGet
- Fixes issue #500
  - Thanks @Danielku15 for contributing the fix!

## 2.0.65
Commit: 78c63892478b56cfb084ffd7f36aeec30f32d318
- Included in Visual Studio 16.2
- Changed NPM package search to use registry.npmjs.org instead of skimdb.npmjs.com, as NPM deprecated the latter
- Bug fixes, including #395, #475

## 2.0.48
Commit: 1636572a13a24022d26117501a24917306499fb3
- Included in Visual Studio 16.1
- Packages updated on NuGet
- Added support for proxy server settings
- Bug fixes

## 2.0.22
Commit: bf21b33dbeef67e3f47e5d88e7b546b11e3a4c62
- Included in Visual Studio 16.0
- Absorbs breaking changes in VS JSON editor extensibility UI
- Added a retry when downloading files to improve reliability from flaky CDN downloads
- Converted all providers to use HTTPS
- Various bug fixes since 1.0.151

## 1.0.172
Commit: 2a33cb3eeb98acdf85b6acc5252dcb02c8e12406
- Packages updated on NuGet
- Adds support for new JsDelivr provider
  - Thanks @Drgy for providing this new feature!

## 1.0.163
Commit: 45474d37ed6977b744786e102c397b4f558a8096
- Packages updated on NuGet
- Introduces LibMan CLI as a global tool

## 1.0.151
Commit: c4fdfd23799763bba74ace066940b45d91e96ccd
- Included in Visual Studio 15.9
- Downgrades System.ValueTuple to match Visual Studio.

## 1.0.149
Commit: 9b05d9391b94a5dc975a9d096e4a37d4ebb01e14
- Included in Visual Studio 15.8
- Adds the Install Client Side Library UI
- Several bug fixes and improvements

## 1.0.113
Commit: 10f080dcb8c8d9bb444439be9be8a0584dc7b67c
- Package updated on NuGet
- Various fixes

## 1.0.30
Commit: 28ba21c86dccaeca651f3bc76359cffbdebb44ee
- Included in Visual Studio 15.7
- Light bulb actions for uninstall library
- Documentation on wiki and https://docs.microsoft.com/aspnet/core/client-side/libman
  - Thanks @scottaddie for microsoft.com documentation!

## 1.0.20
Commit: 3a85c785fce5e5d58f2345b43fd86532a84b35da
- Package released on NuGet
- Various bug fixes

## 1.0.0-alpha

- Initial release
- JSON Intellisense
- Clean libraries command
- Restore from manifest file
- Solution-wide restore
- Error List support
- Output Window logging
