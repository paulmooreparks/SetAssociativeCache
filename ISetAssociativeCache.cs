using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ParksComputing.SetAssociativeCache {
    //
    // Summary:
    //     Represents a generic set-associative cache of key/value pairs.
    //     Full disclosure: I cribbed most of these comments and the overall structure from
    //     System.Collections.Generic.IDictionary<>, since that seems to be a good basis for 
    //     this interface as well.
    //
    // Type parameters:
    //   TKey:
    //     The type of keys in the cache.
    //
    //   TValue:
    //     The type of values in the cache.
    public interface ISetAssociativeCache<TKey, TValue> : ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable {
        //
        // Summary:
        //     Gets or sets the element with the specified key.
        //
        // Parameters:
        //   key:
        //     The key of the element to get or set.
        //
        // Returns:
        //     The element with the specified key.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     key is null.
        //
        //   T:System.Collections.Generic.KeyNotFoundException:
        //     The property is retrieved and key is not found.
        //
        //   T:System.NotSupportedException:
        //     The property is set and the System.Collections.Generic.ISetAssociativeCache`2 is read-only.
        TValue this[TKey key] {
            get;
            set;
        }

        //
        // Summary:
        //     Gets an System.Collections.Generic.ICollection`1 containing the keys of the System.Collections.Generic.ISetAssociativeCache`2.
        //
        // Returns:
        //     An System.Collections.Generic.ICollection`1 containing the keys of the object
        //     that implements System.Collections.Generic.ISetAssociativeCache`2.
        ICollection<TKey> Keys {
            get;
        }

        //
        // Summary:
        //     Gets an System.Collections.Generic.ICollection`1 containing the values in the
        //     System.Collections.Generic.ISetAssociativeCache`2.
        //
        // Returns:
        //     An System.Collections.Generic.ICollection`1 containing the values in the object
        //     that implements System.Collections.Generic.ISetAssociativeCache`2.
        ICollection<TValue> Values {
            get;
        }

        int Ways { get; }

        int Sets { get; }

        int Capacity { get; }

        //
        // Summary:
        //     Adds an element with the provided key and value to the System.Collections.Generic.ISetAssociativeCache`2.
        //
        // Parameters:
        //   key:
        //     The object to use as the key of the element to add.
        //
        //   value:
        //     The object to use as the value of the element to add.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     key is null.
        //
        //   T:System.ArgumentException:
        //     An element with the same key already exists in the System.Collections.Generic.ISetAssociativeCache`2.
        //
        //   T:System.NotSupportedException:
        //     The System.Collections.Generic.ISetAssociativeCache`2 is read-only.
        void Add(TKey key, TValue value);

        //
        // Summary:
        //     Determines whether the System.Collections.Generic.ISetAssociativeCache`2 contains an element
        //     with the specified key.
        //
        // Parameters:
        //   key:
        //     The key to locate in the System.Collections.Generic.ISetAssociativeCache`2.
        //
        // Returns:
        //     true if the System.Collections.Generic.ISetAssociativeCache`2 contains an element with
        //     the key; otherwise, false.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     key is null.
        bool ContainsKey(TKey key);

        //
        // Summary:
        //     Removes the element with the specified key from the System.Collections.Generic.ISetAssociativeCache`2.
        //
        // Parameters:
        //   key:
        //     The key of the element to remove.
        //
        // Returns:
        //     true if the element is successfully removed; otherwise, false. This method also
        //     returns false if key was not found in the original System.Collections.Generic.ISetAssociativeCache`2.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     key is null.
        //
        //   T:System.NotSupportedException:
        //     The System.Collections.Generic.ISetAssociativeCache`2 is read-only.
        bool Remove(TKey key);

        //
        // Summary:
        //     Gets the value associated with the specified key.
        //
        // Parameters:
        //   key:
        //     The key whose value to get.
        //
        //   value:
        //     When this method returns, the value associated with the specified key, if the
        //     key is found; otherwise, the default value for the type of the value parameter.
        //     This parameter is passed uninitialized.
        //
        // Returns:
        //     true if the object that implements System.Collections.Generic.ISetAssociativeCache`2 contains
        //     an element with the specified key; otherwise, false.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     key is null.
        bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value);
    }
}
