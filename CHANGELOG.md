# Road map

- [ ] .NET naming rules dynamic validation
- [ ] .NET naming rules dynamic Intellisense
- [ ] Add item template for .editorconfig files

Features that have a checkmark are complete and available for
download in the
[CI build](http://vsixgallery.com/extension/1209461d-57f8-46a4-814a-dbe5fecef941/).

# Change log

These are the changes to each version that has been released
on the official Visual Studio extension gallery.

## 1.17

- [x] .NET naming rules basic Intellisense and validation
- [x] .NET naming rules value Intellisense

## 1.16

- [x] Fixed typo
- [x] Fixed ArgumentOutOfRangeException in Intellisense

## 1.15

- [x] Validate spaces in globbing patterns
- [x] Implement error suppression
- [x] Add suppressions from light bulbs
- [x] Intellisense for error codes in suppression
- [x] Hover tooltips for suppressions
- [x] Document suppression feature in [readme.md](README.md)
- [x] Suppress errors from Error List context menu
- [x] Hide _Navigate to parent_ from other file types

## 1.14

- [x] Always require severity for supporting properties
- [x] Fixed issue with whitespace validation
- [x] Support for code snippets
- [x] Fix order of completion items

## 1.13

- [x] Fixed issue with auto-list members
- [x] Add *Navigate To Parent* to context menu
- [x] Enable async completion
- [x] Remove *Code Navigation* group from context menu
- [x] Only show completion for `root` at top of document
- [x] Support *Complete Word* command properly
- [x] Support *Show Member List* command properly
- [x] Stop auto-injecting `=` and `:` if they exist

## 1.12

- [x] Updated schema to allow multiple values
- [x] Validator allows for comma separated values
- [x] Intellisenes triggers on `,` for supported properties
- [x] Made new line rules visible
- [x] Source code adhering to Roslyn coding standards
- [x] Switch icons to the official .editorconfig icon

## 1.11

- [x] Error codes should be clickable
- [x] Add description to each error code on the [wiki](https://github.com/madskristensen/EditorConfigLanguage/wiki/Error-codes)
- [x] Updated severity requirements
- [x] Added "all" to values for C# new-line properties
- [x] Don't invalidate the new "unset" value on standard properties
- [x] Mark "unset" value as unsupported
- [x] Move schema into JSON file
- [x] Show hover tooltip for values
- [x] Add XMLDoc comments
- [x] Add validation for unneeded properties
- [x] Insert "=" and ":" on Intellisense commit
- [x] Added Intellisense options

## 1.10

- [x] Fixed issue with formatter
- [x] Added command to navigate to parent (F12 shortcut)
- [x] SignatureHelp for writing sections
- [x] Normalize property names in hover tooltips
- [x] Performance improvements to validator
- [x] Syntax validation for section strings
- [x] Convert to use new error list API
- [x] Clear errors on document close
- [x] No custom classification definitions
- [x] Stop invalidate undocumented properties

## 1.9

- [x] Updated colors to match the predefined ones
- [x] Added folder name in navigational dropdown
- [x] Indication of sections not matching any files
- [x] Update descriptions of C#/.NET anaylizer rules
- [x] Preview tab support

## 1.8

- [x] Invalidate empty section
- [x] Reduce items in Intellisense while typing
- [x] Intellisense substring filtering
- [x] Light bulb to remove duplicate properties
- [x] Indication of rules that are already defined by parent .editorconfig
- [x] Update tooltip to display larger icon
- [x] Removing empty lines when sorting sections

## 1.7

- [x] Validation options
- [x] Light bulb to sort properties in a section
- [x] Light bulb to sort properties in all sections
- [x] Dismiss QuickInfo on completion sesstion start
- [x] Context menu command to open settings page
- [x] New color scheme
- [x] Align equal chars on format
- [x] Hide severity completion for standard properties
- [x] Validate missing property value
- [x] Run validation on idle
- [x] Overflow long lines in QuickInfo tooltips

## 1.6

- [x] Fixed typing delay
- [x] Rich QuickInfo tooltips
- [x] Rich completion tooltips
- [x] Dark theme support
- [x] Support for AnyCode
- [x] Show Intellisense when severity glyph is clicked
- [x] Validate severity used on standard properties
- [x] Hide severity completion from standard properties

## 1.5

- [x] Updated screenshots in readme file
- [x] Show inheritance hierarchy
- [x] Duplicate section validation
- [x] Glyphs to show severity
- [x] Hide "root" from completion when it isn't valid to use
- [x] Make parsing async
- [x] Show error on duplicate properties
- [x] Implement custom parser
- [x] Show validation error from wrong use of "root"
- [x] Show error messages in hover tooltips
- [x] Support semicolons as comments
- [x] Select matching part of completion item
- [x] Intellisense Filters (icons at bottom of completion list)
- [x] Intellisense substring match on typing
- [x] Light bulb for deleting section

## 1.4

- [x] Navigational drop downs
- [x] Make headings bold
- [x] Intellisense filtering when typing

## 1.3

- [x] Move strings to .resx file
- [x] Updated logo
- [x] Added C# and .NET style rules
- [x] Validate severity values
- [x] Make validator case insensitive
- [x] Validate unknown keywords
- [x] Using built-in brace completion logic
- [x] Filter completion list on typing
- [x] Warning icons for unsupported properties
- [x] Persist outlining state

## 1.2

- [x] Status bar text saying that reopening files are required
- [x] Better Intellisense triggers
- [x] Gesture to create .editorconfig file
- [x] Description of what .editorconfig is in readme.md
- [x] F1 opens http://editorconfig.org
- [x] Mark unsupported keywords

## 1.1

- [x] Outlining
- [x] Brace matching
- [x] Drag 'n drop
- [x] File icon for .editorconfig files
- [x] Formatting command
- [x] Validate numeric values
- [x] Quick Info tooltips

## 1.0

- [x] Initial release
- [x] Syntax highlighting
- [x] Intellisense
- [x] Validation
- [x] Comment/uncomment
- [x] Brace completion