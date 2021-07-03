using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ParksComputing.SetAssociativeCache {
    public abstract class ArrayCacheImplBase<TKey, TValue> : ICachePolicyImpl<TKey, TValue> {

        protected int sets_;
        protected int ways_;

        protected KeyValuePair<TKey, TValue>[] ItemArray { get; set; }
        public int Sets { 
            get => sets_; 
            protected set => sets_ = value; 
        }

        public int Ways { 
            get => ways_; 
            protected set => ways_ = value; 
        }

        public ArrayCacheImplBase(int sets, int ways) {
            Sets = sets;
            Ways = ways;
            ItemArray = new KeyValuePair<TKey, TValue>[Capacity];
        }

        public TValue this[TKey key] {
            get {
                if (TryGetValue(key, out TValue value)) {
                    return value;
                }

                throw new KeyNotFoundException(string.Format("Key '{0}' not found in cache", key));
            }

            set {
                Add(key, value);
            }
        }

        public int Capacity { get => Sets * Ways; }

        public abstract int Count { get; }

        public abstract void Add(TKey key, TValue value);

        public void Add(KeyValuePair<TKey, TValue> item) {
            Add(item.Key, item.Value);
        }

        public abstract bool ContainsKey(TKey key);

        public abstract bool Contains(KeyValuePair<TKey, TValue> item);

        public abstract bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value);

        public abstract bool Remove(TKey key);

        public abstract bool Remove(KeyValuePair<TKey, TValue> item);

        public abstract ICollection<TKey> Keys { get; }
        public abstract ICollection<TValue> Values { get; }

        public abstract void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex);

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            return Array.AsReadOnly<KeyValuePair<TKey, TValue>>(ItemArray).GetEnumerator();
        }

        public bool IsReadOnly { get => false; }

        public abstract void Clear();

        protected int FindSet(TKey key) {
            /* For integer types, GetHashCode() returns the integer, so what we end up with here is 
            a simple MOD operation. A better hashing algorithm is probably a good idea. */
            /* The bitwise OR removes the high bit so that we only get a positive number */
            int hashCode = key.GetHashCode() & 0x7FFFFFFF; 
            return hashCode % sets_;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ItemArray.GetEnumerator();
        }

    }
}