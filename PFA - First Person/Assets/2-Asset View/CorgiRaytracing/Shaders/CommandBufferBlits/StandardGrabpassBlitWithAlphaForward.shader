Shader "Hidden/StandardGrabpassBlitWithAlphaForward"
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

                Texture2D _CopyBlitTex;
                SamplerState sampler_CopyBlitTex;

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
                };

                FragOutput Frag(VaryingsDefault i)
                {
                    FragOutput output = (FragOutput)0;

                    output.target0 = _CopyBlitTex.Sample(sampler_CopyBlitTex, i.uv);

                    return output;
                }
            ENDHLSL
        }
    }
}