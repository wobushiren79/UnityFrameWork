using System.Collections.Generic;
using System.Linq;

public class DictionaryList<A, B> where A : notnull
{
    private readonly List<B> _list = new List<B>();
    private readonly Dictionary<A, B> _keyToValue = new Dictionary<A, B>();
    private readonly Dictionary<B, A> _valueToKey = new Dictionary<B, A>();

    public int Count => _list.Count;

    public void Clear()
    {
        _list.Clear();
        _keyToValue.Clear();
        _valueToKey.Clear();
    }

    public bool Add(A key, B value)
    {
        // 检查键是否已存在
        if (_keyToValue.ContainsKey(key))
            return false;
            
        // 检查值是否已存在（如果需要值唯一）
        if (_valueToKey.ContainsKey(value))
            return false;

        _list.Add(value);
        _keyToValue[key] = value;
        _valueToKey[value] = key;
        
        return true;
    }
    
    public bool Replace(A key, B newValue)
    {
        // 检查键是否存在
        if (!_keyToValue.TryGetValue(key, out var oldValue))
            return false;

        // 检查新值是否已存在（如果需要值唯一）
        if (!oldValue.Equals(newValue) && _valueToKey.ContainsKey(newValue))
            return false;

        // 更新字典和列表
        _keyToValue[key] = newValue;
        _valueToKey.Remove(oldValue);
        _valueToKey[newValue] = key;

        // 更新列表中的值
        int index = _list.IndexOf(oldValue);
        if (index >= 0)
        {
            _list[index] = newValue;
        }

        return true;
    }

    public bool RemoveByKey(A key)
    {
        if (_keyToValue.TryGetValue(key, out var value))
        {
            _keyToValue.Remove(key);
            _valueToKey.Remove(value);
            return _list.Remove(value);
        }
        return false;
    }

    public bool RemoveByValue(B value)
    {
        if (_valueToKey.TryGetValue(value, out var key))
        {
            _valueToKey.Remove(value);
            _keyToValue.Remove(key);
            return _list.Remove(value);
        }
        return false;
    }

    public bool ContainsKey(A key) => _keyToValue.ContainsKey(key);
    public bool ContainsValue(B value) => _valueToKey.ContainsKey(value);

    public bool TryGetValue(A key, out B value) => _keyToValue.TryGetValue(key, out value);
    public bool TryGetKey(B value, out A key) => _valueToKey.TryGetValue(value, out key);

    // 只读访问
    // public IReadOnlyList<B> List => _list;
    // public IReadOnlyDictionary<A, B> Dictionary => _keyToValue;
    public List<B> List => _list;
    public Dictionary<A, B> Dictionary => _keyToValue;
    // 遍历支持
    public IEnumerable<KeyValuePair<A, B>> KeyValuePairs => 
        _list.Select(value => new KeyValuePair<A, B>(_valueToKey[value], value));
}