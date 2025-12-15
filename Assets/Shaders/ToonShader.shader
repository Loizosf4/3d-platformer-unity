Shader "Custom/ToonShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        
        [Header(Cel Shading)]
        _ShadowColor ("Shadow Color", Color) = (0.3, 0.3, 0.4, 1)
        _ShadowThreshold ("Shadow Threshold", Range(0, 1)) = 0.5
        _ShadowSoftness ("Shadow Softness", Range(0, 0.5)) = 0.01
        _ShadowTolerance ("Shadow Tolerance", Range(0, 1)) = 0.0
        
        [Header(Highlight)]
        _HighlightColor ("Highlight Color", Color) = (1, 1, 1, 1)
        _HighlightThreshold ("Highlight Threshold", Range(0, 1)) = 0.8
        _HighlightSoftness ("Highlight Softness", Range(0, 0.5)) = 0.01
        
        [Header(Rim Light)]
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _RimIntensity ("Rim Intensity", Range(0, 1)) = 0.5
        
        [Header(Outline)]
        [Toggle(OUTLINE_ON)] _OutlineEnabled ("Enable Outline", Float) = 1
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.03)) = 0.005
        [KeywordEnum(Normal, Position, UV2)] _OutlineMode ("Outline Mode", Float) = 0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        
        // Outline Pass - uses clip space offset for accuracy
        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode"="Always" }
            Cull Front
            ZWrite On
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature OUTLINE_ON
            #pragma shader_feature _OUTLINEMODE_NORMAL _OUTLINEMODE_POSITION _OUTLINEMODE_UV2
            #include "UnityCG.cginc"
            
            float _OutlineWidth;
            float4 _OutlineColor;
            float _OutlineEnabled;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv2 : TEXCOORD1;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                
                #ifdef OUTLINE_ON
                    // Transform to clip space first
                    float4 clipPos = UnityObjectToClipPos(v.vertex);
                    
                    // Get normal in clip space for screen-space consistent outlines
                    float3 clipNormal = mul((float3x3)UNITY_MATRIX_VP, mul((float3x3)unity_ObjectToWorld, v.normal));
                    
                    // Calculate outline offset in clip space (screen-space consistent)
                    float2 offset = normalize(clipNormal.xy) * _OutlineWidth * clipPos.w * 2;
                    
                    // Apply offset
                    clipPos.xy += offset;
                    
                    o.pos = clipPos;
                #else
                    // No outline - collapse to zero
                    o.pos = float4(0, 0, 0, 1);
                #endif
                
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                #ifdef OUTLINE_ON
                    return _OutlineColor;
                #else
                    discard;
                    return fixed4(0,0,0,0);
                #endif
            }
            ENDCG
        }
        
        // Main Toon Pass
        Pass
        {
            Name "TOON"
            Tags { "LightMode"="ForwardBase" }
            Cull Back
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            
            float4 _ShadowColor;
            float _ShadowThreshold;
            float _ShadowSoftness;
            float _ShadowTolerance;
            
            float4 _HighlightColor;
            float _HighlightThreshold;
            float _HighlightSoftness;
            
            float4 _RimColor;
            float _RimPower;
            float _RimIntensity;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                float3 viewDir : TEXCOORD3;
                SHADOW_COORDS(4)
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                TRANSFER_SHADOW(o);
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // Sample texture
                fixed4 texColor = tex2D(_MainTex, i.uv) * _Color;
                
                // Normalize vectors
                float3 normal = normalize(i.worldNormal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 viewDir = normalize(i.viewDir);
                
                // Calculate NdotL for cel shading
                float NdotL = dot(normal, lightDir);
                
                // Shadow with hard edge (cel shading)
                float shadow = smoothstep(_ShadowThreshold - _ShadowSoftness, _ShadowThreshold + _ShadowSoftness, NdotL * 0.5 + 0.5);
                
                // Apply shadow tolerance - reduces shadow intensity
                shadow = lerp(shadow, 1.0, _ShadowTolerance);
                
                // Highlight with hard edge
                float highlight = smoothstep(_HighlightThreshold - _HighlightSoftness, _HighlightThreshold + _HighlightSoftness, NdotL);
                
                // Rim lighting
                float rim = 1.0 - saturate(dot(viewDir, normal));
                rim = pow(rim, _RimPower) * _RimIntensity;
                
                // Unity shadow
                float unityShadow = SHADOW_ATTENUATION(i);
                shadow *= unityShadow;
                
                // Combine colors
                float3 baseColor = texColor.rgb;
                float3 shadowedColor = lerp(baseColor * _ShadowColor.rgb, baseColor, shadow);
                float3 highlightedColor = lerp(shadowedColor, _HighlightColor.rgb, highlight * 0.3);
                float3 finalColor = highlightedColor + (_RimColor.rgb * rim);
                
                // Add ambient light
                float3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb * baseColor;
                finalColor += ambient * 0.5;
                
                return fixed4(finalColor, texColor.a);
            }
            ENDCG
        }
        
        // Shadow caster pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            
            struct v2f
            {
                V2F_SHADOW_CASTER;
            };
            
            v2f vert(appdata_base v)
            {
                v2f o;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o);
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i);
            }
            ENDCG
        }
    }
    
    FallBack "Diffuse"
}
