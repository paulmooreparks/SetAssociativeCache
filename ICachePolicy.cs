using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParksComputing.SetAssociativeCache {

    public interface ICachePolicy {
        internal ICachePolicyImpl<TKey, TValue> MakeInstance<TKey, TValue>(int sets, int ways);
    }

    public interface ICachePolicyImpl<TKey, TValue> : ISetAssociativeCache<TKey, TValue> {
    }
}
