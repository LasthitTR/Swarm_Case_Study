using UnityEngine;
using UnityEngine.AI;

public class UGVController : MonoBehaviour
{
    public Transform targetNode;

    // HATA ÇÖZÜMÜ BURASI: private yerine public yapıldı!
    public NavMeshAgent agent;

    [Header("Reynolds Boids - Separation Katmanı")]
    public float separationDistance = 6f; // Etkileşim (itme) eşik mesafesi
    public float separationForce = 3f;    // İtme şiddeti
    private UGVController[] allUGVs;      // Sahnedeki diğer araçları tanımak için
    private LineRenderer bidLine;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        // Unity 6 uyumluluğu: Sarı uyarı (deprecated) almamak için güncel metod
        allUGVs = FindObjectsByType<UGVController>(FindObjectsSortMode.None);

        // Otomatik LineRenderer Kurulumu (İhale Çizgileri İçin)
        bidLine = gameObject.GetComponent<LineRenderer>();
        if (bidLine == null) bidLine = gameObject.AddComponent<LineRenderer>();

        bidLine.startWidth = 0.3f;
        bidLine.endWidth = 0.3f;
        bidLine.material = new Material(Shader.Find("Sprites/Default"));
        bidLine.enabled = false;
    }

    void Update()
    {
        if (targetNode != null && agent != null && agent.isOnNavMesh)
        {
            ApplySeparationBehavior();

            // HATA ÇÖZÜMÜ: Sadece hedef yer değiştirdiyse SetDestination çalışsın
            if (Vector3.Distance(agent.destination, targetNode.position) > 1f)
            {
                agent.SetDestination(targetNode.position);
            }
        }
    }

    /// <summary>
    /// Multi-Robot Task Allocation (MRTA) - Contract Net Protocol kapsamında aracın hedefe olan gerçek maliyetini hesaplar.
    /// </summary>
    public float CalculatePathCost(Vector3 targetPosition)
    {
        if (agent == null || !agent.isOnNavMesh) return Mathf.Infinity;

        NavMeshPath path = new NavMeshPath();

        if (agent.CalculatePath(targetPosition, path) && path.status == NavMeshPathStatus.PathComplete)
        {
            float totalDistance = 0f;
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                totalDistance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            }
            return totalDistance;
        }

        return Mathf.Infinity;
    }

    /// <summary>
    /// Reynolds Boids - Separation (Ayrılma) Davranışı
    /// </summary>
    private void ApplySeparationBehavior()
    {
        if (targetNode != null && agent != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, targetNode.position);
            // Durma mesafesine 2 birim kala itiş-kakışı bırak ve nizami park et
            if (distanceToTarget <= agent.stoppingDistance + 2f) return;
        }

        Vector3 separationVector = Vector3.zero;
        int agentCount = 0;

        foreach (var otherUGV in allUGVs)
        {
            if (otherUGV == this || otherUGV == null) continue;

            float distance = Vector3.Distance(transform.position, otherUGV.transform.position);

            if (distance < separationDistance)
            {
                Vector3 pushAwayDir = transform.position - otherUGV.transform.position;
                separationVector += pushAwayDir.normalized / (distance + 0.1f);
                agentCount++;
            }
        }

        if (agentCount > 0)
        {
            agent.velocity += separationVector * separationForce * Time.deltaTime;
        }
    }

    public void DrawBidLine(Vector3 targetPos)
    {
        if (bidLine == null) return;
        bidLine.enabled = true;
        bidLine.SetPosition(0, transform.position);
        bidLine.SetPosition(1, targetPos);
        bidLine.startColor = Color.yellow;
        bidLine.endColor = Color.yellow;
    }

    public void SetWinnerLine(bool isWinner)
    {
        if (bidLine == null) return;
        if (isWinner)
        {
            bidLine.startColor = Color.green;
            bidLine.endColor = Color.green;
            bidLine.startWidth = 0.6f;
        }
        else
        {
            bidLine.enabled = false;
        }
    }
}