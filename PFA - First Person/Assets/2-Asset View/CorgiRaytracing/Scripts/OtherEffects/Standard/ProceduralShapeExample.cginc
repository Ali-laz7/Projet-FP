#include "../../../Shaders/Includes/ProceduralHelpers.cginc"

float sdf_object(float3 position)
{
    float3 posRot = mul(AngleAxis3x3(float3(1, 0, 0), _Time.y), position);
    float torus0 = sdf_torus(0 - position, float2(0.5, 0.025));
    float torus1 = sdf_torus(0 - posRot, float2(0.5, 0.025));
    return op_smooth_union(torus0, torus1, 0.1);
}

[shader("intersection")]
void IntersectionMain()
{
    RTX_RAYMARCH(sdf_object, 1024);
}