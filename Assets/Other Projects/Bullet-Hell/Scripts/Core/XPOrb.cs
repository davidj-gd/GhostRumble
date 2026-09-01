using UnityEngine;

/// <summary>
/// Dropped when an enemy dies. Floats toward the player when close, then grants XP.
///
/// Setup on the XPOrb prefab:
///  - SpriteRenderer (square, green, scale ~0.25)
///  - CircleCollider2D (Is Trigger = true, small radius)
///  - This script
/// </summary>
public class XPOrb : MonoBehaviour
{
    [Header("Pickup")]
    public float xpValue = 1f;
    public float magnetRange = 2.5f;
    public float magnetSpeed = 6f;
    public float pickupRange = 0.35f;

    private Transform player;
    private PlayerStats playerStats;
    private bool collected;

    public void Init(float xp)
    {
        xpValue = xp;
    }

    private void Awake()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerStats = playerObj.GetComponent<PlayerStats>();
        }
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerStats = playerObj.GetComponent<PlayerStats>();
            }
        }
    }

    private void Update()
    {
        if (collected || player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        if (dist <= pickupRange)
        {
            Collect();
            return;
        }

        if (dist <= magnetRange)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                player.position,
                magnetSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;
        Collect();
    }

    private void Collect()
    {
        if (collected) return;
        collected = true;

        if (playerStats != null)
            playerStats.AddXP(xpValue);

        Destroy(gameObject);
    }
}
