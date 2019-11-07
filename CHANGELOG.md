# Change log

## 2.0.96
- Packages updated on NuGet
- Fixed issue where mismatching versions between VS and the Microsoft.Web.LibraryManager.Build package would cause API conflicts.
- Enable roll-forward-on-major-version for CLI tool (to run on machines with netcore3.0 but not netcore2.1)

## 2.0.87
- Included in Visual Studio 16.4
- Addresses a UI hang in the wizard waiting for network requests

## 2.0.83
- Included in Visual Studio 16.3
- Improves completion for scoped NPM packages in the UI wizard
- Automatically append latest version when completing package names for Unpkg and JsDelivr (like how Cdnjs has done)

## 2.0.76
- Packages updated on NuGet
- Fixes issue #500
  - Thanks @Danielku15 for contirbuting the fix!

## 2.0.65
- Included in Visual Studio 16.2
- Changed NPM package search to use registry.npmjs.org instead of skimdb.npmjs.com, as NPM deprecated the latter
- Bug fixes, including #395, #475

## 2.0.48
- Included in Visual Studio 16.1
- Packages updated on NuGet
- Added support for proxy server settings
- Bug fixes

## 2.0.22
- Included in Visual Studio 16.0
- Absorbs breaking changes in VS JSON editor extensibility UI
- Added a retry when downloading files to improve reliability from flaky CDN downloads
- Converted all providers to use HTTPS
- Various bug fixes since 1.0.151

## 1.0.172
- Packages updated on NuGet
- Adds support for new JsDelivr provider
  - Thanks @Drgy for providing this new feature!

## 1.0.163
- Packages updated on NuGet
- Introduces LibMan CLI as a global tool

## 1.0.151
- Included in Visual Studio 15.9
- Downgrades System.ValueTuple to match Visual Studio.

## 1.0.149
- Included in Visual Studio 15.8
- Adds the Install Client Side Library UI
- Several bug fixes and improvements

## 1.0.113
- Package updated on NuGet
- Various fixes

## 1.0.30
- Included in Visual Studio 15.7
- Light bulb actions for uninstall library
- Documentation on wiki and https://docs.microsoft.com/aspnet/core/client-side/libman
  - Thanks @scottaddie for microsoft.com documentation!

## 1.0.20
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
