
// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1032:Implement standard exception constructors", Justification = "Custom exception types require multiple parameters to be useful")]

// Suppress issues in code from external sources
[assembly: SuppressMessage("Design", "CA1064:Exceptions should be public", Justification = "<Pending>", Scope = "type", Target = "~T:Microsoft.Extensions.CommandLineUtils.CommandParsingException")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Extensions.CommandLineUtils.CommandOption.#ctor(System.String,Microsoft.Extensions.CommandLineUtils.CommandOptionType)")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Extensions.CommandLineUtils.AnsiConsole.WriteLine(System.String)")]
[assembly: SuppressMessage("Globalization", "CA1307:Specify StringComparison", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Extensions.CommandLineUtils.CommandLineApplication.Execute(System.String[])~System.Int32")]
[assembly: SuppressMessage("Globalization", "CA1310:Specify StringComparison for correctness", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Extensions.CommandLineUtils.CommandOption.#ctor(System.String,Microsoft.Extensions.CommandLineUtils.CommandOptionType)")]
[assembly: SuppressMessage("Globalization", "CA1310:Specify StringComparison for correctness", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Extensions.CommandLineUtils.AnsiConsole.WriteLine(System.String)")]
[assembly: SuppressMessage("Globalization", "CA1310:Specify StringComparison for correctness", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Extensions.CommandLineUtils.CommandLineApplication.Execute(System.String[])~System.Int32")]
[assembly: SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "<Pending>", Scope = "member", Target = "~M:Microsoft.Extensions.CommandLineUtils.CommandLineApplication.Execute(System.String[])~System.Int32")]
