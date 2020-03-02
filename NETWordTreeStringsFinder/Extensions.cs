using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace NETWordTreeStringsFinder
{
    //https://stackoverflow.com/questions/13167934/how-to-async-files-readalllines-and-await-for-results
    public static class FileExtensions
    {
        /// <summary>
        /// This is the same default buffer size as
        /// <see cref="StreamReader"/> and <see cref="FileStream"/>.
        /// </summary>
        private const int DefaultBufferSize = 4096;

        /// <summary>
        /// Indicates that
        /// 1. The file is to be used for asynchronous reading.
        /// 2. The file is to be accessed sequentially from beginning to end.
        /// </summary>
        private const FileOptions DefaultOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;

        public static Task<List<string>> ReadAllLinesAsync(string path)
        {
            return ReadAllLinesAsync(path, Encoding.UTF8);
        }

        public static async Task<List<string>> ReadAllLinesAsync(string path, Encoding encoding)
        {
            var lines = new List<string>();

            // Open the FileStream with the same FileMode, FileAccess
            // and FileShare as a call to File.OpenText would've done.
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, DefaultBufferSize, DefaultOptions))
            using (var reader = new StreamReader(stream, encoding))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    lines.Add(line);
                }
            }

            return lines;
        }
    }

    public static class IDictionaryExtensions
    {
        public static void AddOrUpdateList<T, R>(this IDictionary<T, IList<R>> source, T key, R value)
        {
            if (!source.ContainsKey(key))
                source.Add(key, (new List<R>() { value }) as IList<R>);
            else
                source[key].Add(value);
        }
        public static void AddOrUpdateList<T, R>(this IDictionary<T, List<R>> source, T key, List<R> value)
        {
            if (!source.ContainsKey(key))
                source.Add(key, value);
            else
                source[key].AddRange(value);
        }
        public static void MergeInSource<T, R>(this IDictionary<T, List<R>> source, IDictionary<T, List<R>> other)
        {
            foreach (var otherKVP in other)
            {
                source.AddOrUpdateList(otherKVP.Key, otherKVP.Value);
            }
        }
        public static void MergeInSource<T, R>(this IDictionary<T, List<R>> source, IEnumerable<KeyValuePair<T, List<R>>> other)
        {
            foreach (var otherKVP in other)
            {
                source.AddOrUpdateList(otherKVP.Key, otherKVP.Value);
            }
        }
        public static Dictionary<T, List<R>> JoinDictionaries<T, R>(this IEnumerable<IDictionary<T, List<R>>> source)
        {
            var result = new Dictionary<T, List<R>>();

            foreach (var dict in source)
                foreach (var otherKVP in dict)
                {
                    result.AddOrUpdateList(otherKVP.Key, otherKVP.Value);
                }

            return result;
        }
    }
}