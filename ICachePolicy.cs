using System;
using System.Collections.Generic;

namespace ParksComputing.SetAssociativeCache {

    //
    // Summary:
    //     Represents the cache policy of a generic set-associative cache of key/value pairs.
    //
    // Type parameters:
    //   TKey:
    //     The type of keys in the cache.
    //
    //   TValue:
    //     The type of values in the cache.
    public interface ICachePolicy {
        //
        // Summary:
        //     Creates an instance of a policy class.
        public ICachePolicyImpl<TKey, TValue> MakeInstance<TKey, TValue>(int sets, int ways);
    }

    //
    // Summary:
    //     Represents the implementation of the cache policy of a generic set-associative cache of key/value pairs.
    //
    // Type parameters:
    //   TKey:
    //     The type of keys in the cache.
    //
    //   TValue:
    //     The type of values in the cache.
    public interface ICachePolicyImpl<TKey, TValue> : ISetAssociativeCache<TKey, TValue> {
    }
}
