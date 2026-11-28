using UnityEngine;

// Attach to the enemy root, or reference another active object with this component.
public class WallImpactDebris : MonoBehaviour
{
    [Header("Debris")]
    [SerializeField] private GameObject cubePrefab;
    [SerializeField] private Material debrisMaterial;
    [SerializeField, Range(1, 40)] private int debrisCount = 14;
    [SerializeField, Min(0.01f)] private float minSize = 0.12f;
    [SerializeField, Min(0.01f)] private float maxSize = 0.35f;
    [SerializeField, Min(0.1f)] private float lifeTime = 2.5f;
    [SerializeField, Min(0f)] private float spreadSpeed = 3f;
    [SerializeField, Min(0f)] private float liftSpeed = 3f;
    [SerializeField] private string debrisLayerName = "HitDebris";

    public void Spawn(Vector3 point, Vector3 normal, float impactSpeed)
    {
        int layer = LayerMask.NameToLayer(debrisLayerName);
        // Require an isolated layer so decorative debris cannot interfere
        // with enemy / player collision or weapon hit detection.
        if (layer < 0)
        {
            Debug.LogWarning("[WallImpactDebris] Create the HitDebris layer first.", this);
            return;
        }

        for (int i = 0; i < debrisCount; i++)
        {
            float size = Random.Range(Mathf.Min(minSize, maxSize),
                                      Mathf.Max(minSize, maxSize));
            Vector3 offset = normal * (size * 0.9f + 0.05f)
                           + Vector3.ProjectOnPlane(Random.insideUnitSphere, normal) * 0.22f;
            GameObject piece = cubePrefab != null
                ? Instantiate(cubePrefab, point + offset, Random.rotation)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);
            piece.name = "WallHitDebris";
            piece.transform.SetPositionAndRotation(point + offset, Random.rotation);
            piece.transform.localScale = Vector3.one * size;

            foreach (Transform child in piece.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = layer;

            if (debrisMaterial != null)
            {
                foreach (Renderer renderer in piece.GetComponentsInChildren<Renderer>())
                    renderer.sharedMaterial = debrisMaterial;
            }

            Rigidbody rb = piece.GetComponent<Rigidbody>();
            if (rb == null) rb = piece.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.mass = 0.1f;
            rb.constraints = RigidbodyConstraints.None;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            float power = Mathf.Clamp(impactSpeed * 0.12f, 0.6f, 2f);
            Vector3 velocity = (normal * Random.Range(0.8f, 1.5f)
                + Random.insideUnitSphere * spreadSpeed
                + Vector3.up * liftSpeed) * power;
            rb.AddForce(velocity, ForceMode.VelocityChange);
            rb.AddTorque(Random.insideUnitSphere * 8f, ForceMode.VelocityChange);
            Destroy(piece, lifeTime);
        }
    }
}
