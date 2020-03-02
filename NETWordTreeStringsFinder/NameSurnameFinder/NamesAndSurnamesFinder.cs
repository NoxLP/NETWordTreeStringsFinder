using NETWordTreeStringsFinder.StringFinderBase;
using NETWordTreeStringsFinder.WordsTree;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NETWordTreeStringsFinder
{
    public sealed class NamesAndSurnamesFinder : aStringFinderBase
    {
        #region fields
        private static List<string> _Names;
        private static List<string> _Surnames;
        private static List<string> _Complements;
        private static WordsTrees _NamesTrees;
        private static WordsTrees _SurnamesTrees;
        private static WordsTrees _ComplementsTrees;
        private static string _Separators = @"./ -_,;";

        private static SemaphoreSlim _Semaphore = new SemaphoreSlim(1);
        private static Dictionary<string, List<string>> _SpecialWordsLists = new Dictionary<string, List<string>>();
        private static Dictionary<string, WordsTrees> _SpecialWordsTrees = new Dictionary<string, WordsTrees>();
        #endregion

        #region properties
        public ReadOnlyCollection<string> Names { get => _Names.AsReadOnly(); }
        public ReadOnlyCollection<string> Surnames { get => _Surnames.AsReadOnly(); }
        public ReadOnlyCollection<string> Complements { get => _Complements.AsReadOnly(); }
        public string Separators { get => _Separators; }
        #endregion

        #region init
        private static async Task ReadDictionariesAsync(FileInfo namesDictionary, FileInfo surnamesDictionary, FileInfo complementsDictionary)
        {
            List<string>[] data = null;
            try
            {
                var readData = new Task<List<string>>[]
                {
                    FileExtensions.ReadAllLinesAsync(namesDictionary.FullName),
                    FileExtensions.ReadAllLinesAsync(surnamesDictionary.FullName),
                    FileExtensions.ReadAllLinesAsync(complementsDictionary.FullName)
                };

                data = await Task.WhenAll(readData);
            }
            catch (Exception e)
            {
                LastErrorMsg = e.Message;
                throw;
            }

            if (data != null)
            {
                _Names = new List<string>(data[0]);
                _Surnames = new List<string>(data[1]);
                _Complements = new List<string>(data[2]);
            }
        }
        public static async Task InitAsync(FileInfo namesDictionary, FileInfo surnamesDictionary, FileInfo complementsDictionary)
        {
            await _Semaphore.WaitAsync();
            await ReadDictionariesAsync(namesDictionary, surnamesDictionary, complementsDictionary);
            var tasks = new Task[3]
            {
                Task.Run(() => _NamesTrees = new WordsTrees(_Names)),
                Task.Run(() => _SurnamesTrees = new WordsTrees(_Surnames)),
                Task.Run(() => _ComplementsTrees = new WordsTrees(_Complements))
            };

            await Task.WhenAll(tasks);
            SetLastException(null);
            _Semaphore.Release();
        }
        public static async Task InitAsync(
            FileInfo namesDictionary, 
            FileInfo surnamesDictionary, 
            FileInfo complementsDictionary, 
            params char[] separators)
        {
            await _Semaphore.WaitAsync();
            await ReadDictionariesAsync(namesDictionary, surnamesDictionary, complementsDictionary);
            var tasks = new Task[3]
            {
                Task.Run(() => _NamesTrees = new WordsTrees(_Names)),
                Task.Run(() => _SurnamesTrees = new WordsTrees(_Surnames)),
                Task.Run(() => _ComplementsTrees = new WordsTrees(_Complements))
            };
            _Separators = separators.ToString();

            await Task.WhenAll(tasks);
            SetLastException(null);
            _Semaphore.Release();
        }
        public static async Task<bool> NewSpecialWordsTreeAsync(string key, List<string> words)
        {
            if (_SpecialWordsLists.ContainsKey(key) || _SpecialWordsTrees.ContainsKey(key))
            {
                SetLastException(new ArgumentException("Se intentó guardar un WordsTree especial con una key que ya existía"));
                return false;
            }

            try
            {
                await _Semaphore.WaitAsync();
                _SpecialWordsLists.Add(key, words);
                _SpecialWordsTrees.Add(key, new WordsTrees(words));
                _Semaphore.Release();
            }
            catch(Exception e)
            {
                SetLastException(e);
                throw e;
            }

            return true;
        }
        public static async Task<bool> UpdateSpecialWordsTreeAsync(string key, List<string> words)
        {
            if (!_SpecialWordsLists.ContainsKey(key) || !_SpecialWordsTrees.ContainsKey(key))
            {
                SetLastException(new KeyNotFoundException("Se intentó actualizar un WordsTree especial con una key que NO existía"));
                return false;
            }

            try
            {
                await _Semaphore.WaitAsync();
                _SpecialWordsLists[key] = words;
                _SpecialWordsTrees[key] = new WordsTrees(words);
                _Semaphore.Release();
            }
            catch(Exception e)
            {
                SetLastException(e);
                throw e;
            }

            return true;
        }
        public static bool SpecialWordsTreesContains(string key)
        {
            return _SpecialWordsLists.ContainsKey(key) || _SpecialWordsTrees.ContainsKey(key);
        }
        public static async Task<bool> RemoveSpecialWordsTreeAsync(string key)
        {
            await _Semaphore.WaitAsync();
            if (_SpecialWordsLists.ContainsKey(key))
            {
                _SpecialWordsLists.Remove(key);

                if (_SpecialWordsTrees.ContainsKey(key))
                    _SpecialWordsTrees.Remove(key);

                _Semaphore.Release();
                return true;
            }
            else if (_SpecialWordsTrees.ContainsKey(key))
            {
                _SpecialWordsTrees.Remove(key);
                _Semaphore.Release();
                return true;
            }

            SetLastException(new ArgumentException("Se intentó borrar un WordTree especial inexistente"));
            _Semaphore.Release();
            return false;
        }
        #endregion


        public async Task<List<StringFinderMatch>> GetMatchesInSpecialWordsTreeAsync(string target, string specialWordsTreeKey, Predicate<StringFinderMatch> conditions)
        {
            if (!string.IsNullOrEmpty(LastErrorMsg) || 
                !_SpecialWordsLists.ContainsKey(specialWordsTreeKey) ||
                !_SpecialWordsTrees.ContainsKey(specialWordsTreeKey))
                throw new ArgumentException($"Error in static data initialization: {LastErrorMsg}");

            target = target.ToLower() + " ";
            var wordTree = _SpecialWordsTrees[specialWordsTreeKey];
            List<char> wordChars = new List<char>();
            List<char> compChars = new List<char>();
            List<StringFinderMatch> matches = new List<StringFinderMatch>();
            StringFinderCompMatch? lastComp = null;
            int index = 0;
            int wordFirstIndex = -1;
            int compFirstIndex = -1;
            bool compFound = false;
            bool compFoundThisIteration = false;
            foreach (var c in target)
            {
                if (_ComplementsTrees.NextCharIs(c))
                {
                    compFoundThisIteration = true;
                    if (!compFound)
                        compFound = true;

                    if (compFirstIndex == -1)
                        compFirstIndex = index;
                    compChars.Add(c);
                }

                if (wordTree.NextCharIs(c))
                {
                    if (wordFirstIndex == -1)
                        wordFirstIndex = index;
                    wordChars.Add(c);
                }
                else if (wordChars.Count > 0)
                {
                    if (compFound && compFirstIndex < wordFirstIndex)
                    {
                        string comp = string.Join("", compChars);
                        if ((_ComplementsTrees.ReachedEndOfBranch || _Complements.Contains(comp)) &&
                            (compFirstIndex + comp.Length) < wordFirstIndex)
                        {
                            int prevIndex = compFirstIndex == 0 ? 0 : compFirstIndex - 1;
                            char? prevSep = _Separators.Contains(target[prevIndex]) ? (char?)target[prevIndex] : null;
                            char? follSep = (target.Length > index && _Separators.Contains(target[index - 1])) ?
                                (char?)target[compFirstIndex - 1] :
                                null;

                            lastComp = new StringFinderCompMatch(
                                comp,
                                await wordTree.CurrentChild.GetPossibleWordsAsync(),
                                compFirstIndex,
                                prevSep,
                                follSep);
                        }

                        compFound = false;
                    }

                    var smatch = string.Join("", wordChars);
                    var match = new StringFinderMatch(
                        smatch,
                        target,
                        await wordTree.CurrentChild.GetPossibleWordsAsync(),
                        wordTree.ReachedEndOfBranch ? MatchTypes.Total : MatchTypes.Partial,
                        wordFirstIndex,
                        lastComp,
                        _SpecialWordsLists[specialWordsTreeKey].Contains(smatch));

                    if (conditions == null || conditions(match))
                    {
                        matches.Add(match);
                        if (lastComp.HasValue)
                            lastComp = null;
                    }
                    wordFirstIndex = -1;
                    wordTree.Reset();
                    wordChars.Clear();
                }

                if (compFound)
                {
                    string comp = string.Join("", compChars);
                    if (_ComplementsTrees.ReachedEndOfBranch &&
                        (wordChars.Count == 0 || (compFirstIndex + comp.Length) < wordFirstIndex))
                    {
                        int prevIndex = compFirstIndex == 0 ? 0 : compFirstIndex - 1;
                        char? prevSep = _Separators.Contains(target[prevIndex]) ? (char?)target[prevIndex] : null;
                        char? follSep = (target.Length > index && _Separators.Contains(target[index - 1])) ?
                            (char?)target[compFirstIndex - 1] :
                            null;

                        lastComp = new StringFinderCompMatch(
                            comp,
                            await wordTree.CurrentChild.GetPossibleWordsAsync(),
                            compFirstIndex,
                            prevSep,
                            follSep);
                    }
                    else if (!compFoundThisIteration)
                    {
                        compFound = false;
                        lastComp = null;
                        compFirstIndex = -1;
                    }
                }
                index++;
                compFoundThisIteration = false;
            }

            if (matches.Count == 0)
                return null;

            return matches;
        }
        public async Task<List<StringFinderMatch>> GetMatchesInAsync(string target, NamesSurnamesEnum type, Predicate<StringFinderMatch> conditions)
        {
            if (!string.IsNullOrEmpty(LastErrorMsg) || !await GetIfStaticDataIsOKAsync())
                throw new ArgumentException($"Error in static data initialization: {LastErrorMsg}");

            target = target.ToLower() + " ";
            var wordTree = type == NamesSurnamesEnum.Name ? _NamesTrees : _SurnamesTrees;
            List<char> wordChars = new List<char>();
            List<char> compChars = new List<char>();
            List<StringFinderMatch> matches = new List<StringFinderMatch>();
            StringFinderCompMatch? lastComp = null;
            int index = 0;
            int wordFirstIndex = -1;
            int compFirstIndex = -1;
            bool compFound = false;
            bool compFoundThisIteration = false;
            foreach (var c in target)
            {
                if (_ComplementsTrees.NextCharIs(c))
                {
                    compFoundThisIteration = true;
                    if (!compFound)
                        compFound = true;

                    if (compFirstIndex == -1)
                        compFirstIndex = index;
                    compChars.Add(c);
                }

                if (wordTree.NextCharIs(c))
                {
                    if (wordFirstIndex == -1)
                        wordFirstIndex = index;
                    wordChars.Add(c);
                }
                else if (wordChars.Count > 0)
                {
                    if (compFound && compFirstIndex < wordFirstIndex)
                    {
                        string comp = string.Join("", compChars);
                        if ((_ComplementsTrees.ReachedEndOfBranch || _Complements.Contains(comp)) &&
                            (compFirstIndex + comp.Length) < wordFirstIndex)
                        {
                            int prevIndex = compFirstIndex == 0 ? 0 : compFirstIndex - 1;
                            char? prevSep = _Separators.Contains(target[prevIndex]) ? (char?)target[prevIndex] : null;
                            char? follSep = (target.Length > index && _Separators.Contains(target[index - 1])) ?
                                (char?)target[compFirstIndex - 1] :
                                null;

                            lastComp = new StringFinderCompMatch(
                                comp,
                                await wordTree.CurrentChild.GetPossibleWordsAsync(),
                                compFirstIndex,
                                prevSep,
                                follSep);
                        }

                        compFound = false;
                    }

                    var smatch = string.Join("", wordChars);
                    var match = new StringFinderMatch(
                        smatch,
                        target,
                        await wordTree.CurrentChild.GetPossibleWordsAsync(),
                        wordTree.ReachedEndOfBranch ? MatchTypes.Total : MatchTypes.Partial,
                        wordFirstIndex,
                        lastComp,
                        type == NamesSurnamesEnum.Name ? _Names.Contains(smatch) : _Surnames.Contains(smatch));

                    if (conditions == null || conditions(match))
                    {
                        matches.Add(match);
                        if (lastComp.HasValue)
                            lastComp = null;
                    }
                    wordFirstIndex = -1;
                    wordTree.Reset();
                    wordChars.Clear();
                }

                if (compFound)
                {
                    string comp = string.Join("", compChars);
                    if (_ComplementsTrees.ReachedEndOfBranch &&
                        (wordChars.Count == 0 || (compFirstIndex + comp.Length) < wordFirstIndex))
                    {
                        int prevIndex = compFirstIndex == 0 ? 0 : compFirstIndex - 1;
                        char? prevSep = _Separators.Contains(target[prevIndex]) ? (char?)target[prevIndex] : null;
                        char? follSep = (target.Length > index && _Separators.Contains(target[index - 1])) ?
                            (char?)target[compFirstIndex - 1] :
                            null;

                        lastComp = new StringFinderCompMatch(
                            comp,
                            await wordTree.CurrentChild.GetPossibleWordsAsync(),
                            compFirstIndex,
                            prevSep,
                            follSep);
                    }
                    else if (!compFoundThisIteration)
                    {
                        compFound = false;
                        lastComp = null;
                        compFirstIndex = -1;
                    }
                }
                index++;
                compFoundThisIteration = false;
            }

            if (matches.Count == 0)
                return null;

            return matches;
        }

        protected async override Task<bool> GetIfStaticDataIsOKAsync()
        {
            return _NamesTrees != null && _SurnamesTrees != null;
        }
    }
}