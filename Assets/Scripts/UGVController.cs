using UnityEngine;
using UnityEngine.AI;

public class UGVController : MonoBehaviour
{
    public Transform targetNode;
    private NavMeshAgent agent;

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
        bidLine.material = new Material(Shader.Find("Sprites/Default")); // Basit renklendirilebilir materyal
        bidLine.enabled = false;
    }

    void Update()
    {
        // 1. HEDEF KONTROLÜ: Araç sadece ihaleyi kazanıp bir hedefe (targetNode) yönlendirildiğinde hareket ve kaçınma yapsın.
        if (targetNode != null && agent != null && agent.isOnNavMesh)
        {
            // 2. GÜVENLİK KATMANI: Sadece hareket halindeyken (idle değilken) Boids çalışsın
            ApplySeparationBehavior();

            agent.SetDestination(targetNode.position);
        }
    }

    /// <summary>
    /// Multi-Robot Task Allocation (MRTA) - Contract Net Protocol kapsamında aracın hedefe olan gerçek maliyetini hesaplar.
    /// </summary>
    public float CalculatePathCost(Vector3 targetPosition)
    {
        // 1. GÜVENLİK KONTROLÜ: Ajan hazır değilse veya NavMesh üzerinde spawn olmamışsa ihaleye giremez
        if (agent == null || !agent.isOnNavMesh) return Mathf.Infinity;

        NavMeshPath path = new NavMeshPath();

        // 2. KUSURSUZ YOL KONTROLÜ: Yol hesaplanabiliyorsa VE hedef yarım/kısmi değil, tam ulaşılabilirse (PathComplete)
        if (agent.CalculatePath(targetPosition, path) && path.status == NavMeshPathStatus.PathComplete)
        {
            float totalDistance = 0f;
            // Kuş uçuşu mesafe yanıltıcıdır. Binaların etrafından dönen gerçek sokak mesafesini (Path Length) buluyoruz.
            for (int i = 0; i < path.corners.Length - 1; i++)
            {
                totalDistance += Vector3.Distance(path.corners[i], path.corners[i + 1]);
            }
            return totalDistance;
        }

        // Yol kopuksa, binaların içindeyse veya ulaşılamıyorsa maliyeti sonsuz yap.
        return Mathf.Infinity;
    }

    /// <summary>
    /// Reynolds Boids - Separation (Ayrılma) Davranışı:
    /// Bu sistem, NavMeshAgent'ın mevcut 'Avoidance Priority' (49/50) altyapısı ile ÇAKIŞMAZ.
    /// Tam aksine, onu TAMAMLAYAN reaktif bir yerel güvenlik katmanıdır (Local Reactive Layer).
    /// İki araç separationDistance eşiğinin altına girdiğinde, path iptal edilmez; 
    /// sadece aracın anlık hız vektörüne (agent.velocity) zıt yönde, organik bir "itme/kaydırma" gücü (repulsion) uygulanır.
    /// </summary>
    private void ApplySeparationBehavior()
    {
        // YENİ: Hedefe çok yaklaştıysak (park ediyorsak) Boids'i devre dışı bırak ki titreme (jitter) olmasın
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
            // Kendini hesaba katma ve diğer ajan yok edilmişse es geç
            if (otherUGV == this || otherUGV == null) continue;

            float distance = Vector3.Distance(transform.position, otherUGV.transform.position);

            // Eğer diğer araç kişisel alanımıza (separationDistance) girdiyse
            if (distance < separationDistance)
            {
                // Ondan uzaklaşacak ters bir vektör yönü bul
                Vector3 pushAwayDir = transform.position - otherUGV.transform.position;

                // Araç ne kadar yakınsa, itiş gücü o kadar şiddetli olur (ters orantı)
                separationVector += pushAwayDir.normalized / (distance + 0.1f);
                agentCount++;
            }
        }

        // Eğer yakınımızda araç(lar) varsa, hesaplanan itme vektörünü NavMesh'in fiziksel hızına ekle
        if (agentCount > 0)
        {
            // Velocity manipülasyonu, aracın rotasını (SetDestination) bozmadan sadece fiziksel olarak onu biraz yana kaydırır.
            agent.velocity += separationVector * separationForce * Time.deltaTime;
        }
    }
    public void DrawBidLine(Vector3 targetPos)
    {
        if (bidLine == null) return;
        bidLine.enabled = true;
        bidLine.SetPosition(0, transform.position);
        bidLine.SetPosition(1, targetPos);
        bidLine.startColor = Color.yellow; // Teklif aşamasında sarı
        bidLine.endColor = Color.yellow;
    }

    public void SetWinnerLine(bool isWinner)
    {
        if (bidLine == null) return;
        if (isWinner)
        {
            bidLine.startColor = Color.green; // Kazanan yeşil olur
            bidLine.endColor = Color.green;
            bidLine.startWidth = 0.6f; // Çizgi kalınlaşır
        }
        else
        {
            bidLine.enabled = false; // Kaybedenin çizgisi silinir
        }
    }
}