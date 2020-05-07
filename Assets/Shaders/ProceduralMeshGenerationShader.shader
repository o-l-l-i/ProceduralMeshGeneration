Shader "Procedural/ProceduralMeshGeneration"
{

    Properties
    {
        _Color ("Diffuse Color", Color) = (1,1,1,1)
        _SpecularStrength ("Specular Strength", float) = 100
        _SpecularExp ("Specular exponent", float) = 32
        _SpecularColor ("Specular Color", Color) = (1,1,1,1)
    }

    SubShader
    {

        Tags
        {
            "Queue"="Geometry"
            "RenderType"="Opaque"
        }

        LOD 100

        Pass
        {
            Tags { "LightMode" = "ForwardBase" }

            CGPROGRAM

            // https://forum.unity.com/threads/rwstructuredbuffer-in-vertex-shader.406592/
            // https://forum.unity.com/threads/appendstructuredbuffer-as-output-of-compute-buffer-how-to-transfer-to-geometry-shader.664927/

            #pragma target 4.5

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            #pragma multi_compile_fwdbase


            #if SHADER_TARGET >= 45
                uniform StructuredBuffer<float3> vertexBuffer;
                uniform StructuredBuffer<float3> normalBuffer;
            #endif


            float4 _Color;
            float _SpecularStrength;
            float _SpecularExp;
            float4 _SpecularColor;


            struct appdata
            {
                float4 vertex : POSITION;
                // https://docs.unity3d.com/Manual/SL-ShaderSemantics.html
                uint instanceID : SV_VertexID; // vertex ID, needs to be uint
            };


            struct v2f
            {
                float4 pos : POSITION;
                float3 worldNormal : WORLDNORMAL;
                float3 worldPos : TEXCOORD2;
            };


            v2f vert (appdata v)
            {
                v2f o;

                #if SHADER_TARGET >= 45
                    float3 worldPos = vertexBuffer[v.instanceID];
                    float4 pos = UnityObjectToClipPos(float4(worldPos, 1));
                    float3 worldNormal = normalBuffer[v.instanceID];
                    o.pos = pos;
                    o.worldPos = worldPos;
                    o.worldNormal = worldNormal;
                #else
                    o.pos = UnityObjectToClipPos(v.vertex);
                #endif

                return o;
            }


            fixed4 frag (v2f i) : COLOR
            {
                // Light direction
                float3 lightDir = _WorldSpaceLightPos0;
                float3 normal = normalize(i.worldNormal);

                // NDotL
                float NDotL = saturate(dot(normal, lightDir));

                // Light color
                float3 lightColor = _LightColor0.rgb;
                float3 diffuse = NDotL * _Color * lightColor;

                // Specular
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float3 reflectDir = reflect(-lightDir, normal);
                float spec = pow(max(dot(viewDir, reflectDir), 0.0), _SpecularExp);
                float3 specular = _SpecularStrength * spec * _SpecularColor;

                // Ambient
                float3 ambient = UNITY_LIGHTMODEL_AMBIENT;

                // Final composite
                float3 shadedColor = diffuse + ambient + specular;
                UNITY_APPLY_FOG(i.fogCoord, shadedColor);

                return float4(shadedColor, 1);

            }

            ENDCG
        }

    }

}
