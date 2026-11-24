#ifndef JORDAN_ROAD_CURVE_INCLUDED
#define JORDAN_ROAD_CURVE_INCLUDED

// (runner world Z, straight distance, distance to half heading, signed terminal slope)
float4 _JordanRoadCurve;
float _JordanRoadBendPower;
// (start relative to runner Z, distance to half pitch, softness, terminal downward slope)
float4 _JordanRoadVerticalCurve;

struct JordanCurveFrame
{
    float3 displacement;
    float2 forward;
    float2 right;
    float laneRate;
    float verticalSlope;
    bool hasHorizontal;
};

// The ramp starts flat. Scale its rate so the requested blend falls at entryDistance.
void JordanEvaluateCurve(float ahead, float entryDistance, float power, float halfBlend,
                         out float blend, out float integral, out float blendRate)
{
    float shape = exp(power);
    float halfRamp = sqrt(halfBlend);
    float rate = log((1.0 + halfRamp * shape) / (1.0 - halfRamp)) / max(entryDistance, 0.001);
    float x = rate * ahead;
    float decay = exp(-x);
    float denominator = 1.0 + shape * decay;
    float ramp = (1.0 - decay) / denominator;
    blend = ramp * ramp;

    // Integral of the squared exponential ramp from the end of the straight.
    float area;
    if (x < 0.1)
    {
        // Near the origin the closed form subtracts nearly equal values.
        float initialRamp = x / (1.0 + shape);
        float skew = (shape - 1.0) / (shape + 1.0);
        area = x * initialRamp * initialRamp * (1.0 / 3.0 + 0.25 * x * skew);
    }
    else
    {
        float inverseShape = 1.0 / shape;
        area = x - (1.0 - inverseShape * inverseShape) * log((1.0 + shape) / denominator)
                 - (1.0 + inverseShape) * ramp;
    }
    integral = max(area, 0.0) / rate;
    blendRate = 2.0 * rate * ramp * (1.0 + shape) * decay / (denominator * denominator);
}

// Both bends use original world Z. World X=0 is the middle lane.
bool JordanGetCurveFrame(float3 positionOS, out JordanCurveFrame frame)
{
    frame = (JordanCurveFrame)0;
#ifdef SHADERGRAPH_PREVIEW
    return false;
#else
    float3 worldPosition = GetAbsolutePositionWS(TransformObjectToWorld(positionOS));
    float runnerAhead = worldPosition.z - _JordanRoadCurve.x;
    float ahead = runnerAhead - _JordanRoadCurve.y;
    float strength = _JordanRoadCurve.w;
    frame.hasHorizontal = ahead > 0.0 && strength != 0.0;
    float verticalAhead = runnerAhead - _JordanRoadVerticalCurve.x;
    float downwardSlope = _JordanRoadVerticalCurve.w;
    bool hasVertical = verticalAhead > 0.0 && downwardSlope > 0.0;
    if (!frame.hasHorizontal && !hasVertical)
        return false;

    if (frame.hasHorizontal)
    {
        float blend, integral, blendRate;
        // Half blend bisects the straight and terminal unit directions.
        JordanEvaluateCurve(ahead, _JordanRoadCurve.z, clamp(_JordanRoadBendPower, 1.0, 5.0),
                            0.5, blend, integral, blendRate);
        float2 straight = float2(0.0, 1.0);
        float2 directionChange = normalize(float2(strength, 1.0)) - straight;
        float2 derivative = straight + blend * directionChange;
        float travelRate = length(derivative);
        frame.forward = derivative / travelRate;
        frame.right = float2(frame.forward.y, -frame.forward.x);
        frame.displacement.xz = directionChange * integral + worldPosition.x * (frame.right - float2(1.0, 0.0));

        // Frenet frame derivative accounts for the shorter path of the inside lane.
        float yawRate = directionChange.x * blendRate / dot(derivative, derivative);
        frame.laneRate = travelRate - worldPosition.x * yawRate;
    }
    if (hasVertical)
    {
        // tan(halfPitch) / tan(fullPitch) expressed in terms of the terminal slope.
        float halfBlend = rcp(sqrt(1.0 + downwardSlope * downwardSlope) + 1.0);
        float blend, integral, blendRate;
        JordanEvaluateCurve(verticalAhead, _JordanRoadVerticalCurve.y, clamp(_JordanRoadVerticalCurve.z, 2.0, 5.0),
                            halfBlend, blend, integral, blendRate);
        frame.displacement.y = -downwardSlope * integral;
        frame.verticalSlope = -downwardSlope * blend;
    }
    return true;
#endif
}

// Separate Graph nodes keep position-only passes independent of mesh normal
// and tangent attributes (not supplied by the URP motion-vector pass).
void JordanBendPosition_float(float3 positionOS, out float3 bentPositionOS)
{
    bentPositionOS = positionOS;
    JordanCurveFrame frame;
    if (JordanGetCurveFrame(positionOS, frame))
        bentPositionOS += TransformWorldToObjectDir(frame.displacement, false);
}

void JordanBendNormal_float(float3 positionOS, float3 normalOS, out float3 bentNormalOS)
{
    bentNormalOS = normalOS;
    JordanCurveFrame frame;
    if (!JordanGetCurveFrame(positionOS, frame))
        return;

    float3 normalWS = TransformObjectToWorldNormal(normalOS, false);
    normalWS.z -= normalWS.y * frame.verticalSlope;
    if (frame.hasHorizontal)
    {
        float safeRate = (frame.laneRate < 0.0 ? -1.0 : 1.0) * max(abs(frame.laneRate), 0.0001);
        normalWS.xz = frame.right * normalWS.x + frame.forward * (normalWS.z / safeRate);
    }
    bentNormalOS = TransformWorldToObjectNormal(normalWS);
}

void JordanBendTangent_float(float3 positionOS, float3 tangentOS, out float3 bentTangentOS)
{
    bentTangentOS = tangentOS;
    JordanCurveFrame frame;
    if (!JordanGetCurveFrame(positionOS, frame))
        return;

    float3 tangentWS = TransformObjectToWorldDir(tangentOS, false);
    tangentWS.y += tangentWS.z * frame.verticalSlope;
    if (frame.hasHorizontal)
        tangentWS.xz = frame.right * tangentWS.x + frame.forward * (tangentWS.z * frame.laneRate);
    bentTangentOS = TransformWorldToObjectDir(tangentWS);
}
#endif
