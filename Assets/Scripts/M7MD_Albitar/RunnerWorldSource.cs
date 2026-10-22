using UnityEngine;

/// <summary>عقد بسيط لمصدر حركة العالم؛ يسمح باستبدال مدير الماب دون تغيير كود اللاعب.</summary>
public abstract class RunnerWorldSource : MonoBehaviour
{
    public abstract float CurrentSpeed { get; }
    public abstract bool IsMoving { get; }
    public virtual float StepDistance => IsMoving ? Mathf.Max(0f, CurrentSpeed) * Time.fixedDeltaTime : 0f;

    // نلتقط ترتيب البداية مرة واحدة، ثم نعيده عند بدء جولة جديدة.
    public abstract void CaptureInitialState();
    public abstract void ResetWorld();
    public abstract void SetMoving(bool moving);
}

