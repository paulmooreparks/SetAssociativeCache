using System;
using System.Collections.Generic;

namespace ParksComputing.SetAssociativeCache {
    /// <summary>
    /// Base class for caches with an eviction policy based on frequency of item usage.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
    /// <typeparam name="TValue">The type of values in the cache.</typeparam>
    /// <remarks>
    /// XfuCache works by tracking accesses to each cache item via the key array. Recall that 
    /// the key array stores key/value pairs; the key is the index of the associate cache item 
    /// in the value array, and the value may be used by the cache policy. This cache policy 
    /// and its derivatives interprets the value element as the count of accesses for each 
    /// cache item. As the counts are updated, the set is sorted such that lower counts are 
    /// stored at lower indices in the key array and higher counts are stored at higher indices. 
    /// (Cache items in the value array DO NOT move around, only the pointer elements in the 
    /// key array do that.)
    /// </remarks>
    public abstract class XfuCache<TKey, TValue> : CacheImplBase<TKey, TValue> {
        /// <summary>
        /// Create a new <c>XfuArrayCache</c> instance.
        /// </summary>
        /// <param name="sets">The number of sets into which the cache is divided.</param>
        /// <param name="ways">The number of storage slots in each set.</param>
        public XfuCache(int sets, int ways) : base(sets, ways) {
        }

        /// <summary>
        /// Update the key array with the index into the value array and adjust the key array as 
        /// necessary according to the details of the cache policy.
        /// </summary>
        /// <param name="set">Which set to update.</param>
        /// <param name="pointerIndex">The index into the key array.</param>
        override protected void UpdateSet(int set, int pointerIndex) {
            //int headIndex = set * ways_;
            //int len = ways_ - 1;
            //int tailIndex = headIndex + ways_ - 1;
            //var newTailItem = new KeyValuePair<int, int>(pointerArray_[pointerIndex].Key, 1);
            //System.Array.Copy(pointerArray_, headIndex + 1, pointerArray_, headIndex, len);
            //pointerArray_[tailIndex] = newTailItem;
            PromoteKey(set, pointerIndex);
        }

        /// <summary>
        /// Increment the count for the last cache item accessed, then sort the set based on all counts.
        /// </summary>
        /// <param name="set">The set in which the key is stored.</param>
        /// <param name="pointerIndex">The index into the key array.</param>
        protected override void PromoteKey(int set, int pointerIndex) {
            int headIndex = set * ways_;
            int newHeadItemKey = pointerArray_[pointerIndex].Key;
            int newHeadItemValue = pointerArray_[pointerIndex].Value;

            /* Increment the frequency count, checking for overflow. */
            try {
                newHeadItemValue = checked(newHeadItemValue + 1);
            }
            catch (OverflowException) {
                /* If the item has been in the cache long enough for the counter to wrap around, 
                it's probably time to evict it. Regardless, we'll just set it back to one. */
                newHeadItemValue = 1;
            }

            pointerArray_[pointerIndex] = new KeyValuePair<int, int>(newHeadItemKey, newHeadItemValue);
            Array.Sort(pointerArray_, headIndex, ways_, lfuComparer_);
        }

        /// <summary>
        /// Set an item's count to zero (removal from cache, for example), then sort the set based on all counts.
        /// </summary>
        /// <param name="set">The set in which the key is stored.</param>
        /// <param name="pointerIndex">The index into the key array.</param>
        protected override void DemoteKey(int set, int pointerIndex) {
            int headIndex = set * ways_;
            int newTailItemKey = pointerArray_[pointerIndex].Key;
            int newTailItemValue = 0;
            pointerArray_[pointerIndex] = new KeyValuePair<int, int>(newTailItemKey, newTailItemValue);
            Array.Sort(pointerArray_, headIndex, ways_, lfuComparer_);
        }

        /* Comparer object used to sort items in indexArray in LFU order. */
        readonly IComparer<KeyValuePair<int, int>> lfuComparer_ = new XfuComparer();

        /// <summary>
        /// Custom comparer used to sort the items in indexArray in LFU order.
        /// </summary>
        internal class XfuComparer : Comparer<KeyValuePair<int, int>> {
            // Compares by Length, Height, and Width.
            public override int Compare(KeyValuePair<int, int> x, KeyValuePair<int, int> y) {
                /* Reverse sort */
                if (x.Value < y.Value) {
                    return 1;
                }

                if (x.Value > y.Value) {
                    return -1;
                }

                return 0;
            }
        }

    }
}
