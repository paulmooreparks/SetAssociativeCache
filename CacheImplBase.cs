using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ParksComputing.SetAssociativeCache {
    /// <summary>
    /// Abstract base class for implementations of a generic set-associative cache of key/value pairs.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
    /// <typeparam name="TValue">The type of values in the cache.</typeparam>
    /// <remarks>
    /// THIS CLASS IS NOT THREAD SAFE! Thread-safe access is the reponsibility of the client.
    /// </remarks>
    public abstract class CacheImplBase<TKey, TValue> : ISetAssociativeCache<TKey, TValue>, IReadOnlyDictionary<TKey, TValue> {

        /* By using fields instead of properties, I've found that I can eliminate function 
        alls from the release version of the JITted assembly code, at least on Intel/AMD x64. 
        That actually surprises me. The properties still exist, though, in case external clients 
        need to know the values for some reason. */
        protected int sets_; // Number of sets in the cache
        protected int ways_; // Capacity of each set in the cache
        protected int count_; // Number of items in the cache
        protected int version_; // Increments each time an element is added or removed, to invalidate enumerators.

        /* There are two arrays of key/value pairs which are used to track and store the actual cache items. */

        /* pointerArray_ stores the indices of the actual cache items stored in the value array. Each 
        item in the array is a key/value pair of two integers. The first integer is an index into 
        the value array. The second index is interpreted by the derived class that implements a 
        specific eviction policy. This array is sorted/rearranged/whatever according to the needs 
        of the cache policy, since it's faster to move integer pairs around, and they're close 
        together in the CPU cache. */
        protected KeyValuePair<int, int>[][] pointerArray_;

        /* valueArray_ stores the key/value pairs of the actual cache items. Once an item is placed 
        at an index in this array, it stays there unless it is replaced with a new key/value pair or 
        copied out to the client of the cache. Otherwise, these data aren't rearranged or otherwise 
        messed with. */
        protected KeyValuePair<TKey, TValue>?[][] valueArray_;

        /* This value is used as a sentinel to mark empty slots in the pointer array. */
        protected const int EMPTY_MARKER = int.MinValue;

        /// <summary>
        /// Create a new <c>CacheImplBase</c> instance.
        /// </summary>
        /// <param name="sets">The number of sets into which the cache is divided.</param>
        /// <param name="ways">The number of storage slots in each set.</param>
        public CacheImplBase(int sets, int ways) {
            sets_ = sets;
            ways_ = ways;
            Clear();
        }

        /// <summary>
        /// Removes all items from the System.Collections.Generic.ICollection.
        /// </summary>
        public virtual void Clear() {
            /* Invalidate any outstanding interators. */
            ++version_;
            count_ = 0;

            pointerArray_ = new KeyValuePair<int, int>[sets_][];
            valueArray_ = new KeyValuePair<TKey, TValue>?[sets_][];

            for (int set = 0; set < sets_; set++) {
                pointerArray_[set] = new KeyValuePair<int, int>[ways_];
                Array.Fill(pointerArray_[set], new KeyValuePair<int, int>(EMPTY_MARKER, 0));
                valueArray_[set] = new KeyValuePair<TKey, TValue>?[ways_];
            }

        }

        /// <summary>
        /// Gets or sets the element with the specified key.
        /// </summary>
        /// <param name="key">The key of the element to get or set.</param>
        /// <returns>The element with the specified key.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">The property is retrieved and key is not found.</exception>
        public TValue this[TKey key] {
            get {
                if (TryGetValue(key, out TValue value)) {
                    return value;
                }

                throw new KeyNotFoundException(string.Format("Key '{0}' not found in cache", key));
            }

            set {
                AddOrUpdate(key, value, (key, value, set, pointerIndex, valueIndex) => {
                    /* Decrement the count for the item that we're replacing. */
                    --count_;
                    /* Delegate adding the cache item and managing the data for the cache policy 
                    to the Add method. */
                    Add(key, value, set, pointerIndex, valueIndex);
                });
            }
        }

        /// <summary>
        /// Gets the number of sets in the cache
        /// </summary>
        /// <value>
        /// The number of sets in the cache
        /// </value>
        public int Sets => sets_;

        /// <summary>
        /// Gets the capacity in each set
        /// </summary>
        /// <value>
        /// The number of elements which may be stored in a set.
        /// </value>
        public int Ways => ways_;

        /// <summary>
        /// Gets the capacity of the cache
        /// </summary>
        /// <value>
        /// The number of elements which may be stored in the cache.
        /// </value>
        public int Capacity => sets_ * ways_;

        /// <summary>
        /// Gets the number of elements contained in the System.Collections.Generic.ICollection.
        /// </summary>
        /// <value>
        /// The number of elements contained in the System.Collections.Generic.ICollection.
        /// </value>
        public int Count => count_;

        /// <summary>
        /// Gets the offset into the pointer set for the item which should be evicted from the cache.
        /// </summary>
        /// <remarks>
        /// This is implemented in the derived class that actually defines and implements 
        /// the cache policy.
        /// </remarks>
        protected abstract int EvictionPointerIndex { get; }

        /// <summary>
        /// Adds an element with the provided <paramref name="key"/> and <paramref name="value"/> 
        /// to the ParksComputing.ISetAssociativeCache if the <paramref name="key"/> is not found. 
        /// If the <paramref name="key"/> is found, call the <paramref name="onKeyExists"/> 
        /// delegate to handle the situation.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <param name="onKeyExists">The delegate to call when the <paramref name="key"/> 
        /// is already present.</param>
        /// <remarks>
        /// This function lets us delegate behavior for when a key is already present. If called 
        /// from the index method (this[key]), we want to update an existing key. If called from 
        /// the Add method, we want to throw an <c>ArgumentException</c>.
        /// </remarks>
        /// <exception cref="System.ArgumentNullException">Either <paramref name="key"/> or 
        /// <paramref name="onKeyExists"/> is null.</exception>
        protected void AddOrUpdate(TKey key, TValue value, Action<TKey, TValue, int, int, int> onKeyExists) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            if (onKeyExists == null) {
                throw new ArgumentNullException(nameof(onKeyExists));
            }

            /* Get the number of the set that would contain the new key. */
            var set = FindSet(key);
            int pointerIndex; // Index into the the pointer array where the value index is stored.
            int valueIndex; // Index into the value array where the actual cache item (key/value pair) is stored

            /* Loop over the set */
            for (pointerIndex = 0; pointerIndex < ways_; ++pointerIndex) {
                /* Get the index into the value array */
                valueIndex = pointerArray_[set][pointerIndex].Key;

                /* If the index is a sentinel value for "nothing stored here"... */
                if (valueIndex == EMPTY_MARKER) {
                    /* Find the first empty entry in the value array */
                    valueIndex = 0;

                    while (valueIndex < ways_ && valueArray_[set][valueIndex] != null) {
                        ++valueIndex;
                    }

                    /* Create a new entry in the pointer array. */
                    pointerArray_[set][pointerIndex] = new KeyValuePair<int, int>(valueIndex, pointerArray_[set][pointerIndex].Value);

                    /* Delegate adding the cache item and managing the data for the cache policy 
                    to the Add method. */
                    Add(key, value, set, pointerIndex, valueIndex);
                    return;
                }

                /* If the new key is equal to the key at the current position... */
                if (valueArray_[set][valueIndex].Value.Key.Equals(key)) {
                    valueIndex = pointerArray_[set][pointerIndex].Key;
                    /* Delegate behavior to the onKeyExists delegate. */
                    onKeyExists(key, value, set, pointerIndex, valueIndex);
                    return;
                }
            }

            /* If we get here, then the set is full. Evict the appropriate element depending on 
            policy, then add the new value at that offset. */

            --count_;

            /* The ReplacementOffset property gives us the offset into the set for the key that 
            will be evicted. */
            pointerIndex = EvictionPointerIndex;

            /* Get the index into the value array for where the evicted cache item is stored. */
            valueIndex = pointerArray_[set][pointerIndex].Key;
            /* Delegate adding the cache item and managing the data for the cache policy to the 
            Add method. */
            Add(key, value, set, pointerIndex, valueIndex);
            return;
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
        public virtual void Add(TKey key, TValue value) {
            AddOrUpdate(key, value, (key, value, set, pointerIndex, valueIndex) => {
                throw new ArgumentException($"Item with key already exists. Key: {key}", nameof(key));
            });
        }

        /// <summary>
        /// Adds an item to the cache.
        /// </summary>
        /// <param name="item">The key/value pair to add to the cache.</param>
        /// <exception cref="System.ArgumentException">Key field in <paramref name="item"/> is null.</exception>
        public void Add(KeyValuePair<TKey, TValue> item) {
            if (item.Key == null) {
                throw new ArgumentException($"Key field in item is null", nameof(item));
            }

            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Determines whether the ParksComputing.ISetAssociativeCache contains an element with 
        /// the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The key to locate in the ParksComputing.ISetAssociativeCache.</param>
        /// <returns>
        /// true if the ParksComputing.ISetAssociativeCache contains an element with the 
        /// <paramref name="key"/>; otherwise, false.
        /// </returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        public virtual bool ContainsKey(TKey key) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            var set = FindSet(key);

            for (int pointerIndex = 0; pointerIndex < ways_; ++pointerIndex) {
                int valueIndex = pointerArray_[set][pointerIndex].Key;

                /* If the key is found in the value array... */
                if (valueIndex != EMPTY_MARKER &&
                    valueArray_[set][valueIndex].Value.Key.Equals(key)) {
                    /* "Touch" the key to note that it has been accessed. */
                    PromoteKey(set, pointerIndex);
                    return true;
                }

            };

            return false;
        }

        /// <summary>
        /// Determines whether the System.Collections.Generic.ICollection contains a specific object.
        /// </summary>
        /// <param name="item">The object to locate in the System.Collections.Generic.ICollection.</param>
        /// <returns>true if <paramref name="item"/> is found in the cache; otherwise, false.</returns>
        /// <exception cref="System.ArgumentException">Key field in <paramref name="item"/> is null.</exception>
        public virtual bool Contains(KeyValuePair<TKey, TValue> item) {
            if (item.Key == null) {
                throw new ArgumentException($"Key field in item is null", nameof(item));
            }

            var set = FindSet(item.Key);

            for (int pointerIndex = 0; pointerIndex < ways_; ++pointerIndex) {
                int valueIndex = pointerArray_[set][pointerIndex].Key;

                /* If the key is found in the value array, and the value at that key matches the provided value... */
                if (valueIndex != EMPTY_MARKER &&
                    valueArray_[set][valueIndex].Value.Key.Equals(item.Key) &&
                    valueArray_[set][valueIndex].Value.Value.Equals(item.Value)) {
                    /* "Touch" the key to note that it has been accessed. */
                    PromoteKey(set, pointerIndex);
                    return true;
                }
            };

            return false;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">
        /// When this method returns, the value associated with the specified <paramref name="key"/>, 
        /// if the <paramref name="key"/> is found; otherwise, the default value for the type of the 
        /// <paramref name="value"/> parameter. This parameter is passed uninitialized.
        /// </param>
        /// <returns>
        /// true if the object that implements ParksComputing.ISetAssociativeCache contains 
        /// an element with the specified <paramref name="key"/>; otherwise, false.
        /// </returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            value = default;
            var set = FindSet(key);

            for (int pointerIndex = 0; pointerIndex < ways_; ++pointerIndex) {
                int valueIndex = pointerArray_[set][pointerIndex].Key;

                /* If the key is found in the value array... */
                if (valueIndex != EMPTY_MARKER && valueArray_[set][valueIndex].Value.Key.Equals(key)) {
                    /* "Touch" the key to note that it has been accessed. */
                    PromoteKey(set, pointerIndex);
                    /* Return the value found at this location in the value array. */
                    value = valueArray_[set][valueIndex].Value.Value;
                    return true;
                }
            };

            return false;
        }

        bool IReadOnlyDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value) => TryGetValue(key, out value);

        /// <summary>
        /// Removes the element with the specified <paramref name="key"/> from the cache.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>
        /// true if the element is successfully removed; otherwise, false. This method also returns false if 
        /// <paramref name="key"/> was not found in the cache.
        /// </returns>
        public virtual bool Remove(TKey key) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            var set = FindSet(key);

            for (int pointerIndex = 0; pointerIndex < ways_; ++pointerIndex) {
                int valueIndex = pointerArray_[set][pointerIndex].Key;

                if (valueIndex != EMPTY_MARKER && valueArray_[set][valueIndex].Value.Key.Equals(key)) {
                    RemoveItem(set, pointerIndex, valueIndex);
                    return true;
                }
            };

            return false;
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the cache.
        /// </summary>
        /// <param name="item">The object to remove from the cache.</param>
        /// <returns>
        /// true if <paramref name="item"/> was successfully removed from the cache; otherwise, 
        /// false. This method also returns false if item is not found in the original cache.
        /// </returns>
        /// <exception cref="System.ArgumentException">Key field in <paramref name="item"/> is null.</exception>
        public virtual bool Remove(KeyValuePair<TKey, TValue> item) {
            if (item.Key == null) {
                throw new ArgumentException($"Key field in item is null", nameof(item));
            }

            var set = FindSet(item.Key);

            for (int pointerIndex = 0; pointerIndex < ways_; ++pointerIndex) {
                int valueIndex = pointerArray_[set][pointerIndex].Key;

                if (valueIndex != EMPTY_MARKER &&
                    valueArray_[set][valueIndex].Value.Key.Equals(item.Key) &&
                    valueArray_[set][valueIndex].Value.Value.Equals(item.Value)) {
                    RemoveItem(set, pointerIndex, valueIndex);
                    return true;
                }
            };

            return false;
        }

        /// <summary>
        /// Remove an item from the value array and mark it as empty in the pointer array.
        /// </summary>
        /// <param name="set">The set in which the item is located.</param>
        /// <param name="pointerIndex">The index into the pointer array.</param>
        /// <param name="valueIndex">The index into the value array.</param>
        private void RemoveItem(int set, int pointerIndex, int valueIndex) {
            /* Clear the value from the cache */
            valueArray_[set][valueIndex] = null;

            /* Invalidate any outstanding interators. */
            ++version_;
            /* Mark this location in the pointer array as available. */
            pointerArray_[set][pointerIndex] = new KeyValuePair<int, int>(EMPTY_MARKER, pointerArray_[set][pointerIndex].Value);
            DemoteKey(set, pointerIndex);
            --count_;
        }

        /// <summary>
        /// Walks over each element of a set in the pointer array and calls a delegate for each 
        /// element, passing the set and the pointer-array index corresponding to the current 
        /// element to the delegate. If the delegate returns <c>true</c>, the iteration stops.
        /// </summary>
        /// <param name="set">The set to iterate over.</param>
        /// <param name="func">The delegate to call for each element.</param>
        /// <returns>
        /// <c>true</c> if the delegate returns <c>true</c> at any time; <c>false</c> otherwise.
        /// </returns>
        /// <remarks>
        /// This is a hold-over from an earlier version of the code where iteration was more 
        /// complicated. I kept this here in case it might be useful in the future, but with 
        /// the simplified iteration the cache uses now it's not much use currently.
        /// </remarks>
        protected bool WalkSet(int set, Func<int, int, bool> func) {
            /* Loop over the set */
            for (int pointerIndex = 0; pointerIndex < ways_; ++pointerIndex) {
                if (func(set, pointerIndex)) {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets a System.Collections.Generic.ICollection containing the keys of the 
        /// ParksComputing.ISetAssociativeCache.
        /// </summary>
        /// <value>
        /// A System.Collections.Generic.ICollection containing the keys of the object that 
        /// implements ParksComputing.ISetAssociativeCache.
        /// </value>
        public virtual ICollection<TKey> Keys {
            get {
                List<TKey> value = new();

                for (var set = 0; set < sets_; ++set) {
                    foreach (var valueIndex in pointerArray_[set]) {
                        if (valueIndex.Key != EMPTY_MARKER) {
                            value.Add(valueArray_[set][valueIndex.Key].Value.Key);
                        }
                    }
                }

                return value;
            }
        }

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        /// <summary>
        /// Gets a System.Collections.Generic.ICollection containing the values in the 
        /// ParksComputing.ISetAssociativeCache.
        /// </summary>
        /// <value>
        /// A System.Collections.Generic.ICollection containing the values in the object that 
        /// implements ParksComputing.ISetAssociativeCache.
        /// </value>
        public virtual ICollection<TValue> Values {
            get {
                List<TValue> value = new();

                for (var set = 0; set < sets_; ++set) {
                    foreach (var valueIndex in pointerArray_[set]) {
                        if (valueIndex.Key != EMPTY_MARKER) {
                            value.Add(valueArray_[set][valueIndex.Key].Value.Value);
                        }
                    }
                }

                return value;
            }
        }

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        /// <summary>
        /// Copies the elements of the System.Collections.Generic.ICollection to an 
        /// System.Array, starting at a particular System.Array index.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional System.Array that is the destination of the elements copied from 
        /// System.Collections.Generic.ICollection. The System.Array must have zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">
        /// The zero-based index in array at which copying begins.
        /// </param>
        /// <exception cref="System.ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
        /// <exception cref="System.ArgumentException">
        /// The number of elements in the source System.Collections.Generic.ICollection is greater than the 
        /// available space from arrayIndex to the end of the destination array.
        /// </exception>
        public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
            if (array == null) {
                throw new ArgumentNullException(nameof(array));
            }

            if (arrayIndex < 0) {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            }

            for (var set = 0; set < sets_; ++set) {
                foreach (KeyValuePair<int, int> keyArrayItem in pointerArray_[set]) {
                    if (keyArrayItem.Key != EMPTY_MARKER) {
                        array[arrayIndex] = new KeyValuePair<TKey, TValue>(valueArray_[set][keyArrayItem.Key].Value.Key, valueArray_[set][keyArrayItem.Key].Value.Value);
                        ++arrayIndex;
                    }
                }
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An System.Collections.IEnumerator object that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            return new CacheEnumerator(this);
        }

        /// <summary>
        /// Emumerator class used to support calling foreach (or equivalent iteration structures) 
        /// on values in this container.
        /// </summary>
        [Serializable]
        private sealed class CacheEnumerator : IEnumerator<KeyValuePair<TKey, TValue>> {
            readonly CacheImplBase<TKey, TValue> cache_;
            readonly int version_;
            readonly int count_;
            int set_;
            int way_;
            int index_;
            KeyValuePair<TKey, TValue> current_;

            public CacheEnumerator(CacheImplBase<TKey, TValue> cache) {
                this.cache_ = cache;
                this.version_ = cache.version_;
                this.count_ = cache.Count;
                Reset();
            }

            public KeyValuePair<TKey, TValue> Current => current_;

            object IEnumerator.Current {
                get {
                    if (index_ == 0 || index_ == count_ + 1) {
                        throw new InvalidOperationException();
                    }

                    return new KeyValuePair<TKey, TValue>(current_.Key, current_.Value);
                }
            }

            public bool MoveNext() {
                if (version_ != cache_.version_) {
                    throw new InvalidOperationException("Emumerator is invalid due to cache update");
                }

                while (index_ < count_) {
                    while (set_ < cache_.sets_) {
                        while (way_ < cache_.ways_) {
                            if (cache_.valueArray_[set_][way_] != null) {
                                current_ = new KeyValuePair<TKey, TValue>(cache_.valueArray_[set_][way_].Value.Key, cache_.valueArray_[set_][way_].Value.Value);
                                ++way_;
                                ++index_;
                                return true;
                            }

                            ++way_;
                        }

                        way_ = 0;
                        ++set_;
                    }

                    ++index_;
                }

                index_ = count_ + 1;
                current_ = new KeyValuePair<TKey, TValue>();
                return false;
            }

            public void Reset() {
                if (version_ != cache_.version_) {
                    throw new InvalidOperationException("Emumerator is invalid due to cache update");
                }

                this.index_ = 0;
                this.set_ = 0;
                this.way_ = 0;
                this.current_ = new KeyValuePair<TKey, TValue>();
            }

            public void Dispose() {
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>An System.Collections.IEnumerator object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        /// <summary>
        /// Gets a value indicating whether the System.Collections.Generic.ICollection is read-only.
        /// </summary>
        /// <value>
        /// <c>true</c> if the System.Collections.Generic.ICollection is read-only; otherwise, <c>false</c>.
        /// </value>
        public bool IsReadOnly { get => false; }

        /// <summary>
        /// If the given <paramref name="key"/> would cause an existing key to be evicted, return <c>true</c> and set 
        /// <paramref name="evictKey"/> to the key of the item that would be evicted if the new <paramref name="key"/> 
        /// were added.
        /// </summary>
        /// <param name="key">Key to test.</param>
        /// <param name="evictKey">Key of cache item that would be evicted, or default key value if return is false.</param>
        /// <returns><c>true</c> if a key would be evicted; <c>false</c> otherwise.</returns>
        /// <exception cref="System.ArgumentNullException">key is null.</exception>
        public bool TryGetEvictKey(TKey key, out TKey evictKey) {
            evictKey = default;

            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            int valueIndex; // Index into the value array where the actual cache item (key/value pair) is stored

            /* Get the number of the set that would contain the new key. */
            var set = FindSet(key);
            int pointerIndex;

            for (pointerIndex = 0; pointerIndex < ways_; ++pointerIndex) {
                valueIndex = pointerArray_[set][pointerIndex].Key;

                /* If the key is found, no eviction. */
                if (valueArray_[set][valueIndex].Value.Key.Equals(key)) {
                    return false;
                }

                /* If the slot is empty, no eviction. */
                if (valueIndex == EMPTY_MARKER) {
                    return false;
                }
            };

            /* If we get here, the set is full and adding the key would cause an eviction. Report 
            the key that would be evicted by this replacement, according to the policy of the 
            derived class. */
            pointerIndex = EvictionPointerIndex;
            valueIndex = pointerArray_[set][pointerIndex].Key;
            evictKey = valueArray_[set][valueIndex].Value.Key;
            return true;
        }

        /// <summary>
        /// Given a <paramref name="key"/>, find the set into which the key should be placed.
        /// </summary>
        /// <param name="key">The key used to find the appropriate set.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <returns>The set in which the <paramref name="key"/> should be kept.</returns>
        protected int FindSet(TKey key) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            ulong keyHash = key.GetHashValue();
            return (int)(keyHash % (ulong)sets_);
        }

        /// <summary>
        /// Adds an element with the provided <paramref name="key"/> and value to the ParksComputing.ISetAssociativeCache.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <param name="set">The set in which to add the element.</param>
        /// <param name="pointerIndex">The index into the pointer set at which to add the element's value index.</param>
        /// <param name="valueIndex">The index into the item set at which the element is stored.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        protected void Add(TKey key, TValue value, int set, int pointerIndex, int valueIndex) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            /* Invalidate any outstanding interators. */
            ++version_;
            /* Store the key/value pair at the designated location in the value array. */
            valueArray_[set][valueIndex] = new KeyValuePair<TKey, TValue>(key, value);
            ++count_;

            UpdateSet(set, pointerIndex);
        }

        /// <summary>
        /// Update and adjust the pointer array as necessary according to the details of the cache policy.
        /// </summary>
        /// <param name="set">Which set to update.</param>
        /// <param name="pointerIndex">The index into the pointer set.</param>
        protected abstract void UpdateSet(int set, int pointerIndex);

        /// <summary>
        /// Mark an item as having the highest rank in the set.
        /// </summary>
        /// <param name="set">The set in which the key is stored.</param>
        /// <param name="pointerIndex">The index into the pointer set.</param>
        protected abstract void PromoteKey(int set, int pointerIndex);

        /// <summary>
        /// Mark an item as having the lowest rank in the set.
        /// </summary>
        /// <param name="set">The set in which the key is stored.</param>
        /// <param name="pointerIndex">The index into the pointer set.</param>
        protected abstract void DemoteKey(int set, int pointerIndex);

    }
}
