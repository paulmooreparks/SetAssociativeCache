namespace ParksComputing.SetAssociativeCache {
    /// <summary>
    /// Cache with least-recently-used (LRU) eviction policy.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class MruCache<TKey, TValue> : XruCache<TKey, TValue> {
        /// <summary>
        /// Create a new <c>MruArrayCache</c> instance.
        /// </summary>
        /// <param name="sets">The number of sets into which the cache is divided.</param>
        /// <param name="ways">The number of storage slots in each set.</param>
        public MruCache(int sets, int ways) : base(sets, ways) {
        }

        /// <summary>
        /// The offset into the set for the item which should be evicted from the cache.
        /// </summary>
        protected override int ReplacementOffset => 0; // MRU is at the lowest index in the set
    }
}
