#define HIT_KIND_TRIANGLE_FRONT_FACE 254
#define HIT_KIND_TRIANGLE_BACK_FACE 255

/// sdf helpers for custom intersection shaders
// https://www.iquilezles.org/www/articles/distfunctions/distfunctions.htm
float sdf_sphere(float3 p, float r)
{
    return length(p) - r;
}

float sdf_box(float3 p, float3 b)
{
    float3 q = abs(p) - b;
    return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0);
}

float sdf_torus(float3 p, float2 t)
{
    float2 q = float2(length(p.xz) - t.x, p.y);
    return length(q) - t.y;
}

float sdf_octahedron(float3 p, float s)
{
    p = abs(p);
    float m = p.x + p.y + p.z - s;
    float3 q;
    if (3.0 * p.x < m) q = p.xyz;
    else if (3.0 * p.y < m) q = p.yzx;
    else if (3.0 * p.z < m) q = p.zxy;
    else return m * 0.57735027;

    float k = clamp(0.5 * (q.z - q.y + s), 0.0, s);
    return length(float3(q.x, q.y - s + k, q.z - k));
}

float sdf_cone(float3 p, float2 c, float h)
{
    // c is the sin/cos of the angle, h is height
    // Alternatively pass q instead of (c,h),
    // which is the point at the base in 2D
    float2 q = h * float2(c.x / c.y, -1.0);

    float2 w = float2(length(p.xz), p.y);
    float2 a = w - q * clamp(dot(w, q) / dot(q, q), 0.0, 1.0);
    float2 b = w - q * float2(clamp(w.x / q.x, 0.0, 1.0), 1.0);
    float k = sign(q.y);
    float d = min(dot(a, a), dot(b, b));
    float s = max(k * (w.x * q.y - w.y * q.x), k * (w.y - q.y));
    return sqrt(d) * sign(s);
}

float op_union(float a, float b)
{
    return min(a, b);
}

float op_intersection(float a, float b)
{
    return max(a, b);
}

float op_subtraction(float a, float b)
{
    return max(-a, b);
}

float op_smooth_union(float a, float b, float s)
{
    float h = saturate(0.5 + 0.5 * (b - a) / s);
    return lerp(b, a, h) - s * h * (1.0 - h);
}

float op_smooth_intersection(float a, float b, float s)
{
    float h = saturate(0.5 - 0.5 * (b - a) / s);
    return lerp(b, a, h) + s * h * (1.0 - h);
}

float op_smooth_subtraction(float a, float b, float s)
{
    float h = saturate(0.5 - 0.5 * (b + a) / s);
    return lerp(b, -a, h) + s * h * (1.0 - h);
}

// Rotation with angle (in radians) and axis
float3x3 AngleAxis3x3(float3 axis, float angle)
{
    float c, s;
    sincos(angle, s, c);

    float t = 1 - c;
    float x = axis.x;
    float y = axis.y;
    float z = axis.z;

    return float3x3(
        t * x * x + c, t * x * y - s * z, t * x * z + s * y,
        t * x * y + s * z, t * y * y + c, t * y * z - s * x,
        t * x * z - s * y, t * y * z + s * x, t * z * z + c
        );
}

// can re-use for raymarching intersection shaders
// see: ProceduralShapeShader
#define RTX_RAYMARCH(get_distance, max_steps)                                        \
    float3 direction = ObjectRayDirection();                                         \
    float3 origin = ObjectRayOrigin();                                               \
    AttributeData attributes = (AttributeData) 0;                                    \
    attributes.barycentrics = float2(0, 0);                                          \
    attributes.normalOS = float3(0, 1, 0);                                           \
    float distance = 0.01;                                                           \
    float t = 0.001;                                                                 \
    float3 pos = origin;                                                             \
    bool found = false;                                                              \
    for (int i = 0; i < max_steps; ++i) {                                            \
        pos = origin + direction * t;                                                \
        distance = get_distance(pos);                                                \
        t += distance;                                                               \
        if (distance <= 0.0001) {                                                    \
            t += distance;                                                           \
            pos = origin + direction * t;                                            \
            found = true;                                                            \
            break;                                                                   \
        }                                                                            \
    }                                                                                \
    if (found) {                                                                     \
        const float h = 0.0001;                                                      \
        const float2 k = float2(1, -1);                                              \
        attributes.normalOS = normalize(k.xyy * get_distance(pos + k.xyy * h) +      \
            k.yyx * get_distance(pos + k.yyx * h) +                                  \
            k.yxy * get_distance(pos + k.yxy * h) +                                  \
            k.xxx * get_distance(pos + k.xxx * h));                                  \
        ReportHit(t, HIT_KIND_TRIANGLE_FRONT_FACE, attributes);                      \
    }                                                                  