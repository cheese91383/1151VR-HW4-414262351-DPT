using UnityEngine;

// 掛在 corridor（檢查站）或 hangar（終點，勾 Is Finish）上。
// 遊戲開始時會自動產生一個包住整個模型的觸發範圍，飛機穿過去就算通過。
public class Checkpoint : MonoBehaviour
{
    public bool isFinish = false;
    [HideInInspector] public bool reached = false;

    void Awake()
    {
        Bounds b = new Bounds();
        bool first = true;
        foreach (MeshFilter mf in GetComponentsInChildren<MeshFilter>())
        {
            if (mf.sharedMesh == null) continue;
            Bounds m = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = m.center + Vector3.Scale(m.extents, new Vector3(
                    (i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                Vector3 local = transform.InverseTransformPoint(mf.transform.TransformPoint(corner));
                if (first) { b = new Bounds(local, Vector3.zero); first = false; }
                else b.Encapsulate(local);
            }
        }

        BoxCollider box = gameObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = b.center;
        box.size = b.size;
    }
}
