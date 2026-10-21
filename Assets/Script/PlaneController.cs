using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlaneController : MonoBehaviour
{
    [Header("移動")]
    public float forwardSpeed = 15f;  // 往前飛的速度
    public float moveSpeed = 10f;     // 上下左右的速度
    public float minHeight = 1f;
    public float maxHeight = 20f;

    [Header("音效")]
    public AudioClip hitClip;   // 撞到障礙物
    public AudioClip passClip;  // 順利通過

    Rigidbody rb;
    AudioSource sfx;
    float h, v;

    // 自動判斷「通過」：記下每個障礙物的 Z 位置
    List<float> obstacleZ = new List<float>();
    int nextIndex = 0;
    bool hitThisRow = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        sfx = gameObject.AddComponent<AudioSource>();

        foreach (GameObject o in GameObject.FindGameObjectsWithTag("Obstacle"))
            obstacleZ.Add(o.transform.position.z);
        obstacleZ.Sort();
    }

    void Update()
    {
        // 電腦：方向鍵 / WASD
        h = Input.GetAxis("Horizontal");
        v = Input.GetAxis("Vertical");

        // 手機：手指按在螢幕哪裡，就往那個方向飛
        if (Input.touchCount > 0)
        {
            Vector2 p = Input.GetTouch(0).position;
            h = Mathf.Clamp((p.x / Screen.width - 0.5f) * 2f, -1f, 1f);
            v = Mathf.Clamp((p.y / Screen.height - 0.5f) * 2f, -1f, 1f);
        }
    }

    void FixedUpdate()
    {
        Vector3 move = Vector3.forward * forwardSpeed + new Vector3(h, v, 0f) * moveSpeed;
        Vector3 next = rb.position + move * Time.fixedDeltaTime;
        next.y = Mathf.Clamp(next.y, minHeight, maxHeight);
        rb.MovePosition(next);

        // 飛過下一排障礙物，而且沒撞到 → 播通過音效
        if (nextIndex < obstacleZ.Count && rb.position.z > obstacleZ[nextIndex] + 2f)
        {
            if (!hitThisRow)
                sfx.PlayOneShot(passClip);
            hitThisRow = false;

            // Z 差不到 3 的障礙物算同一排，一起跳過
            float rowZ = obstacleZ[nextIndex];
            while (nextIndex < obstacleZ.Count && obstacleZ[nextIndex] < rowZ + 3f)
                nextIndex++;
        }
    }

    void OnCollisionEnter(Collision c)
    {
        if (c.collider.CompareTag("Obstacle"))
        {
            sfx.PlayOneShot(hitClip);
            hitThisRow = true;
        }
    }
}