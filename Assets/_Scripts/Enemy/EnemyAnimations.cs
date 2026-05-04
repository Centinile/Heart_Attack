using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAnimations : MonoBehaviour
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private Vector3 baseScale;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        baseScale = transform.localScale;
    }

    private void Update()
    {

    }

    public void RotateToPointer(Vector2 lookDirection)
    {
        if (lookDirection == Vector2.zero) return;

        // Just flip the sprite
        spriteRenderer.flipX = (lookDirection.x < 0);
    }

    public void PlayAnimation(Vector2 movementInput)
    {
        animator.SetBool("Running", movementInput.magnitude > 0.01f);
    }

    public void PlayHitAnimation() => animator.SetTrigger("GetHit");
    public void PlayDeathAnimation() => animator.SetTrigger("Die");
    public void PlayAttackAnimation() => animator.SetTrigger("Attack");

    public void SetAnimatorController(RuntimeAnimatorController c) //something something, to assign an animator to player
    {
        if (!animator)
            animator = GetComponent<Animator>();

        animator.runtimeAnimatorController = c;
    }
}
