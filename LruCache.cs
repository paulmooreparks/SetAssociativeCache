namespace ParksComputing.SetAssociativeCache {
    /// <summary>
    /// Cache with least-recently-used (LRU) eviction policy.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class LruCache<TKey, TValue> : XruCache<TKey, TValue> {
        /// <summary>
        /// Create a new <c>LruArrayCache</c> instance.
        /// </summary>
        /// <param name="sets">The number of sets into which the cache is divided.</param>
        /// <param name="ways">The number of storage slots in each set.</param>
        public LruCache(int sets, int ways) : base(sets, ways) {
        }

        /// <summary>
        /// The offset into the set for the item which should be evicted from the cache.
        /// </summary>
        protected override int ReplacementOffset => ways_ - 1; // LRU is at the highest index in the set
    }
}
