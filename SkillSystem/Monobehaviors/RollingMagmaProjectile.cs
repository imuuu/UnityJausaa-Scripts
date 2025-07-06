using UnityEngine;

public class RollingMagmaProjectile : MonoBehaviour
{
    [Header("Trajectory Settings")]
    public float speed = 20f;
    public float launchAngle = 45f;  // in degrees
    public float gravity = 9.81f;
    public int maxBounces = 3;

    // Internal state
    private Vector3 launchDirection;
    private float horizontalSpeed;
    private float verticalSpeed;
    private float elapsedTime;
    private Vector3 startPosition;

    // Call this method immediately after spawning the projectile.
    public void Launch(Vector3 direction, int remainingBounces)
    {
        startPosition = transform.position;
        launchDirection = direction.normalized;
        maxBounces = remainingBounces;
        // Decompose speed into horizontal and vertical components.
        horizontalSpeed = speed * Mathf.Cos(launchAngle * Mathf.Deg2Rad);
        verticalSpeed = speed * Mathf.Sin(launchAngle * Mathf.Deg2Rad);
        elapsedTime = 0f;
    }

    void Update()
    {
        elapsedTime += Time.deltaTime;
        // Calculate horizontal displacement
        Vector3 horizontalDisp = launchDirection * horizontalSpeed * elapsedTime;
        // Calculate vertical displacement (s = ut - 1/2*g*t^2)
        float verticalDisp = verticalSpeed * elapsedTime - 0.5f * gravity * elapsedTime * elapsedTime;
        // New position based on the starting point
        Vector3 newPosition = startPosition + horizontalDisp + Vector3.up * verticalDisp;
        transform.position = newPosition;

        // Check for impact with ground (assuming ground is at y = 0)
        if (newPosition.y <= 0.1f)
        {
            Impact();
        }
    }

    private void Impact()
    {
        // Snap to ground level if desired:
        Vector3 impactPosition = new Vector3(transform.position.x, 0f, transform.position.z);
        transform.position = impactPosition;

        // TODO: Trigger explosion effects, area damage, particle systems, and sound.
        Explode();

        // Handle bounce chaining.
        if (maxBounces > 0)
        {
            // Option 1: Reinitialize this projectile for a bounce.
            maxBounces--;
            startPosition = impactPosition;
            elapsedTime = 0f;  // Reset time for the new bounce
            // Optionally, you might modify speed or angle for the bounce effect.
            // For example, you could reduce speed to simulate energy loss.
        }
        else
        {
            // Option 2: Return to pool or destroy the projectile.
            // For example: ManagerPrefabPooler.Instance.ReturnToPool(gameObject);
            Destroy(gameObject);
        }
    }

    void Explode()
    {
        // Implement your area-of-effect damage logic.
        // For example:
        // Collider[] hitEnemies = Physics.OverlapSphere(transform.position, explosionRadius, enemyLayerMask);
        // foreach (Collider enemy in hitEnemies) { ApplyDamage(enemy.gameObject); }
        Debug.Log("Explosion triggered at: " + transform.position);
    }
}
