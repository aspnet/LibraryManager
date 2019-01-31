// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// TODO: resolve why we are using fields instead of properties.
#pragma warning disable CA1051 // Do not declare visible instance fields

namespace Microsoft.Web.LibraryManager.Contracts
{
    /// <summary>
    /// A span for use by completion
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CompletionSet : IEquatable<CompletionSet>
    {
        // IMPORTANT: Do not change the order of the fields below!!!

        /// <summary>
        /// The start position of the span
        /// </summary>
        public int Start;

        /// <summary>
        /// The length of the span
        /// </summary>
        public int Length;

        /// <summary>
        /// The list of completions for the span
        /// </summary>
        public IEnumerable<CompletionItem> Completions;

        /// <summary>
        /// The type of the completion item sorting.
        /// </summary>
        public CompletionSortOrder CompletionType;

        /// <summary>
        /// Returns whether the objects are equal.
        /// </summary>
        /// <param name="obj"></param>
        public override bool Equals(object obj)
        {
            if (obj is CompletionSet other)
            {
                // TODO: Should this also compare Completions?
                return Equals(other);
            }
            return false;
        }

        /// <summary>
        /// Returns whether the CompletionSets are equal.
        /// </summary>
        /// <param name="other"></param>
        public bool Equals(CompletionSet other)
        {
            // TODO: Should this also compare Completions?
            return Start == other.Start
                && Length == other.Length
                && CompletionType == other.CompletionType;
        }

        /// <summary>
        /// Equality operator for CompletionSets
        /// </summary>
        public static bool operator ==(CompletionSet left, CompletionSet right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator for CompletionSets
        /// </summary>
        public static bool operator !=(CompletionSet left, CompletionSet right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Gets a hash code for the object.
        /// </summary>
        public override int GetHashCode()
        {
            return Start ^ Length ^ Completions.GetHashCode() ^ CompletionType.GetHashCode();
        }
    }

    /// <summary>
    /// The completion item to show in supporting editors.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct CompletionItem : IEquatable<CompletionItem>
    {
        // IMPORTANT: Do not change the order of the fields below!!!

        /// <summary>
        /// The display text of the completion item.
        /// </summary>
        public string DisplayText;

        /// <summary>
        /// The insertion text is what is being insert when completion is committed.
        /// </summary>
        public string InsertionText;

        /// <summary>
        /// The description is shown in tooltips and parameter info.
        /// </summary>
        public string Description;

        /// <summary>
        /// Returns whether the two objects are equal.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is CompletionItem other)
            {
                return Equals(other);
            }

            return false;
        }

        /// <summary>
        /// Returns whether the two CompletionItems are equal
        /// </summary>
        public bool Equals(CompletionItem other)
        {
            return InsertionText == other.InsertionText
                && DisplayText == other.DisplayText
                && Description == other.Description;
        }

        /// <summary>
        /// Gets a hash code for the object
        /// </summary>
        public override int GetHashCode()
        {
            // Much of the time, InsertionText == DisplayText, so XORing those would just cancel out.
            return InsertionText.GetHashCode() ^ Description.GetHashCode();
        }

        /// <summary>
        /// Equality operator for CompletionItems
        /// </summary>
        public static bool operator ==(CompletionItem left, CompletionItem right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Inequality operator for CompletionItems
        /// </summary>
        public static bool operator !=(CompletionItem left, CompletionItem right)
        {
            return !(left == right);
        }
    }

    /// <summary>
    /// The completion sort order is to indicate the sorting type of the completion item.
    /// </summary>
    public enum CompletionSortOrder
    {
        /// <summary>The completion item is alphabetical sorted.</summary>
        Alphabetical,
        /// <summary>The completion item is for library version completion.</summary>
        Version,
        /// <summary>The completion item is sorted by providers.</summary>
        AsSpecified
    }
}

#pragma warning restore CA1051 // Do not declare visible instance fields

