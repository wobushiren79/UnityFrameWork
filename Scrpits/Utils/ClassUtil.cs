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
    /// 打包后可能有点问题
    /// </summary>
    public static T DeepCopy<T>(T obj)
    {
        if (obj == null) return default(T);

        Type type = obj.GetType();

        // 如果对象是值类型或字符串，则直接返回
        if (type.IsValueType || type == typeof(string))
        {
            return obj;
        }
        // 如果对象是数组，则创建一个新数组，并复制每个元素
        else if (type.IsArray)
        {
            Type elementType = Type.GetType(
                type.FullName.Replace("[]", string.Empty));
            var array = obj as Array;
            Array copied = Array.CreateInstance(elementType, array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                copied.SetValue(DeepCopy(array.GetValue(i)), i);
            }
            return (T)(object)copied;
        }
        // 如果对象是类，则递归复制每个属性
        else if (type.IsClass)
        {
            object copiedObj = Activator.CreateInstance(obj.GetType());
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (PropertyInfo property in properties)
            {
                if (!property.CanWrite || !property.CanRead) continue;
                object propertyValue = property.GetValue(obj, null);
                if (propertyValue == null) continue;
                property.SetValue(copiedObj, DeepCopy(propertyValue), null);
            }
            return (T)copiedObj;
        }
        else
        {
            throw new ArgumentException("Unsupported type");
        }
    }

    public static T DeepCopyBySerialize<T>(T obj)
    {
        if (obj == null) return default(T);

        BinaryFormatter formatter = new BinaryFormatter();
        using (MemoryStream stream = new MemoryStream())
        {
            formatter.Serialize(stream, obj);
            stream.Seek(0, SeekOrigin.Begin);
            return (T)formatter.Deserialize(stream);
        }
    }


    public static T DeepCopyByXml<T>(T obj)
    {
        object retval;
        using (MemoryStream ms = new MemoryStream())
        {
            XmlSerializer xml = new XmlSerializer(typeof(T));
            xml.Serialize(ms, obj);
            ms.Seek(0, SeekOrigin.Begin);
            retval = xml.Deserialize(ms);
            ms.Close();
        }
        return (T)retval;
    }

    public static T DeepCopyByBin<T>(T obj)
    {
        object retval;
        using (MemoryStream ms = new MemoryStream())
        {
            BinaryFormatter bf = new BinaryFormatter();
            //序列化成流
            bf.Serialize(ms, obj);
            ms.Seek(0, SeekOrigin.Begin);
            //反序列化成对象
            retval = bf.Deserialize(ms);
            ms.Close();
        }
        return (T)retval;
    }

}