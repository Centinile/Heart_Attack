using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 2f;
    public int health = 1;
    public Transform[] wayPoints;
    public int currentWayPoint = 0;
    void Update()
    {
        if( wayPoints == null || wayPoints.Length == 0) return;

        Transform target = wayPoints[currentWayPoint];
        Vector3 dir = (target.position - transform.position).normalized;

        transform.position += dir * speed * Time.deltaTime;

        if(Vector3.Distance(transform.position, target.position) < 0.05f)
        {
            currentWayPoint++;

            if(currentWayPoint >= wayPoints.Length)
            {
                Destroy(gameObject);
            }
        }
        
    }
}
