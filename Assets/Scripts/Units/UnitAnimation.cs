using UnityEngine;

// Shared by every walking unit: goblin, orc, wulf and the barracks soldiers.
// Put it on the SAME object as the Animator and the SpriteRenderer.
// It works out walking and facing by itself, Enemy and Soldier only tell it about attacks and death.
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class UnitAnimation : MonoBehaviour
{
    private static readonly int MovingHash = Animator.StringToHash("Moving");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DeathHash = Animator.StringToHash("Death");

    [Tooltip("Slower than this (units per second) counts as standing still.")]
    [SerializeField] private float movingThreshold = 0.05f;
    [Tooltip("How long the body stays on the ground after dying, before it is removed.")]
    [SerializeField] private float deathLingerTime = 1f;
    [Tooltip("ON if the sprites face right (the goblin and soldier sheets do).")]
    [SerializeField] private bool artFacesRight = true;

    public float DeathLinger => deathLingerTime;

    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector3 lastPosition;
    private bool dead;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        lastPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (dead || Time.deltaTime <= 0f) return;

        Vector3 delta = transform.position - lastPosition;
        lastPosition = transform.position;

        bool moving = delta.magnitude / Time.deltaTime > movingThreshold;
        animator.SetBool(MovingHash, moving);

        // While walking, face the way we are walking
        if (moving) Face(delta.x);
    }

    // Used when fighting: look at whoever we are hitting
    public void FaceTowards(Vector3 worldPosition)
    {
        Face(worldPosition.x - transform.position.x);
    }

    public void PlayAttack()
    {
        if (!dead) animator.SetTrigger(AttackHash);
    }

    public void PlayDeath()
    {
        if (dead) return;

        dead = true;
        animator.SetBool(MovingHash, false);
        animator.SetTrigger(DeathHash);
    }

    private void Face(float deltaX)
    {
        if (Mathf.Abs(deltaX) < 0.01f) return;

        bool faceRight = deltaX > 0f;
        spriteRenderer.flipX = artFacesRight ? !faceRight : faceRight;
    }
}