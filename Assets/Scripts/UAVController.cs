using System.Collections.Generic;
using UnityEngine;

public class UAVController : MonoBehaviour
{
    [Header("Sürü İletişimi")]
    public SwarmManager swarmManager;
    public Transform targetToFind;

    [Header("Gelişmiş Tarama Ayarları")]
    public float scanRadius = 25f;
    public float flightSpeed = 15f;

    [Header("Harita Sınırları")]
    public float minX = -141.64f;
    public float maxX = 17f;
    public float minZ = -7.5f;
    public float maxZ = 150.31f;

    // Nizami devriye rotası için liste
    private List<Vector3> waypoints = new List<Vector3>();
    private int currentWaypointIndex = 0;
    private bool isTargetFound = false;

    // YENİ: Ping-Pong tarama yönü (1: İleri, -1: Geri)
    private int waypointStep = 1;

    void Start()
    {
        // Oyun başlar başlamaz zikzak rotasını hesapla
        GenerateSearchPattern();
    }

    void Update()
    {
        if (isTargetFound || waypoints.Count == 0) return;

        Vector3 targetWaypoint = waypoints[currentWaypointIndex];

        // 1. Hedefe doğru uç
        transform.position = Vector3.MoveTowards(transform.position, targetWaypoint, flightSpeed * Time.deltaTime);

        // 2. Drone'un burnunu çevir
        Vector3 direction = (targetWaypoint - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        // 3. Noktaya ulaştıysa sıradaki noktaya geç (YENİLENMİŞ PİNG-PONG MANTIĞI)
        if (Vector3.Distance(transform.position, targetWaypoint) < 2f)
        {
            currentWaypointIndex += waypointStep;

            // Listenin sonuna geldiyse yönü eksiye çevir ve tarayarak geri dön
            if (currentWaypointIndex >= waypoints.Count)
            {
                waypointStep = -1;
                currentWaypointIndex = waypoints.Count - 2;
            }
            // Geri döne döne en başa geldiyse, tekrar ileri doğru tara
            else if (currentWaypointIndex < 0)
            {
                waypointStep = 1;
                currentWaypointIndex = 1;
            }
        }

        // 4. Radarı çalıştır (2D Tarama)
        Vector3 flatDronePos = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 flatTargetPos = new Vector3(targetToFind.position.x, 0, targetToFind.position.z);

        if (Vector3.Distance(flatDronePos, flatTargetPos) <= scanRadius)
        {
            isTargetFound = true;
            Debug.Log("İHA (UAV): Kayıp araç tespit edildi! Koordinatlar İKA'lara iletiliyor...");
            swarmManager.OnTargetFound(targetToFind);
        }
    }

    void GenerateSearchPattern()
    {
        waypoints.Clear(); // Listeyi temizle
        float stepSize = scanRadius * 1.5f;

        // Drone haritanın üst kısmında mı yoksa alt kısmında mı?
        // Başlangıç konumuna göre ilk hareket yönünü belirliyoruz.
        bool startFromTop = (transform.position.z > (minZ + maxZ) / 2f);

        // Eğer drone üstteyse ilk şeridi yukarıdan aşağıya (movingUp = false) çizecek
        bool movingUp = !startFromTop;

        // X ekseninde şerit şerit ilerle
        for (float x = minX; x <= maxX; x += stepSize)
        {
            if (movingUp)
            {
                waypoints.Add(new Vector3(x, transform.position.y, minZ)); // Aşağıdan yukarıya uç
                waypoints.Add(new Vector3(x, transform.position.y, maxZ));
            }
            else
            {
                waypoints.Add(new Vector3(x, transform.position.y, maxZ)); // Yukarıdan aşağıya uç
                waypoints.Add(new Vector3(x, transform.position.y, minZ));
            }
            // Sonraki şeride geçerken yönü tersine çevir (Zikzak mantığı)
            movingUp = !movingUp;
        }
    }

    // YENİ: SwarmManager tarafından hedef ışınlandığında çağrılır
    public void ResumeSearch()
    {
        isTargetFound = false; // Aramaya devam et
        Debug.Log("İHA (UAV): Hedef hareket etti, tarama yeniden başlatılıyor...");
    }
}