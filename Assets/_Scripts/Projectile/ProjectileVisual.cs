using UnityEngine;

public class ProjectileVisual : MonoBehaviour
{
    private Transform target;

    public void SetTarget(Transform target)
    {
        this.target = target;
    }

    public void UpdateFacing(Vector3 moveDir)
    {
        if (moveDir == Vector3.zero) return;
        float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
}