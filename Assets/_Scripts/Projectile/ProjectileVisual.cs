using UnityEngine;

public class ProjectileVisual : MonoBehaviour
{
    private Transform target;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    public void SetTarget(Transform target)
    {
        this.target = target;
    }

    public void UpdateFacing(Vector3 moveDir)
    {
        if (moveDir == Vector3.zero) return;
        if (spriteRenderer == null) return;

        float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg - 90f;
        spriteRenderer.transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
}