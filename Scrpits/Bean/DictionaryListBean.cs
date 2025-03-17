using System.Collections.Generic;

public class DictionaryList<A, B>
{
    private List<B> _list = new List<B>();
    private Dictionary<A, B> _idIndex = new Dictionary<A, B>();

    public void Clear()
    {
        _list.Clear();
        _idIndex.Clear();
    }

    public void Add(A key, B value)
    {
        if (!_list.Contains(value))
        {
            _list.Add(value);
        }
        if (!_idIndex.ContainsKey(key))
        {
            _idIndex[key] = value;
        }
    }

    public void RemoveByKey(A key)
    {
        if (_idIndex.TryGetValue(key, out var value))
        {
            _idIndex.Remove(key);
            if (_list.Contains(value))
            {
                _list.Remove(value);
            }
        }
    }

    public bool ContainsKey(A key)
    {
        if (_idIndex.ContainsKey(key))
        {
            return true;
        }
        return false;
    }

    public bool TryGetValue(A key, out B value)
    {
        return _idIndex.TryGetValue(key, out value);
    }

      // 遍历时直接使用 _list
    public List<B> List => _list;
}
