using System;
using System.Collections.Generic;

namespace ParksComputing.SetAssociativeCache {
    /// <summary>
    /// Cache with time-aware least-recently-used (TLRU) eviction policy.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class TlruCache<TKey, TValue> : XruCache<TKey, TValue, DateTime> {
        /// <summary>
        /// Create a new <c>TlruCache</c> instance.
        /// </summary>
        /// <param name="sets">The number of sets into which the cache is divided.</param>
        /// <param name="ways">The number of storage slots in each set.</param>
        public TlruCache(int sets, int ways) : base(sets, ways) {
        }

        protected long defaultTtu_ = 300; // in seconds. Just a made-up number for now.

        /// <summary>
        /// Default time-to-use in seconds, after which the cache item will expire.
        /// </summary>
        public long DefaultTTU {
            get { return defaultTtu_; }
            set { defaultTtu_ = value; }
        }

        /// <summary>
        /// Gets the expiration time for element with the specified key.
        /// </summary>
        /// <param name="key">The key of the element to get.</param>
        /// <returns>The expiration date and time for the requested element.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">The <paramref name="key"/> is not found.</exception>
        public DateTime GetExpirationTime(TKey key) {
            return GetMetaData(key);
        }

        /// <summary>
        /// Sets the expiration time for the element with the specified key.
        /// </summary>
        /// <param name="key">The key of the element to modify.</param>
        /// <param name="expTime">The expiration time to set for the specified key.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">The <paramref name="key"/> is not found.</exception>
        public void SetExpirationTime(TKey key, DateTime expTime) {
            SetMetaData(key, expTime);
        }

        /// <summary>
        /// Sets the timeout, in seconds, for the element with the specified key.
        /// </summary>
        /// <param name="key">The key of the element to get.</param>
        /// <param name="expTime">The timeout, in seconds, to set for the specified key.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">The <paramref name="key"/> is not found.</exception>
        public void SetTimeout(TKey key, long ttuSeconds) {
            DateTime expTime = DateTime.UtcNow.AddSeconds(ttuSeconds);
            SetExpirationTime(key, expTime);
        }

        /// <summary>
        /// Sets the timeout for the element with the specified key.
        /// </summary>
        /// <param name="key">The key of the element to get.</param>
        /// <param name="timeSpan">The timeout to set for the specified key.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">The <paramref name="key"/> is not found.</exception>
        public void SetTimeout(TKey key, TimeSpan timeSpan) {
            DateTime expTime = DateTime.UtcNow + timeSpan;
            SetExpirationTime(key, expTime);
        }

        /// <summary>
        /// Gets the index into the pointer array for the item which should be evicted from the set.
        /// </summary>
        protected override int GetEvictionPointerIndex(int set) {
            return ways_ - 1;
        }

        /// <summary>
        /// Try to add a cache item at the specified set and pointer index.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <param name="meta">DateTime object specifying when the cache item will expire.</param>
        /// <param name="set">The set in which to add the element.</param>
        /// <param name="pointerIndex">The index into the pointer set at which to add the element's value index.</param>
        /// <param name="valueIndex">The index into the item set at which the element is stored.</param>
        /// <param name="OnKeyExists">The delegate to call when the <paramref name="key"/> 
        /// is already present.</param>
        /// <returns><c>true</c> if cache item added; <c>false</c> otherwise.</returns>
        protected override bool TryAddAtIndex(TKey key, TValue value, DateTime meta, int set, int pointerIndex, int valueIndex, Action<TKey, TValue, DateTime, int, int, int> OnKeyExists) {
            bool result = base.TryAddAtIndex(key, value, meta, set, pointerIndex, valueIndex, OnKeyExists);

            if (!result && IsExpired(set, pointerIndex)) {
                ReplaceItem(key, value, meta, set, pointerIndex, valueIndex);
                PromoteKey(set, pointerIndex);
                return true;
            }

            return result;
        }

        /// <summary>
        /// Determines if an item that has been retrieved from the cache is now invalid (expired, 
        /// etc.). If so, removes it from the cache and returns <c>true</c> to indicate that the 
        /// item has been filtered.
        /// </summary>
        /// <param name="key">The key of the element to test.</param>
        /// <param name="value">The value of the element to test.</param>
        /// <param name="set">The set in which the element exists.</param>
        /// <param name="pointerIndex">The index into the pointer set at which the element's value index is kept.</param>
        /// <param name="valueIndex">The index into the item set at which the element is stored.</param>
        protected override bool FilterGetValue(TKey key, TValue value, int set, int pointerIndex, int valueIndex) {
            bool result = base.FilterGetValue(key, value, set, pointerIndex, valueIndex);

            if (!result && IsExpired(set, pointerIndex)) {
                Remove(key);
                return true;
            }

            return result;
        }

        /// <summary>
        /// Determine if the item in the given <paramref name="set"/> at the given 
        /// <paramref name="pointerIndex"/> can be evicted.
        /// </summary>
        /// <param name="set">The set in which the item to test exists.</param>
        /// <param name="pointerIndex">The index into the <paramref name="set"/> array for the 
        /// item being tested.</param>
        /// <returns><c>true</c> if the item at <paramref name="pointerIndex"/> in <paramref name="set"/> 
        /// can be replaced by the addition of an item with the given <paramref name="key"/>; 
        /// <c>false </c> otherwise.</returns>
        protected override bool IsItemEvictable(int set, int pointerIndex) {
            bool result = base.IsItemEvictable(set, pointerIndex);

            if (!result && IsExpired(set, pointerIndex)) {
                return true;
            }

            return result;
        }

        /// <summary>
        /// Gets or sets the element with the specified key.
        /// </summary>
        /// <param name="key">The key of the element to get or set.</param>
        /// <returns>The element with the specified key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">The property is retrieved and key is not found.</exception>
        public override TValue this[TKey key] { 
            get => base[key];
            set {
                DateTime expTime = DateTime.UtcNow.AddSeconds(defaultTtu_);
                AddOrUpdate(key, value, expTime, (key, value, meta, set, pointerIndex, valueIndex) => {
                    ReplaceItem(key, value, expTime, set, pointerIndex, valueIndex);
                    PromoteKey(set, pointerIndex);
                });
            }
        }

        /// <summary>
        /// Adds an element with the provided <paramref name="key"/> and <paramref name="value"/> 
        /// to the ParksComputing.ISetAssociativeCache.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <exception cref="System.ArgumentException">An element with the same <paramref name="key"/> 
        /// already exists in the cache.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        public override void Add(TKey key, TValue value) {
            DateTime expTime = DateTime.UtcNow.AddSeconds(defaultTtu_);
            base.Add(key, value, expTime);
        }

        /// <summary>
        /// Determine if the cache item in the specified set at the specified pointer index has 
        /// expired, according to the associated expiration time.
        /// </summary>
        /// <param name="set">The set in which the <paramref name="key"/> would be added.</param>
        /// <param name="pointerIndex">The index into the <paramref name="set"/> array for the 
        /// item being tested.</param>
        /// <returns><c>true</c> if the item has expired; <c>false</c> otherwise.</returns>
        protected virtual bool IsExpired(int set, int pointerIndex) {
            var expTime = pointerArray_[set][pointerIndex].Value;

            /* If expiration time is in the past... */
            return DateTime.Compare(expTime, DateTime.UtcNow) < 0;
        }
    }
}
