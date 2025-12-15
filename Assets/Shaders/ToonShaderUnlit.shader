Shader "Custom/ToonShaderUnlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        
        [Header(Outline)]
        [Toggle(OUTLINE_ON)] _OutlineEnabled ("Enable Outline", Float) = 1
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth ("Outline Width", Range(0, 0.03)) = 0.005
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        
        // Outline Pass - clip space for accuracy
        Pass
        {
            Name "OUTLINE"
            Cull Front
            ZWrite On
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature OUTLINE_ON
            #include "UnityCG.cginc"
            
            float _OutlineWidth;
            float4 _OutlineColor;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                
                #ifdef OUTLINE_ON
                    float4 clipPos = UnityObjectToClipPos(v.vertex);
                    float3 clipNormal = mul((float3x3)UNITY_MATRIX_VP, mul((float3x3)unity_ObjectToWorld, v.normal));
                    float2 offset = normalize(clipNormal.xy) * _OutlineWidth * clipPos.w * 2;
                    clipPos.xy += offset;
                    o.pos = clipPos;
                #else
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
        
        // Main Pass (unlit)
        Pass
        {
            Name "UNLIT"
            Cull Back
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv) * _Color;
            }
            ENDCG
        }
    }
    
    FallBack "Unlit/Texture"
}
