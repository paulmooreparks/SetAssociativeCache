using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ParksComputing.SetAssociativeCache {
    public class SetAssociativeCache<TKey, TValue, TPolicy> : ISetAssociativeCache<TKey, TValue> where TPolicy : ICachePolicy, new() {

        public SetAssociativeCache(int sets, int ways) {
            if (sets == 0) {
                throw new ArgumentOutOfRangeException("sets", "Value of sets must be greater than zero.");
            }

            if (ways == 0) {
                throw new ArgumentOutOfRangeException("capacity", "Value of ways must be greater than zero.");
            }

            impl = policy.MakeInstance<TKey, TValue>(sets, ways);
        }

        ICachePolicyImpl<TKey, TValue> impl;
        TPolicy policy = new();

        public TValue this[TKey key] {
            get => impl[key];
            set => impl[key] = value;
        }

        public int Ways => impl.Ways;

        public int Sets => impl.Sets;

        public int Capacity => impl.Capacity;

        public ICollection<TKey> Keys => impl.Keys;
        public ICollection<TValue> Values => impl.Values;
        public int Count => impl.Count;
        public bool IsReadOnly => impl.IsReadOnly;

        public void Add(TKey key, TValue value) {
            impl.Add(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item) {
            impl.Add(item);
        }

        public void Clear() {
            impl.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) {
            return impl.Contains(item);
        }

        public bool ContainsKey(TKey key) {
            return impl.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
            impl.CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            return impl.GetEnumerator();
        }

        public bool Remove(TKey key) {
            return impl.Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item) {
            return impl.Remove(item);
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) {
            return impl.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return impl.GetEnumerator();
        }
    }
}
