using UnityEngine;

public class NPCFollower : MonoBehaviour
{
    public Transform player;
    public float speed = 3f;
    public float followDistance = 1.5f;

    private bool followPlayer = false;

    void Update()
    {
        if (!followPlayer) return;

        float dist = Vector2.Distance(transform.position, player.position);

        if (dist > followDistance)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                player.position,
                speed * Time.deltaTime
            );
        }
    }

    public void StartFollowing()
    {
        followPlayer = true;
    }
}
