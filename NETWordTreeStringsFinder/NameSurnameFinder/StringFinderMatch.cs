using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETWordTreeStringsFinder
{
    public struct StringFinderMatch : IEquatable<StringFinderMatch>
    {
        public StringFinderMatch(string match, string originalTarget, string[] possible, MatchTypes type, int indexOfFirstCharInSourceString, StringFinderCompMatch? previousComplemento, bool wordContainedInDictionary) : this()
        {
            Match = match;
            OriginalTarget = originalTarget;
            _PossibleWords = possible;
            Type = type;
            IndexOfFirstCharInSourceString = indexOfFirstCharInSourceString;
            PreviousComplemento = previousComplemento;
            WordContainedInDictionary = wordContainedInDictionary;
        }

        private string[] _PossibleWords;

        public string Match { get; private set; }
        public string OriginalTarget { get; private set; }
        public ReadOnlyCollection<string> PossibleWords { get => Array.AsReadOnly(_PossibleWords); }
        public MatchTypes Type { get; private set; }
        public int IndexOfFirstCharInSourceString { get; private set; }
        public StringFinderCompMatch? PreviousComplemento { get; private set; }
        public bool WordContainedInDictionary { get; private set; }

        public string GetCompleteString()
        {
            string comp = "";
            if (PreviousComplemento.HasValue)
                comp = PreviousComplemento.Value.GetCompleteString();

            return $"{comp}{Match}";
        }

        public bool Equals(StringFinderMatch other)
        {
            return Match.Equals(other.Match) &&
                Type.Equals(other.Type) &&
                IndexOfFirstCharInSourceString == other.IndexOfFirstCharInSourceString &&
                PreviousComplemento.Equals(other.PreviousComplemento) &&
                WordContainedInDictionary == other.WordContainedInDictionary;
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if (obj is StringFinderMatch)
                return base.Equals((StringFinderMatch)obj);
            return false;
        }
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();
            unchecked
            {
                hash += (Match.GetHashCode() * 7);
                hash += (Type.GetHashCode() * 11);
                hash += (IndexOfFirstCharInSourceString.GetHashCode() * 17);
                hash += (PreviousComplemento.GetHashCode() * 23);
                hash += (WordContainedInDictionary.GetHashCode() * 31);
            }
            return hash;
        }
    }

    public struct StringFinderCompMatch : IEquatable<StringFinderCompMatch>
    {
        public StringFinderCompMatch(string match, string[] original, int indexOfFirstCharInSourceString, char? previousSeparator, char? followingSeparator) : this()
        {
            Match = match;
            _OriginalWords = original;
            IndexOfFirstCharInSourceString = indexOfFirstCharInSourceString;
            PreviousSeparator = previousSeparator;
            FollowingSeparator = followingSeparator;
        }

        private string[] _OriginalWords;

        public string Match { get; private set; }
        public ReadOnlyCollection<string> OriginalWords { get => Array.AsReadOnly(_OriginalWords); }
        public int IndexOfFirstCharInSourceString { get; private set; }
        public char? PreviousSeparator { get; private set; }
        public char? FollowingSeparator { get; private set; }

        public string GetCompleteString()
        {
            return $"{(PreviousSeparator.HasValue ? PreviousSeparator.Value.ToString() : "")}{Match}{(FollowingSeparator.HasValue ? FollowingSeparator.Value.ToString() : "")}";
        }

        public bool Equals(StringFinderCompMatch other)
        {
            return Match.Equals(other.Match) &&
                IndexOfFirstCharInSourceString == other.IndexOfFirstCharInSourceString &&
                ((!PreviousSeparator.HasValue && !other.PreviousSeparator.HasValue) ||
                    (PreviousSeparator.HasValue && other.PreviousSeparator.HasValue && PreviousSeparator.Value == other.PreviousSeparator.Value)) &&
                ((!FollowingSeparator.HasValue && !other.FollowingSeparator.HasValue) ||
                    (FollowingSeparator.HasValue && other.FollowingSeparator.HasValue && FollowingSeparator.Value == other.FollowingSeparator.Value));
        }
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;
            if(obj is StringFinderCompMatch)
                return base.Equals((StringFinderCompMatch)obj);

            return false;
        }
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();
            unchecked
            {
                hash += (Match.GetHashCode() * 7);
                hash += (IndexOfFirstCharInSourceString.GetHashCode() * 11);
                hash += (PreviousSeparator.GetHashCode() * 17);
                hash += (FollowingSeparator.GetHashCode() * 31);
            }
            return hash;
        }
    }
}
