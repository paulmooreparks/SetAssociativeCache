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
    public abstract class CacheImplBase<TKey, TValue> : ISetAssociativeCache<TKey, TValue> {

        /* I'm being a very naughty object-oriented developer. By using fields instead of 
        properties, I've found that I can eliminate function calls from the release version 
        of the JITted assembly code, at least on Intel/AMD x64. That actually surprises me. 
        The properties still exist, though, in case external clients need to know the values 
        for some reason. */
        protected int sets_; // Number of sets in the cache
        protected int ways_; // Capacity of each set in the cache
        protected int version_; // Increments each time an element is added or removed, to invalidate enumerators.

        /* There are two arrays of key/value pairs which are used to track and store the actual cache items. */

        /* keyArray_ stores the indices of the actual cache items stored in the value array. Each 
        item in the array is a key/value pair of two integers. The first integer is an index into 
        the value array. The second index is interpreted by the derived class that implements a 
        specific eviction policy. This array is sorted/rearranged/whatever according to the needs 
        of the cache policy, since it's faster to move integer pairs around, and they're close 
        together in the CPU cache. */
        protected KeyValuePair<int, int>[] keyArray_;

        /* valueArray_ stores the key/value pairs of the actual cache items. Once an item is placed 
        at an index in this array, it stays there unless it is replaced with a new key/value pair or 
        copied out to the client of the cache. Otherwise, these data aren't rearranged or otherwise 
        messed with. */
        protected KeyValuePair<TKey, TValue>[] valueArray_;

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
            valueArray_ = new KeyValuePair<TKey, TValue>[Capacity];
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
        public int Count {
            get {
                int value = 0;

                /* Loop over the key array and count the items which aren't marked with a 
                sentinel value for "empty slot". This is "slower" than keeping track of the 
                count as items are added or removed, but: 
                a.) it's really, really fast to loop over a small array like this (cache 
                    locality is the whole idea, remember?)
                b.) this keeps us from slowing down and complicating the important code 
                    paths with the bookkeeping code necessary to maintain the count. */
                foreach (var keyIndex in keyArray_) {
                    if (keyIndex.Key != EMPTY_MARKER) {
                        ++value;
                    }
                }

                return value;
            }
        }

        /// <summary>
        /// The offset into the set for the item which should be evicted from the cache.
        /// </summary>
        /// <remarks>
        /// This is implemented in the derived class that actually defines and implements 
        /// the cache policy.
        /// </remarks>
        protected abstract int ReplacementOffset { get; }

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

            /* Get the number of the set that would contain the new key. */
            var set = FindSet(key);
            /* Get the first array index for the set; in other words, where in the array does the set start? */
            var setBegin = set * ways_; 

            int setOffset; // Offset into the set in the index array for where the key is stored
            int keyIndex; // Actual array location in the key array for setOffset
            int valueIndex; // Index into the value array where the actual cache item (key/value pair) is stored

            /* Loop over the set, incrementing both the set offset (setOffset) and the key-array index (keyIndex) */
            for (setOffset = 0, keyIndex = setBegin; setOffset < ways_; ++setOffset, ++keyIndex) {
                /* Get the index into the value array */
                valueIndex = keyArray_[keyIndex].Key;

                /* If the index is a sentinel value for "nothing stored here"... */
                if (valueIndex == EMPTY_MARKER) {
                    /* When the slots in a set are being filled initially, the index of each empty spot in the set in 
                    the key index corresponds to the index of the empty spot in the set in the value index. Therefore, 
                    we set the value index to the current key index. */
                    valueIndex = keyIndex;
                    /* Create a new entry in the key array. */
                    keyArray_[keyIndex] = KeyValuePair.Create(valueIndex, keyArray_[keyIndex].Value);
                    /* Delegate adding the cache item and managing the data for the cache policy to the Add method. */
                    Add(key, value, set, setOffset, valueIndex);
                    return;
                }

                /* If the new key is equal to the key at the current position... */
                if (valueArray_[valueIndex].Key.Equals(key)) {
                    /* We'll replace the value in the item array with this value. */
                    valueIndex = keyArray_[keyIndex].Key;
                    /* Delegate adding the cache item and managing the data for the cache policy to the Add method. */
                    Add(key, value, set, setOffset, valueIndex);
                    return;
                }
            }

            /* If we get here, then the set is full. Evict the appropriate element depending on 
            policy, then add the new value at that offset. */

            /* The ReplacementOffset property gives us the offset into the set for the key that will be evicted. */
            setOffset = ReplacementOffset;

            /* Convert setOffset into an actual index into the key array */
            keyIndex = setBegin + setOffset;
            /* Get the index into the value array for where the evicted cache item is stored. */
            valueIndex = keyArray_[keyIndex].Key;
            /* Delegate adding the cache item and managing the data for the cache policy to the Add method. */
            Add(key, value, set, setOffset, valueIndex);
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
            var setBegin = set * ways_;

            /* Loop over the set, incrementing both the set offset (setOffset) and the key-array index (keyIndex) */
            for (int setOffset = 0, keyIndex = setBegin; setOffset < ways_; ++setOffset, ++keyIndex) {
                int valueIndex = keyArray_[keyIndex].Key;

                /* If the key is found in the value array... */
                if (valueIndex != EMPTY_MARKER &&
                    valueArray_[valueIndex].Key.Equals(key)) {
                    /* "Touch" the key to note that it has been accessed. */
                    PromoteKey(set, setOffset);
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
            var setBegin = set * ways_;

            /* Loop over the set, incrementing both the set offset (setOffset) and the key-array index (keyIndex) */
            for (int setOffset = 0, keyIndex = setBegin; setOffset < ways_; ++setOffset, ++keyIndex) {
                int valueIndex = keyArray_[keyIndex].Key;

                /* If the key is found in the value array, and the value at that key matches the provided value... */
                if (valueIndex != EMPTY_MARKER &&
                    valueArray_[valueIndex].Key.Equals(item.Key) &&
                    valueArray_[valueIndex].Value.Equals(item.Value)) {
                    /* "Touch" the key to note that it has been accessed. */
                    PromoteKey(set, setOffset);
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
            var setBegin = set * ways_;

            /* Loop over the set, incrementing both the set offset (setOffset) and the key-array index (keyIndex) */
            for (int setOffset = 0, keyIndex = setBegin; setOffset < ways_; ++setOffset, ++keyIndex) {
                int valueIndex = keyArray_[keyIndex].Key;

                /* If the key is found in the value array... */
                if (valueIndex != EMPTY_MARKER && valueArray_[valueIndex].Key.Equals(key)) {
                    /* "Touch" the key to note that it has been accessed. */
                    PromoteKey(set, setOffset);
                    /* Return the value found at this location in the value array. */
                    value = valueArray_[valueIndex].Value;
                    return true;
                }
            }

            value = default(TValue);
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
            var setBegin = set * ways_;

            /* Loop over the set, incrementing both the set offset (setOffset) and the key-array index (keyIndex) */
            for (int setOffset = 0, keyIndex = setBegin; setOffset < ways_; ++setOffset, ++keyIndex) {
                int valueIndex = keyArray_[keyIndex].Key;

                if (valueIndex != EMPTY_MARKER && valueArray_[valueIndex].Key.Equals(key)) {
                    /* Since all access to the cache values goes through the index array first, 
                    I'll try leaving the value in the cache and see how that goes. It's faster, 
                    but for some reason it make me nervous. I suppose I could make value replacement 
                    a feature of the policy class. */

                    /* Invalidate any outstanding interators. */
                    ++version_;
                    /* Mark this location in the key array as available. */
                    keyArray_[keyIndex] = KeyValuePair.Create(EMPTY_MARKER, keyArray_[keyIndex].Value);
                    DemoteKey(set, setOffset);
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
            var setBegin = set * ways_;

            /* Loop over the set, incrementing both the set offset (setOffset) and the key-array index (keyIndex) */
            for (int setOffset = 0, keyIndex = setBegin; setOffset < ways_; ++setOffset, ++keyIndex) {
                int valueIndex = keyArray_[keyIndex].Key;

                if (valueIndex != EMPTY_MARKER &&
                    valueArray_[valueIndex].Key.Equals(item.Key) &&
                    valueArray_[valueIndex].Value.Equals(item.Value)) {
                    /* Since all access to the cache values goes through the index array first, 
                    I'll try leaving the value in the cache and see how that goes. It's faster, 
                    but for some reason it make me nervous. I suppose I could make value replacement 
                    a feature of the policy class. */

                    /* Invalidate any outstanding interators. */
                    ++version_;
                    keyArray_[keyIndex] = KeyValuePair.Create(EMPTY_MARKER, keyArray_[keyIndex].Value);
                    DemoteKey(set, setOffset);
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

                foreach (var valueIndex in keyArray_) {
                    if (valueIndex.Key != EMPTY_MARKER) {
                        value.Add(valueArray_[valueIndex.Key].Key);
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

                foreach (var valueIndex in keyArray_) {
                    if (valueIndex.Key != EMPTY_MARKER) {
                        value.Add(valueArray_[valueIndex.Key].Value);
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

            foreach (KeyValuePair<int, int> keyArrayItem in keyArray_) {
                if (keyArrayItem.Key != EMPTY_MARKER) {
                    array[arrayIndex] = KeyValuePair.Create(valueArray_[keyArrayItem.Key].Key, valueArray_[keyArrayItem.Key].Value);
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
            CacheImplBase<TKey, TValue> cache_;
            int version_;
            int count_;
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
                    current_ = new KeyValuePair<TKey, TValue>(cache_.valueArray_[index_].Key, cache_.valueArray_[index_].Value);
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

            /* Wipe the key array. */
            keyArray_ = new KeyValuePair<int, int>[Capacity];
            Array.Fill(keyArray_, KeyValuePair.Create(EMPTY_MARKER, 0));
        }

        /// <summary>
        /// Given a key, find the set into which the key should be placed. Since this is an 
        /// internal function, the <paramref name="key"/> parameter is assumed to be not null.
        /// </summary>
        /// <param name="key">The key used to find the appropriate set.</param>
        /// <returns>The set in which the key should be kept.</returns>
        protected int FindSet(TKey key) {
            /* For integer types, GetHashCode() returns the integer, so what we end up with here is 
            a simple MOD operation. A better hashing algorithm is probably a good idea. */
            /* The bitwise OR removes the high bit so that we only get a positive number */
            int hashCode = key.GetHashCode() & 0x7FFFFFFF; 
            return hashCode % sets_;
        }

        /// <summary>
        /// Adds an element with the provided key and value to the ParksComputing.ISetAssociativeCache.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <param name="set">The set in which to add the element.</param>
        /// <param name="setOffset">The offset into the set at which to add the element.</param>
        /// <param name="valueIndex">The index into the item array at which the element is stored.</param>
        protected void Add(TKey key, TValue value, int set, int setOffset, int valueIndex) {
            /* Invalidate any outstanding interators. */
            ++version_;
            /* Store the key/value pair at the designated location in the value array. */
            valueArray_[valueIndex] = KeyValuePair.Create(key, value);

            /* Update the key array with the index into the value array and adjust the key array as 
            necessary according to the details of the cache policy. */
            SetNewItemIndex(set, setOffset);
        }

        /// <summary>
        /// Update the key array with the index into the value array and adjust the key array as 
        /// necessary according to the details of the cache policy.
        /// </summary>
        /// <param name="set">Which set to update.</param>
        /// <param name="setOffset">The offset into the set to update.</param>
        protected abstract void SetNewItemIndex(int set, int setOffset);

        /// <summary>
        /// Increment the count for the last cache item accessed, then sort the set based on all counts.
        /// </summary>
        /// <param name="set">The set in which the key is stored.</param>
        /// <param name="setOffset">The offset into the set at which the key is stored.</param>
        protected abstract void PromoteKey(int set, int setOffset);

        /// <summary>
        /// Set an item's count to zero (removal from cache, for example), then sort the set based on all counts.
        /// </summary>
        /// <param name="set">The set in which the key is stored.</param>
        /// <param name="setOffset">The offset into the set at which the key is stored.</param>
        protected abstract void DemoteKey(int set, int setOffset);

        /// <summary>
        /// If the given <paramref name="key"/> would cause an existing key to be evicted, return <c>true</c> and set 
        /// <paramref name="evictKey"/> to the key of the item that would be evicted if the new <paramref name="key"/> 
        /// were added.
        /// </summary>
        /// <param name="key">Key to test.</param>
        /// <param name="evictKey">Key of cache item that would be evicted, or default key value if return is false.</param>
        /// <returns><c>true</c> if a key would be evicted; <c>false</c> otherwise.</returns>
        public bool TryGetEvictKey(TKey key, out TKey evictKey) {
            evictKey = default(TKey);

            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            var set = FindSet(key);
            var setBegin = set * ways_;
            int setOffset;
            int keyIndex;
            int valueIndex;

            /* Loop over the set, incrementing both the set offset (setOffset) and the key-array index (keyIndex) */
            for (setOffset = 0, keyIndex = setBegin; setOffset < ways_; ++setOffset, ++keyIndex) {
                valueIndex = keyArray_[keyIndex].Key;

                /* If the slot is empty, no eviction. */
                if (valueIndex == EMPTY_MARKER) {
                    return false;
                }

                /* If the key is found, no eviction. */
                if (valueArray_[valueIndex].Key.Equals(key)) {
                    return false;
                }
            }

            /* If we get here, the set is full and adding the key would cause an eviction. Report 
            the key that would be evicted by this replacement, according to the policy of the 
            derived class. */
            setOffset = ReplacementOffset;
            keyIndex = setBegin + setOffset;
            valueIndex = keyArray_[keyIndex].Key;
            evictKey = valueArray_[valueIndex].Key;
            return true;
        }

    }
}
