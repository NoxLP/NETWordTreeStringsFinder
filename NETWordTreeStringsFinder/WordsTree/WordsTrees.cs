using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETWordTreeStringsFinder.WordsTree
{
    public sealed class WordsTrees
    {
        public WordsTrees(IEnumerable<string> words)
        {
            Dictionary<char, List<string>> firstLetters = new Dictionary<char, List<string>>();
            bool allWordsEmpty = true;
            foreach (var word in words)
            {
                if (string.IsNullOrEmpty(word))
                    continue;

                if (allWordsEmpty)
                    allWordsEmpty = false;

                firstLetters.AddOrUpdateList(word[0], new List<string>() { word });
            }

            int currentKey;
            string currentKeyS;
            WordsTreeChild parent = null;
            foreach (var word in words)
            {
                currentKeyS = word;
                currentKey = -1;
                parent = null;

                foreach (var c in word)
                {
                    currentKey++;
                    currentKeyS += $"{currentKey}{c}";
                    if (currentKey == 0)
                    {
                        if (!_RootChildren.ContainsKey(c))
                            _RootChildren.Add(c, new WordsTreeChildRoot(firstLetters[c], currentKeyS, null, c));
                        parent = _RootChildren[c];
                    }
                    else
                    {
                        if(parent.Children == null || !parent.Children.ContainsKey(c))
                        {
                            var child = new WordsTreeChild(currentKeyS, parent, c);
                            parent.AddChild(child);
                            parent = child;
                        }
                        else
                        {
                            parent = parent.Children[c];
                        }
                    }
                }
            }
        }

        private Dictionary<char, WordsTreeChildRoot> _RootChildren = new Dictionary<char, WordsTreeChildRoot>();

        public WordsTreeChild CurrentChild { get; private set; }
        public int Level { get; private set; }
        public bool ReachedEndOfBranch { get => CurrentChild != null && CurrentChild.IsEndOfBranch; }

        public async Task<bool> NextCharIsAsync(char c)
        {
            if (CurrentChild != null && CurrentChild.IsEndOfBranch)
                return false;

            if (Level == 0)
            {
                if (!_RootChildren.ContainsKey(c))
                    return false;

                Level++;
                CurrentChild = _RootChildren[c];
                return true;
            }
            
            if (!CurrentChild.HasChildsWith(c))
                return false;

            Level++;
            CurrentChild = CurrentChild.Children[c];
            return true;
        }
        public bool NextCharIs(char c)
        {
            if (CurrentChild != null && CurrentChild.IsEndOfBranch)
                return false;

            if (Level == 0)
            {
                if (!_RootChildren.ContainsKey(c))
                    return false;

                Level++;
                CurrentChild = _RootChildren[c];
                return true;
            }

            if (!CurrentChild.HasChildsWith(c))
                return false;

            Level++;
            CurrentChild = CurrentChild.Children[c];
            return true;
        }
        public void Reset()
        {
            CurrentChild = null;
            Level = 0;
        }
    }
}
