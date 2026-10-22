using UnityEngine;

/// <summary>ينقل حدث نهاية الزحلقة من Animator الموجود على المجسم إلى كود الحركة.</summary>
public sealed class RunnerAnimationEventRelay : MonoBehaviour
{
    public PlayerMovements player;
    public void GoBackToRunning() { if (player) player.GoBackToRunning(); }
}
