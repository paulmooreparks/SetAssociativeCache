using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ParksComputing.SetAssociativeCache {
    /// <summary>
    /// Base class for caches with an eviction policy based on frequency of item usage.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
    /// <typeparam name="TValue">The type of values in the cache.</typeparam>
    public abstract class XfuCache<TKey, TValue> : CacheImplBase<TKey, TValue> {
        /// <summary>
        /// Create a new <c>XfuArrayCache</c> instance.
        /// </summary>
        /// <param name="sets">The number of sets into which the cache is divided.</param>
        /// <param name="ways">The number of storage slots in each set.</param>
        public XfuCache(int sets, int ways) : base(sets, ways) {
        }

        override protected void SetNewItemIndex(int set, int setOffset) {
            int headIndex = set * ways_;
            int keyIndex = headIndex + setOffset;
            var newHeadItem = KeyValuePair.Create(keyArray_[keyIndex].Key, 1);

            /* The new index gets sorted to the front, but with a count of 1. A newly-cached item 
            should not be immediately evicted, so it's safe until pushed down by other new items. */
            System.Array.Copy(keyArray_, headIndex, keyArray_, headIndex + 1, setOffset);
            keyArray_[headIndex] = newHeadItem;
        }

        /// <summary>
        /// Increment the count for the last cache item accessed, then sort the set based on all counts.
        /// </summary>
        /// <param name="set">The set in which the key is stored.</param>
        /// <param name="setOffset">The offset into the set at which the key is stored.</param>
        protected override void PromoteKey(int set, int setOffset) {
            int headIndex = set * ways_;
            int keyIndex = headIndex + setOffset;
            int newHeadItemKey = keyArray_[keyIndex].Key;
            int newHeadItemValue = keyArray_[keyIndex].Value + 1;
            keyArray_[keyIndex] = KeyValuePair.Create(newHeadItemKey, newHeadItemValue);

            Array.Sort(keyArray_, headIndex, ways_, lfuComparer_);
        }

        /// <summary>
        /// Set an item's count to zero (removal from cache, for example), then sort the set based on all counts.
        /// </summary>
        /// <param name="set">The set in which the key is stored.</param>
        /// <param name="setOffset">The offset into the set at which the key is stored.</param>
        protected override void DemoteKey(int set, int setOffset) {
            int headIndex = set * ways_;
            int keyIndex = headIndex + setOffset;
            int newTailItemKey = keyArray_[keyIndex].Key;
            int newTailItemValue = 0;
            keyArray_[keyIndex] = KeyValuePair.Create(newTailItemKey, newTailItemValue);

            Array.Sort(keyArray_, headIndex, ways_, lfuComparer_);
        }

        /* Comparer object used to sort items in indexArray in LFU order. */
        readonly IComparer<KeyValuePair<int, int>> lfuComparer_ = new XfuComparer();

        /// <summary>
        /// Custom comparer used to sort the items in indexArray in LFU order.
        /// </summary>
        internal class XfuComparer : Comparer<KeyValuePair<int, int>> {
            // Compares by Length, Height, and Width.
            public override int Compare(KeyValuePair<int, int> x, KeyValuePair<int, int> y) {
                /* I reversed the sign of < and > because I want a reverse sort */
                if (x.Value < y.Value) {
                    return 1;
                }
                else if (x.Value > y.Value) {
                    return -1;
                }
                else {
                    return 0;
                }
            }
        }

    }
}
