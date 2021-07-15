using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ParksComputing.SetAssociativeCache {
    /// <summary>
    /// Base class for caches with an eviction policy based on recency of item usage.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
    /// <typeparam name="TValue">The type of values in the cache.</typeparam>
    /// <remarks>
    /// XruCache works by moving the key for the most recently access cached item to the lowest 
    /// index in the relevant set in the key array. Recall that the key array stores key/value 
    /// pairs; the key is the index of the associate cache item in the value array, and the value 
    /// may be used by the cache policy. This cache policy does not use the value element. 
    /// (Cache items in the value array DO NOT move around, only the pointer elements in the key 
    /// array do that.)
    /// </remarks>
    public abstract class XruCache<TKey, TValue> : CacheImplBase<TKey, TValue> {
        /// <summary>
        /// Create a new <c>XfuArrayCache</c> instance.
        /// </summary>
        /// <param name="sets">The number of sets into which the cache is divided.</param>
        /// <param name="ways">The number of storage slots in each set.</param>
        public XruCache(int sets, int ways) : base(sets, ways) {
        }

        /// <summary>
        /// Update the key array with the index into the value array and adjust the key array as 
        /// necessary according to the details of the cache policy.
        /// </summary>
        /// <param name="set">Which set to update.</param>
        /// <param name="pointerIndex">The offset into the set to update.</param>
        override protected void UpdateSet(int set, int pointerIndex) {
            PromoteKey(set, pointerIndex);
        }

        /// <summary>
        /// Move the key in the given set at the given offset to the front of the set. 
        /// </summary>
        /// <param name="set">The set in which the key is stored.</param>
        /// <param name="pointerIndex">The index into the key array.</param>
        protected override void PromoteKey(int set, int pointerIndex) {
            int headIndex = set * ways_;
            int setOffset = pointerIndex % ways_;
            int newHeadItemKey = pointerArray_[pointerIndex].Key;
            int newHeadItemValue = pointerArray_[pointerIndex].Value;
            /* Move the key to the lowest index in the set. */
            System.Array.Copy(pointerArray_, headIndex, pointerArray_, headIndex + 1, setOffset);
            pointerArray_[headIndex] = new KeyValuePair<int, int>(newHeadItemKey, newHeadItemValue);
        }

        /// <summary>
        /// Move the key in the given set at the given offset to the end of the set. 
        /// </summary>
        /// <param name="set">The set in which the key is stored.</param>
        /// <param name="pointerIndex">The index into the key array.</param>
        protected override void DemoteKey(int set, int pointerIndex) {
            int headIndex = set * ways_;
            int setOffset = pointerIndex % ways_;
            int tailIndex = headIndex + ways_ - 1;
            int count = ways_ - setOffset - 1;
            int newTailItemKey = pointerArray_[pointerIndex].Key;
            int newTailItemValue = pointerArray_[pointerIndex].Value;
            /* Move the key to the highest index in the set. */
            System.Array.Copy(pointerArray_, pointerIndex + 1, pointerArray_, pointerIndex, count);
            pointerArray_[tailIndex] = new KeyValuePair<int, int>(newTailItemKey, newTailItemValue);
        }
    }
}
