using System.Collections;
using UnityEngine;

public class StructureAnimations : MonoBehaviour
{
    [Header("References")]
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    [Header("Unpowered Visuals")]
    [SerializeField] private GameObject unpoweredIconPrefab;
    [SerializeField] private Vector3 iconOffset = new Vector3(0, 1f, 0);
    [SerializeField] private Color unpoweredTint = new Color(0.4f, 0.4f, 0.4f, 1f);

    private GameObject unpoweredIconInstance;
    private Color originalColor;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    // --- Power Visuals ---

    public void ShowUnpowered()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = unpoweredTint;

        if (unpoweredIconPrefab != null && unpoweredIconInstance == null)
            unpoweredIconInstance = Instantiate(unpoweredIconPrefab,
                transform.position + iconOffset, Quaternion.identity, transform);
    }

    public void ShowPowered()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        if (unpoweredIconInstance != null)
        {
            Destroy(unpoweredIconInstance);
            unpoweredIconInstance = null;
        }
    }

    // --- Animations ---

    public void PlayAttackAnimation() => TrySetTrigger("Attack");

    // Returns the clip length so Building can delay Destroy
    public float PlayDeathAnimation()
    {
        if (animator == null) return 0f;
        animator.SetTrigger("Destroyed");

        // Find the Destroyed clip length
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == "Destroyed")
                return clip.length;
        }
        return 0f;
    }

    public void SetAnimatorController(RuntimeAnimatorController controller)
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        animator.runtimeAnimatorController = controller;
    }

    public void FlipToward(Vector2 direction)
    {
        if (spriteRenderer != null && direction.x != 0)
            spriteRenderer.flipX = direction.x < 0;
    }

    private void TrySetTrigger(string triggerName)
    {
        if (animator != null)
            animator.SetTrigger(triggerName);
    }
}