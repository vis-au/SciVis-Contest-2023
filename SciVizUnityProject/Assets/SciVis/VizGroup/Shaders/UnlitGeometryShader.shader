Shader "Unlit/UnlitGeometryShader"
{
    Properties
    {
        _Size("Float with range", Range(0.01, 0.04)) = 0.02
        // _Color ("Main Color", Color) = (1,1,1,1)
        // _MainTex ("Base (RGB)", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" 
               "LightMode" = "ForwardBase"}

        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geo

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            

            struct VertexInput {
                float4 vertex : POSITION;
                float4 color: COLOR;
                half3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2g
            {
                float4 vertex : SV_POSITION;
                float4 color: COLOR;
                half3 normal : NORMAL;

                UNITY_VERTEX_INPUT_INSTANCE_ID 
                UNITY_VERTEX_OUTPUT_STEREO
            };

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float4 color: COLOR0;
                fixed4 diff : COLOR1; // diffuse lighting color
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _Size;

            v2g vert (VertexInput v)
            {
                v2g o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2g,o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                
				UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = v.vertex;
                o.color = v.color;
                o.normal = v.normal;
                return o;
            }

  
            [maxvertexcount(36)]
            void geo(point v2g IN[1], inout TriangleStream<g2f> triStream)
            {
                g2f o;
                UNITY_SETUP_INSTANCE_ID(IN[0]);
                UNITY_INITIALIZE_OUTPUT(g2f,o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                float offset = _Size / 2.0f;


                float4 pos = IN[0].vertex;

                // get vertex normal in world space
                half3 worldNormal = UnityObjectToWorldNormal(half3(0,-1,0));
                // dot product between normal and light direction for
                // standard diffuse (Lambert) lighting
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
             

                //quad 1
                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(0, 0, 0, 1)); //+ float4(0.5, 0, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(_Size, 0, 0, 1)); //+ float4(-0.5, 0, 0, 1)
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(0, 0, _Size, 1)); //+ float4(0, 1, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);  

                triStream.RestartStrip();

                ///
                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(_Size, 0, 0, 1)); //+ float4(0.5, 0, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(_Size, 0, _Size, 1)); //+ float4(-0.5, 0, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(0, 0, _Size, 1)); //+ float4(0, 1, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);        

                triStream.RestartStrip();

                // get vertex normal in world space
                worldNormal = UnityObjectToWorldNormal(half3(0, 0, -1));
                // dot product between normal and light direction for
                // standard diffuse (Lambert) lighting
                nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));

                //quad 2
                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(0, 0, 0, 1)); //+ float4(0.5, 0, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(0, _Size, 0, 1)); //+ float4(0, 1, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(_Size, 0, 0, 1)); //+ float4(-0.5, 0, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                triStream.RestartStrip();


                ///
                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(0, _Size, 0, 1)); //+ float4(0, 1, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(_Size, _Size, 0, 1)); //+ float4(-0.5, 0, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(_Size, 0, 0, 1)); //+ float4(0.5, 0, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

           
                triStream.RestartStrip();

                // get vertex normal in world space
                worldNormal = UnityObjectToWorldNormal(half3(-1, 0, 0));
                // dot product between normal and light direction for
                // standard diffuse (Lambert) lighting
                nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));

                //quad 3
                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(0, 0, 0, 1)); //+ float4(0.5, 0, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(0, 0, _Size, 1)); //+ float4(-0.5, 0, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(0, _Size, 0, 1)); //+ float4(0, 1, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                triStream.RestartStrip();

                ///
                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(0, _Size, 0, 1)); //+ float4(0, 1, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(0, 0, _Size, 1)); //+ float4(-0.5, 0, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(0, _Size, _Size, 1)); //+ float4(-0.5, 0, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

       
                triStream.RestartStrip();

                // get vertex normal in world space
                worldNormal = UnityObjectToWorldNormal(half3(0, 1, 0));
                // dot product between normal and light direction for
                // standard diffuse (Lambert) lighting
                nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));

                //quad 4 (quad 1 translated in y direction)
                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(0, _Size, 0, 1)); //+ float4(0.5, 0, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(0, _Size, _Size, 1)); //+ float4(0, 1, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(_Size, _Size, 0, 1)); //+ float4(-0.5, 0, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                triStream.RestartStrip();
  
                ///
                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(_Size, _Size, 0, 1)); //+ float4(0.5, 0, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(0, _Size, _Size, 1)); //+ float4(0, 1, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(_Size, _Size, _Size, 1)); //+ float4(-0.5, 0, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);
     

                triStream.RestartStrip();


                // get vertex normal in world space
                worldNormal = UnityObjectToWorldNormal(half3(0, 0, 1));
                // dot product between normal and light direction for
                // standard diffuse (Lambert) lighting
                nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));

                //quad 5 (quad 2 translated in z direction)
                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(0, 0, _Size, 1)); //+ float4(0.5, 0, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(_Size, 0, _Size, 1)); //+ float4(-0.5, 0, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(0, _Size, _Size, 1)); //+ float4(0, 1, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                triStream.RestartStrip();


                ///
                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(0, _Size, _Size, 1)); //+ float4(0, 1, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(_Size, 0, _Size, 1)); //+ float4(0.5, 0, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(_Size, _Size, _Size, 1)); //+ float4(-0.5, 0, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);
     

                triStream.RestartStrip();

                // get vertex normal in world space
                worldNormal = UnityObjectToWorldNormal(half3(1, 0, 0));
                // dot product between normal and light direction for
                // standard diffuse (Lambert) lighting
                nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));

                //quad 6 (quad 3 translated in x direction)
                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(_Size, 0, 0, 1)); //+ float4(0.5, 0, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(_Size, _Size, 0, 1)); //+ float4(0, 1, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(_Size, 0, _Size, 1)); //+ float4(-0.5, 0, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                triStream.RestartStrip();


                ///

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(_Size, _Size, 0, 1)); //+ float4(0, 1, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(_Size, _Size, _Size, 1)); //+ float4(-0.5, 0, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                o.vertex = UnityObjectToClipPos(pos + float4(-offset, -offset, -offset, 1) + float4(_Size, 0, _Size, 1)); //+ float4(0.5, 0, 0, 1);
                o.diff = nl * _LightColor0;
                o.color = IN[0].color;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                triStream.Append(o);

                triStream.RestartStrip();
                     
            }

            UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);

            fixed4 frag(g2f i) : SV_Target
            {
                
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                // sample the texture
                fixed4 col = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex,i.color);
                col *= i.diff;
                return col;
            }
            ENDCG
        }
    }
}
