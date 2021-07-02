using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ParksComputing.SetAssociativeCache {
    public abstract class ArrayCacheImplBase<TKey, TValue> : ICachePolicyImpl<TKey, TValue> {

        public ArrayCacheImplBase(int sets, int ways) {
            Sets = sets;
            Ways = ways;
            Capacity = Sets * Ways;
            itemArray = new KeyValuePair<TKey, TValue>[Capacity];
            offsetArray = new int[Capacity];
            Array.Fill(offsetArray, int.MaxValue);
        }

        protected KeyValuePair<TKey, TValue>[] itemArray;
        protected int[] offsetArray;

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

        public int Sets { get; protected set; }
        public int Ways { get; protected set; }
        public int Capacity { get; protected set; }

        public ICollection<TKey> Keys { get; }
        public ICollection<TValue> Values { get; }
        public int Count { get; protected set; }
        public bool IsReadOnly { get; }

        public abstract void Add(TKey key, TValue value);

        public void Add(KeyValuePair<TKey, TValue> item) {
            Add(item.Key, item.Value);
        }

        public void Clear() {
            throw new NotImplementedException();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) {
            throw new NotImplementedException();
        }

        protected int FindSet(TKey key) {
            return key.GetHashCode() % Sets;
        }

        protected int FindOffsetIndex(TKey key) {
            var offsetIndexBegin = FindSet(key);
            var offsetIndex = offsetIndexBegin;

            for (int i = 0; i < Ways; ++i) {
                var itemIndex = offsetArray[offsetIndex];

                if (itemIndex == int.MaxValue) {
                    return offsetIndex;
                }

                if (itemArray[itemIndex].Key.Equals(key)) {
                    return offsetIndex;
                }

                ++offsetIndex;
            }

            return int.MaxValue;
        }

        protected int FindItemIndex(TKey key) {
            var offsetIndexBegin = FindOffsetIndex(key);
            var offsetIndex = offsetIndexBegin;

            for (int i = 0; i < Ways; ++i) {
                var itemIndex = offsetArray[offsetIndex];

                if (itemIndex == int.MaxValue) {
                    // itemIndex is same as offsetIndex when the set is not full
                    // offsetArray[offsetIndex] = offsetIndex;
                    return offsetIndex; 
                }

                ++offsetIndex;
            }

            return int.MaxValue;
        }

        protected int FindAndTouchItemIndex(TKey key) {
            var itemIndex = FindItemIndex(key);

            if (itemIndex != int.MaxValue) {
                var offset = itemIndex % Ways;
                var setUsageIndexBegin = itemIndex - offset;
                var setUsageIndex = setUsageIndexBegin;

                for (int i = 0; i < Ways; ++i) {
                    if (offsetArray[setUsageIndex] == itemIndex) {
                        var touchIndex = offsetArray[setUsageIndex];
                        System.Array.Copy(offsetArray, setUsageIndexBegin, offsetArray, setUsageIndexBegin + 1, Ways - 1);
                        offsetArray[setUsageIndexBegin] = touchIndex;
                        break;
                    }

                    ++setUsageIndex;
                }

            }
            else {
                var offsetIndexBegin = FindSet(key);
                offsetArray[offsetIndexBegin] = itemIndex;
            }

            return itemIndex;
        }

        protected int RotateSetOffsets(int set, int offset) {
            int retVal = int.MaxValue;
            int setStart = set * Ways;
            int headItem = offsetArray[setStart + offset];

            System.Array.Copy(offsetArray, setStart, offsetArray, setStart + 1, offset);
            offsetArray[setStart] = headItem;

            return retVal;
        }

        public bool ContainsKey(TKey key) {
            return FindItemIndex(key) == int.MaxValue;
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) {
            var index = FindAndTouchItemIndex(key);

            if (index != int.MaxValue) {
                var pair = itemArray[index];
                value = pair.Value;
                return true;
            }

            value = default(TValue);
            return false;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            throw new NotImplementedException();
        }

        public bool Remove(TKey key) {
            throw new NotImplementedException();
        }

        public bool Remove(KeyValuePair<TKey, TValue> item) {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            throw new NotImplementedException();
        }
    }
}