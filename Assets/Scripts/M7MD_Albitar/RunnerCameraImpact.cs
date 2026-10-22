using UnityEngine;
using Unity.Cinemachine;

/// <summary>يضيف أثر الصدمة بعد التتبّع دون تغيير موقع اللاعب أو منافسة Cinemachine على الكاميرا.</summary>
public sealed class RunnerCameraImpact : CinemachineExtension
{
    public RunnerCameraFollow source;
    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,
        CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        if (stage != CinemachineCore.Stage.Finalize || !source) return;
        state.PositionCorrection += source.ImpactOffset;
        state.OrientationCorrection *= Quaternion.Euler(0, 0, source.ImpactRoll);
    }
}
