Shader "Unlit/UnlitGeometryShaderForConnections"
{
    Properties
    {
        _Length("Length of connection", Range(0.01, 1.5)) = 0.01
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
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
                float4 color: COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float _Length;

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

  
            [maxvertexcount(6)]
            void geo(line v2g IN[2], inout LineStream<g2f> lineStream)
            {
                g2f o;
                UNITY_SETUP_INSTANCE_ID(IN[0]);
                UNITY_INITIALIZE_OUTPUT(g2f,o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                //triangle 1
              /*  o.color = IN[0].color;
                o.vertex = UnityObjectToClipPos(IN[0].vertex);
                triStream.Append(o);
                o.vertex = UnityObjectToClipPos(IN[1].vertex);
                triStream.Append(o);
                o.vertex = UnityObjectToClipPos(IN[1].vertex + float4(0, 0.01, 0, 1));
                triStream.Append(o);

                triStream.RestartStrip();

                //triangle 2
                o.color = IN[0].color;
                o.vertex = UnityObjectToClipPos(IN[0].vertex);
                triStream.Append(o);
                o.vertex = UnityObjectToClipPos(IN[0].vertex + float4(0, 0.01, 0, 1));
                triStream.Append(o);
                o.vertex = UnityObjectToClipPos(IN[1].vertex + float4(0, 0.01, 0, 1));
                triStream.Append(o);

                triStream.RestartStrip();*/

             /*   o.color = IN[0].color;
                o.vertex = posa;
                lineStream.Append(o);

                o.color = IN[1].color;
                o.vertex = posb;
                lineStream.Append(o);*/
          

                o.color = IN[0].color;
                float4 pos1 = UnityObjectToClipPos(IN[0].vertex);
                float4 pos2 = UnityObjectToClipPos(IN[1].vertex);

                float delta_x = IN[0].vertex.x - IN[1].vertex.x ;
                float delta_y = IN[0].vertex.y - IN[1].vertex.y ;                
                float distance  = delta_x*delta_x + delta_y*delta_y; 

                o.color = IN[0].color;
                o.vertex = pos1;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                lineStream.Append(o);

                o.color = IN[1].color;
                o.vertex = pos2;
                UNITY_TRANSFER_VERTEX_OUTPUT_STEREO(IN[0], o);
                lineStream.Append(o);
                             
                     
            }

            fixed4 frag(g2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                // sample the texture
                fixed4 col = i.color;
                return col;
            }
            ENDCG
        }
    }
}

