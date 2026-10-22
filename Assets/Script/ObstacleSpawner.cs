using UnityEngine;

// 遊戲開始時隨機擺障礙物：不會跟其他有 Collider 的物件重疊，也不會太靠近出生點。
public class ObstacleSpawner : MonoBehaviour
{
    [Header("要隨機擺的障礙物（從 Project 拖模型進來）")]
    public GameObject[] groundPrefabs;    // 放在地上的，例如石頭
    public GameObject[] floatingPrefabs;  // 浮在空中的，例如隕石
    public int groundCount = 15;
    public int floatingCount = 10;

    [Header("範圍")]
    public Transform spawnPoint;          // 把飛機拖進來
    public float safeRadius = 30f;        // 出生點附近多遠內不放
    public float minX = -20f, maxX = 20f;
    public float minZ = 20f, maxZ = 250f; // maxZ 設在終點 hangar 前面
    public float floatMinHeight = 4f, floatMaxHeight = 16f;
    public float gap = 3f;                // 和其他物件至少隔多遠

    [Header("大小：每個障礙物會在這兩個倍數之間隨機放大縮小")]
    public float minScale = 2f;
    public float maxScale = 4f;

    [Header("0 = 每次都不一樣；其他數字 = 固定排法")]
    public int seed = 0;

    void Start()
    {
        if (seed != 0) Random.InitState(seed);
        Physics.SyncTransforms();
        Spawn(groundPrefabs, groundCount, false);
        Spawn(floatingPrefabs, floatingCount, true);
    }

    void Spawn(GameObject[] prefabs, int count, bool floating)
    {
        if (prefabs == null || prefabs.Length == 0) return;

        int placed = 0;
        for (int attempt = 0; attempt < count * 30 && placed < count; attempt++)
        {
            Vector3 p = new Vector3(Random.Range(minX, maxX), 0f, Random.Range(minZ, maxZ));

            // 太靠近出生點就換位置
            if (spawnPoint != null)
            {
                Vector3 s = spawnPoint.position;
                if (Vector2.Distance(new Vector2(p.x, p.z), new Vector2(s.x, s.z)) < safeRadius) continue;
            }

            GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
            Quaternion rot = floating ? Random.rotation : Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GameObject go = Instantiate(prefab, p, rot, transform);
            go.transform.localScale *= Random.Range(minScale, maxScale);

            // 地上的：底部貼地；空中的：浮在地面上方隨機高度
            Bounds b = GetBounds(go);
            float ground = GroundHeight(p);
            float shift = floating
                ? ground + Random.Range(floatMinHeight, floatMaxHeight) - b.center.y
                : ground - b.min.y;
            go.transform.position += Vector3.up * shift;
            b.center += Vector3.up * shift;

            // 檢查有沒有撞到其他東西（地形不算）
            bool blocked = false;
            foreach (Collider c in Physics.OverlapBox(b.center, b.extents + Vector3.one * gap,
                         Quaternion.identity, ~0, QueryTriggerInteraction.Collide))
            {
                if (c is TerrainCollider || c.transform.IsChildOf(go.transform)) continue;
                blocked = true;
                break;
            }
            if (blocked)
            {
                DestroyImmediate(go);
                continue;
            }

            MakeObstacle(go);
            Physics.SyncTransforms();
            placed++;
        }

        if (placed < count)
            Debug.LogWarning("ObstacleSpawner：空間不夠，只放了 " + placed + " / " + count + " 個。可以把範圍加大或數量調少。");
    }

    float GroundHeight(Vector3 p)
    {
        Terrain t = Terrain.activeTerrain;
        if (t == null) return 0f;
        return t.SampleHeight(p) + t.transform.position.y;
    }

    static Bounds GetBounds(GameObject go)
    {
        Renderer[] rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.one);
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    static void MakeObstacle(GameObject go)
    {
        go.tag = "Obstacle";
        foreach (MeshFilter mf in go.GetComponentsInChildren<MeshFilter>())
        {
            mf.gameObject.tag = "Obstacle";
            if (mf.GetComponent<Collider>() == null)
                mf.gameObject.AddComponent<MeshCollider>();
        }
    }
}
