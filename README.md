# Microsoft Library Manager

Install and consume 3rd-party client-side libraries with ease.

[![Build status](https://ci.appveyor.com/api/projects/status/vc2ixijbk1ak780e?svg=true)](https://ci.appveyor.com/project/madskristensen/LibraryManager)

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

- Add any library from [cdnjs.com](https://cdnjs.com/)
- Add any file from file system, network share or remote URL
- Only add the file(s) you need
- Can install any file into any folder in your project/solution
- Optional manifest file (`libman.json`)
- Very fast
- Full Visual Studio 2017 integration

## Getting started
Right-click any web project in Solution Explorer and hit **Manage Client-side Libraries...**.

![Context menu](art/context-menu-project.png)

This will create a `libman.json` file in the root of the project.

## libman.json

### Context menu
Right-click `libman.json` in Solution Explorer to access commands that help managing the libraries.

![context menu libman.json](art/context-menu-config.png)

### Intellisense
Edit the libman.json file to install libraries. Every time the file is saved, Visual Studio will install/restore the packages.

![libman.json](art/library.json%20typing.gif)

See [libman.json reference](https://github.com/aspnet/LibraryManager/wiki/library.json-reference) for more information.

### Light bulbs
Inside libman.json there are light bulbs that show up with helpful commands.

![Light bulbs](art/light-bulbs.png)

## Road map and release notes
See the [CHANGELOG](CHANGELOG.md) for road map and release notes

# Feedback

Check out the [contributing](.github/CONTRIBUTING.md) page to see the best places to log issues and start discussions.
