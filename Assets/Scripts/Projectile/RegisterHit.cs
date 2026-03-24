using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.VFX;


public class RegisterHit : MonoBehaviour
{
    private Collider _collider;
    [SerializeField] private PunchProjectile punchProjectile;
    [SerializeField] private GameObject hitVfx;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    public void SetPunchProjectile(PunchProjectile punch)
    {
        punchProjectile = punch;
    }

    void OnTriggerEnter(Collider other)
    {
        if (_collider == null) return;
        if (other == _collider) return;
        if (punchProjectile == null || !punchProjectile.punchIsThrown) return;

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        punchProjectile.OnProjectileHit(amount: 10, hitPoint);
    }

    public void PlayHitVFX(Vector3 hitPoint)
    {
        if (hitVfx == null) return;
        hitVfx.SetActive(true);
        hitVfx.transform.position = hitPoint;
        
    }
}
