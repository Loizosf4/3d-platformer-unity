Shader "Custom/ToonShaderTransparent"
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
        
        [Header(Rim Light)]
        _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower ("Rim Power", Range(0.5, 8)) = 3
        _RimIntensity ("Rim Intensity", Range(0, 1)) = 0.5
    }
    
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        
        Pass
        {
            Name "TOON_TRANSPARENT"
            Tags { "LightMode"="ForwardBase" }
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            
            float4 _ShadowColor;
            float _ShadowThreshold;
            float _ShadowSoftness;
            float _ShadowTolerance;
            
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
                float3 viewDir : TEXCOORD2;
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.viewDir = normalize(WorldSpaceViewDir(v.vertex));
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv) * _Color;
                
                float3 normal = normalize(i.worldNormal);
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float3 viewDir = normalize(i.viewDir);
                
                float NdotL = dot(normal, lightDir);
                float shadow = smoothstep(_ShadowThreshold - _ShadowSoftness, _ShadowThreshold + _ShadowSoftness, NdotL * 0.5 + 0.5);
                
                // Apply shadow tolerance - reduces shadow intensity
                shadow = lerp(shadow, 1.0, _ShadowTolerance);
                
                float rim = 1.0 - saturate(dot(viewDir, normal));
                rim = pow(rim, _RimPower) * _RimIntensity;
                
                float3 baseColor = texColor.rgb;
                float3 shadowedColor = lerp(baseColor * _ShadowColor.rgb, baseColor, shadow);
                float3 finalColor = shadowedColor + (_RimColor.rgb * rim);
                
                return fixed4(finalColor, texColor.a);
            }
            ENDCG
        }
    }
    
    FallBack "Transparent/Diffuse"
}
