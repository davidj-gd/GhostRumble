using UnityEngine;

public class AIBaseMovement : MonoBehaviour
{
    [SerializeField] string playerTag = "Player";
    Transform _player;

    void Awake()
    {
        var p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null) _player = p.transform;
    }

    void FixedUpdate()
    {
        if (_player == null) return;
        Vector3 to = _player.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(-to.normalized);
    }
}
