using UnityEngine;

public sealed class BossArenaController : MonoBehaviour
{
    private BossArenaDefinition _def; private Vector3 _center; private GameObject _ringVFX;

    public static BossArenaController Spawn(BossArenaDefinition def, Vector3 center, Transform parent)
    {
        var go = new GameObject("BossArena"); if (parent != null) go.transform.SetParent(parent);
        go.transform.position = center;
        var c = go.AddComponent<BossArenaController>();
        c._def = def; c._center = center;
        if (def.RingPrefab) c._ringVFX = Instantiate(def.RingPrefab, center, Quaternion.identity, go.transform);
        return c;
    }

    public void Despawn() { if (_ringVFX) Destroy(_ringVFX); Destroy(gameObject); }

    public bool IsInside(Vector3 pos)
    {
        if (_def.Type == ARENA_TYPE.CIRCLE)
        {
            Vector3 d = pos - _center; d.y = 0f; return d.sqrMagnitude <= _def.Radius * _def.Radius;
        }
        // Polygon: point in polygon test on XZ plane
        return IsInsidePolygonXZ(pos);
    }

    private bool IsInsidePolygonXZ(Vector3 pos)
    {
        if (_def.Points == null || _def.Points.Length < 3) return true;
        bool inside = false; float x = pos.x, z = pos.z; var pts = _def.Points; int j = pts.Length - 1;
        for (int i = 0; i < pts.Length; i++)
        {
            float xi = pts[i].x + _center.x, zi = pts[i].z + _center.z;
            float xj = pts[j].x + _center.x, zj = pts[j].z + _center.z;
            bool intersect = ((zi > z) != (zj > z)) && (x < (xj - xi) * (z - zi) / ((zj - zi) + 1e-6f) + xi);
            if (intersect) inside = !inside; j = i;
        }
        return inside;
    }
}
