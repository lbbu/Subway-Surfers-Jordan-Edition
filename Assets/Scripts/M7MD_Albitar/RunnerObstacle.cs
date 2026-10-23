using UnityEngine;

/// <summary>بيانات واضحة للمصمم؛ الفيزياء تعتمد على المصادمات، والتوليد يعتمد على نوع النمط.</summary>
public sealed class RunnerObstacle : MonoBehaviour
{
    public enum ActionType { Jump, Slide, Climb }
    public ActionType requiredAction;
}
