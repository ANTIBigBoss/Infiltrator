using UnityEngine;
using System.Collections;

public class Player : MonoBehaviour
{
    [Header("Movement & Rotation")]
    public float moveSpeed = 5f;
    public float rotationTime = 1f;

    [Header("Animation")]
    public Animator animator;

    [Header("Terminal Settings")]
    public Sprite terminalGreenSprite;

    [Header("Collision Sounds")]
    public AudioClip collisionWallSound;
    public AudioClip collisionCrateSound;
    public AudioClip collisionFloorSound;
    public AudioClip collisionGoalSound;
    public AudioClip collisionDoorSound;
    public AudioClip collisionGuardSound;
    public AudioClip collisionTerminalSound;

    [Header("Collision Sound Volumes (0 to 1)")]
    [Range(0f, 1f)] public float collisionWallVolume = 1f;
    [Range(0f, 1f)] public float collisionCrateVolume = 1f;
    [Range(0f, 1f)] public float collisionFloorVolume = 1f;
    [Range(0f, 1f)] public float collisionGoalVolume = 1f;
    [Range(0f, 1f)] public float collisionDoorVolume = 1f;
    [Range(0f, 1f)] public float collisionGuardVolume = 1f;
    [Range(0f, 1f)] public float collisionTerminalVolume = 1f;

    [Header("Background Music")]
    public AudioClip bgmMain;
    public AudioClip bgmGameOver;
    [Header("BGM Volumes (0 to 1)")]
    [Range(0f, 1f)] public float bgmMainVolume = 1f;
    [Range(0f, 1f)] public float bgmGameOverVolume = 1f;

    [Header("Door Unlock Settings")]
    [Tooltip("Target position for the Door when the Terminal is activated.")]
    public Vector2 doorTargetPosition;
    [Tooltip("Time (in seconds) for the Door to move to target position.")]
    public float doorMoveDuration = 2f;

    public float invulnerabilityDuration = 2f;
    private float spawnTime;
    private bool terminalActivated = false;
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Coroutine rotateCoroutine = null;
    private float targetAngle;
    private AudioSource audioSource;
    private bool isDead = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        transform.rotation = Quaternion.Euler(0, 0, 0);
        targetAngle = 0f;
        spawnTime = Time.time;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (bgmMain != null)
        {
            audioSource.clip = bgmMain;
            audioSource.loop = true;
            audioSource.volume = bgmMainVolume;
            audioSource.Play();
        }
    }

    void Update()
    {
        if (GuardPatrol.freezeAllGuards)
        {
            moveInput = Vector2.zero;
            if (animator != null && animator.enabled)
                animator.enabled = false;
            return;
        }
        else if (animator != null && !animator.enabled)
        {
            animator.enabled = true;
        }

        float horizontal = 0f, vertical = 0f;
        if (Input.GetKey(KeyCode.W))
            vertical = 1f;
        else if (Input.GetKey(KeyCode.S))
            vertical = -1f;
        if (Input.GetKey(KeyCode.A))
            horizontal = -1f;
        else if (Input.GetKey(KeyCode.D))
            horizontal = 1f;
        moveInput = new Vector2(horizontal, vertical);

        bool keyPressed = false;
        float desiredAngle = targetAngle;
        if (Input.GetKey(KeyCode.W))
        {
            desiredAngle = 0f;
            keyPressed = true;
        }
        else if (Input.GetKey(KeyCode.A))
        {
            desiredAngle = 90f;
            keyPressed = true;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            desiredAngle = 180f;
            keyPressed = true;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            desiredAngle = 270f;
            keyPressed = true;
        }
        if (keyPressed && !Mathf.Approximately(desiredAngle, targetAngle))
        {
            targetAngle = desiredAngle;
            if (rotateCoroutine != null)
                StopCoroutine(rotateCoroutine);
            rotateCoroutine = StartCoroutine(RotateToAngle(targetAngle, rotationTime));
        }

        if (moveInput.sqrMagnitude > 0.001f)
            animator.Play("Crouch Up");
        else
            animator.Play("Idle Up");
    }

    void FixedUpdate()
    {
        if (GuardPatrol.freezeAllGuards)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        if (!isDead)
            rb.linearVelocity = moveInput.normalized * moveSpeed;
    }

    IEnumerator RotateToAngle(float newAngle, float duration)
    {
        float elapsed = 0f;
        float startAngle = transform.rotation.eulerAngles.z;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float currentAngle = Mathf.LerpAngle(startAngle, newAngle, t);
            transform.rotation = Quaternion.Euler(0, 0, currentAngle);
            yield return null;
        }
        transform.rotation = Quaternion.Euler(0, 0, newAngle);
        rotateCoroutine = null;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead)
            return;

        string otherName = collision.gameObject.name;
        Debug.Log("Player collided with: " + otherName);

        if (otherName == "Terminal" && !terminalActivated)
        {
            terminalActivated = true;
            SpriteRenderer sr = collision.gameObject.GetComponent<SpriteRenderer>();
            if (sr != null && terminalGreenSprite != null)
                sr.sprite = terminalGreenSprite;
            if (collisionTerminalSound != null)
                AudioSource.PlayClipAtPoint(collisionTerminalSound, transform.position, collisionTerminalVolume);
            GameObject doorObj = GameObject.Find("Door");
            if (doorObj != null)
                StartCoroutine(MoveDoor(doorObj, doorTargetPosition, doorMoveDuration));
        }
        else if (otherName == "Wall")
        {
            if (collisionWallSound != null)
                AudioSource.PlayClipAtPoint(collisionWallSound, transform.position, collisionWallVolume);
        }
        else if (otherName == "Crate")
        {
            if (collisionCrateSound != null)
                AudioSource.PlayClipAtPoint(collisionCrateSound, transform.position, collisionCrateVolume);
        }
        else if (otherName == "Floor")
        {
            if (collisionFloorSound != null)
                AudioSource.PlayClipAtPoint(collisionFloorSound, transform.position, collisionFloorVolume);
        }
        else if (otherName == "Goal")
        {
            if (collisionGoalSound != null)
                AudioSource.PlayClipAtPoint(collisionGoalSound, transform.position, collisionGoalVolume);
            if (audioSource != null)
                audioSource.Stop();
            GameState.TriggerLevelComplete();
        }
        else if (otherName == "Door")
        {
            if (collisionDoorSound != null)
                AudioSource.PlayClipAtPoint(collisionDoorSound, transform.position, collisionDoorVolume);
        }
        else if (otherName == "Guard")
        {
            if (collisionGuardSound != null)
                AudioSource.PlayClipAtPoint(collisionGuardSound, transform.position, collisionGuardVolume);
            if (audioSource != null)
                audioSource.Stop();
            GameState.TriggerGameOver();
        }
    }

    public void PerformGameOver()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        if (audioSource != null)
            audioSource.Stop();
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.enabled = false;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
        Destroy(gameObject, 0.1f);
    }

    public void PerformLevelComplete()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        if (audioSource != null)
            audioSource.Stop();
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.enabled = false;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
    }

    IEnumerator MoveDoor(GameObject doorObj, Vector2 targetPos, float duration)
    {
        Vector3 startPos = doorObj.transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            doorObj.transform.position = Vector3.Lerp(startPos, new Vector3(targetPos.x, targetPos.y, startPos.z), t);
            yield return null;
        }
        doorObj.transform.position = new Vector3(targetPos.x, targetPos.y, startPos.z);
    }
}