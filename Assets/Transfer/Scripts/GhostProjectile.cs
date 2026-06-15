using UnityEngine;
using System.Collections;

/// <summary>
/// GhostProjectile — attach to the Ball prefab.
/// Deals 1 damage to any PlayerHealth component it hits.
/// </summary>
public class GhostProjectile : MonoBehaviour
{
    [Header("Impact Effect")]
    public ParticleSystem hitVFX;

    [Header("Damage")]
    public int damage = 1;

    [Header("Spawn Grace Period")]
    public float colliderDelay = 0.08f;

    bool hasHit = false;

    void Start() => StartCoroutine(EnableColliderAfterDelay());

    IEnumerator EnableColliderAfterDelay()
    {
        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (var c in cols) c.enabled = false;
        yield return new WaitForSeconds(colliderDelay);
        if (!hasHit)
            foreach (var c in cols) c.enabled = true;
    }

    void OnCollisionEnter(Collision col)
    {
        
        BossEnemy boss = col.gameObject.GetComponent<BossEnemy>();
        if (boss != null) boss.RegisterHit();

        if (hasHit) return;
        hasHit = true;

        // Deal damage if we hit a player
        col.gameObject.GetComponent<PlayerHealth>()?.TakeDamage(damage);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.linearVelocity = Vector3.zero; rb.isKinematic = true; }

        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>())  c.enabled = false;

        Vector3 contactPoint  = col.contacts.Length > 0 ? col.contacts[0].point  : transform.position;
        Vector3 contactNormal = col.contacts.Length > 0 ? col.contacts[0].normal : Vector3.up;

        float destroyDelay = 0.5f;
        if (hitVFX != null)
        {
            ParticleSystem fx = Instantiate(hitVFX, contactPoint, Quaternion.LookRotation(contactNormal));
            fx.Play();
            float d = fx.main.duration + fx.main.startLifetime.constantMax;
            destroyDelay = d + 0.5f;
            Destroy(fx.gameObject, destroyDelay);
        }

        Destroy(gameObject, destroyDelay);
    }
}
