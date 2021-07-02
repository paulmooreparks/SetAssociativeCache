using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ParksComputing.SetAssociativeCache {
    public abstract class ArrayCacheImplBase<TKey, TValue> : ICachePolicyImpl<TKey, TValue> {

        public ArrayCacheImplBase(int sets, int ways) {
            Sets = sets;
            Ways = ways;
            itemArray = new KeyValuePair<TKey, TValue>[Capacity];
            indexArray = new int[Capacity];
            Array.Fill(indexArray, int.MaxValue);
        }

        protected KeyValuePair<TKey, TValue>[] itemArray;
        protected int[] indexArray;
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

        public int Count {
            get {
                int value = 0;

                foreach (int itemIndex in indexArray) {
                    if (itemIndex != int.MaxValue) {
                        ++value;
                    }
                }

                return value;
            }
        }

        public abstract void Add(TKey key, TValue value);

        public void Add(KeyValuePair<TKey, TValue> item) {
            Add(item.Key, item.Value);
        }

        public bool ContainsKey(TKey key) {
            var set = FindSet(key);
            var setBegin = set * Ways;
            
            for (int setOffset = 0, offsetIndex = setBegin; setOffset < Ways; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray[offsetIndex];

                if (itemIndex != int.MaxValue && 
                    itemArray[itemIndex].Key.Equals(key)) {
                    RotateSet(set, setOffset);
                    return true;
                }
            }

            return false;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) {
            var set = FindSet(item.Key);
            var setBegin = set * Ways;

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < Ways; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray[offsetIndex];

                if (itemIndex != int.MaxValue && 
                    itemArray[itemIndex].Key.Equals(item.Key) &&
                    itemArray[itemIndex].Value.Equals(item.Value)) {
                    RotateSet(set, setOffset);
                    return true;
                }
            }

            return false;
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) {
            var set = FindSet(key);
            var setBegin = set * Ways;

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < Ways; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray[offsetIndex];

                if (itemIndex != int.MaxValue && itemArray[itemIndex].Key.Equals(key)) {
                    RotateSet(set, setOffset);
                    value = itemArray[itemIndex].Value;
                    return true;
                }
            }

            value = default(TValue);
            return false;
        }

        public bool Remove(TKey key) {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<TKey, TValue> item) {
            throw new NotImplementedException();
        }

        public void Clear() {
            throw new NotImplementedException();
        }

        public ICollection<TKey> Keys { get; }
        public ICollection<TValue> Values { get; }
        public bool IsReadOnly { get; }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            throw new NotImplementedException();
        }

        protected int FindSet(TKey key) {
            return key.GetHashCode() % Sets;
        }

        protected int RotateSet(int set, int offset) {
            int retVal = int.MaxValue;
            int setStart = set * Ways;
            int headItem = indexArray[setStart + offset];

            System.Array.Copy(indexArray, setStart, indexArray, setStart + 1, offset);
            indexArray[setStart] = headItem;

            return retVal;
        }

        protected void Add(TKey key, TValue value, int set, int setOffset, int itemIndex) {
            itemArray[itemIndex] = KeyValuePair.Create(key, value);
            RotateSet(set, setOffset);
        }

    }
}