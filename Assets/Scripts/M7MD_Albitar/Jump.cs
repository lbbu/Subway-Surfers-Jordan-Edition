using UnityEngine;

/// <summary>
/// Compatibility shell for references in other scenes. All jump/input/physics logic
/// now lives in PlayerMovements. M7MD_Albitar no longer needs this component.
/// </summary>
[AddComponentMenu("")]
public class Jump : MonoBehaviour
{
    // Retain legacy serialized tuning for reference; tune PlayerMovements from now on.
    [HideInInspector] public float jumpHeight = 2f;
    [HideInInspector] public float timeToJumpApex = 0.4f;

    public void GoBackToRunning()
    {
        var movement = GetComponent<PlayerMovements>();
        if (movement) movement.GoBackToRunning();
    }
}
