Shader "Tidebound/Coastal Water"
{
    Properties { _Color ("Water", Color) = (0.07,0.62,0.66,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        CGPROGRAM
        #pragma surface surf Lambert
        #pragma target 3.0
        fixed4 _Color;
        struct Input { float3 worldPos; };
        void surf(Input IN, inout SurfaceOutput o)
        {
            float2 p=IN.worldPos.xz;
            float ripple=sin(p.x*2.8+sin(p.y*.7+_Time.y)*1.5+_Time.y*.8)*sin(p.y*2.2-_Time.y*.5);
            float glint=smoothstep(.82,.98,ripple);
            float broad=sin(p.x*.16+p.y*.22+_Time.y*.25)*.035;
            o.Albedo=_Color.rgb+broad+glint*float3(.22,.27,.20);
            o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Diffuse"
}
