using System.Collections.Generic;
using System.Linq;

public class DictionaryList<A, B> where A : notnull
{
    private readonly List<A> _keyList = new List<A>();
    private readonly List<B> _valueList = new List<B>();
    private readonly Dictionary<A, B> _keyToValue = new Dictionary<A, B>();
    private readonly Dictionary<B, A> _valueToKey = new Dictionary<B, A>();

    public int Count => _valueList.Count;

    public void Clear()
    {
        _keyList.Clear();
        _valueList.Clear();
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

        _keyList.Add(key);
        _valueList.Add(value);
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
        int index = _valueList.IndexOf(oldValue);
        if (index >= 0)
        {
            _valueList[index] = newValue;
            // keyList 保持不变，因为键没有变化
        }

        return true;
    }

    public bool RemoveByKey(A key)
    {
        if (_keyToValue.TryGetValue(key, out var value))
        {
            _keyToValue.Remove(key);
            _valueToKey.Remove(value);
            
            int index = _valueList.IndexOf(value);
            if (index >= 0)
            {
                _keyList.RemoveAt(index);
                _valueList.RemoveAt(index);
            }
            return true;
        }
        return false;
    }

    public bool RemoveByValue(B value)
    {
        if (_valueToKey.TryGetValue(value, out var key))
        {
            _valueToKey.Remove(value);
            _keyToValue.Remove(key);
            
            int index = _valueList.IndexOf(value);
            if (index >= 0)
            {
                _keyList.RemoveAt(index);
                _valueList.RemoveAt(index);
            }
            return true;
        }
        return false;
    }

    public bool ContainsKey(A key) => _keyToValue.ContainsKey(key);
    public bool ContainsValue(B value) => _valueToKey.ContainsKey(value);

    public bool TryGetValue(A key, out B value) => _keyToValue.TryGetValue(key, out value);
    public bool TryGetKey(B value, out A key) => _valueToKey.TryGetValue(value, out key);

    // 列表访问
    public List<A> ListKey => _keyList;
    public List<B> List => _valueList;
    
    // 字典访问
    public Dictionary<A, B> Dictionary => _keyToValue;

    // 遍历支持
    public IEnumerable<KeyValuePair<A, B>> KeyValuePairs => 
        _valueList.Select(value => new KeyValuePair<A, B>(_valueToKey[value], value));

    // 索引器访问
    public A GetKeyAt(int index) => _keyList[index];
    public B GetValueAt(int index) => _valueList[index];
    public KeyValuePair<A, B> GetKeyValueAt(int index) => 
        new KeyValuePair<A, B>(_keyList[index], _valueList[index]);

    // 批量操作
    public void AddRange(IEnumerable<KeyValuePair<A, B>> items)
    {
        foreach (var item in items)
        {
            Add(item.Key, item.Value);
        }
    }

    // 查找索引
    public int IndexOfKey(A key)
    {
        return _keyList.IndexOf(key);
    }

    public int IndexOfValue(B value)
    {
        return _valueList.IndexOf(value);
    }
}