// Sprint 4 / BOT-A07: URP forward translucent glass shader for Botanika
// windows + greenhouse panels. HLSL/ShaderLab (NOT ShaderGraph) for
// batchmode-stability. Receives baked + realtime URP lighting via
// UniversalFragmentPBR with a custom fresnel rim added on top.
//
// Surface: Transparent, SrcAlpha OneMinusSrcAlpha, Cull Back, ZWrite Off.
// Default tint: warm (1, 0.95, 0.85, 0.4) — alpha 0.4 translucency.
// Smoothness 0.95. Fresnel rim adds glassy edge highlight independent of
// view-direction lighting term so glass reads as glass even in shadow.
//
// M1 8GB constraint: single forward pass, no additional lights pass for
// shadow caster (translucent glass doesn't cast shadows here).

Shader "Afterhumans/AH_Glass"
{
    Properties
    {
        [MainColor] _BaseColor ("Base Color (RGB) Opacity (A)", Color) =
            (1.0, 0.95, 0.85, 0.4)
        [MainTexture] _BaseMap ("Base Map", 2D) = "white" {}

        _Smoothness ("Smoothness", Range(0.0, 1.0)) = 0.95
        _Metallic   ("Metallic",   Range(0.0, 1.0)) = 0.0

        _FresnelColor ("Fresnel Rim Color", Color) = (1.0, 0.96, 0.86, 1.0)
        _FresnelPower ("Fresnel Power",  Range(0.5, 8.0))   = 3.0
        _FresnelStrength ("Fresnel Strength", Range(0.0, 4.0)) = 1.5

        // Stencil/utility hidden props (URP convention for inspector).
        [HideInInspector] _Surface ("__surface", Float) = 1.0  // Transparent
        [HideInInspector] _Blend   ("__blend",   Float) = 0.0  // Alpha
        [HideInInspector] _Cull    ("__cull",    Float) = 2.0  // Back
        [HideInInspector] _ZWrite  ("__zwrite",  Float) = 0.0
        [HideInInspector] _AlphaClip ("__clip",  Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex   AH_GlassVert
            #pragma fragment AH_GlassFrag

            // URP keyword fences — forward path, fog, lightmaps + main light
            // shadows. Keep light-loop minimal for M1 8GB.
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4  _BaseColor;
                half   _Smoothness;
                half   _Metallic;
                half4  _FresnelColor;
                half   _FresnelPower;
                half   _FresnelStrength;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float2 lightmapUV : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 3);
                float  fogFactor   : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings AH_GlassVert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posIn =
                    GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   nrmIn =
                    GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posIn.positionCS;
                OUT.positionWS = posIn.positionWS;
                OUT.normalWS   = nrmIn.normalWS;
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor  = ComputeFogFactor(posIn.positionCS.z);

                OUTPUT_LIGHTMAP_UV(IN.lightmapUV, unity_LightmapST,
                                   OUT.lightmapUV);
                OUTPUT_SH(OUT.normalWS.xyz, OUT.vertexSH);

                return OUT;
            }

            half4 AH_GlassFrag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                half4 baseTex = SAMPLE_TEXTURE2D(
                    _BaseMap, sampler_BaseMap, IN.uv);
                half4 albedo  = baseTex * _BaseColor;

                float3 N = normalize(IN.normalWS);
                float3 V = normalize(
                    GetWorldSpaceViewDir(IN.positionWS));

                // SurfaceData for URP PBR call.
                SurfaceData surf = (SurfaceData)0;
                surf.albedo            = albedo.rgb;
                surf.metallic          = _Metallic;
                surf.specular          = half3(0.0, 0.0, 0.0);
                surf.smoothness        = _Smoothness;
                surf.normalTS          = half3(0.0, 0.0, 1.0);
                surf.emission          = half3(0.0, 0.0, 0.0);
                surf.occlusion         = 1.0;
                surf.alpha             = albedo.a;
                surf.clearCoatMask     = 0.0;
                surf.clearCoatSmoothness = 0.0;

                // InputData for URP lighting.
                InputData lightInput = (InputData)0;
                lightInput.positionWS         = IN.positionWS;
                lightInput.normalWS           = N;
                lightInput.viewDirectionWS    = V;
                lightInput.fogCoord           = IN.fogFactor;
                lightInput.shadowCoord        =
                    TransformWorldToShadowCoord(IN.positionWS);
                lightInput.bakedGI            =
                    SAMPLE_GI(IN.lightmapUV, IN.vertexSH, N);
                lightInput.normalizedScreenSpaceUV = float2(0, 0);
                lightInput.shadowMask         = half4(1, 1, 1, 1);

                half4 lit = UniversalFragmentPBR(lightInput, surf);

                // Fresnel rim on top (independent of light dir, for glass
                // edge readability even in shadow).
                half  fresnel = pow(saturate(1.0 - dot(N, V)),
                                    _FresnelPower);
                half3 rim     = _FresnelColor.rgb * fresnel
                              * _FresnelStrength;
                lit.rgb += rim;

                // Boost alpha at grazing angles so glass edges are more
                // visible (glassy feel without losing translucency face-on).
                half alphaOut = saturate(albedo.a + fresnel * 0.35);

                lit.rgb = MixFog(lit.rgb, IN.fogFactor);
                return half4(lit.rgb, alphaOut);
            }
            ENDHLSL
        }

        // Depth-only pass for SSAO / DoF compatibility.
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex   DepthOnlyVert
            #pragma fragment DepthOnlyFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct AttributesD { float4 positionOS : POSITION; };
            struct VaryingsD   { float4 positionCS : SV_POSITION; };

            VaryingsD DepthOnlyVert(AttributesD IN)
            {
                VaryingsD OUT;
                OUT.positionCS =
                    TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half DepthOnlyFrag(VaryingsD IN) : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LitShader"
}
