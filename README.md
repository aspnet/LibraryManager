# Microsoft Library Manager

Install and consume 3rd-party client-side libraries with ease.

[![Build Status](https://dev.azure.com/azure-public/vswebtools/_apis/build/status/aspnet.LibraryManager?branchName=main)](https://dev.azure.com/azure-public/vswebtools/_build/latest?definitionId=43&branchName=main)

## Reasons to use LibMan
1.	For apps not currently using another package manager
2.	For projects where you think Bower and npm are overkill
3.	For developers that don't want to use Bower/Yarn or npm
4.	For developers that value simplicity in their tools
5.	For using custom or private packages/files
6.  For orchestrating file placement within your project

## Reasons NOT to use LibMan
1.	Developer/team likes using tools such Bower, Yarn or npm
2.	For apps that uses WebPack or Browserify for module loading/bundling

## Features

- Add any library from [cdnjs.com](https://cdnjs.com/), [jsdelivr.com](https://www.jsdelivr.com/), or [unpkg.com](https://unpkg.com/)
- Add any file from file system, network share or remote URL
- Only add the file(s) you need
- Can install any file into any folder in your project/solution
- Very fast

## Installation
- IDE integration pre-installed with Visual Studio 2017 and newer
- Available as a dotnet global tool - [Microsoft.Web.LibraryManager.Cli](https://www.nuget.org/packages/Microsoft.Web.LibraryManager.Cli/)
- MSBuild integration as a NuGet package - [Microsoft.Web.LibraryManager.Build](https://www.nuget.org/packages/Microsoft.Web.LibraryManager.Build)

## Getting started
In Visual Studio, right-click any web project in Solution Explorer and hit **Manage Client-side Libraries...**.

![Context menu](art/context-menu-project.png)

This will create a `libman.json` file in the root of the project.

Or with the CLI tool, run `libman init` to create a libman.json in the current folder.

## libman.json

### Context menu
Right-click `libman.json` in Solution Explorer to access commands that help managing the libraries.

![context menu libman.json](art/context-menu-config.png)

### Intellisense
Edit the libman.json file to install libraries. Every time the file is saved, Visual Studio will install/restore the packages.

![libman.json](art/libmanjsontyping.gif)

See [libman.json reference](https://github.com/aspnet/LibraryManager/wiki/libman.json-reference) for more information.

### Light bulbs
Inside libman.json there are light bulbs that show up with helpful commands.

![Light bulbs](art/light-bulbs.png)

## Telemetry
The Visual Studio experience for Library Manager follows all of the settings for telemetry from Visual Studio.

## Road map and release notes
See the [CHANGELOG](CHANGELOG.md) for road map and release notes

# Feedback

Check out the [contributing](.github/CONTRIBUTING.md) page to see the best places to log issues and start discussions.

# Reporting Security Issues

Please refer to [SECURITY.md](SECURITY.md)

## Trademarks
This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft trademarks or logos is subject to and must follow Microsoft's Trademark & Brand Guidelines. Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship. Any use of third-party trademarks or logos are subject to those third-party's policies.
