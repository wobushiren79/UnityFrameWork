using UnityEditor;
using UnityEngine;
public class CreateToolsForPlanetTextureBean
{
    public int size = 32;
    public int seed;
    public Color deepOcean = new Color(0.05f, 0.15f, 0.4f);
    public Color shallowOcean = new Color(0.2f, 0.4f, 0.8f);
    public Color coast = new Color(0.6f, 0.7f, 0.3f);
    public Color land = new Color(0.3f, 0.6f, 0.2f);
    public Color mountain = new Color(0.4f, 0.3f, 0.2f);
    public Color cloud = new Color(1f, 1f, 1f, 0.7f);
    //是否有光照
    public bool isLight = true;
    // 大陆形状
    public float scale1_HeightMapScale = 0.2f;
    // 细节噪声
    public float scale2_HeightMapScale = 0.01f;
    public CreateToolsForPlanetTextureBean(int seed)
    {
        this.seed = seed;
        Random.InitState(seed);
        
        // 生成随机颜色组合
        deepOcean = RandomColor(0.05f, 0.15f, 0.1f, 0.2f, 0.3f, 0.5f);
        shallowOcean = RandomColor(0.1f, 0.3f, 0.3f, 0.6f, 0.6f, 0.9f);
        coast = RandomColor(0.5f, 0.7f, 0.6f, 0.8f, 0.2f, 0.4f);
        
        // 陆地颜色分支：50%概率绿色系，50%概率棕色系
        land = Random.value > 0.5f ? 
            RandomColor(0.2f, 0.4f, 0.5f, 0.8f, 0.1f, 0.3f) : 
            RandomColor(0.4f, 0.6f, 0.3f, 0.5f, 0.1f, 0.2f);
        
        mountain = RandomColor(0.3f, 0.5f, 0.2f, 0.4f, 0.1f, 0.3f);
        cloud = RandomColor(0.9f, 1f, 0.9f, 1f, 0.9f, 1f, 0.6f, 0.8f);
    }

        // 颜色生成方法
    private static Color RandomColor(float rMin, float rMax, 
                                    float gMin, float gMax,
                                    float bMin, float bMax,
                                    float aMin = 1f, float aMax = 1f)
    {
        return new Color(
            Random.Range(rMin, rMax),
            Random.Range(gMin, gMax),
            Random.Range(bMin, bMax),
            Random.Range(aMin, aMax)
        );
    }
}
public static class CreateTools
{


    /// <summary>
    /// 生成星球图片
    /// </summary>
    public static Texture2D CreatePlanetTexture(CreateToolsForPlanetTextureBean createData)
    {
        int seed = createData.seed;
        int size = createData.size;
        Random.InitState(seed); // 初始化随机种子
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        // 修正中心坐标计算（关键修改点）
        Vector2 center = new Vector2((size - 1) / 2f, (size - 1) / 2f); // 精确浮点中心
        float radius = (size - 1) / 2f; // 精确半径

        // 生成带随机种子的地形蒙版
        float[,] heightMap = GenerateHeightMap(size, seed, createData.scale1_HeightMapScale, createData.scale2_HeightMapScale);

        // 颜色配置
        Color deepOcean = createData.deepOcean;
        Color shallowOcean = createData.shallowOcean;
        Color coast = createData.coast;
        Color land = createData.land;
        Color mountain = createData.mountain;
        Color cloud = createData.cloud;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 pos = new Vector2(x, y);
                float dist = Vector2.Distance(pos, center);

                // 基础圆形检测
                if (dist > radius + 0.5f)
                {
                    tex.SetPixel(x, y, Color.clear);
                    continue;
                }

                // 获取高度值
                float height = heightMap[x, y];
                float depth = 1 - (dist / radius); // 中心到边缘的深度

                // 确定基础地形颜色
                Color baseColor;
                if (height < 0.2f) // 深海
                {
                    baseColor = Color.Lerp(deepOcean, shallowOcean, height / 0.3f);
                }
                else if (height < 0.25f) // 浅海
                {
                    baseColor = shallowOcean;
                }
                else if (height < 0.3f) // 海岸线
                {
                    baseColor = coast;
                }
                else if (height < 0.8f) // 陆地
                {
                    baseColor = Color.Lerp(land, mountain, (height - 0.4f) / 0.3f);
                }
                else // 山脉
                {
                    baseColor = mountain;
                }

                // 添加光照效果
                if (createData.isLight)
                {
                    Vector2 lightDir = new Vector2(0.8f, 0.6f).normalized;
                    float dot = Vector2.Dot((pos - center).normalized, lightDir);
                    baseColor *= 0.8f + Mathf.Clamp01(dot) * 0.4f;
                }


                // 添加深度衰减
                baseColor *= 0.9f + depth * 0.2f;

                // 添加随机云层
                if (Random.value < 0.3f && height > 0.4f)
                {
                    baseColor = Color.Lerp(baseColor, cloud, 0.4f);
                }

                tex.SetPixel(x, y, baseColor);
            }
        }

        tex.Apply();
        return tex;
    }

    /// <summary>
    /// 生成高度地图
    /// </summary>
    /// <param name="size"></param>
    /// <param name="seed"></param>
    /// <param name="scale1">大陆形状</param>
    /// <param name="scale2">细节噪声</param>
    /// <returns></returns>
    public static float[,] GenerateHeightMap(int size, int seed, float scale1, float scale2)
    {
        float[,] map = new float[size, size];
        // 初始化随机种子
        Random.InitState(seed);
        float offsetX = Random.Range(0f, 100f);
        float offsetY = Random.Range(0f, 100f);

        // 生成基础大陆形状
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = offsetX + x * scale1;
                float ny = offsetY + y * scale1;
                float baseHeight = Mathf.PerlinNoise(nx, ny) * 0.8f;

                // 添加细节噪声
                float detail = Mathf.PerlinNoise(offsetX + x * scale2, offsetY + y * scale2) * 0.2f;
                map[x, y] = Mathf.Clamp01(baseHeight + detail);
            }
        }

        // 应用圆形遮罩
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float mask = Mathf.Clamp01(1 - (dist / radius));
                map[x, y] *= mask;
            }
        }

        return map;
    }
}
