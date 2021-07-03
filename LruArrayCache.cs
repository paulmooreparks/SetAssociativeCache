using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParksComputing.SetAssociativeCache {
    public class LruArrayCache : ICachePolicy {
        ICachePolicyImpl<TKey, TValue> ICachePolicy.MakeInstance<TKey, TValue>(int sets, int ways) {
            return new LruArrayCacheImpl<TKey, TValue>(sets, ways);
        }
    }

    class LruArrayCacheImpl<TKey, TValue> : ArrayCacheImplBase<TKey, TValue> {
        /* Array of indices into ItemArray, split into sets, where each set is sorted from MRU to LRU. */
        protected int[] indexArray;

        public LruArrayCacheImpl(int sets, int ways) : base(sets, ways) {
            Clear();
        }

        override public void Add(TKey key, TValue value) {
            var set = FindSet(key);
            var setBegin = set * Ways;
            int setOffset;
            int offsetIndex;
            int itemIndex;

            for (setOffset = 0, offsetIndex = setBegin; setOffset < Ways; ++setOffset, ++offsetIndex) {
                itemIndex = indexArray[offsetIndex];

                if (itemIndex == int.MaxValue) {
                    itemIndex = offsetIndex;
                    indexArray[offsetIndex] = itemIndex;
                    Add(key, value, set, setOffset, itemIndex);
                    return;
                }

                if (ItemArray[itemIndex].Key.Equals(key)) {
                    itemIndex = indexArray[offsetIndex];
                    Add(key, value, set, setOffset, itemIndex);
                    return;
                }
            }

            /* If we get here, then the set is full. We'll replace the last item in the set, which is the 
            least-recently used, then rotate it to the front. */
            setOffset = Ways - 1;
            offsetIndex = setBegin + setOffset;
            itemIndex = indexArray[offsetIndex];
            Add(key, value, set, setOffset, itemIndex);
            return;
        }

        public override int Count {
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

        public override bool ContainsKey(TKey key) {
            var set = FindSet(key);
            var setBegin = set * Ways;

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < Ways; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray[offsetIndex];

                if (itemIndex != int.MaxValue &&
                    ItemArray[itemIndex].Key.Equals(key)) {
                    PromoteKey(set, setOffset);
                    return true;
                }
            }

            return false;
        }

        public override bool Contains(KeyValuePair<TKey, TValue> item) {
            var set = FindSet(item.Key);
            var setBegin = set * Ways;

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < Ways; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray[offsetIndex];

                if (itemIndex != int.MaxValue &&
                    ItemArray[itemIndex].Key.Equals(item.Key) &&
                    ItemArray[itemIndex].Value.Equals(item.Value)) {
                    PromoteKey(set, setOffset);
                    return true;
                }
            }

            return false;
        }

        public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) {
            var set = FindSet(key);
            var setBegin = set * Ways;

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < Ways; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray[offsetIndex];

                if (itemIndex != int.MaxValue && ItemArray[itemIndex].Key.Equals(key)) {
                    PromoteKey(set, setOffset);
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
                int itemIndex = indexArray[offsetIndex];

                if (itemIndex != int.MaxValue && ItemArray[itemIndex].Key.Equals(key)) {
                    /* Since all access to the cache values goes through the index array first, 
                    I'll try leaving the value in the cache and see how that goes. It's faster, 
                    but for some reason it make me nervous. I suppose I could make value replacement 
                    a feature of the policy class. */
                    indexArray[offsetIndex] = int.MaxValue;
                    DemoteKey(set, setOffset);
                    return true;
                }
            }

            return false;
        }

        public override bool Remove(KeyValuePair<TKey, TValue> item) {
            var set = FindSet(item.Key);
            var setBegin = set * Ways;

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < Ways; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray[offsetIndex];

                if (itemIndex != int.MaxValue &&
                    ItemArray[itemIndex].Key.Equals(item.Key) &&
                    ItemArray[itemIndex].Value.Equals(item.Value)) {
                    /* Since all access to the cache values goes through the index array first, 
                    I'll try leaving the value in the cache and see how that goes. It's faster, 
                    but for some reason it make me nervous. I suppose I could make value replacement 
                    a feature of the policy class. */
                    indexArray[offsetIndex] = int.MaxValue;
                    DemoteKey(set, setOffset);
                    return true;
                }
            }

            return false;
        }

        public override ICollection<TKey> Keys {
            get {
                List<TKey> value = new();

                foreach (int itemIndex in indexArray) {
                    if (itemIndex != int.MaxValue) {
                        value.Add(ItemArray[itemIndex].Key);
                    }
                }

                return value;
            }
        }
        public override ICollection<TValue> Values {
            get {
                List<TValue> value = new();

                foreach (int itemIndex in indexArray) {
                    if (itemIndex != int.MaxValue) {
                        value.Add(ItemArray[itemIndex].Value);
                    }
                }

                return value;
            }
        }

        public override void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
            foreach (int itemIndex in indexArray) {
                if (itemIndex != int.MaxValue) {
                    array[arrayIndex] = ItemArray[itemIndex];
                    ++arrayIndex;
                }
            }
        }

        public override void Clear() {
            /* Keep in mind that the data aren't cleared. We are clearing the indices which point 
            to the data. With no indices, the data aren't accessible. */
            indexArray = new int[Capacity];
            Array.Fill(indexArray, int.MaxValue);
        }


        protected void Add(TKey key, TValue value, int set, int setOffset, int itemIndex) {
            ItemArray[itemIndex] = KeyValuePair.Create(key, value);
            PromoteKey(set, setOffset);
        }

        /* Move the key in the given set at the given offset to the front of the set. */
        protected void PromoteKey(int set, int setOffset) {
            int headIndex = set * Ways;
            int keyIndex = headIndex + setOffset;
            int newHeadItem = indexArray[keyIndex];

            System.Array.Copy(indexArray, headIndex, indexArray, headIndex + 1, setOffset);
            indexArray[headIndex] = newHeadItem;
        }

        /* Move the key in the given set at the given offset to the end of the set. */
        protected void DemoteKey(int set, int setOffset) {
            int headIndex = set * Ways;
            int keyIndex = headIndex + setOffset;
            int tailIndex = headIndex + Ways - 1;
            int count = Ways - setOffset - 1;
            int newTailItem = indexArray[keyIndex];

            System.Array.Copy(indexArray, keyIndex + 1, indexArray, keyIndex, count);
            indexArray[tailIndex] = newTailItem;
        }

    }
}
