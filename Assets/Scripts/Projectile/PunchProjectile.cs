using UnityEngine;
using System.Collections;   

public class PunchProjectile : MonoBehaviour
{
    [Header("Punch Settings")]
    [SerializeField] private float punchForce = 15f;
    [SerializeField] private ForceMode forceMode = ForceMode.Impulse;
    [SerializeField] private GameObject punchPrefab;
    [SerializeField] private Transform punchSpawnPoint;
    [SerializeField] private float projectileLifetime;
    [SerializeField] private float damageAmount;

    public void ThrowPunch()
    {
        GameObject punchInstance = Instantiate(punchPrefab, punchSpawnPoint.position, punchSpawnPoint.rotation);
        Rigidbody projectileRb = punchInstance.GetComponent<Rigidbody>();
        if (projectileRb != null)
        {
            Vector3 direction = punchSpawnPoint.forward;
            projectileRb.AddForce(direction * punchForce, forceMode);
        }
        StartCoroutine(DestroyAfterLifetime(punchInstance));
    }

    private IEnumerator DestroyAfterLifetime(GameObject instance)
    {
        yield return new WaitForSeconds(projectileLifetime);
        if (instance != null)
            Object.Destroy(instance);
    }
}
