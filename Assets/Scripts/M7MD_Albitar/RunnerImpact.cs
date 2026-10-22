using UnityEngine;

/// <summary>وصف الاصطدام؛ التسلسل يميز ضربة مستقلة عن تكرار نفس التلامس.</summary>
public readonly struct RunnerImpact
{
    public enum Kind { Side, Front, Fall }
    public readonly Kind Type;
    public readonly int Sequence;
    public readonly int Direction;
    public readonly Vector3 Point;
    public readonly Collider Obstacle;
    public RunnerImpact(Kind type, int sequence, int direction, Vector3 point, Collider obstacle)
    { Type = type; Sequence = sequence; Direction = direction; Point = point; Obstacle = obstacle; }
}
