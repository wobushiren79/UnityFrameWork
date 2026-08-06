using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class AIBaseEntity : BaseEvent
{
    //意图列表
    public List<AIIntentEnum> listIntentEnum = new List<AIIntentEnum>();

    //当前意图
    public AIBaseIntent currentIntent;
    public AIIntentEnum currentIntentEnum;

    //意图池
    public Dictionary<AIIntentEnum, AIBaseIntent> dicIntentPool = new Dictionary<AIIntentEnum, AIBaseIntent>();

    //意图工厂注册表（避免反射 + 字符串拼接类名导致的运行时静默失败）
    private static readonly Dictionary<AIIntentEnum, Func<AIBaseIntent>> dicIntentFactory = new Dictionary<AIIntentEnum, Func<AIBaseIntent>>();

    /// <summary>
    /// 注册意图工厂
    /// </summary>
    public static void RegisterIntentFactory(AIIntentEnum intentEnum, Func<AIBaseIntent> factory)
    {
        dicIntentFactory[intentEnum] = factory;
    }

    /// <summary>
    /// 通过工厂创建意图实例（找不到时返回 null）
    /// </summary>
    private static AIBaseIntent CreateIntentByFactory(AIIntentEnum intentEnum)
    {
        if (dicIntentFactory.TryGetValue(intentEnum, out var factory))
        {
            return factory();
        }
        return null;
    }

    /// <summary>
    /// 初始化数据
    /// </summary>
    public virtual void InitData()
    {
        InitIntentEntity();
    }
    
    public virtual void Update()
    {
        if (currentIntent != null)
        {
            currentIntent.IntentUpdate(this);
        }
    }

    public virtual void FixedUpdate()
    {
        if (currentIntent != null)
        {
            currentIntent.IntentFixUpdate(this);
        }
    }

    /// <summary>
    /// 增加意图
    /// </summary>
    public void AddIntent(AIBaseIntent intent)
    {
        if (!dicIntentPool.ContainsKey(intent.aiIntent))
        {
            dicIntentPool.Add(intent.aiIntent, intent);
        }
    }

    /// <summary>
    /// 获取意图
    /// </summary>
    public T GetIntent<T>(AIIntentEnum aIIntent) where T : AIBaseIntent
    {
        if (dicIntentPool.TryGetValue(aIIntent, out AIBaseIntent value))
        {
            return value as T;
        }
        return null;
    }

    /// <summary>
    /// 移除意图
    /// </summary>
    public void RemoveIntent(AIBaseIntent intent)
    {
        if (dicIntentPool.ContainsKey(intent.aiIntent))
        {
            dicIntentPool.Remove(intent.aiIntent);
        }
    }

    /// <summary>
    /// 改变意图
    /// </summary>
    /// <param name="aiIntent"></param>
    public AIBaseIntent ChangeIntent(AIIntentEnum aiIntent)
    {
        if (dicIntentPool.IsNull())
        {
            LogUtil.LogError("转换AI意图" + aiIntent.ToString() + "失败，还没有初始化相关AI意图");
            return currentIntent;
        }
        if (currentIntent != null)
        {
            currentIntent.IntentLeaving(this);
        }
        AIBaseIntent changeIntent = GetIntent<AIBaseIntent>(aiIntent);
        if (changeIntent == null)
        {
            LogUtil.LogError("转换AI意图" + aiIntent.ToString() + "失败，意图池里没有此意图");
            return currentIntent;
        }
        currentIntentEnum = aiIntent;
        currentIntent = changeIntent;
        currentIntent.IntentEntering(this);
        return currentIntent;
    }

    /// <summary>
    /// 初始化意图
    /// </summary>
    /// <typeparam name="I"></typeparam>
    public virtual void InitIntentEntity()
    {
        listIntentEnum.Clear();
        dicIntentPool.Clear();

        InitIntentEnum(listIntentEnum);
        for (int i = 0; i < listIntentEnum.Count; i++)
        {
            AIIntentEnum itemIntent = listIntentEnum[i];
            //首先获取类池里面是否有这个意图
            if (!dicIntentPool.TryGetValue(itemIntent, out AIBaseIntent intentClass))
            {
                //优先使用工厂注册表创建（编译期保证类型存在，避免反射 + 字符串拼接类名导致的运行时静默失败）
                intentClass = CreateIntentByFactory(itemIntent);
                //兜底反射，保留旧的行为以兼容未注册的扩展意图
                if (intentClass == null)
                {
                    string intentName = itemIntent.GetEnumName();
                    string className = $"AIIntent{intentName}";
                    intentClass = ReflexUtil.CreateInstance<AIBaseIntent>(className);
                    if (intentClass == null)
                    {
                        LogUtil.LogError($"创建AI意图失败：未在工厂中注册且反射也未找到类 {className}（枚举 {itemIntent}）");
                        continue;
                    }
                }
            }
            intentClass.InitData(itemIntent, this);
            AddIntent(intentClass);
        }
    }

    /// <summary>
    /// 清理数据
    /// </summary>
    public virtual void ClearData()
    {
        UnRegisterAllEvent();
    }

    /// <summary>
    /// 启动AI实例
    /// </summary>
    public abstract void StartAIEntity();

    /// <summary>
    /// 关闭AI实例
    /// </summary>
    public abstract void CloseAIEntity();

    /// <summary>
    /// 初始化所有意图
    /// </summary>
    public abstract void InitIntentEnum(List<AIIntentEnum> listIntentEnum);
}