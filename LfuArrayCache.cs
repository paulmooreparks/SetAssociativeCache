using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParksComputing.SetAssociativeCache {
    public class LfuArrayCache : ICachePolicy {
        ICachePolicyImpl<TKey, TValue> ICachePolicy.MakeInstance<TKey, TValue> (int capacity, int sets) {
            return new LfuArrayCacheImpl<TKey, TValue>(capacity, sets);
        }
    }

    class LfuArrayCacheImpl<TKey, TValue> : ArrayCacheImplBase<TKey, TValue> {

        public LfuArrayCacheImpl(int sets, int ways) : base(sets, ways) {
            indexArray = new KeyValuePair<int, int>[Capacity];
            Array.Fill(indexArray, KeyValuePair.Create(int.MaxValue, 0));
        }

        /* Key is index into ItemArray; Value is usage count. */
        protected KeyValuePair<int, int>[] indexArray;

        public class LruComparer : Comparer<KeyValuePair<int,int>> {
            // Compares by Length, Height, and Width.
            public override int Compare(KeyValuePair<int, int> x, KeyValuePair<int, int> y) {
                if (x.Value > y.Value) {
                    return 1;
                }
                else if (x.Value < y.Value) {
                    return -1;
                }
                else {
                    return 0;
                }
            }
        }

        IComparer<KeyValuePair<int, int>> lruComparer = new LruComparer();

        override public void Add(TKey key, TValue value) {
            var set = FindSet(key);
            var setBegin = set * Ways;
            int setOffset;
            int offsetIndex;
            int itemIndex;

            for (setOffset = 0, offsetIndex = setBegin; setOffset < Ways; ++setOffset, ++offsetIndex) {
                itemIndex = indexArray[offsetIndex].Key;

                if (itemIndex == int.MaxValue) {
                    itemIndex = offsetIndex;
                    indexArray[offsetIndex] = KeyValuePair.Create(itemIndex, indexArray[offsetIndex].Value);
                    Add(key, value, set, setOffset, itemIndex);
                    return;
                }

                if (ItemArray[itemIndex].Key.Equals(key)) {
                    itemIndex = indexArray[offsetIndex].Key;
                    Add(key, value, set, setOffset, itemIndex);
                    return;
                }
            }

            /* If we get here, then the set is full. We'll evict the last item in the set, which is the 
            least-recently used, then rotate it to the front. */
            setOffset = Ways - 1;
            offsetIndex = setBegin + setOffset;
            itemIndex = indexArray[offsetIndex].Key;
            Add(key, value, set, setOffset, itemIndex);
            return;
        }

        public override int Count {
            get {
                int value = 0;

                foreach (KeyValuePair<int, int> itemIndex in indexArray) {
                    if (itemIndex.Key != int.MaxValue) {
                        ++value;
                    }
                }

                return value;
            }
        }

        public override bool ContainsKey(TKey key) {
            var set = FindSet(key);
            var setBegin = set * Ways;

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < Ways; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray[offsetIndex].Key;

                if (itemIndex != int.MaxValue &&
                    ItemArray[itemIndex].Key.Equals(key)) {
                    UpdateSet(set, setOffset);
                    return true;
                }
            }

            return false;
        }

        public override bool Contains(KeyValuePair<TKey, TValue> item) {
            var set = FindSet(item.Key);
            var setBegin = set * Ways;

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < Ways; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray[offsetIndex].Key;

                if (itemIndex != int.MaxValue &&
                    ItemArray[itemIndex].Key.Equals(item.Key) &&
                    ItemArray[itemIndex].Value.Equals(item.Value)) {
                    UpdateSet(set, setOffset);
                    return true;
                }
            }

            return false;
        }

        public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) {
            var set = FindSet(key);
            var setBegin = set * Ways;

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < Ways; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray[offsetIndex].Key;

                if (itemIndex != int.MaxValue && ItemArray[itemIndex].Key.Equals(key)) {
                    UpdateSet(set, setOffset);
                    value = ItemArray[itemIndex].Value;
                    return true;
                }
            }

            value = default(TValue);
            return false;
        }

        public override bool Remove(TKey key) {
            var set = FindSet(key);
            var setBegin = set * Ways;

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < Ways; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray[offsetIndex].Key;

                if (itemIndex != int.MaxValue && ItemArray[itemIndex].Key.Equals(key)) {
                    /* Since all access to the cache values goes through the index array first, 
                    I'll try leaving the value in the cache and see how that goes. It's faster, 
                    but for some reason it make me nervous. I suppose I could make value replacement 
                    a feature of the policy class. */
                    indexArray[offsetIndex] = KeyValuePair.Create(itemIndex, indexArray[offsetIndex].Value);
                    UpdateSet(set, setOffset);
                    return true;
                }
            }

            return false;
        }

        public override bool Remove(KeyValuePair<TKey, TValue> item) {
            var set = FindSet(item.Key);
            var setBegin = set * Ways;

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < Ways; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray[offsetIndex].Key;

                if (itemIndex != int.MaxValue &&
                    ItemArray[itemIndex].Key.Equals(item.Key) &&
                    ItemArray[itemIndex].Value.Equals(item.Value)) {
                    /* Since all access to the cache values goes through the index array first, 
                    I'll try leaving the value in the cache and see how that goes. It's faster, 
                    but for some reason it make me nervous. I suppose I could make value replacement 
                    a feature of the policy class. */
                    indexArray[offsetIndex] = KeyValuePair.Create(itemIndex, indexArray[offsetIndex].Value);
                    UpdateSet(set, setOffset);
                    return true;
                }
            }

            return false;
        }

        public override ICollection<TKey> Keys {
            get {
                List<TKey> value = new();

                foreach (KeyValuePair<int, int> itemIndex in indexArray) {
                    if (itemIndex.Key != int.MaxValue) {
                        value.Add(ItemArray[itemIndex.Key].Key);
                    }
                }

                return value;
            }
        }
        public override ICollection<TValue> Values {
            get {
                List<TValue> value = new();

                foreach (KeyValuePair<int, int> itemIndex in indexArray) {
                    if (itemIndex.Key != int.MaxValue) {
                        value.Add(ItemArray[itemIndex.Key].Value);
                    }
                }

                return value;
            }
        }

        public override void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
            foreach (KeyValuePair<int,int> itemIndex in indexArray) {
                if (itemIndex.Key != int.MaxValue) {
                    array[arrayIndex] = ItemArray[itemIndex.Key];
                    ++arrayIndex;
                }
            }
        }

        public override IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            throw new NotImplementedException();
        }

        public override bool IsReadOnly { get => false; }

        public override void Clear() {
            throw new NotImplementedException();
        }

        protected void UpdateSet(int set, int offset) {
            int setStart = set * Ways;
            Array.Sort(indexArray, setStart, Ways, lruComparer);
        }

        protected void Add(TKey key, TValue value, int set, int setOffset, int itemIndex) {
            ItemArray[itemIndex] = KeyValuePair.Create(key, value);
            UpdateSet(set, setOffset);
        }

    }
}
