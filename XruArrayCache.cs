using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ParksComputing.SetAssociativeCache {
    /// <summary>
    /// Base class for caches with an eviction policy based on recency of item usage.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
    /// <typeparam name="TValue">The type of values in the cache.</typeparam>
    public abstract class XruArrayCache<TKey, TValue> : ArrayCacheImplBase<TKey, TValue> {
        /// <summary>
        /// Create a new <c>XfuArrayCache</c> instance.
        /// </summary>
        /// <param name="sets">The number of sets into which the cache is divided.</param>
        /// <param name="ways">The number of storage slots in each set.</param>
        public XruArrayCache(int sets, int ways) : base(sets, ways) {
        }

        override protected void SetNewItemIndex(int set, int setOffset) {
            PromoteKey(set, setOffset);
        }

        /// <summary>
        /// Move the key in the given set at the given offset to the front of the set. 
        /// </summary>
        /// <param name="set">The set in which the key is stored.</param>
        /// <param name="setOffset">The offset into the set at which the key is stored.</param>
        protected override void PromoteKey(int set, int setOffset) {
            int headIndex = set * ways_;
            int keyIndex = headIndex + setOffset;
            int newHeadItemKey = indexArray_[keyIndex].Key;
            int newHeadItemValue = indexArray_[keyIndex].Value;

            System.Array.Copy(indexArray_, headIndex, indexArray_, headIndex + 1, setOffset);
            indexArray_[headIndex] = KeyValuePair.Create(newHeadItemKey, newHeadItemValue);
        }

        /// <summary>
        /// Move the key in the given set at the given offset to the end of the set. 
        /// </summary>
        /// <param name="set">The set in which the key is stored.</param>
        /// <param name="setOffset">The offset into the set at which the key is stored.</param>
        protected override void DemoteKey(int set, int setOffset) {
            int headIndex = set * ways_;
            int keyIndex = headIndex + setOffset;
            int tailIndex = headIndex + ways_ - 1;
            int count = ways_ - setOffset - 1;
            int newTailItemKey = indexArray_[keyIndex].Key;
            int newTailItemValue = indexArray_[keyIndex].Value;

            System.Array.Copy(indexArray_, keyIndex + 1, indexArray_, keyIndex, count);
            indexArray_[tailIndex] = KeyValuePair.Create(newTailItemKey, newTailItemValue);
        }
    }
}
