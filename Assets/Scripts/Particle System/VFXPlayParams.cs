using UnityEngine;

public struct VFXPlayParams
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Color? Color;
    public float? Scale;
    public float? SimulationSpeed;

    public VFXPlayParams(Vector3 position, Quaternion? rotation = null)
    {
        Position = position;
        Rotation = rotation ?? Quaternion.identity;
        Color = null;
        Scale = null;
        SimulationSpeed = null;
    }
}
