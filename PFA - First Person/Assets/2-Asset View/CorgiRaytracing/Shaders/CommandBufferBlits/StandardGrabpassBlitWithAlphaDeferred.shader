Shader "Hidden/StandardGrabpassBlitWithAlphaDeferred"
{
    SubShader
    {
        Blend SrcAlpha OneMinusSrcAlpha

        Cull Off 
        ZWrite Off 
        ZTest Always

        Pass
        {
            HLSLPROGRAM
                #include "UnityCG.cginc"
                #include "UnityStandardUtils.cginc"

                #pragma vertex VertDefault
                #pragma fragment Frag
                #pragma target 5.0

                struct AttributesDefault
                {
                    float3 vertex : POSITION;
                };

                struct VaryingsDefault
                {
                    float4 vertex : SV_POSITION;
                    float2 uv : TEXCOORD0;
                };

                SamplerState _LinearClamp;

                Texture2D _CopyBlitTex0;
                Texture2D _CopyBlitTex1;
                Texture2D _CopyBlitTex2;
                Texture2D _CopyBlitTex3;
                SamplerState sampler_CopyBlitTex0;
                SamplerState sampler_CopyBlitTex1;
                SamplerState sampler_CopyBlitTex2;
                SamplerState sampler_CopyBlitTex3;

                float2 ClampSampleUV(float2 uv)
                {
                    return UnityStereoTransformScreenSpaceTex(saturate(uv));
                }

                float4x4 _CameraToWorld;
                float4x4 _InverseProjection;

                VaryingsDefault VertDefault(AttributesDefault v)
                {
                    VaryingsDefault o;
                    o.vertex = float4(v.vertex.xy, 0.0, 1.0);
                    o.uv = (v.vertex.xy + 1.0) * 0.5;

                    #if UNITY_UV_STARTS_AT_TOP
                            o.uv = o.uv * float2(1.0, -1.0) + float2(0.0, 1.0);
                    #endif

                    o.uv = TransformStereoScreenSpaceTex(o.uv, 1.0);

                    return o;
                }

                struct FragOutput
                {
                    float4 target0 : SV_Target0; // colors 
                    float4 target1 : SV_Target1; // specular
                    float4 target2 : SV_Target2; // normals
                    float4 target3 : SV_Target3; // emission 
                };

                FragOutput Frag(VaryingsDefault i)
                {
                    FragOutput output = (FragOutput)0;

                    output.target0 = _CopyBlitTex0.Sample(sampler_CopyBlitTex0, i.uv);
                    output.target1 = _CopyBlitTex1.Sample(sampler_CopyBlitTex1, i.uv);
                    output.target2 = _CopyBlitTex2.Sample(sampler_CopyBlitTex2, i.uv);
                    output.target3 = _CopyBlitTex3.Sample(sampler_CopyBlitTex3, i.uv);

                    return output;
                }
            ENDHLSL
        }
    }
}