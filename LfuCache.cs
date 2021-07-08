using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ParksComputing.SetAssociativeCache {
    /// <summary>
    /// Cache with least-frequently-used (LFU) eviction policy.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class LfuCache<TKey, TValue> : XfuCache<TKey, TValue> {
        /// <summary>
        /// Create a new <c>LfuArrayCache</c> instance.
        /// </summary>
        /// <param name="sets">The number of sets into which the cache is divided.</param>
        /// <param name="ways">The number of storage slots in each set.</param>
        public LfuCache(int sets, int ways) : base(sets, ways) {
            Clear();
        }

        /// <summary>
        /// The offset into the set for the item which should be evicted from the cache.
        /// </summary>
        protected override int ReplacementOffset => ways_ - 1;
    }
}
