using UnityEngine;
using System.Collections;   
using UnityEngine.VFX;

public class PunchProjectile : MonoBehaviour
{
    [Header("Punch Settings")]
    [SerializeField] private float punchForce = 15f;
    [SerializeField] private ForceMode forceMode = ForceMode.Impulse;
    [SerializeField] private GameObject punchPrefab;
    [SerializeField] private Transform punchSpawnPoint;
    [SerializeField] private float projectileLifetime;
    [SerializeField] private float damageAmount;
    public bool punchIsThrown = false;

    [SerializeField] private RegisterHit registerHit;

    public void ThrowPunch()
    {
        GameObject punchInstance = Instantiate(punchPrefab, punchSpawnPoint.position, punchSpawnPoint.rotation);
        Rigidbody projectileRb = punchInstance.GetComponent<Rigidbody>();
        if (projectileRb != null)
        {
            punchIsThrown = true;
            Vector3 direction = punchSpawnPoint.forward;
            projectileRb.AddForce(direction * punchForce, forceMode);
        }

        var registerHitOnProjectile = punchInstance.GetComponent<RegisterHit>();
        if (registerHitOnProjectile != null)
            registerHitOnProjectile.SetPunchProjectile(this);

        StartCoroutine(DestroyAfterLifetime(punchInstance));
    }

    private IEnumerator DestroyAfterLifetime(GameObject instance)
    {
        yield return new WaitForSeconds(projectileLifetime);
        punchIsThrown = false;
        if (instance != null)
            Object.Destroy(instance);
    }

    public void OnProjectileHit(int amount, Vector3 hitPoint)
    {
        if (registerHit != null)
            registerHit.PlayHitVFX(hitPoint);
        Debug.Log($"Hit {amount}");
    }
}
