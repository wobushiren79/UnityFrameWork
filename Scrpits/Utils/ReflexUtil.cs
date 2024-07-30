using UnityEngine;
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine.Rendering;

public class ReflexUtil
{
    /// <summary>
    /// 通过反射自动连接自己的数据
    /// </summary>
    /// <param name="obj">对象</param>
    /// <param name="markStr">标记文字</param>
    public static void AutoLinkDataForSelf<T>(T obj, string markStr) where T : BaseMonoBehaviour
    {
        Type trueType = obj.GetType();
        FieldInfo[] fields = trueType.GetFields();
        for (int i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            if (!field.Name.Contains(markStr))
            {
                continue;
            }
            Component tmpCom = obj.GetComponent(field.FieldType);
            if (tmpCom == null)
            {
                //Debug.LogWarning("window " + trueType.Name + ",can not find：" + field.Name.Replace(markStr, ""));
                continue;
            }
            field.SetValue(obj, tmpCom);
        }
    }

    /// <summary>
    /// 通过反射自动连接空间里数据
    /// </summary>
    /// <param name="obj">对象</param>
    /// <param name="markStr">标记文字</param>
    public static void AutoLinkDataForChild<T>(T obj, string markStr) where T : BaseMonoBehaviour
    {
        Type trueType = obj.GetType();
        FieldInfo[] fields = trueType.GetFields();
        for (int i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            var fieldValue = field.GetValue(obj);
            if (fieldValue != null)
            {
                if (fieldValue.ToString().Equals("null"))
                {
                    //如果是一些特殊类 需要通过null字符来判断
                }
                else
                {
                    continue;
                }
            }
            if (!field.Name.Contains(markStr))
                continue;
            Component tmpCom = obj.GetComponentInChildren(field.Name.Replace(markStr, ""), field.FieldType, true);
            if (tmpCom == null)
            {
                //Debug.LogWarning("window " + trueType.Name + ",can not find：" + field.Name.Replace(markStr, ""));
                continue;
            }
            field.SetValue(obj, tmpCom);
        }
    }

    public static void AutoLinkData<T>(T obj, string markStr) where T : BaseMonoBehaviour
    {
        Type trueType = obj.GetType();
        FieldInfo[] fields = trueType.GetFields();
        for (int i = 0; i < fields.Length; i++)
        {
            var field = fields[i];
            if (field.GetValue(obj) != null)
                continue;
            if (!field.Name.Contains(markStr))
                continue;
            Component tmpCom = obj.Find(field.Name.Replace(markStr, ""), field.FieldType);
            if (tmpCom == null)
            {
                //Debug.LogWarning("window " + trueType.Name + ",can not find：" + field.Name.Replace(markStr, ""));
                continue;
            }
            field.SetValue(obj, tmpCom);
        }
    }

    /// <summary>
    /// 根据反射获取所有属性名称
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static List<string> GetAllName<T>()
    {
        List<string> listName = new List<string>();
        Type type = typeof(T);
        FieldInfo[] fieldInfos = type.GetFields();
        if (fieldInfos == null)
            return listName;

        int propertyInfoSize = fieldInfos.Length;
        for (int i = 0; i < propertyInfoSize; i++)
        {
            FieldInfo fieldInfo = fieldInfos[i];
            listName.Add(fieldInfo.Name);
        };
        return listName;
    }

    /// <summary>
    /// 根据反射获取所有属性名称及值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="classType"></param>
    /// <returns></returns>
    public static Dictionary<string, object> GetAllNameAndValue<T>(T classType)
    {
        Dictionary<string, object> listData = new Dictionary<string, object>();
        Type type = classType.GetType();
        FieldInfo[] fieldInfos = type.GetFields();

        if (fieldInfos == null)
            return listData;

        int propertyInfoSize = fieldInfos.Length;
        for (int i = 0; i < propertyInfoSize; i++)
        {
            FieldInfo fieldInfo = fieldInfos[i];
            listData.Add(fieldInfo.Name, fieldInfo.GetValue(classType));
        };
        return listData;
    }

    /// <summary>
    /// 获取所有的值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="classType"></param>
    /// <returns></returns>
    public static List<object> GetAllValue<T>(T classType,out List<string> allName)
    {
        List<object> listData = new List<object>();
        allName = new List<string>();
        Type type = classType.GetType();
        FieldInfo[] fieldInfos = type.GetFields();

        if (fieldInfos == null)
            return listData;

        int propertyInfoSize = fieldInfos.Length;
        for (int i = 0; i < propertyInfoSize; i++)
        {
            FieldInfo fieldInfo = fieldInfos[i];
            listData.Add(fieldInfo.GetValue(classType));
            allName.Add(fieldInfo.Name);
        };
        return listData;
    }

    /// <summary>
    /// 获取所有值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="classType"></param>
    /// <returns></returns>
    public static List<object> GetAllValue<T>(T classType)
    {
        List<object> listData = new List<object>();
        Type type = classType.GetType();
        FieldInfo[] fieldInfos = type.GetFields();

        if (fieldInfos == null)
            return listData;

        int propertyInfoSize = fieldInfos.Length;
        for (int i = 0; i < propertyInfoSize; i++)
        {
            FieldInfo fieldInfo = fieldInfos[i];
            listData.Add(fieldInfo.GetValue(classType));
        };
        return listData;
    }

    /// <summary>
    /// 根据反射获取属性名称及类型
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="classType"></param>
    /// <returns></returns>
    public static Dictionary<string, Type> GetAllNameAndType<T>(T classType)
    {
        Type type = classType.GetType();
        return GetAllNameAndType(type);
    }

    /// <summary>
    /// 根据反射获取基类属性名称及类型
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="classType"></param>
    /// <returns></returns>
    public static Dictionary<string, Type> GetAllNameAndTypeFromBase<T>(T classType)
    {
        Type baseType = classType.GetType().BaseType;
        return GetAllNameAndType(baseType);
    }

    /// <summary>
    /// 根据反射获取属性名称及类型
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static Dictionary<string, Type> GetAllNameAndType(Type type)
    {
        Dictionary<string, Type> listData = new Dictionary<string, Type>();
        FieldInfo[] fieldInfos = type.GetFields();

        if (fieldInfos == null)
            return listData;

        int propertyInfoSize = fieldInfos.Length;
        for (int i = 0; i < propertyInfoSize; i++)
        {
            FieldInfo fieldInfo = fieldInfos[i];
            listData.Add(fieldInfo.Name, fieldInfo.FieldType);
        };
        return listData;
    }

    /// <summary>
    /// 根据反射 设置值
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="classType"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public static void SetValueByName<T>(T classType, string name, object value)
    {
        Type type = classType.GetType();
        FieldInfo fieldInfo = type.GetField(name);
        if (fieldInfo == null)
            return;
        fieldInfo.SetValue(classType, value);
    }
    public static T SetValueByNameForStruct<T>(T classType, string name, object value)
    {
        object obj = classType;
        Type type = classType.GetType();
        FieldInfo fieldInfo = type.GetField(name);
        if (fieldInfo == null)
            return default(T);

        fieldInfo.SetValue(obj, value);
        return (T)obj;
    }

    /// <summary>
    /// 设置反射 设置值（性能开销更小 没有装箱开销）
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="classType"></param>
    /// <param name="name"></param>
    /// <param name="value"></param>
    public static void SetValueDirectByName<T>(T classType, string name, object value)
    {
        Type type = classType.GetType();
        FieldInfo fieldInfo = type.GetField(name);
        if (fieldInfo == null)
            return;
        fieldInfo.SetValueDirect(__makeref(classType), value);
    }

    /// <summary>
    /// 通过反射调用类的方法（SayHello(string name)）
    /// </summary>
    public static string GetInvokeMethod(GameObject gameObject, string componentName, string methodName, List<string> listParameter)
    {
        Component component = gameObject.GetComponent(componentName);
        // 1.Load(命名空间名称)，GetType(命名空间.类名)
        Type type = component.GetType();
        // Type type = Assembly.Load(className).GetType(className+"."+className);
        //2.GetMethod(需要调用的方法名称)
        MethodInfo method = type.GetMethod(methodName);
        // 3.调用的实例化方法（非静态方法）需要创建类型的一个实例
        //object obj = Activator.CreateInstance(type);
        //4.方法需要传入的参数
        //object[] parameters = new object[] { 1 };
        // 5.调用方法，如果调用的是一个静态方法，就不需要第3步（创建类型的实例）
        // 相应地调用静态方法时，Invoke的第一个参数为null
        object[] parameters;
        if (listParameter.IsNull())
        {
            parameters = new object[0];
        }
        else
        {
            parameters = new object[listParameter.Count];
            for (int i = 0; i < listParameter.Count; i++)
            {
                string itemData = listParameter[i];
                if (itemData.Equals("true"))
                {
                    parameters[i] = true;
                }
                else if (itemData.Equals("false"))
                {
                    parameters[i] = false;
                }
                else if (int.TryParse(itemData, out int outInt))
                {
                    parameters[i] = outInt;
                }
                else if (long.TryParse(itemData, out long outLong))
                {
                    parameters[i] = outLong;
                }
                else if (float.TryParse(itemData, out float outFloat))
                {
                    parameters[i] = outFloat;
                }
                else
                {
                    parameters[i] = itemData;
                }
            }
        }
        string result = (string)method.Invoke(component, parameters);
        return result;
    }

    /// <summary>
    /// 创建对象实例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="fullName">命名空间.类型名</param>
    /// <param name="assemblyName">程序集</param>
    /// <returns></returns>
    public static T CreateInstance<T>(string fullName, string assemblyName)
    {
        try
        {
            string path = fullName + "," + assemblyName;//命名空间.类型名,程序集
            Type o = Type.GetType(path);//加载类型
            object obj = Activator.CreateInstance(o, true);//根据类型创建实例
            return (T)obj;//类型转换并返回
        }
        catch (Exception e)
        {
            LogUtil.LogError("实例化失败，缺少 " + fullName + "," + assemblyName + " 。" + e.Message);
            return default(T);
        }
    }

    /// <summary>
    /// 创建对象实例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="fullName">命名空间.类型名</param>
    /// <param name="assemblyName">程序集</param>
    /// <returns></returns>
    public static T CreateInstance<T>(string className, object[] data = null)
    {
        try
        {
            Type o = Type.GetType(className);//加载类型
            object obj;//根据类型创建实例
            if (data.IsNull())
            {
                obj = Activator.CreateInstance(o);//根据类型创建实例
            }
            else
            {
                obj = Activator.CreateInstance(o, data);//根据类型创建实例
            }
            return (T)obj;//类型转换并返回
        }
        catch
        {
            return default(T);
        }
    }

    public static T CreateInstance<T>()
    {
        try
        {
            T obj = Activator.CreateInstance<T>();//根据类型创建实例
            return obj;//类型转换并返回
        }
        catch
        {
            return default(T);
        }
    }
}