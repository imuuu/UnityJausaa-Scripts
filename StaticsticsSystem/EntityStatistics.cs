using UnityEngine;


//TODO maybe move this to MobDataController.cs?
public class EntityStatistics : MonoBehaviour, IStatistics
{
    [Header("Entity Dimensions")]
    [SerializeField]
    private float height = 1.8f;
    [SerializeField]
    private float width = 0.6f; 

    [Header("Debug Options")]
    [SerializeField]
    private bool isDebug = false; 

    public float Height => height;
    public float Width => width;

    public Vector3 FeetPosition => transform.position;

    public Vector3 HeadPosition => 
    new Vector3(this.transform.position.x, this.transform.position.y + height, this.transform.position.z);

    public void SetPosition(Vector3 newPosition)
    {
        transform.position = newPosition;
    }

    private void OnDrawGizmosSelected()
    {
        if (!isDebug)
            return;

        if (!isDebug)
            return;

        // --- Height Debugging ---
        // Draw a green sphere at the feet (base) position.
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(FeetPosition, 0.1f);

        // Draw a red sphere at the head position.
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(HeadPosition, 0.1f);

        // Draw a blue line connecting the feet to the head.
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(FeetPosition, HeadPosition);

        // --- Width Debugging ---
        // Calculate left and right positions at the feet level.
        Vector3 leftFeet = FeetPosition - transform.right * (width * 0.5f);
        Vector3 rightFeet = FeetPosition + transform.right * (width * 0.5f);

        // Calculate left and right positions at the head level.
        Vector3 leftHead = HeadPosition - transform.right * (width * 0.5f);
        Vector3 rightHead = HeadPosition + transform.right * (width * 0.5f);

        // Set color for width markers.
        Gizmos.color = Color.yellow;

        // Draw spheres at the boundaries on the feet level.
        Gizmos.DrawSphere(leftFeet, 0.1f);
        Gizmos.DrawSphere(rightFeet, 0.1f);

        // Draw spheres at the boundaries on the head level.
        Gizmos.DrawSphere(leftHead, 0.1f);
        Gizmos.DrawSphere(rightHead, 0.1f);

        // Draw horizontal lines to represent the width at both the feet and head levels.
        Gizmos.DrawLine(leftFeet, rightFeet);
        Gizmos.DrawLine(leftHead, rightHead);

        // Optionally, draw vertical lines connecting the corresponding left and right points.
        Gizmos.DrawLine(leftFeet, leftHead);
        Gizmos.DrawLine(rightFeet, rightHead);
    }
}
