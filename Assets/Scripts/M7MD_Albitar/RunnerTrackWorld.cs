using System.Collections.Generic;
using UnityEngine;

/// <summary>يحرك الطريق للخلف ويعيد القطعة المنتهية إلى المقدمة بدون إنشاء قطع أثناء اللعب.</summary>
[DefaultExecutionOrder(-100)]
public sealed class RunnerTrackWorld : RunnerWorldSource
{
    [SerializeField] private RunnerTrackSegment[] segments;
    [SerializeField] private RunnerCoinPool coins;
    [SerializeField] private RunnerPatternDirector patterns;
    [SerializeField, Min(0)] private float initialSpeed = 5f;
    [SerializeField, Min(0)] private float acceleration = 0.1f;
    [SerializeField, Min(1)] private float maximumSpeed = 35f;
    [SerializeField] private float recycleBehindZ = -60f;
    private readonly List<RunnerTrackSegment> ordered = new List<RunnerTrackSegment>();
    private Vector3[] initialPositions;
    private Quaternion[] initialRotations;
    private bool moving, captured;
    private float speed, stepDistance;
    public override float CurrentSpeed => speed;
    public override bool IsMoving => moving;
    public override float StepDistance => moving ? stepDistance : 0f;
    public double DistanceTravelled { get; private set; }
    public int RecycleCount { get; private set; }
    public IReadOnlyList<RunnerTrackSegment> OrderedSegments => ordered;

    // نتوقع سرعة الوصول إلى بداية القوس بدل استخدام سرعة توليد القطعة البعيدة.
    public float PredictSpeedAfterDistance(float distance)
    {
        float current = Mathf.Clamp(speed, 0f, maximumSpeed);
        return Mathf.Min(maximumSpeed, Mathf.Sqrt(current * current + 2f * acceleration * Mathf.Max(0f, distance)));
    }

    // المسافة خلال زمن القفزة، مع تقسيم الرحلة عند بلوغ حد السرعة.
    public float PredictTravelDistance(float seconds, float startingSpeed)
    {
        seconds = Mathf.Max(0f, seconds);
        startingSpeed = Mathf.Clamp(startingSpeed, 0f, maximumSpeed);
        if (acceleration <= 0f) return startingSpeed * seconds;
        float acceleratingTime = Mathf.Min(seconds, (maximumSpeed - startingSpeed) / acceleration);
        return startingSpeed * acceleratingTime + .5f * acceleration * acceleratingTime * acceleratingTime
            + maximumSpeed * (seconds - acceleratingTime);
    }

    public override void CaptureInitialState()
    {
        captured = false;
        if (segments == null || segments.Length < 2)
        {
            Debug.LogError("RunnerTrackWorld: اربط قطعتين على الأقل لتغطية الطريق.", this);
            return;
        }
        var unique = new HashSet<RunnerTrackSegment>();
        initialPositions = new Vector3[segments.Length];
        initialRotations = new Quaternion[segments.Length];
        for (int i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            if (!segment || segment.Length < 1f || !unique.Add(segment))
            {
                Debug.LogError("RunnerTrackWorld: قطعة مفقودة أو مكررة أو حدود طول غير صالحة.", this);
                return;
            }
            initialPositions[i] = segment.transform.position;
            initialRotations[i] = segment.transform.rotation;
        }
        captured = true;
    }

    public override void ResetWorld()
    {
        moving = false;
        stepDistance = 0;
        speed = Mathf.Clamp(initialSpeed, 0, maximumSpeed);
        DistanceTravelled = 0;
        RecycleCount = 0;
        if (!captured) return;
        ordered.Clear();
        for (int i = 0; i < segments.Length; i++)
        {
            segments[i].transform.SetPositionAndRotation(initialPositions[i], initialRotations[i]);
            segments[i].gameObject.SetActive(true);
            ordered.Add(segments[i]);
        }
        ordered.Sort((a, b) => a.StartZ.CompareTo(b.StartZ));
        // ربط البداية بالنهاية يدعم قطعًا بأطوال مختلفة وتعديل طول القطع من Inspector.
        for (int i = 1; i < ordered.Count; i++) ordered[i].PlaceStartAt(ordered[i - 1].EndZ);
        if (patterns && patterns.isActiveAndEnabled) { patterns.ResetSequence(); foreach (var segment in ordered) patterns.Prepare(segment, 0, maximumSpeed); }
        if (coins)
        {
            coins.ResetCounter();
            foreach (var segment in ordered) coins.Populate(segment);
        }
        Physics.SyncTransforms();
    }

    public override void SetMoving(bool value)
    {
        moving = value && captured;
        if (!moving) stepDistance = 0;
    }

    private void FixedUpdate() => Simulate(Time.fixedDeltaTime);

    // نفس الدالة تستخدم في الفيزياء والاختبارات؛ اللاعب يقرأ المسافة الفعلية بعد تحريك الطريق.
    public void Simulate(float deltaTime)
    {
        stepDistance = 0;
        if (!moving || deltaTime <= 0 || ordered.Count < 2) return;
        speed = Mathf.Min(maximumSpeed, speed + acceleration * deltaTime);
        stepDistance = speed * deltaTime;
        DistanceTravelled += stepDistance;
        foreach (var segment in ordered) segment.transform.position += Vector3.back * stepDistance;
        // while تعالج عبور أكثر من قطعة في خطوة واحدة دون تفويت قطعة بسبب حذف عنصر أثناء for.
        while (ordered[0].EndZ < recycleBehindZ)
        {
            var segment = ordered[0];
            float nextStart = ordered[ordered.Count - 1].EndZ;
            ordered.RemoveAt(0);
            segment.PlaceStartAt(nextStart);
            ordered.Add(segment);
            RecycleCount++;
            if (patterns && patterns.isActiveAndEnabled) patterns.Prepare(segment, DistanceTravelled, maximumSpeed);
            if (coins) coins.Populate(segment);
        }
        Physics.SyncTransforms();
    }
}
