using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace NETWordTreeStringsFinder.WordsTree
{
    public class WordsTreeChildRoot : WordsTreeChild
    {
        public WordsTreeChildRoot(IEnumerable<string> possibleWords, string key, WordsTreeChild prev, char ckey)
            : base(key,prev, ckey)
        {
            _PossibleWords = possibleWords.ToArray();
        }

        internal string[] _PossibleWords;
    }
}
