using UnityEngine;
using System.Collections.Generic;

public class TunnelPath : MonoBehaviour
{
    public static TunnelPath Instance;

    public List<Vector2> mainNodes = new List<Vector2>();
    private List<Vector2> branchNodes = null;

    public float stepLength = 6f;
    public float turnChance = 0.15f;
    public float turnAngle = 35f * Mathf.Deg2Rad;
    public int maxSamplesPerChunk = 64;

    private Vector2 direction = Vector2.up;
    private Vector2 head = Vector2.zero;

    // branching control
    public float branchChance = 0.05f; // chance to start a loop
    private bool branchActive = false;
    private Vector2 branchDir;
    private int rejoinIndexOffset = 40; // how far ahead along main path to reconnect
    private int rejoinTargetIndex = -1;

    private float maxDeviation = 45f * Mathf.Deg2Rad;

    public float minBranchLength = 50f;     // how far branch must travel before returning
    public float minSideDistance = 25f;
    private Vector2 branchStart;


    private int lastRejoinNodeCount = 0;
    public int branchCooldownNodes = 80;

    private float branchAccumulator = 0f;
    public float branchProbabilityIncrease = 0.003f;
    void Awake()
    {
        Instance = this;
        mainNodes.Add(head);
    }

    public void EnsureLengthUpTo(float targetWorldZ)
    {
        while (head.y < targetWorldZ + 64f)
        {
            // accumulate chance over time
            branchAccumulator += branchProbabilityIncrease;

            if (!branchActive
                && branchAccumulator >= Random.value
                && mainNodes.Count > rejoinIndexOffset * 2
                && mainNodes.Count - lastRejoinNodeCount > branchCooldownNodes)
            {
                branchAccumulator = 0f; // reset chance when starting a branch

                branchActive = true;
                branchNodes = new List<Vector2>();
                branchNodes.Add(mainNodes[mainNodes.Count - 1]);
                branchStart = mainNodes[mainNodes.Count - 1];
                branchDir = direction;
                rejoinTargetIndex = Mathf.Clamp(mainNodes.Count - 1 + rejoinIndexOffset, 0, mainNodes.Count - 1);
            }


            // Always grow main path
            direction = ApplyRandomTurnWithClamping(direction);
            head += direction * stepLength;
            mainNodes.Add(head);

            // Grow branch path if active
            if (branchActive)
            {
                branchDir = ApplyBranchSteering(branchDir);
                Vector2 branchHead = branchNodes[branchNodes.Count - 1] + branchDir * stepLength;
                branchNodes.Add(branchHead);

                // Always aim toward a point further ahead along the *current* main path.
                rejoinTargetIndex = mainNodes.Count - 1 + rejoinIndexOffset;
                rejoinTargetIndex = Mathf.Clamp(rejoinTargetIndex, 0, mainNodes.Count - 1);

                Vector2 rejoinTarget = mainNodes[rejoinTargetIndex];

                if ((branchHead - rejoinTarget).sqrMagnitude < 6f)
                {
                    mainNodes.AddRange(branchNodes);
                    branchNodes = null;
                    branchActive = false;

                    // NEW: start cooldown to avoid immediate re-branch
                    lastRejoinNodeCount = mainNodes.Count;
                }
            }

        }
    }

    private Vector2 ApplyRandomTurnWithClamping(Vector2 dir)
    {
        if (Random.value < turnChance)
        {
            float sign = Random.value < 0.5f ? -1 : 1;
            float angle = turnAngle * sign;

            Vector2 newDir = new Vector2(
                dir.x * Mathf.Cos(angle) - dir.y * Mathf.Sin(angle),
                dir.x * Mathf.Sin(angle) + dir.y * Mathf.Cos(angle)
            ).normalized;

            float globalAngle = Mathf.Atan2(newDir.x, newDir.y);
            globalAngle = Mathf.Clamp(globalAngle, -maxDeviation, maxDeviation);

            return new Vector2(Mathf.Sin(globalAngle), Mathf.Cos(globalAngle));
        }
        return dir;
    }

    // Steering for branch: diverge first, then gradually aim to rejoin
    private Vector2 ApplyBranchSteering(Vector2 dir)
    {
        Vector2 current = branchNodes[branchNodes.Count - 1];

        float branchTravel = (current - branchStart).magnitude;
        Vector2 rejoinPoint = mainNodes[Mathf.Clamp(rejoinTargetIndex, 0, mainNodes.Count - 1)];
        float sideDist = (current - rejoinPoint).magnitude;

        bool inDivergencePhase = sideDist < minSideDistance;

        if (inDivergencePhase)
        {
            // Phase 1: PUSH AWAY from main path
            Vector2 away = (current - rejoinPoint).normalized;
            dir = (dir * 0.7f + away * 0.3f).normalized;
        }
        else if (branchTravel > minBranchLength)
        {
            // Phase 2: RETURN smoothly toward rejoin point
            Vector2 forward = Vector2.up; // world forward = +Z
            Vector2 toTarget = (rejoinPoint - current).normalized;

            // Blend: mostly forward, slightly toward target
            dir = (forward * 0.65f + toTarget * 0.35f).normalized;

        }
        else
        {
            // Middle segment: gentle random wandering, but reduced noise
            if (Random.value < 0.05f) // reduced noise
                dir = ApplyRandomTurnWithClamping(dir);
        }

        return dir.normalized;
    }



    public float DistanceSqToPath(Vector2 point)
    {
        float best = DistanceSqToNodes(point, mainNodes);

        if (branchActive && branchNodes != null)
        {
            float branchDist = DistanceSqToNodes(point, branchNodes);
            if (branchDist < best) best = branchDist;
        }

        return best;
    }

    private float DistanceSqToNodes(Vector2 point, List<Vector2> nodes)
    {
        float best = float.MaxValue;
        for (int i = 1; i < nodes.Count; i++)
        {
            Vector2 a = nodes[i - 1];
            Vector2 b = nodes[i];
            Vector2 ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / ab.sqrMagnitude);
            Vector2 closest = a + t * ab;
            float d = (point - closest).sqrMagnitude;
            if (d < best) best = d;
        }
        return best;
    }
}
