using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LibraryManager.Vsix.Controls.Search;

namespace LibraryManager.Vsix.UI.Controls.Search
{
    internal class PackageSearchUtil
    {
        private static PackageSearchUtil _current;

        private readonly string[] _parts;

        private PackageSearchUtil(string searchTerm)
        {
            SearchTerm = searchTerm;
            _parts = searchTerm.Split(new[] { ' ', '-', '.' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public string SearchTerm { get; }

        public static PackageSearchUtil ForTerm(string term)
        {
            if (string.Equals(term, _current?.SearchTerm, StringComparison.Ordinal))
            {
                return _current;
            }

            return _current = new PackageSearchUtil(term);
        }

        private static int CalculatePartScore(string alias, string part)
        {
            int matchIndex = alias?.IndexOf(part, StringComparison.OrdinalIgnoreCase) ?? -1;
            if (matchIndex > -1)
            {
                double pctUsed = (double)part.Length / alias.Length;
                double pctThrough = 1 - (double)matchIndex / alias.Length;
                int score = (int)(100 * pctUsed * pctThrough);
                return score & 0x7F;
            }

            return 0;
        }

        public int CalculateMatchStrength(ISearchItem searchItem)
        {
            if (string.IsNullOrEmpty(SearchTerm))
            {
                return 1;
            }

            int wholeScore = CalculatePartScore(searchItem.Alias, SearchTerm);
            if (wholeScore > 0)
            {
                return wholeScore << 24;
            }

            int runningScore = 0;
            int partLengthMatched = 0;
            for (int i = 0; i < _parts.Length; ++i)
            {
                int partScore = CalculatePartScore(searchItem.Alias, _parts[i]);
                if (partScore > 0)
                {
                    partLengthMatched += _parts[i].Length;
                    runningScore += partScore & 0x7F;
                }
            }

            return ((partLengthMatched & 0x7F) << 16) | (runningScore & 0xFFFF);
        }

        public bool IsMatch(ISearchItem searchItem)
        {
            return CalculateMatchStrength(searchItem) > 0;
        }

        public IReadOnlyList<Range> GetMatchesInText(string text)
        {
            List<Range> ranges = new List<Range>();

            string pattern = Regex.Escape(SearchTerm);
            Regex r = new Regex(pattern, RegexOptions.IgnoreCase);
            MatchCollection matches = r.Matches(text);
            ProcessMatchCollection(text, matches, ranges);

            for (int i = 0; i < _parts.Length; ++i)
            {
                pattern = Regex.Escape(_parts[i]);
                r = new Regex(pattern, RegexOptions.IgnoreCase);
                matches = r.Matches(text);
                ProcessMatchCollection(text, matches, ranges);
            }

            return ranges;
        }

        private void ProcessMatchCollection(string sourceString, MatchCollection matches, List<Range> ranges)
        {
            foreach (Match match in matches)
            {
                Range range = new Range(match.Index, match.Length, sourceString);
                if (ranges.Count == 0)
                {
                    ranges.Add(range);
                }
                else
                {
                    bool included = false;
                    int sortPosition = 0;
                    for (int i = 0; i < ranges.Count; ++i)
                    {
                        if (ranges[i].Start < range.Start)
                        {
                            sortPosition = i + 1;
                        }

                        Range tmp;
                        if (range.TryUnion(ranges[i], out tmp))
                        {
                            included = true;
                            ranges[i] = tmp;

                            if (i < ranges.Count - 1 && tmp.TryUnion(ranges[i + 1], out range))
                            {
                                ranges[i] = range;
                                ranges.RemoveAt(i + 1);
                                break;
                            }
                        }
                    }

                    if (!included)
                    {
                        if (sortPosition == ranges.Count)
                        {
                            ranges.Add(range);
                        }
                        else
                        {
                            ranges.Insert(sortPosition, range);
                        }
                    }
                }
            }
        }

        public struct Range
        {
            public Range(int start, int length, string sourceString)
            {
                Start = start;
                Length = length;
                SourceString = sourceString;
            }

            public readonly int Length;

            public readonly int Start;

            public readonly string SourceString;

            public bool TryUnion(Range other, out Range composite)
            {
                if (!string.Equals(other.SourceString, SourceString, StringComparison.Ordinal))
                {
                    composite = default(Range);
                    return false;
                }

                bool isLow = Start <= other.Start;
                Range low = isLow ? this : other;
                Range high = isLow ? other : this;

                int lowHigh = low.Start + low.Length;

                if (high.Start > lowHigh)
                {
                    composite = default(Range);
                    return false;
                }

                int highHigh = high.Start + high.Length;

                if (highHigh <= lowHigh)
                {
                    composite = low;
                    return true;
                }

                composite = new Range(low.Start, highHigh - low.Start, SourceString);
                return true;
            }

            public override string ToString()
            {
                return SourceString.Substring(Start, Length);
            }
        }
    }

}
