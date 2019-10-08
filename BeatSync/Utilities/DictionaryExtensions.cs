using System.Collections.Generic;
using System.Linq;

namespace BeatSync.Utilities
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Merges two dictionaries. If overwrite is true, the target dictionary's value will be overwritten.
        /// From https://stackoverflow.com/a/57490396
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <param name="overwrite"></param>
        public static void Merge<K, V>(this IDictionary<K, V> target, IEnumerable<KeyValuePair<K, V>> source, bool overwrite = false)
        {
            source.ToList().ForEach(_ => {
                if ((!target.ContainsKey(_.Key)) || overwrite)
                    target[_.Key] = _.Value;
            });
        }

        //public static void Merge<K, V>(this IDictionary<K, V> target, IReadOnlyDictionary<K, V> source, bool overwrite = false)
        //{
        //    source.ToList().ForEach(_ => {
        //        if ((!target.ContainsKey(_.Key)) || overwrite)
        //            target[_.Key] = _.Value;
        //    });
        //}
    }
}
