Shader "Custom/ToonShaderOutlineOnly"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _MetallicGlossMap ("Metallic", 2D) = "white" {}
        _OcclusionMap ("Occlusion", 2D) = "white" {}
        _EmissionMap ("Emission", 2D) = "white" {}
        
        [Space(20)]
        [Header(Outline Settings)]
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0.001, 0.03)) = 0.005
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200
        
        // Main pass - use the original material's rendering
        Pass
        {
            Name "FORWARD"
            Tags { "LightMode" = "ForwardBase" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            sampler2D _BumpMap;
            sampler2D _MetallicGlossMap;
            sampler2D _OcclusionMap;
            sampler2D _EmissionMap;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                SHADOW_COORDS(3)
            };
            
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                TRANSFER_SHADOW(o);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Sample textures
                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                
                // Basic lighting
                float3 normal = normalize(i.worldNormal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = max(0, dot(normal, lightDir));
                
                // Apply shadow
                UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
                
                // Final color with lighting
                col.rgb *= NdotL * atten * _LightColor0.rgb + UNITY_LIGHTMODEL_AMBIENT.rgb;
                
                return col;
            }
            ENDCG
        }
        
        // Outline pass
        Pass
        {
            Name "OUTLINE"
            Tags { "LightMode" = "Always" }
            Cull Front
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            float _OutlineWidth;
            fixed4 _OutlineColor;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
            };
            
            v2f vert (appdata v)
            {
                v2f o;
                
                // Extrude vertex along normal in clip space
                float4 clipPos = UnityObjectToClipPos(v.vertex);
                float3 clipNormal = mul((float3x3)UNITY_MATRIX_VP, mul((float3x3)UNITY_MATRIX_M, v.normal));
                
                // Normalize the normal and apply outline width
                float2 offset = normalize(clipNormal.xy) * _OutlineWidth;
                clipPos.xy += offset * clipPos.w;
                
                o.pos = clipPos;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                return _OutlineColor;
            }
            ENDCG
        }
    }
    
    FallBack "Diffuse"
}
