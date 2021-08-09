﻿using System;
using System.Collections.Generic;

namespace ParksComputing.SetAssociativeCache {
    /// <summary>
    /// Base class for caches with an eviction policy based on frequency of item usage.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
    /// <typeparam name="TValue">The type of values in the cache.</typeparam>
    /// <remarks>
    /// XfuCache works by tracking accesses to each cache item via the pointer array. Recall that 
    /// the pointer array stores key/value pairs; the Key is the index of the associated cache 
    /// item in the value array, and the Value may be used by the cache policy. This cache policy 
    /// and its derivatives interprets the value element as the count of accesses for each cache 
    /// item. As the counts are updated, the set is sorted such that lower counts are stored at 
    /// lower indices in the pointer array and higher counts are stored at higher indices. (Cache 
    /// items in the value array DO NOT move around, only the elements in the pointer array do that.)
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
            PromoteKey(set, pointerIndex);
        }

        /// <summary>
        /// Mark an item as having the highest rank in the set.
        /// </summary>
        /// <param name="set">The set in which the key is stored.</param>
        /// <param name="pointerIndex">The index into the key array.</param>
        protected override void PromoteKey(int set, int pointerIndex) {
            int newKey = pointerArray_[set][pointerIndex].Key;
            long newValue = pointerArray_[set][pointerIndex].Value;

            /* Increment the frequency count, checking for overflow. */
            try {
                newValue = checked(newValue + 1);
            }
            catch (OverflowException) {
                /* If the item has been in the cache long enough for the counter to wrap around, 
                it's probably time to evict it. Regardless, we'll just set it back to one. */
                newValue = 1;
            }

            pointerArray_[set][pointerIndex] = new KeyValuePair<int, long>(newKey, newValue);
            Array.Sort(pointerArray_[set], 0, ways_, lfuComparer_);
        }

        /// <summary>
        /// Mark an item as having the lowest rank in the set.
        /// </summary>
        /// <param name="set">The set in which the key is stored.</param>
        /// <param name="pointerIndex">The index into the key array.</param>
        protected override void DemoteKey(int set, int pointerIndex) {
            int newKey = pointerArray_[set][pointerIndex].Key;
            long newValue = 0;
            pointerArray_[set][pointerIndex] = new KeyValuePair<int, long>(newKey, newValue);
            Array.Sort(pointerArray_[set], 0, ways_, lfuComparer_);
        }

        /* Comparer object used to sort items in indexArray in LFU order. */
        readonly IComparer<KeyValuePair<int, long>> lfuComparer_ = new XfuComparer();

        /// <summary>
        /// Custom comparer used to sort the items in indexArray in LFU order.
        /// </summary>
        internal class XfuComparer : Comparer<KeyValuePair<int, long>> {
            // Compares by Length, Height, and Width.
            public override int Compare(KeyValuePair<int, long> x, KeyValuePair<int, long> y) {
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
