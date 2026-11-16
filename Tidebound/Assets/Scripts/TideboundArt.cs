using System.Collections.Generic;
using UnityEngine;

namespace Tidebound
{
    // Authored procedural scenery: faceted silhouettes, warm wood and a bright coast.
    public static class TideboundArt
    {
        static readonly Dictionary<Color,Material> materials=new Dictionary<Color,Material>();
        public static Material Material(Color c)
        {
            if(materials.TryGetValue(c,out var found)&&found!=null)return found;
            var m=new Material(Shader.Find("Standard"));m.color=c;m.SetFloat("_Glossiness",.05f);
            materials[c]=m;return m;
        }
        public static GameObject Block(string name,Vector3 position,Vector3 size,Color color, bool collider=true)
        {
            var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.position=position;g.transform.localScale=size;
            g.GetComponent<Renderer>().sharedMaterial=Material(color);if(!collider)Object.Destroy(g.GetComponent<Collider>());return g;
        }
        public static GameObject Facet(string name, Vector3 p, Vector3 size, Color c, int sides=7, float top=.45f)
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                Vector3 loA=new Vector3(Mathf.Cos(a)*.5f,-.5f,Mathf.Sin(a)*.5f),loB=new Vector3(Mathf.Cos(b)*.5f,-.5f,Mathf.Sin(b)*.5f);
                Vector3 hiA=new Vector3(loA.x*top,.5f,loA.z*top),hiB=new Vector3(loB.x*top,.5f,loB.z*top);
                AddTriangle(vertices,triangles,loA,hiA,hiB);AddTriangle(vertices,triangles,loA,hiB,loB);
                AddTriangle(vertices,triangles,Vector3.up*.5f,hiB,hiA);
                AddTriangle(vertices,triangles,Vector3.down*.5f,loA,loB);
            }
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            var g=new GameObject(name);g.transform.position=p;g.transform.localScale=size;
            g.AddComponent<MeshFilter>().sharedMesh=mesh;g.AddComponent<MeshRenderer>().sharedMaterial=Material(c);return g;
        }
        static void AddTriangle(List<Vector3> v,List<int> t,Vector3 a,Vector3 b,Vector3 c)
        {int n=v.Count;v.Add(a);v.Add(b);v.Add(c);t.Add(n);t.Add(n+1);t.Add(n+2);}
        public static void Tree(Vector3 p, Color leaf, int variant)
        {
            Block("Angular tree trunk",p+Vector3.up*.9f,new Vector3(.4f,2,.4f),new Color(.38f,.25f,.16f));
            for(int k=0;k<3;k++) Facet("Layered evergreen crown",p+Vector3.up*(2+k*.75f),new Vector3(3.3f-k*.65f,1.9f,3.3f-k*.65f),leaf*(1+k*.08f),5+variant%2,.02f);
        }
        public static void DressIsland(Vector3 o,int zone)
        {
            Color wood=new Color(.49f,.31f,.18f),cream=new Color(.97f,.87f,.64f);
            Color accent=zone==0?new Color(.22f,.58f,.54f):zone==1?new Color(.77f,.32f,.22f):new Color(.37f,.39f,.67f);
            // The footpath stays clear between the pier, stall and village.
            for(int j=0;j<7;j++)
                Block("Stepping stone",o+new Vector3(-.4f+j%2*.35f,.34f,8-j*1.1f),new Vector3(1.5f,.08f,.75f),cream*.85f);
            for(int j=0;j<9;j++)
            {
                float a=j*2.4f;Vector3 p=o+new Vector3(Mathf.Cos(a)*10,.4f,Mathf.Sin(a)*10);
                Facet("Coastal boulder",p,new Vector3(1.7f+j%3*.4f,1.3f,1.8f),new Color(.43f,.5f,.48f),5,.55f);
            }
            for(int j=0;j<3;j++)
            {
                Vector3 p=o+new Vector3((j-1)*5,1.45f,-4-Mathf.Abs(j-1));
                Block("Painted village cottage",p,new Vector3(3.5f,2.5f,3),Color.Lerp(cream,accent,j*.2f));
                var roof=Block("Pitched roof left",p+new Vector3(-.9f,1.6f,0),new Vector3(2.3f,.25f,3.7f),accent);roof.transform.rotation=Quaternion.Euler(0,0,25);
                roof=Block("Pitched roof right",p+new Vector3(.9f,1.6f,0),new Vector3(2.3f,.25f,3.7f),accent);roof.transform.rotation=Quaternion.Euler(0,0,-25);
                Block("Cottage door",p+new Vector3(0,-.35f,1.52f),new Vector3(.7f,1.7f,.08f),wood,false);
                foreach(float x in new[]{-1.1f,1.1f})
                {
                    Block("Window frame",p+new Vector3(x,.15f,1.54f),new Vector3(.8f,.85f,.1f),cream,false);
                    Block("Blue window glass",p+new Vector3(x,.15f,1.61f),new Vector3(.62f,.65f,.05f),new Color(.22f,.49f,.63f),false);
                }
                Block("Chimney",p+new Vector3(1,2,0),new Vector3(.5f,1.4f,.5f),wood);
            }
            // Timber stall details: stripe awning, counter, crates and original NPC.
            for(int j=0;j<7;j++)Block("Striped canvas awning",o+new Vector3(-5.5f+j*.5f,2.85f,4.2f),new Vector3(.48f,.12f,1.7f),j%2==0?accent:cream,false);
            Block("Trader counter",o+new Vector3(-4,1,4.4f),new Vector3(3,.3f,.7f),wood);
            for(int j=0;j<3;j++)
            {
                Vector3 p=o+new Vector3(-6+j*.8f,.7f,5.4f);Block("Tackle crate",p,Vector3.one*.65f,wood);
                Block("Crate band",p+Vector3.up*.15f,new Vector3(.68f,.08f,.68f),cream*.6f,false);
            }
            var trader=new GameObject("Mara the tidekeeper");trader.transform.position=o+new Vector3(-4,.35f,4);
            DressAngler(trader.transform,accent);
            foreach(float x in new[]{-1.35f,1.35f})
            {
                Block("Lantern post",o+new Vector3(x,1.4f,16.6f),new Vector3(.16f,2.2f,.16f),wood);
                Block("Lantern cage",o+new Vector3(x,2.5f,16.6f),new Vector3(.4f,.5f,.4f),wood,false);
                var glass=Block("Lantern glow",o+new Vector3(x,2.5f,16.83f),new Vector3(.27f,.34f,.04f),new Color(1,.79f,.35f),false);
                glass.GetComponent<Renderer>().sharedMaterial.EnableKeyword("_EMISSION");glass.GetComponent<Renderer>().sharedMaterial.SetColor("_EmissionColor",new Color(1,.48f,.12f)*.5f);
            }
            Vector3 tower=o+new Vector3(7,2,-2);
            Facet("Beacon tower",tower,new Vector3(2.2f,4.2f,2.2f),cream,8,.7f);
            Facet("Beacon gallery",tower+Vector3.up*2.2f,new Vector3(2.5f,.35f,2.5f),accent,8,1);
            Facet("Beacon lantern",tower+Vector3.up*2.9f,new Vector3(1.3f,1.2f,1.3f),new Color(1,.79f,.39f),8,1);
            Facet("Beacon cap",tower+Vector3.up*3.7f,new Vector3(2.3f,.7f,2.3f),accent,8,0);
            Vector3 boat=o+new Vector3(4,.05f,14);
            Facet("Moored research skiff",boat,new Vector3(2.2f,.6f,4.5f),accent,6,.8f);
            Block("Skiff seat",boat+Vector3.up*.35f,new Vector3(1.8f,.15f,.45f),wood,false);
            Block("Skiff mast",boat+new Vector3(0,1.6f,-.7f),new Vector3(.1f,3,.1f),wood,false);
            Block("Skiff pennant",boat+new Vector3(.45f,2.6f,-.7f),new Vector3(.9f,.55f,.07f),cream,false);
            // Decorative shoreline ribbons and schools sit below the dock collision surface.
            for(int j=0;j<18;j++)
            {
                float a=j*Mathf.PI*2/18;
                var foam=Block("Shoreline foam",o+new Vector3(Mathf.Cos(a)*12.7f,-.12f,Mathf.Sin(a)*12.7f),new Vector3(1.8f,.025f,.16f),new Color(.68f,.93f,.87f),false);
                foam.transform.rotation=Quaternion.Euler(0,-a*Mathf.Rad2Deg,0);
            }
        }
        public static void DressAngler(Transform root,Color coat)
        {
            Attach(root,"Oilskin jacket",new Vector3(0,1.15f,0),new Vector3(.7f,.75f,.4f),coat);
            Attach(root,"Canvas backpack",new Vector3(0,1.2f,-.29f),new Vector3(.5f,.55f,.22f),new Color(.34f,.29f,.19f));
            Attach(root,"Head",new Vector3(0,1.83f,0),new Vector3(.46f,.45f,.43f),new Color(.88f,.68f,.47f));
            Attach(root,"Hat brim",new Vector3(0,2.07f,0),new Vector3(.8f,.09f,.7f),new Color(.23f,.35f,.29f));
            Attach(root,"Hat crown",new Vector3(0,2.19f,0),new Vector3(.5f,.2f,.46f),new Color(.28f,.41f,.31f));
            Attach(root,"Hat ribbon",new Vector3(0,2.12f,.235f),new Vector3(.51f,.07f,.02f),new Color(1,.69f,.24f));
            foreach(float side in new[]{-1f,1f})
            {
                Attach(root,"Boot",new Vector3(side*.2f,.16f,.07f),new Vector3(.29f,.3f,.5f),new Color(.16f,.22f,.23f));
                Attach(root,"Trouser leg",new Vector3(side*.2f,.57f,0),new Vector3(.27f,.55f,.31f),new Color(.19f,.32f,.37f));
                Attach(root,"Sleeve",new Vector3(side*.48f,1.12f,.02f),new Vector3(.24f,.62f,.29f),coat);
                Attach(root,"Hand",new Vector3(side*.48f,.77f,.04f),new Vector3(.22f,.2f,.25f),new Color(.88f,.68f,.47f));
                Attach(root,"Eye",new Vector3(side*.11f,1.88f,.22f),new Vector3(.045f,.055f,.025f),new Color(.12f,.18f,.2f));
            }
        }
        static void Attach(Transform root,string name,Vector3 p,Vector3 size,Color c)
        {var g=Block(name,Vector3.zero,size,c,false);g.transform.SetParent(root,false);g.transform.localPosition=p;}
        public static void Sky()
        {
            for(int i=0;i<16;i++)
            {
                Vector3 p=new Vector3(-60+i*15,25+i%3*5,-55-i%4*15);
                for(int j=0;j<3;j++)Facet("Faceted cloud",p+new Vector3(j*4,j%2,0),new Vector3(10,2.5f,5),new Color(.94f,.97f,.94f),6,.8f);
            }
        }
    }
}
