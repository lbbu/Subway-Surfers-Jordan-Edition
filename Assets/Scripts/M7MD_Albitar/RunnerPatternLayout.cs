using UnityEngine;

/// <summary>نسخ جاهزة للعوائق لكل قطعة؛ نبدل تفعيلها فقط عند إعادة استخدام القطعة.</summary>
public sealed class RunnerPatternLayout : MonoBehaviour
{
    [SerializeField] private RunnerTrackSegment segment;
    [SerializeField] private GameObject[] originalObstacles;
    [SerializeField] private RunnerCoinRow[] originalCoinRows;
    [SerializeField] private GameObject jumpPrefab;
    [SerializeField] private GameObject slidePrefab;
    [SerializeField] private GameObject busPrefab;
    private GameObject[,] instances;
    private RunnerCoinRow safeCoins, jumpCoins;
    public int SafeLane { get; private set; } = 1;
    public int Pattern { get; private set; }
    public int ActiveHazards { get; private set; }
    public float HazardStart => Pattern == 4 ? CentreZ - 9.5f : CentreZ - 1f;
    public float HazardEnd => Pattern == 4 ? CentreZ + 7.2f : CentreZ + 1f;
    private float CentreZ => (segment.StartZ + segment.EndZ) * .5f;
    [SerializeField, Min(1)] private float laneSpacing = 5f;
    private float LaneX(int lane) => (lane - 1) * laneSpacing;
    private void EnsurePool()
    {
        if (instances != null && instances[0, 0]) return;
        instances = new GameObject[3, 3]; var prefabs = new[] { jumpPrefab, slidePrefab, busPrefab };
        for (int kind = 0; kind < 3; kind++) for (int lane = 0; lane < 3; lane++)
        { var item = Instantiate(prefabs[kind], transform); item.SetActive(false); instances[kind, lane] = item; }
        safeCoins = new GameObject("Safe route coins").AddComponent<RunnerCoinRow>(); safeCoins.transform.SetParent(transform, false);
        jumpCoins = new GameObject("Jump reward coins").AddComponent<RunnerCoinRow>(); jumpCoins.transform.SetParent(transform, false);
    }
    public void Apply(int pattern, int safeLane)
    {
        if (!Application.isPlaying) return;
        EnsurePool(); Pattern = pattern; SafeLane = safeLane; ActiveHazards = 0;
        foreach (var item in originalObstacles) if (item) item.SetActive(false);
        foreach (var row in originalCoinRows) if (row) row.enabled = false;
        for (int kind = 0; kind < 3; kind++) for (int lane = 0; lane < 3; lane++) instances[kind, lane].SetActive(false);
        jumpCoins.gameObject.SetActive(false);
        // المسار الآمن متصل بالأجزاء المجاورة، والعملات ترشد إليه دون إجبار اللاعب على قفزة.
        safeCoins.arch = false; safeCoins.spacing = 2f;
        safeCoins.count = Mathf.Max(1, Mathf.FloorToInt((segment.Length - 8f) / safeCoins.spacing));
        safeCoins.transform.position = new Vector3(LaneX(safeLane), .9f, segment.StartZ + 4f);
        if (pattern == 0) return;
        for (int lane = 0; lane < 3; lane++)
        {
            if (lane == safeLane) continue;
            int kind = pattern == 4 ? 2 : pattern == 2 ? 1 : pattern == 3 ? (lane == (safeLane + 1) % 3 ? 0 : 1) : 0;
            var item = instances[kind, lane]; item.transform.position = new Vector3(LaneX(lane), 0, CentreZ); item.SetActive(true); ActiveHazards++;
            if (kind == 0 && !jumpCoins.gameObject.activeSelf)
            {
                jumpCoins.gameObject.SetActive(true); jumpCoins.arch = true; jumpCoins.count = 7; jumpCoins.archLength = 6; jumpCoins.archHeight = 2;
                jumpCoins.transform.position = new Vector3(LaneX(lane), .9f, CentreZ - 3);
            }
        }
    }
}
