using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 残影(afterimage / 虚影拖尾)效果基类：框架层通用能力，封装「对象池 + 生成节奏 + 淡出 + 清理」的公共流程，
/// 子类只需按不同渲染类型实现少量差异（如何快照当前一帧、如何按淡出系数染色、如何回收）。
///
/// 通用用法：
///   1) 把某个子类组件挂到目标物体上；
///   2) 调子类的 Init(源渲染器) 绑定；
///   3) 需要留残影时 StartSpawn(count, duration)（数量随业务决定，均匀铺满 duration）；
///   4) 结束 StopSpawn()（已生成的各自淡出后回池）；
///   5) 挂起/切场景时 ClearAll() 统一销毁池对象。
///
/// 池化语义：残影淡出后回收进空闲池复用（不反复 Instantiate/Destroy），仅 ClearAll / 组件销毁时才真正删除。
///
/// 命名约定：本目录(Component/Other)下同类通用件用「类别前缀在前」命名，残影族统一 AfterimageGhost* 归类。
/// </summary>
public abstract class AfterimageGhostBase : MonoBehaviour
{
    #region 可调参数

    [Header("残影通用参数")]
    /// <summary>单个残影淡出时长(秒)</summary>
    public float ghostLifetime = 0.25f;
    /// <summary>残影染色(满不透明时的颜色，子类按淡出系数 t 处理透明/淡出)</summary>
    public Color ghostTint = new Color(0.6f, 0.75f, 1f, 0.55f);

    #endregion

    #region 池与状态

    /// <summary>生成间隔(秒) = duration / count，由 StartSpawn 计算，把 count 个残影均匀铺满</summary>
    protected float spawnInterval;
    /// <summary>本次剩余待生成的残影数</summary>
    protected int spawnRemaining;
    /// <summary>生成计时</summary>
    protected float spawnTimer;
    /// <summary>是否正在持续生成</summary>
    protected bool spawning;

    /// <summary>正在淡出的活跃残影</summary>
    protected readonly List<GhostItem> listActive = new List<GhostItem>();
    /// <summary>空闲残影池(淡出后回收复用,不销毁)</summary>
    protected readonly Queue<GhostItem> poolIdle = new Queue<GhostItem>();
    /// <summary>所有残影(活跃+空闲),用于统一销毁</summary>
    protected readonly List<GhostItem> listAll = new List<GhostItem>();

    /// <summary>
    /// 单个残影运行时数据基类：只含所有类型通用的字段；各子类派生并附加自己的渲染组件引用。
    /// </summary>
    protected abstract class GhostItem
    {
        /// <summary>残影 GameObject</summary>
        public GameObject go;
        /// <summary>已存活时长(秒)</summary>
        public float age;
    }

    #endregion

    #region 对外接口

    /// <summary>
    /// 开始生成残影：按数量均匀铺满整个时长（数量由调用方按业务决定，如随等级 1级3个/3级9个）
    /// </summary>
    /// <param name="count">本次要生成的残影总数</param>
    /// <param name="duration">铺开时长(秒)，生成间隔 = duration / count</param>
    public void StartSpawn(int count, float duration)
    {
        if (count <= 0 || !CanCapture())
        {
            return;
        }
        spawnRemaining = count;
        spawnInterval = duration / count;
        spawnTimer = 0;
        spawning = true;
        //立即先留一帧(计入数量)
        SpawnOne();
        spawnRemaining--;
        if (spawnRemaining <= 0)
        {
            spawning = false;
        }
    }

    /// <summary>
    /// 停止新增残影（已生成的各自继续淡出后回池）
    /// </summary>
    public void StopSpawn()
    {
        spawning = false;
    }

    /// <summary>
    /// 清空并销毁所有残影（挂起/切场景时调用；平时复用期间不删除）
    /// </summary>
    public void ClearAll()
    {
        for (int i = 0; i < listAll.Count; i++)
        {
            DestroyGhost(listAll[i]);
        }
        listAll.Clear();
        listActive.Clear();
        poolIdle.Clear();
    }

    #endregion

    #region 生命周期

    /// <summary>
    /// 逐帧推进：按间隔生成，并把活跃残影按寿命淡出、到期回池
    /// </summary>
    protected virtual void Update()
    {
        //按数量持续生成(carry 累计余量,尽量凑满目标数量)
        if (spawning && spawnRemaining > 0)
        {
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer -= spawnInterval;
                SpawnOne();
                spawnRemaining--;
                if (spawnRemaining <= 0)
                {
                    spawning = false;
                }
            }
        }
        //淡出与回池(倒序遍历便于移除)
        for (int i = listActive.Count - 1; i >= 0; i--)
        {
            var item = listActive[i];
            //残影对象被外部销毁(极端情况):移除,避免空引用
            if (item.go == null)
            {
                listActive.RemoveAt(i);
                continue;
            }
            item.age += Time.deltaTime;
            float t = 1f - item.age / ghostLifetime;
            if (t <= 0f)
            {
                Recycle(item);
                listActive.RemoveAt(i);
                continue;
            }
            ApplyFade(item, t);
        }
    }

    /// <summary>
    /// 组件销毁(切场景销毁本体等)时兜底清理所有残影，防止克隆的 Mesh/材质 泄漏
    /// </summary>
    protected virtual void OnDestroy()
    {
        ClearAll();
    }

    #endregion

    #region 内部逻辑

    /// <summary>
    /// 生成一个残影：优先复用空闲池对象，快照本体当前帧，压后展示等待淡出
    /// </summary>
    protected void SpawnOne()
    {
        if (!CanCapture())
        {
            return;
        }
        GhostItem item = poolIdle.Count > 0 ? poolIdle.Dequeue() : CreateAndRegister();
        //子类把本体当前一帧快照进该残影
        CaptureInto(item);
        //初始满不透明染色(避免首帧闪原色)
        ApplyFade(item, 1f);
        item.age = 0;
        item.go.SetActive(true);
        listActive.Add(item);
    }

    /// <summary>
    /// 新建残影对象并登记进总表(初始隐藏)
    /// </summary>
    /// <returns>新建的残影数据</returns>
    protected GhostItem CreateAndRegister()
    {
        var item = CreateGhost();
        item.go.SetActive(false);
        listAll.Add(item);
        return item;
    }

    /// <summary>
    /// 回收残影到空闲池(隐藏复用,不销毁)
    /// </summary>
    /// <param name="item">残影数据</param>
    protected void Recycle(GhostItem item)
    {
        item.go.SetActive(false);
        poolIdle.Enqueue(item);
    }

    #endregion

    #region 子类实现(不同渲染类型差异)

    /// <summary>
    /// 源是否就绪、可以快照（子类校验自己绑定的渲染器）
    /// </summary>
    /// <returns>true=可生成残影</returns>
    protected abstract bool CanCapture();

    /// <summary>
    /// 新建一个残影池对象：创建 GameObject 与该类型所需的渲染组件，返回派生的 GhostItem
    /// </summary>
    /// <returns>新建的残影数据</returns>
    protected abstract GhostItem CreateGhost();

    /// <summary>
    /// 把源当前一帧快照进该残影对象（几何/贴图/材质/位置/排序等）
    /// </summary>
    /// <param name="item">目标残影数据</param>
    protected abstract void CaptureInto(GhostItem item);

    /// <summary>
    /// 按淡出系数 t（1→0）给残影染色/降透明
    /// </summary>
    /// <param name="item">目标残影数据</param>
    /// <param name="t">淡出系数(1=刚生成,0=消失)</param>
    protected abstract void ApplyFade(GhostItem item, float t);

    /// <summary>
    /// 真正销毁残影对象及其子类持有的资源（Mesh/材质等）
    /// </summary>
    /// <param name="item">目标残影数据</param>
    protected abstract void DestroyGhost(GhostItem item);

    #endregion
}
