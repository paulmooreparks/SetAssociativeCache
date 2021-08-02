using System.Collections.Generic;

namespace ParksComputing.SetAssociativeCache {

    /// <summary>
    /// Represents a generic set-associative cache of key/value pairs.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
    /// <typeparam name="TValue">The type of values in the cache.</typeparam>
    public interface ISetAssociativeCache<TKey, TValue> : IDictionary<TKey, TValue> {
        /// <summary>
        /// Gets the capacity of the cache
        /// </summary>
        /// <value>
        /// The number of elements which may be stored in the cache.
        /// </value>
        int Capacity { get; }

        /// <summary>
        /// Gets the number of sets in the cache
        /// </summary>
        /// <value>
        /// The number of sets in the cache
        /// </value>
        int Sets { get; }

        /// <summary>
        /// Gets the capacity in each set
        /// </summary>
        /// <value>
        /// The number of elements which may be stored in a set.
        /// </value>
        int Ways { get; }

        /// <summary>
        /// If the given <paramref name="key"/> would cause an existing key to be evicted, return <c>true</c> and set 
        /// <paramref name="evictKey"/> to the key of the item that would be evicted if the new <paramref name="key"/> 
        /// were added.
        /// </summary>
        /// <param name="key">Key to test.</param>
        /// <param name="evictKey">Key of cache item that would be evicted, or default key value if return is false.</param>
        /// <returns><c>true</c> if a key would be evicted; <c>false</c> otherwise.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        bool TryGetEvictKey(TKey key, out TKey evictKey);
    }
}
