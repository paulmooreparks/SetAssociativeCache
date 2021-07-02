﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ParksComputing.SetAssociativeCache {
    public abstract class ArrayCacheImplBase<TKey, TValue> : ICachePolicyImpl<TKey, TValue> {

        public ArrayCacheImplBase(int sets, int ways) {
            Sets = sets;
            Ways = ways;
            ItemArray = new KeyValuePair<TKey, TValue>[Capacity];
        }

        protected KeyValuePair<TKey, TValue>[] ItemArray { get; set; }
        public int Sets { get; protected set; }
        public int Ways { get; protected set; }

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

        public abstract IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator();

        public abstract bool IsReadOnly { get; }

        public abstract void Clear();

        protected int FindSet(TKey key) {
            return key.GetHashCode() % Sets;
        }

        IEnumerator IEnumerable.GetEnumerator() {
            throw new NotImplementedException();
        }

    }
}