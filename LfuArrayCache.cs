using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParksComputing.SetAssociativeCache {
    public class LfuArrayCache : ICachePolicy {
        ICachePolicyImpl<TKey, TValue> ICachePolicy.MakeInstance<TKey, TValue> (int capacity, int sets) {
            return new LfuArrayCacheImpl<TKey, TValue>(capacity, sets);
        }
    }

    class LfuArrayCacheImpl<TKey, TValue> : ArrayCacheImplBase<TKey, TValue> {

        public LfuArrayCacheImpl(int sets, int ways) : base(sets, ways) {
        }

        override public void Add(TKey key, TValue value) {
            var set = FindSet(key);
            var setBegin = set * Ways;
            var offsetIndex = setBegin;
            var itemIndex = int.MaxValue;
            int setOffset = 0;

            while (setOffset < Ways) {
                offsetIndex = setBegin + setOffset;
                itemIndex = indexArray[offsetIndex];

                if (itemIndex == int.MaxValue) {
                    itemIndex = offsetIndex;
                    indexArray[offsetIndex] = itemIndex;
                    break;
                }

                if (itemArray[itemIndex].Key.Equals(key)) {
                    offsetIndex = setBegin + setOffset;
                    itemIndex = indexArray[offsetIndex];
                    break;
                }

                ++setOffset;
            }

            RotateSet(set, setOffset);
            itemArray[itemIndex] = KeyValuePair.Create(key, value);
            return;
        }

    }
}
