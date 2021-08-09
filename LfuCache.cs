namespace ParksComputing.SetAssociativeCache {
    /// <summary>
    /// Cache with least-frequently-used (LFU) eviction policy.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class LfuCache<TKey, TValue> : XfuCache<TKey, TValue> {
        /// <summary>
        /// Create a new <c>LfuCache</c> instance.
        /// </summary>
        /// <param name="sets">The number of sets into which the cache is divided.</param>
        /// <param name="ways">The number of storage slots in each set.</param>
        public LfuCache(int sets, int ways) : base(sets, ways) {
        }

        /// <summary>
        /// Gets the index into the pointer array for the item which should be evicted from the set.
        /// </summary>
        protected override int GetEvictionPointerIndex(int set) {
            return ways_ - 1; // LFU is at the highest index in the set
        }
    }
}
