using UnityEngine;
using System.Collections;

#region Enumerations
public enum TurnPreset { Right, Up, Left, Down, Custom }
public enum TurnMethod { ForcedClockwise, ForcedCounterclockwise, Shortest }
#endregion

#region Data Class
[System.Serializable]
public class PatrolPoint
{
    public float x;
    public float y;
    [Header("Turning Settings")]
    public TurnPreset presetRotation;
    public float customRotation;
    public float turnDuration;
    public TurnMethod turnMethod = TurnMethod.ForcedClockwise;
}
#endregion

public class GuardPatrol : MonoBehaviour
{
    [Header("Patrol Settings")]
    public PatrolPoint[] patrolPoints;
    public float moveSpeed = 2f;
    [Header("Animation Settings")]
    public Animator animator;
    public AnimationClip walkAnimation;
    public AnimationClip turnAnimation;
    [Header("Vision Settings")]
    [Tooltip("Set this to include obstacle layers (e.g., Crate, Wall, Door, Terminal) that block vision.")]
    public LayerMask visionObstructionLayers;
    [Tooltip("Thickness (radius) used in CircleCast for vision.")]
    public float visionRadius = 0.5f;
    [Tooltip("Maximum distance the guard can see.")]
    public float maxVisionDistance = 100f;
    [Header("Exclamation Alert Settings")]
    [Tooltip("Prefab for the exclamation mark to display when a guard spots the player.")]
    public GameObject exclamationMarkPrefab;
    [Tooltip("Distance behind the guard at which the exclamation mark spawns.")]
    public float exclamationOffset = 1f;
    [Tooltip("Vertical offset to raise the exclamation mark above the guard.")]
    public float exclamationVerticalOffset = 1.5f;
    [Tooltip("Alert sound that plays immediately when a guard spots the player.")]
    public AudioClip guardDetectedSound;
    [Tooltip("Volume multiplier for the alert sound (0 - 1).")]
    [Range(0f, 1f)]
    public float guardAlertVolume = 1f;
    [Header("Debug Options")]
    [Tooltip("Toggle to draw a debug ray for the guard's line of sight.")]
    public bool showVisionDebug = false;
    private AudioSource audioSource;
    public static bool freezeAllGuards = false;
    public static bool alertTriggered = false;
    private int currentPatrolIndex = 0;
    private bool isTurning = false;

    void Start()
    {
        if (patrolPoints == null || patrolPoints.Length < 1)
        {
            Debug.LogError("No patrol points defined for GuardPatrol.");
            enabled = false;
            return;
        }
        transform.position = new Vector3(patrolPoints[0].x, patrolPoints[0].y, transform.position.z);
        float initAngle = GetTargetAngle(patrolPoints[0]);
        transform.rotation = Quaternion.Euler(0, 0, initAngle);

        if (animator != null && walkAnimation != null)
            animator.Play(walkAnimation.name);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (showVisionDebug)
        {
            Collider2D selfCol = GetComponent<Collider2D>();
            if (selfCol != null)
            {
                float angleRad = transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)).normalized;
                Vector2 rayOrigin = (Vector2)transform.position + direction * (selfCol.bounds.extents.magnitude + 0.1f);
                Debug.DrawRay(rayOrigin, direction * maxVisionDistance, Color.red);
            }
        }

        if (freezeAllGuards)
        {
            if (animator != null && animator.enabled)
                animator.enabled = false;
            return;
        }
        else if (animator != null && !animator.enabled)
        {
            animator.enabled = true;
        }

        if (!isTurning && patrolPoints.Length >= 2)
        {
            int nextIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            PatrolPoint p = patrolPoints[nextIndex];
            Vector3 targetPos = new Vector3(p.x, p.y, transform.position.z);
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            if (Vector3.Distance(transform.position, targetPos) < 0.1f)
            {
                float targetAngle = GetTargetAngle(p);
                StartCoroutine(TurnToTarget(targetAngle, p.turnDuration, p.turnMethod));
                currentPatrolIndex = nextIndex;
            }
        }
        CheckVision();
    }

    float GetTargetAngle(PatrolPoint p)
    {
        switch (p.presetRotation)
        {
            case TurnPreset.Right: return 0f;
            case TurnPreset.Up: return 90f;
            case TurnPreset.Left: return 180f;
            case TurnPreset.Down: return 270f;
            case TurnPreset.Custom: return p.customRotation;
            default: return 0f;
        }
    }

    IEnumerator TurnToTarget(float targetAngle, float turnDuration, TurnMethod method)
    {
        isTurning = true;
        if (animator != null && turnAnimation != null)
            animator.Play(turnAnimation.name);

        float elapsed = 0f;
        float startAngle = transform.rotation.eulerAngles.z;
        float finalAngle = targetAngle % 360f;
        float adjustedStartAngle = startAngle;
        if (method == TurnMethod.ForcedClockwise)
        {
            if (adjustedStartAngle <= finalAngle)
                adjustedStartAngle += 360f;
        }
        else if (method == TurnMethod.ForcedCounterclockwise)
        {
            if (adjustedStartAngle >= finalAngle)
                adjustedStartAngle -= 360f;
        }
        while (elapsed < turnDuration)
        {
            float t = elapsed / turnDuration;
            float newAngle = (method == TurnMethod.Shortest)
                ? Mathf.LerpAngle(startAngle, finalAngle, t)
                : Mathf.Lerp(adjustedStartAngle, finalAngle, t) % 360f;
            transform.rotation = Quaternion.Euler(0, 0, newAngle);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.rotation = Quaternion.Euler(0, 0, finalAngle);
        if (animator != null && walkAnimation != null)
            animator.Play(walkAnimation.name);
        isTurning = false;
    }

    void CheckVision()
    {
        if (GameState.state == GameState.GameOver || GameState.state == GameState.LevelComplete)
            return;

        Collider2D selfCol = GetComponent<Collider2D>();
        if (selfCol == null)
            return;

        float angleRad = transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)).normalized;
        float offset = selfCol.bounds.extents.magnitude + 0.1f;
        Vector2 rayOrigin = (Vector2)transform.position + direction * offset;

        int guardLayer = LayerMask.NameToLayer("Guard");
        int finalMask = visionObstructionLayers.value & ~(1 << guardLayer);

        RaycastHit2D hit = Physics2D.CircleCast(rayOrigin, visionRadius, direction, maxVisionDistance, finalMask);
        if (hit.collider != null)
        {
            Debug.Log("Guard vision hit: " + hit.collider.gameObject.name);
            if (hit.collider.gameObject.name == "Player")
            {
                if (!alertTriggered)
                {
                    alertTriggered = true;
                    Debug.Log("Guard sees the Player -> Freezing and showing alert");
                    StartCoroutine(FreezeAndShowAlert());
                }
            }
        }
    }

    IEnumerator FreezeAndShowAlert()
    {
        freezeAllGuards = true;
        if (animator != null)
            animator.enabled = false;

        if (guardDetectedSound != null)
            audioSource.PlayOneShot(guardDetectedSound, guardAlertVolume);

        float angleRad = transform.rotation.eulerAngles.z * Mathf.Deg2Rad;
        Vector2 forward = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)).normalized;
        Vector2 spawnPos = (Vector2)transform.position - forward * exclamationOffset + Vector2.up * exclamationVerticalOffset;
        if (exclamationMarkPrefab != null)
            Instantiate(exclamationMarkPrefab, spawnPos, Quaternion.identity);

        GameObject lineObj = new GameObject("DetectionLine");
        LineRenderer line = lineObj.AddComponent<LineRenderer>();
        Material lineMat = new Material(Shader.Find("Sprites/Default"));
        line.material = lineMat;
        line.material.color = Color.red;
        line.startWidth = 0.3f;
        line.endWidth = 0.3f;
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.sortingLayerName = "Default";
        line.sortingOrder = 10;

        float alertDuration = 3f;
        float timer = 0f;
        while (timer < alertDuration)
        {
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                line.SetPosition(0, transform.position);
                line.SetPosition(1, playerObj.transform.position);
            }
            timer += Time.deltaTime;
            yield return null;
        }
        Destroy(lineObj);

        freezeAllGuards = false;
        if (animator != null)
            animator.enabled = true;

        GameState.TriggerGameOver();
    }
}
