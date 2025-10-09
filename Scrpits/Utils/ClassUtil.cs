using UnityEngine;
using System.Collections;
using System;
using System.Reflection;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

public class ClassUtil : MonoBehaviour
{
    /// <summary>
    /// 深度复制 注意：使用这个方法需要给类添加一个默认的构造函数
    /// </summary>
    public static T DeepCopy<T>(T obj)
    {
        if (obj == null) return default(T);

        // 如果是Unity对象，使用JsonUtility
        if (obj is UnityEngine.Object)
        {
            return DeepCopyUnityObject(obj);
        }

        return DeepCopySystemObject(obj);
    }

    /// <summary>
    /// 深度复制 注意：使用这个方法需要给类添加一个默认的构造函数
    /// </summary>
    private static T DeepCopyUnityObject<T>(T obj)
    {
        string json = JsonUtility.ToJson(obj);
        T copy = (T)Activator.CreateInstance(obj.GetType());
        JsonUtility.FromJsonOverwrite(json, copy);
        return copy;
    }
    
    /// <summary>
    /// 深度复制 注意：使用这个方法需要给类添加一个默认的构造函数
    /// </summary>
    private static T DeepCopySystemObject<T>(T obj)
    {
        if (obj == null) return default(T);

        Type type = obj.GetType();

        if (type.IsValueType || type == typeof(string))
            return obj;

        if (type.IsArray)
        {
            Type elementType = type.GetElementType();
            var array = obj as Array;
            Array copied = Array.CreateInstance(elementType, array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                copied.SetValue(DeepCopy(array.GetValue(i)), i);
            }
            return (T)(object)copied;
        }

        if (type.IsClass)
        {
            object copy = Activator.CreateInstance(type);
            CopyFields(obj, copy, type);
            CopyProperties(obj, copy, type);
            return (T)copy;
        }

        return default(T);
    }
    
    private static void CopyFields(object source, object target, Type type)
    {
        FieldInfo[] fields = type.GetFields(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
        foreach (FieldInfo field in fields)
        {
            // 跳过只读字段和常量
            if (field.IsLiteral || field.IsInitOnly) continue;
            
            object value = field.GetValue(source);
            if (value != null)
            {
                field.SetValue(target, DeepCopy(value));
            }
            else
            {
                field.SetValue(target, null);
            }
        }
    }
    
    private static void CopyProperties(object source, object target, Type type)
    {
        PropertyInfo[] properties = type.GetProperties(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            
        foreach (PropertyInfo property in properties)
        {
            if (!property.CanRead || !property.CanWrite) continue;
            if (property.GetIndexParameters().Length > 0) continue; // 跳过索引器
            
            object value = property.GetValue(source);
            if (value != null)
            {
                property.SetValue(target, DeepCopy(value));
            }
            else
            {
                property.SetValue(target, null);
            }
        }
    }
    
    public static T CopyComponent<T>(T original, GameObject destination) where T : Component
    {
        Type type = original.GetType();
        Component copy = destination.AddComponent(type);

        FieldInfo[] fields = type.GetFields(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        foreach (FieldInfo field in fields)
        {
            if (field.IsLiteral || field.IsInitOnly) continue;
            field.SetValue(copy, field.GetValue(original));
        }

        return copy as T;
    }
    
    public static T CopyScriptableObject<T>(T original) where T : ScriptableObject
    {
        T instance = ScriptableObject.CreateInstance<T>();
        
        string json = JsonUtility.ToJson(original);
        JsonUtility.FromJsonOverwrite(json, instance);
        
        return instance;
    }

    public static T DeepCopyBinary<T>(T obj)
    {
        if (!typeof(T).IsSerializable)
        {
            throw new ArgumentException("The type must be serializable.", nameof(obj));
        }

        if (obj == null) return default(T);

        using (var stream = new MemoryStream())
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, obj);
            stream.Seek(0, SeekOrigin.Begin);
            return (T)formatter.Deserialize(stream);
        }
    }
}