using System.Collections.Generic;

namespace ParksComputing.SetAssociativeCache {
    /// <summary>
    /// Base class for caches with an eviction policy based on recency of item usage.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
    /// <typeparam name="TValue">The type of values in the cache.</typeparam>
    /// <remarks>
    /// XruCache works by moving the index for the most recently access cached item to the lowest 
    /// index in the relevant set in the pointer array. Recall that the pointer array stores 
    /// key/value pairs; the Key is the index of the associated cache item in the value array, and 
    /// the Value may be used by the cache policy. This cache policy does not use the value 
    /// element. (Cache items in the value array DO NOT move around, only the elements in the 
    /// pointer array do that.)
    /// </remarks>
    public abstract class XruCache<TKey, TValue> : CacheImplBase<TKey, TValue> {
        /// <summary>
        /// Create a new <c>XfuCache</c> instance.
        /// </summary>
        /// <param name="sets">The number of sets into which the cache is divided.</param>
        /// <param name="ways">The number of storage slots in each set.</param>
        public XruCache(int sets, int ways) : base(sets, ways) {
        }

        /// <summary>
        /// Update and adjust the pointer array as necessary according to the details of the cache policy.
        /// </summary>
        /// <param name="set">Which set to update.</param>
        /// <param name="pointerIndex">The index into the pointer set.</param>
        override protected void UpdateSet(int set, int pointerIndex) {
            PromoteKey(set, pointerIndex);
        }

        /// <summary>
        /// Mark an item as having the highest rank in the set.
        /// </summary>
        /// <param name="set">The set in which the key is stored.</param>
        /// <param name="pointerIndex">The index into the key array.</param>
        protected override void PromoteKey(int set, int pointerIndex) {
            int count = pointerIndex;
            int newHeadItemKey = pointerArray_[set][pointerIndex].Key;
            int newHeadItemValue = pointerArray_[set][pointerIndex].Value;

            if (count > 0) {
                /* Move the key to the lowest index in the set. */
                System.Array.Copy(pointerArray_[set], 0, pointerArray_[set], 1, count);
                pointerArray_[set][0] = new KeyValuePair<int, int>(newHeadItemKey, newHeadItemValue);
            }
        }

        /// <summary>
        /// Mark an item as having the lowest rank in the set.
        /// </summary>
        /// <param name="set">The set in which the key is stored.</param>
        /// <param name="pointerIndex">The index into the key array.</param>
        protected override void DemoteKey(int set, int pointerIndex) {
            int tailIndex = ways_ - 1;
            int count = ways_ - pointerIndex - 1;
            int newTailItemKey = pointerArray_[set][pointerIndex].Key;
            int newTailItemValue = pointerArray_[set][pointerIndex].Value;

            if (count > 0 && pointerIndex < tailIndex) {
                /* Move the key to the highest index in the set. */
                System.Array.Copy(pointerArray_[set], pointerIndex + 1, pointerArray_[set], pointerIndex, count);
                pointerArray_[set][tailIndex] = new KeyValuePair<int, int>(newTailItemKey, newTailItemValue);
            }
        }
    }
}
