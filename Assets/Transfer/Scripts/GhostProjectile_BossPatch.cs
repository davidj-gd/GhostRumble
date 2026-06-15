// -----------------------------------------------------------------------
// GhostRumble — GhostProjectile.cs  ·  BossEnemy patch
// -----------------------------------------------------------------------
// Drop this into your existing GhostProjectile.cs OnCollisionEnter method.
// Add it right alongside your existing damage / hit-VFX logic.
// -----------------------------------------------------------------------

/* ---- Inside GhostProjectile.cs → OnCollisionEnter(Collision col) ---- 

    private void OnCollisionEnter(Collision col)
    {
        // ---------- NEW: register hit on BossEnemy ----------
        BossEnemy boss = col.gameObject.GetComponent<BossEnemy>();
        if (boss != null)
        {
            boss.RegisterHit();
        }
        // ----------------------------------------------------

        // ... your existing velocity-zero / mesh-disable / hitVFX / Destroy logic ...
    }

----------------------------------------------------------------------- */

// You can delete this file once you've applied the patch above.
// It exists only as a plain-text reminder — Unity will ignore it
// if it has no class declaration, so keep it as a comment file only
// or remove it from the project.
