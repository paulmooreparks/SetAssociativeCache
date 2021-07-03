# SetAssociativeCache
This is a C# implementation of a set-associative cache with multiple policies (LRU, LFU, etc.). There is a basic generic cache class, 
SetAssociativeCache, and this is parameterized by the key type, value type, and the name of the class that implements the preferred policy. 
This makes the cache class extensible, since the actual cache policy implementation is provided by the policy class.
