using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NETWordTreeStringsFinder.WordsTree
{
    public class WordsTreeChild : IEquatable<WordsTreeChild>
    {
        public WordsTreeChild(string key, WordsTreeChild prev, char ckey)
        {
            _Key = key;
            KeyChar = ckey;

            if(prev != null)
            {
                PreviousChild = prev;
            }
        }

        private string _Key;

        public WordsTreeChild PreviousChild { get; private set; }
        public char KeyChar { get; private set; }
        public Dictionary<char, WordsTreeChild> Children { get; private set; }
        public bool IsEndOfBranch { get => Children == null || Children.Count == 0; }

        internal void AddChild(WordsTreeChild child)
        {
            if (Children == null)
                Children = new Dictionary<char, WordsTreeChild>();

            if (Children.ContainsKey(child.KeyChar))
                return;
            else
                Children.Add(child.KeyChar, child);
        }

        internal bool HasChildsWith(char key)
        {
            return Children.ContainsKey(key);
        }

        public WordsTreeChildRoot GetRootChild()
        {
            WordsTreeChild current = this;
            WordsTreeChildRoot root = current as WordsTreeChildRoot;
            while (root == null)
            {
                current = current.PreviousChild;
                root = current as WordsTreeChildRoot;
            }

            return root;
        }
        public async Task<string[]> GetPossibleWordsAsync()
        {
            WordsTreeChild current = this;
            WordsTreeChildRoot root = current as WordsTreeChildRoot;
            List<char> myChars = new List<char>();
            myChars.Add(current.KeyChar);
            while (root == null)
            {
                current = current.PreviousChild;
                myChars.Add(current.KeyChar);
                root = current as WordsTreeChildRoot;
            }

            string myWord = new string(myChars.ToArray());

            return root._PossibleWords.Where(x => x.Contains(myWord)).ToArray();
        }

        public bool Equals(WordsTreeChild other)
        {
            return _Key == other._Key;
        }
        public override bool Equals(object obj)
        {
            var o = obj as WordsTreeChild;
            if (o == null)
                return false;

            return Equals(o);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode() + (_Key.GetHashCode() * 7);
        }
    }
}
