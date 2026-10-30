using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class PlaneController : MonoBehaviour
{
    [Header("移動")]
    public float forwardSpeed = 30f; 
    public float moveSpeed = 60f;    
    public float minHeight = 10f;
    public float maxHeight = 90f;
    public float maxSide = 200f;      
    public float overshoot = 20f;     

    [Header("音效")]
    public AudioClip hitClip;    
    public AudioClip passClip;    
    public AudioClip finishClip;  
    public AudioClip dodgeClip;   
    public float dodgeDistance = 30f;  

    [Header("UI")]
    public GameObject clearPanel;   
    public GameObject failPanel;    
    public Text checkpointText;     

    Rigidbody rb;
    AudioSource sfx;
    float h, v;
    int passed = 0;
    int total = 0;
    bool finished = false;   
    bool failed = false;
    float endZ = float.MaxValue;
    float startX;

    // 閃過障礙物用
    List<Collider> obstacles;
    int nextObstacle = 0;
    HashSet<Collider> hitObstacles = new HashSet<Collider>();

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;  
        sfx = gameObject.AddComponent<AudioSource>();
        startX = rb.position.x;

        foreach (Checkpoint c in FindObjectsByType<Checkpoint>(FindObjectsSortMode.None))
        {
            if (!c.isFinish) total++;
            else endZ = c.GetComponent<Collider>().bounds.max.z + overshoot;  
        }

        if (clearPanel != null) clearPanel.SetActive(false);
        if (failPanel != null) failPanel.SetActive(false);
        UpdateCheckpointText();
    }

    void Update()
    {
        // 抵達終點後：按 R 或點螢幕重新開始
        if (finished)
        {
            bool tapped = Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
            if (Input.GetKeyDown(KeyCode.R) || tapped)
                Restart();
            return;
        }

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
        rb.linearVelocity = Vector3.zero;  // 撞到東西後不會被彈飛
        if (finished) return;

        Vector3 move = Vector3.forward * forwardSpeed + new Vector3(h, v, 0f) * moveSpeed;
        Vector3 next = rb.position + move * Time.fixedDeltaTime;
        next.y = Mathf.Clamp(next.y, minHeight, maxHeight);
        next.x = Mathf.Clamp(next.x, startX - maxSide, startX + maxSide);
        rb.MovePosition(next);

        CheckDodge();

       
        if (next.z > endZ)
        {
            finished = true;
            failed = true;
            if (hitClip != null) sfx.PlayOneShot(hitClip);
            if (failPanel != null) failPanel.SetActive(true);
        }
    }

    void CheckDodge()
    {
        if (obstacles == null)
        {
            
            obstacles = new List<Collider>();
            foreach (Collider col in FindObjectsByType<Collider>(FindObjectsSortMode.None))
                if (col.CompareTag("Obstacle") && !col.isTrigger) obstacles.Add(col);
            obstacles.Sort((a, b) => a.bounds.max.z.CompareTo(b.bounds.max.z));
        }

        bool dodged = false;
        while (nextObstacle < obstacles.Count && rb.position.z > obstacles[nextObstacle].bounds.max.z)
        {
            Collider col = obstacles[nextObstacle];
            nextObstacle++;
            if (col == null || hitObstacles.Contains(col)) continue;

            Vector3 closest = col.bounds.ClosestPoint(new Vector3(rb.position.x, rb.position.y, col.bounds.center.z));
            if (Vector2.Distance(new Vector2(closest.x, closest.y), new Vector2(rb.position.x, rb.position.y)) < dodgeDistance)
                dodged = true;
        }
        if (dodged)
            sfx.PlayOneShot(dodgeClip != null ? dodgeClip : passClip);
    }

    void OnCollisionEnter(Collision c)
    {
        if (!finished && c.collider.CompareTag("Obstacle"))
        {
            sfx.PlayOneShot(hitClip);
            hitObstacles.Add(c.collider);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        Checkpoint cp = other.GetComponentInParent<Checkpoint>();
        if (cp == null || cp.reached || finished) return;
        cp.reached = true;

        if (cp.isFinish)
        {
            finished = true;
            sfx.PlayOneShot(finishClip != null ? finishClip : passClip);
            if (clearPanel != null) clearPanel.SetActive(true);
        }
        else
        {
            passed++;
            sfx.PlayOneShot(passClip);
            UpdateCheckpointText();
        }
    }

    // 給「重新開始」按鈕用：Button 的 On Click 選 PlaneController > Restart
    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void UpdateCheckpointText()
    {
        if (checkpointText != null)
            checkpointText.text = "檢查站 " + passed + " / " + total;
    }

  
    void OnGUI()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = Screen.height / 20;
        style.normal.textColor = Color.white;
        if (checkpointText == null)
            GUI.Label(new Rect(20, 20, Screen.width, Screen.height / 8f), "檢查站 " + passed + " / " + total, style);

        if (finished && (failed ? failPanel : clearPanel) == null)
        {
            style.fontSize = Screen.height / 12;
            style.alignment = TextAnchor.MiddleCenter;
            string msg = failed ? "沒有抵達終點！" : "抵達終點！";
            GUI.Label(new Rect(0, 0, Screen.width, Screen.height), msg + "\n點螢幕重新開始", style);
        }
    }
}
