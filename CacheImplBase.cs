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
    /// You will notice that I copy a lot of the code that walks through a set looking for 
    /// keys. I tried abstracting this away with a few different attempts, and each was a 
    /// little bit cleaner, maybe, albeit harder to follow, but a lot slower.
    /// </remarks>
    public abstract class CacheImplBase<TKey, TValue> : ISetAssociativeCache<TKey, TValue> {

        /* I'm being a very naughty object-oriented developer. By using fields instead of 
        properties, I've found that I can eliminate function calls from the release version 
        of the JITted assembly code, at least on Intel/AMD x64. That actually surprises me. 
        The properties still exist, though, in case external clients need to know the values 
        for some reason. */
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
        protected KeyValuePair<int, int>[] pointerArray_;

        /* valueArray_ stores the key/value pairs of the actual cache items. Once an item is placed 
        at an index in this array, it stays there unless it is replaced with a new key/value pair or 
        copied out to the client of the cache. Otherwise, these data aren't rearranged or otherwise 
        messed with. */
        protected KeyValuePair<TKey, TValue>?[] valueArray_;

        /* This value is used as a sentinel to mark empty slots in the key array. */
        protected const int EMPTY_MARKER = int.MaxValue;

        /// <summary>
        /// Create a new <c>CacheImplBase</c> instance.
        /// </summary>
        /// <param name="sets">The number of sets into which the cache is divided.</param>
        /// <param name="ways">The number of storage slots in each set.</param>
        public CacheImplBase(int sets, int ways) {
            sets_ = sets;
            ways_ = ways;
            valueArray_ = new KeyValuePair<TKey, TValue>?[Capacity];
            Clear();
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
                Add(key, value);
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
        /// The offset into the set for the item which should be evicted from the cache.
        /// </summary>
        /// <remarks>
        /// This is implemented in the derived class that actually defines and implements 
        /// the cache policy.
        /// </remarks>
        protected abstract int ReplacementOffset { get; }

        /// <summary>
        /// Return an enumerable container of indices into the value array from a set in the key array.
        /// </summary>
        /// <param name="set">The set to operate on.</param>
        /// <remarks>
        /// Generalizing enumeration of sets this way is actually slower -- notably, but not excessively -- 
        /// than copying the loop to all the places in the class that need to enumerate sets. I'll stay 
        /// with this trade-off for now since it gives me some flexibility to modify the implementation 
        /// later.
        /// </remarks>
        protected IEnumerable<int> GetSetPointerIndices(int set) {
            /* Get the first array index for the set; in other words, where in the array does the set start? */
            var setBegin = set * ways_;

            int setOffset; // Offset into the set in the index array for where the key is stored
            int pointerIndex; // Actual array location in the key array for setOffset

            /* Loop over the set, incrementing both the set offset (setOffset) and the pointer-array index (pointerIndex) */
            for (setOffset = 0, pointerIndex = setBegin; setOffset < ways_; ++setOffset, ++pointerIndex) {
                yield return pointerIndex;
            }
        }

        /// <summary>
        /// Adds an element with the provided <paramref name="key"/> and <paramref name="value"/> 
        /// to the ParksComputing.ISetAssociativeCache.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        public virtual void Add(TKey key, TValue value) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            /// <remarks>
            /// The following iteration pattern repeats in most of the methods in this class, so 
            /// it bears explaining. The call to <c>FindSet</c> gets the set in which the <c>key</c>  
            /// would be found. The call to <c>GetSetPointerIndices</c> retrieves an enumerable 
            /// collection of indices into the pointer array. The <c>foreach</c> loop iterates over 
            /// the enumeration to allow searching for keys in the set, looking for empty slots in 
            /// the set, and any other set-level operations.
            /// </remarks>

            /* Get the number of the set that would contain the new key. */
            var set = FindSet(key);
            int valueIndex; // Index into the value array where the actual cache item (key/value pair) is stored

            /* Get the first array index for the set; in other words, where in the array does the 
            set start? */
            var setBegin = set * ways_;
            var setEnd = setBegin + ways_;
            int pointerIndex; // Actual array location in the key array

            /* Loop over the set */
            for (pointerIndex = setBegin; pointerIndex < setEnd; ++pointerIndex) {
                /* Get the index into the value array */
                valueIndex = pointerArray_[pointerIndex].Key;

                /* If the index is a sentinel value for "nothing stored here"... */
                if (valueIndex == EMPTY_MARKER) {
                    /* Find the first empty entry in the value array */
                    valueIndex = setBegin;

                    while (valueIndex < setEnd && valueArray_[valueIndex] != null) {
                        ++valueIndex;
                    }

                    /* Create a new entry in the key array. */
                    pointerArray_[pointerIndex] = new KeyValuePair<int, int>(valueIndex, pointerArray_[pointerIndex].Value);

                    /* Delegate adding the cache item and managing the data for the cache policy 
                    to the Add method. */
                    Add(key, value, set, pointerIndex, valueIndex);
                    return;
                }

                /* If the new key is equal to the key at the current position... */
                if (valueArray_[valueIndex].Value.Key.Equals(key)) {
                    /* We'll replace the value in the item array with this value. */
                    valueIndex = pointerArray_[pointerIndex].Key;
                    /* Delegate adding the cache item and managing the data for the cache policy 
                    to the Add method. */
                    Add(key, value, set, pointerIndex, valueIndex);
                    return;
                }
            }

            /* If we get here, then the set is full. Evict the appropriate element depending on 
            policy, then add the new value at that offset. */

            --count_;

            /* The ReplacementOffset property gives us the offset into the set for the key that 
            will be evicted. */
            int newKeyIndex = setBegin + ReplacementOffset;

            /* Get the index into the value array for where the evicted cache item is stored. */
            valueIndex = pointerArray_[newKeyIndex].Key;
            /* Delegate adding the cache item and managing the data for the cache policy to the 
            Add method. */
            Add(key, value, set, newKeyIndex, valueIndex);
            return;
        }

        /// <summary>
        /// Adds an item to the cache.
        /// </summary>
        /// <param name="item">The key/value pair to add to the cache.</param>
        public void Add(KeyValuePair<TKey, TValue> item) {
            Add(item.Key, item.Value);
        }

        /// <summary>
        /// Determines whether the ParksComputing.ISetAssociativeCache contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the ParksComputing.ISetAssociativeCache.</param>
        /// <returns>
        /// true if the ParksComputing.ISetAssociativeCache contains an element with the key; otherwise, false.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">key is null.</exception>
        public virtual bool ContainsKey(TKey key) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            var set = FindSet(key);

            /* Get the first array index for the set; in other words, where in the array does the 
            set start? */
            var setBegin = set * ways_;
            var setEnd = setBegin + ways_;
            int pointerIndex; // Actual array location in the key array

            /* Loop over the set */
            for (pointerIndex = setBegin; pointerIndex < setEnd; ++pointerIndex) {
                int valueIndex = pointerArray_[pointerIndex].Key;

                /* If the key is found in the value array... */
                if (valueIndex != EMPTY_MARKER &&
                    valueArray_[valueIndex].Value.Key.Equals(key)) {
                    /* "Touch" the key to note that it has been accessed. */
                    PromoteKey(set, pointerIndex);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether the System.Collections.Generic.ICollection contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the System.Collections.Generic.ICollection.</param>
        /// <returns>true if item is found in the System.Collections.Generic.ICollection; otherwise, false.</returns>
        public virtual bool Contains(KeyValuePair<TKey, TValue> item) {
            var set = FindSet(item.Key);

            /* Get the first array index for the set; in other words, where in the array does the 
            set start? */
            var setBegin = set * ways_;
            var setEnd = setBegin + ways_;
            int pointerIndex; // Actual array location in the key array

            /* Loop over the set */
            for (pointerIndex = setBegin; pointerIndex < setEnd; ++pointerIndex) {
                int valueIndex = pointerArray_[pointerIndex].Key;

                /* If the key is found in the value array, and the value at that key matches the provided value... */
                if (valueIndex != EMPTY_MARKER &&
                    valueArray_[valueIndex].Value.Key.Equals(item.Key) &&
                    valueArray_[valueIndex].Value.Value.Equals(item.Value)) {
                    /* "Touch" the key to note that it has been accessed. */
                    PromoteKey(set, pointerIndex);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">
        /// When this method returns, the value associated with the specified key, if the key is found; 
        /// otherwise, the default value for the type of the value parameter. This parameter is passed 
        /// uninitialized.
        /// </param>
        /// <returns>
        /// true if the object that implements ParksComputing.ISetAssociativeCache contains 
        /// an element with the specified key; otherwise, false.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">key is null.</exception>
        public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            var set = FindSet(key);

            /* Get the first array index for the set; in other words, where in the array does the 
            set start? */
            var setBegin = set * ways_;
            var setEnd = setBegin + ways_;
            int pointerIndex; // Actual array location in the key array

            /* Loop over the set */
            for (pointerIndex = setBegin; pointerIndex < setEnd; ++pointerIndex) {
                int valueIndex = pointerArray_[pointerIndex].Key;

                /* If the key is found in the value array... */
                if (valueIndex != EMPTY_MARKER && valueArray_[valueIndex].Value.Key.Equals(key)) {
                    /* "Touch" the key to note that it has been accessed. */
                    PromoteKey(set, pointerIndex);
                    /* Return the value found at this location in the value array. */
                    value = valueArray_[valueIndex].Value.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Removes the element with the specified key from the ParksComputing.ISetAssociativeCache.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>
        /// true if the element is successfully removed; otherwise, false. This method also returns false if key 
        /// was not found in the original ParksComputing.ISetAssociativeCache.
        /// </returns>
        public virtual bool Remove(TKey key) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            var set = FindSet(key);

            /* Get the first array index for the set; in other words, where in the array does the 
            set start? */
            var setBegin = set * ways_;
            var setEnd = setBegin + ways_;
            int pointerIndex; // Actual array location in the key array

            /* Loop over the set */
            for (pointerIndex = setBegin; pointerIndex < setEnd; ++pointerIndex) {
                int valueIndex = pointerArray_[pointerIndex].Key;

                if (valueIndex != EMPTY_MARKER && valueArray_[valueIndex].Value.Key.Equals(key)) {
                    /* Clear the value from the cache */
                    valueArray_[valueIndex] = null;

                    /* Invalidate any outstanding interators. */
                    ++version_;
                    /* Mark this location in the key array as available. */
                    pointerArray_[pointerIndex] = new KeyValuePair<int, int>(EMPTY_MARKER, pointerArray_[pointerIndex].Value);
                    DemoteKey(set, pointerIndex);
                    --count_;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes the element with the specified key from the ParksComputing.ISetAssociativeCache.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>
        /// true if the element is successfully removed; otherwise, false. This method also returns false if key 
        /// was not found in the original ParksComputing.ISetAssociativeCache.
        /// </returns>
        public virtual bool Remove(KeyValuePair<TKey, TValue> item) {
            var set = FindSet(item.Key);

            /* Get the first array index for the set; in other words, where in the array does the 
            set start? */
            var setBegin = set * ways_;
            var setEnd = setBegin + ways_;
            int pointerIndex; // Actual array location in the key array

            /* Loop over the set */
            for (pointerIndex = setBegin; pointerIndex < setEnd; ++pointerIndex) {
                int valueIndex = pointerArray_[pointerIndex].Key;

                if (valueIndex != EMPTY_MARKER &&
                    valueArray_[valueIndex].Value.Key.Equals(item.Key) &&
                    valueArray_[valueIndex].Value.Value.Equals(item.Value)) {
                    /* Clear the value from the cache */
                    valueArray_[valueIndex] = null;

                    /* Invalidate any outstanding interators. */
                    ++version_;
                    pointerArray_[pointerIndex] = new KeyValuePair<int, int>(EMPTY_MARKER, pointerArray_[pointerIndex].Value);
                    DemoteKey(set, pointerIndex);
                    --count_;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets an System.Collections.Generic.ICollection containing the keys of the ParksComputing.ISetAssociativeCache.
        /// </summary>
        /// <value>
        /// An System.Collections.Generic.ICollection containing the keys of the object that implements ParksComputing.ISetAssociativeCache.
        /// </value>
        public virtual ICollection<TKey> Keys {
            get {
                List<TKey> value = new();

                foreach (var valueIndex in pointerArray_) {
                    if (valueIndex.Key != EMPTY_MARKER) {
                        value.Add(valueArray_[valueIndex.Key].Value.Key);
                    }
                }

                return value;
            }
        }

        /// <summary>
        /// Gets an System.Collections.Generic.ICollection containing the values in the ParksComputing.ISetAssociativeCache.
        /// </summary>
        /// <value>
        /// An System.Collections.Generic.ICollection containing the values in the object that implements ParksComputing.ISetAssociativeCache.
        /// </value>
        public virtual ICollection<TValue> Values {
            get {
                List<TValue> value = new();

                foreach (var valueIndex in pointerArray_) {
                    if (valueIndex.Key != EMPTY_MARKER) {
                        value.Add(valueArray_[valueIndex.Key].Value.Value);
                    }
                }

                return value;
            }
        }

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

            foreach (KeyValuePair<int, int> keyArrayItem in pointerArray_) {
                if (keyArrayItem.Key != EMPTY_MARKER) {
                    array[arrayIndex] = new KeyValuePair<TKey, TValue>(valueArray_[keyArrayItem.Key].Value.Key, valueArray_[keyArrayItem.Key].Value.Value);
                    ++arrayIndex;
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
            int index_;
            KeyValuePair<TKey, TValue> current_;

            public CacheEnumerator(CacheImplBase<TKey,TValue> cache) {
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
                    current_ = new KeyValuePair<TKey, TValue>(cache_.valueArray_[index_].Value.Key, cache_.valueArray_[index_].Value.Value);
                    ++index_;
                    return true;
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
        /// Removes all items from the System.Collections.Generic.ICollection.
        /// </summary>
        public virtual void Clear() {
            /* Keep in mind that the data aren't cleared. We are clearing the indices which point 
            to the data. With no indices, the data aren't accessible. If you're storing secure 
            data in the cache... well, just don't. If you must, then you'll need to change this. */

            /* Invalidate any outstanding interators. */
            ++version_;
            count_ = 0;

            /* Wipe the key array. */
            pointerArray_ = new KeyValuePair<int, int>[Capacity];
            Array.Fill(pointerArray_, new KeyValuePair<int, int>(EMPTY_MARKER, 0));
        }

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

            /* Get the first array index for the set; in other words, where in the array does the set start? */
            var setBegin = set * ways_;
            var setEnd = setBegin + ways_;
            int pointerIndex; // Actual array location in the key array

            /* Loop over the set */
            for (pointerIndex = setBegin; pointerIndex < setEnd; ++pointerIndex) {
                valueIndex = pointerArray_[pointerIndex].Key;

                /* If the slot is empty, no eviction. */
                if (valueIndex == EMPTY_MARKER) {
                    return false;
                }

                /* If the key is found, no eviction. */
                if (valueArray_[valueIndex].Value.Key.Equals(key)) {
                    return false;
                }
            }

            /* If we get here, the set is full and adding the key would cause an eviction. Report 
            the key that would be evicted by this replacement, according to the policy of the 
            derived class. */
            int evictKeyIndex = setBegin + ReplacementOffset;
            valueIndex = pointerArray_[evictKeyIndex].Key;
            evictKey = valueArray_[valueIndex].Value.Key;
            return true;
        }

        /// <summary>
        /// Given a key, find the set into which the key should be placed.
        /// </summary>
        /// <param name="key">The key used to find the appropriate set.</param>
        /// <exception cref="System.ArgumentNullException">key is null.</exception>
        /// <returns>The set in which the key should be kept.</returns>
        protected int FindSet(TKey key) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            ulong keyHash = key.GetHashValue();
            return (int)(keyHash % (ulong)sets_);
        }

        /// <summary>
        /// Adds an element with the provided key and value to the ParksComputing.ISetAssociativeCache.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <param name="set">The set in which to add the element.</param>
        /// <param name="pointerIndex">The index into the key array at which to add the element's value index.</param>
        /// <param name="valueIndex">The index into the item array at which the element is stored.</param>
        /// <exception cref="System.ArgumentNullException">key is null.</exception>
        protected void Add(TKey key, TValue value, int set, int pointerIndex, int valueIndex) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            /* Invalidate any outstanding interators. */
            ++version_;
            /* Store the key/value pair at the designated location in the value array. */
            valueArray_[valueIndex] = new KeyValuePair<TKey, TValue>(key, value);
            ++count_;

            /* Update the key array with the index into the value array and adjust the key array as 
            necessary according to the details of the cache policy. */
            UpdateSet(set, pointerIndex);
        }

        /// <summary>
        /// Update the key array with the index into the value array and adjust the key array as 
        /// necessary according to the details of the cache policy.
        /// </summary>
        /// <param name="set">Which set to update.</param>
        /// <param name="pointerIndex">The index into the key array.</param>
        protected abstract void UpdateSet(int set, int pointerIndex);

        /// <summary>
        /// Increment the count for the last cache item accessed, then sort the set based on all counts.
        /// </summary>
        /// <param name="set">The set in which the key is stored.</param>
        /// <param name="pointerIndex">The index into the key array.</param>
        protected abstract void PromoteKey(int set, int pointerIndex);

        /// <summary>
        /// Set an item's count to zero (removal from cache, for example), then sort the set based on all counts.
        /// </summary>
        /// <param name="set">The set in which the key is stored.</param>
        /// <param name="pointerIndex">The index into the key array.</param>
        protected abstract void DemoteKey(int set, int pointerIndex);

    }
}
