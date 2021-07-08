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
    public abstract class CacheImplBase<TKey, TValue> : ISetAssociativeCache<TKey, TValue> {

        /* I'm being a very naughty object-oriented developer. By using fields instead of 
        properties, I've found that I can eliminate function calls from the release version 
        of the JITted assembly code, at least on Intel/AMD x64. That actually surprises me. 
        The properties still exist, though, in case external clients need to know the values 
        for some reason. */
        protected int sets_; // Number of sets in the cache
        protected int ways_; // Capacity of each set in the cache
        protected int version_; // Increments each time an element is added or removed, to invalidate enumerators.
        /* TKey is index into ItemArray; TValue is interpreted by the eviction policy of the derived class. */
        protected KeyValuePair<int, int>[] indexArray_;
        protected KeyValuePair<TKey, TValue>[] itemArray_; // Key/value pairs stored in the cache

        /// <summary>
        /// Create a new <c>ArrayCacheImplBase</c> instance.
        /// </summary>
        /// <param name="sets">The number of sets into which the cache is divided.</param>
        /// <param name="ways">The number of storage slots in each set.</param>
        public CacheImplBase(int sets, int ways) {
            sets_ = sets;
            ways_ = ways;
            itemArray_ = new KeyValuePair<TKey, TValue>[Capacity];
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

                foreach (var itemIndex in indexArray_) {
                    if (itemIndex.Key != int.MaxValue) {
                        ++value;
                    }
                }

                return value;
            }
        }

        /// <summary>
        /// The offset into the set for the item which should be evicted from the cache.
        /// </summary>
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
                throw new ArgumentNullException("key");
            }

            var set = FindSet(key);
            var setBegin = set * ways_;
            int setOffset;
            int offsetIndex;
            int itemIndex;

            for (setOffset = 0, offsetIndex = setBegin; setOffset < ways_; ++setOffset, ++offsetIndex) {
                itemIndex = indexArray_[offsetIndex].Key;

                if (itemIndex == int.MaxValue) {
                    itemIndex = offsetIndex;
                    indexArray_[offsetIndex] = KeyValuePair.Create(itemIndex, indexArray_[offsetIndex].Value);
                    Add(key, value, set, setOffset, itemIndex);
                    return;
                }

                if (itemArray_[itemIndex].Key.Equals(key)) {
                    itemIndex = indexArray_[offsetIndex].Key;
                    Add(key, value, set, setOffset, itemIndex);
                    return;
                }
            }

            /* If we get here, then the set is full. Evict the appropriate element depending on 
            policy, then add the new value at that offset. */
            setOffset = ReplacementOffset;
            offsetIndex = setBegin + setOffset;
            itemIndex = indexArray_[offsetIndex].Key;
            Add(key, value, set, setOffset, itemIndex);
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
                throw new ArgumentNullException("key");
            }

            var set = FindSet(key);
            var setBegin = set * ways_;

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < ways_; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray_[offsetIndex].Key;

                if (itemIndex != int.MaxValue &&
                    itemArray_[itemIndex].Key.Equals(key)) {
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

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < ways_; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray_[offsetIndex].Key;

                if (itemIndex != int.MaxValue &&
                    itemArray_[itemIndex].Key.Equals(item.Key) &&
                    itemArray_[itemIndex].Value.Equals(item.Value)) {
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
                throw new ArgumentNullException("key");
            }

            var set = FindSet(key);
            var setBegin = set * ways_;

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < ways_; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray_[offsetIndex].Key;

                if (itemIndex != int.MaxValue && itemArray_[itemIndex].Key.Equals(key)) {
                    PromoteKey(set, setOffset);
                    value = itemArray_[itemIndex].Value;
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
                throw new ArgumentNullException("key");
            }

            var set = FindSet(key);
            var setBegin = set * ways_;

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < ways_; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray_[offsetIndex].Key;

                if (itemIndex != int.MaxValue && itemArray_[itemIndex].Key.Equals(key)) {
                    /* Since all access to the cache values goes through the index array first, 
                    I'll try leaving the value in the cache and see how that goes. It's faster, 
                    but for some reason it make me nervous. I suppose I could make value replacement 
                    a feature of the policy class. */
                    ++version_;
                    indexArray_[offsetIndex] = KeyValuePair.Create(int.MaxValue, indexArray_[offsetIndex].Value);
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

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < ways_; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray_[offsetIndex].Key;

                if (itemIndex != int.MaxValue &&
                    itemArray_[itemIndex].Key.Equals(item.Key) &&
                    itemArray_[itemIndex].Value.Equals(item.Value)) {
                    /* Since all access to the cache values goes through the index array first, 
                    I'll try leaving the value in the cache and see how that goes. It's faster, 
                    but for some reason it make me nervous. I suppose I could make value replacement 
                    a feature of the policy class. */
                    ++version_;
                    indexArray_[offsetIndex] = KeyValuePair.Create(int.MaxValue, indexArray_[offsetIndex].Value);
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

                foreach (var itemIndex in indexArray_) {
                    if (itemIndex.Key != int.MaxValue) {
                        value.Add(itemArray_[itemIndex.Key].Key);
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

                foreach (var itemIndex in indexArray_) {
                    if (itemIndex.Key != int.MaxValue) {
                        value.Add(itemArray_[itemIndex.Key].Value);
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
                throw new ArgumentNullException("array");
            }

            if (arrayIndex < 0) {
                throw new ArgumentOutOfRangeException("arrayIndex");
            }

            foreach (KeyValuePair<int, int> itemIndex in indexArray_) {
                if (itemIndex.Key != int.MaxValue) {
                    array[arrayIndex] = KeyValuePair.Create(itemArray_[itemIndex.Key].Key, itemArray_[itemIndex.Key].Value);
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

            public KeyValuePair<TKey, TValue> Current { get => current_; }
            
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
                    current_ = new KeyValuePair<TKey, TValue>(cache_.itemArray_[index_].Key, cache_.itemArray_[index_].Value);
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
            to the data. With no indices, the data aren't accessible. */
            ++version_;
            indexArray_ = new KeyValuePair<int, int>[Capacity];
            Array.Fill(indexArray_, KeyValuePair.Create(int.MaxValue, 0));
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
        /// <param name="itemIndex">The index into the item array at which the element is stored.</param>
        protected void Add(TKey key, TValue value, int set, int setOffset, int itemIndex) {
            ++version_;
            itemArray_[itemIndex] = KeyValuePair.Create(key, value);
            SetNewItemIndex(set, setOffset);
        }

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
    }
}
