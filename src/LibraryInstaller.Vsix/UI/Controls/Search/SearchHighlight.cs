// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using LibraryInstaller.Vsix.Models;

namespace LibraryInstaller.Vsix.Controls.Search
{
    public static class SearchHighlight
    {
        public static readonly DependencyProperty SourceTextProperty = DependencyProperty.RegisterAttached(
            "SourceText", typeof(string), typeof(SearchHighlight), new PropertyMetadata("", TextChanged));


        public static readonly DependencyProperty HighlightStyleProperty = DependencyProperty.RegisterAttached(
            "HighlightStyle", typeof(Style), typeof(SearchHighlight), new PropertyMetadata(default(Style), StyleChanged));

        private static void StyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Label lbl = d as Label;
            TextBlock block = d as TextBlock;

            if (lbl != null)
            {
                block = lbl.Content as TextBlock;

                if (block == null)
                {
                    string lblChild = lbl.Content as string;

                    if (lblChild == null)
                    {
                        return;
                    }

                    TextBlock newChild = new TextBlock { Text = lblChild };
                    lbl.Content = newChild;
                    block = newChild;
                }
            }

            if (block == null)
            {
                return;
            }
        }

        public static void SetSourceText(DependencyObject element, string value)
        {
            element.SetValue(SourceTextProperty, value);
        }

        public static string GetSourceText(DependencyObject element)
        {
            return (string)element.GetValue(SourceTextProperty);
        }

        public static void SetHighlightStyle(DependencyObject element, Style value)
        {
            element.SetValue(HighlightStyleProperty, value);
        }

        public static Style GetHighlightStyle(DependencyObject element)
        {
            return (Style)element.GetValue(HighlightStyleProperty);
        }

        public static readonly DependencyProperty HighlightTextProperty = DependencyProperty.RegisterAttached(
            "HighlightText", typeof(string), typeof(SearchHighlight), new PropertyMetadata(default(string), TextChanged));

        private static void TextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            Label lbl = d as Label;
            TextBlock block = d as TextBlock;

            if (lbl != null)
            {
                block = lbl.Content as TextBlock;

                if (block == null)
                {
                    string lblChild = lbl.Content as string;
                    TextBlock newChild = new TextBlock { Text = lblChild ?? "" };
                    lbl.Content = newChild;
                    block = newChild;
                }
            }

            if (block == null)
            {
                return;
            }

            string searchText = GetHighlightText(d);
            string blockText = GetSourceText(d);

            if (blockText == null)
            {
                return;
            }

            int last = 0;
            block.Inlines.Clear();

            if (!string.IsNullOrEmpty(searchText))
            {
                IReadOnlyList<PackageSearchUtil.Range> matches = PackageSearchUtil.ForTerm(searchText).GetMatchesInText(blockText);

                for (int i = 0; i < matches.Count; ++i)
                {
                    if (matches[i].Length == 0)
                    {
                        continue;
                    }

                    if (last < matches[i].Start)
                    {
                        string inserted = blockText.Substring(last, matches[i].Start - last);
                        block.Inlines.Add(inserted);
                        last += inserted.Length;
                    }

                    Run highlight = new Run(matches[i].ToString());
                    highlight.SetBinding(FrameworkContentElement.StyleProperty, new Binding
                    {
                        Mode = BindingMode.OneWay,
                        Source = d,
                        Path = new PropertyPath(HighlightStyleProperty)
                    });
                    block.Inlines.Add(highlight);
                    last += matches[i].Length;
                }
            }

            if (last < blockText.Length)
            {
                block.Inlines.Add(blockText.Substring(last));
            }
        }

        public static void SetHighlightText(DependencyObject element, string value)
        {
            element.SetValue(HighlightTextProperty, value);
        }

        public static string GetHighlightText(DependencyObject element)
        {
            return (string)element.GetValue(HighlightTextProperty);
        }
    }
}
