namespace meteor.Core.Models;

public class LruCache<TKey, TValue>
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cacheMap = new();
    private readonly LinkedList<CacheItem> _lruList = new();

    public LruCache(int capacity)
    {
        _capacity = capacity;
    }

    public bool TryGet(TKey key, out TValue value)
    {
        if (_cacheMap.TryGetValue(key, out var node))
        {
            value = node.Value.Value;
            _lruList.Remove(node);
            _lruList.AddFirst(node);
            return true;
        }

        value = default;
        return false;
    }

    public void Add(TKey key, TValue value)
    {
        if (_cacheMap.Count >= _capacity)
        {
            var last = _lruList.Last;
            _cacheMap.Remove(last.Value.Key);
            _lruList.RemoveLast();
        }

        var cacheItem = new CacheItem { Key = key, Value = value };
        var node = new LinkedListNode<CacheItem>(cacheItem);
        _lruList.AddFirst(node);
        _cacheMap[key] = node;
    }

    public void Clear()
    {
        _cacheMap.Clear();
        _lruList.Clear();
    }

    private class CacheItem
    {
        public TKey Key { get; set; }
        public TValue Value { get; set; }
    }
}