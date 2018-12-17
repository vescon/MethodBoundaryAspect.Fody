using System;
using System.Collections.Generic;

namespace MethodBoundaryAspect.Fody.Ordering
{
    public static class DictionaryExtensions
    {
        /// <summary>
        /// Tries to get the value to the given key. If it's found, the value is returned. If it's not found the setIfNotExist is called with the key as parameter.
        /// 
        /// </summary>
        /// <param name="dictionary">The dictionary in which the key is searched.</param><param name="key">The key to which its value is searched.</param><param name="setIfNotExist">The function which is called, if the key doesn't exist in the dictionary.</param>
        public static TValue TryGetOrSetValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key,
            Func<TKey, TValue> setIfNotExist)
        {
            TValue obj;
            if (!dictionary.TryGetValue(key, out obj))
                dictionary[key] = obj = setIfNotExist(key);
            return obj;
        }
    }
}