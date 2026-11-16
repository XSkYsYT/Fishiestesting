using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tidebound
{
    // Self-contained prototype. No downloaded assets or runtime packages required.
    public sealed class TideboundGame : MonoBehaviour
    {
        enum Phase { Explore, Charge, Waiting, Bite, Reeling }
        [Serializable] public sealed class SaveData
        {
            public int coins, rod, zone;
            public List<int> caught = new List<int>();
            public List<int> bag = new List<int>();
        }
        sealed class Species
        {
            public string name, rarity; public int value, zone; public float difficulty; public Color color;
            public Species(string n, string r, int v, int z, float d, Color c)
            { name=n; rarity=r; value=v; zone=z; difficulty=d; color=c; }
        }
        readonly Species[] fish = {
            new Species("Copperfin", "Common", 18, 0, .65f, new Color(1,.52f,.23f)),
            new Species("Glass Minnow", "Common", 24, 0, .8f, new Color(.5f,.95f,1)),
            new Species("Mossback", "Unusual", 48, 0, 1.05f, new Color(.43f,.75f,.35f)),
            new Species("Lantern Ray", "Rare", 115, 0, 1.3f, new Color(1,.82f,.3f)),
            new Species("Ribbon Eel", "Unusual", 65, 1, 1.05f, new Color(.65f,.4f,.9f)),
            new Species("Ember Koi", "Rare", 150, 1, 1.45f, new Color(1,.3f,.2f)),
            new Species("Crown Nautilus", "Mythic", 390, 1, 1.75f, new Color(1,.75f,.45f)),
            new Species("Moonveil", "Rare", 180, 2, 1.5f, new Color(.4f,.7f,1)),
            new Species("Starwhale Fry", "Mythic", 550, 2, 1.95f, new Color(.8f,.5f,1))
        };
        readonly string[] zoneNames = { "Driftwood Cove", "Ember Atoll", "Moonwake Reach" };
        readonly Vector3[] zoneCenters = { new Vector3(0,0,0), new Vector3(65,0,0), new Vector3(130,0,0) };
        SaveData save;
        Phase phase;
        CharacterController player;
        Camera cam;
        Transform rodTip, bobber;
        LineRenderer line;
        Material water;
        float yaw=20, pitch=18, charge, chargeClock, timer, reel, tension, fishPosition=.5f, cursor=.5f, velocity;
        float elapsed, sonarUntil, sonarReady, noticeUntil;
        int hooked, currentZone;
        bool journal, shop, paused, sonarCast;
        string notice="Welcome to Driftwood Cove. Walk onto the dock and cast toward the sea.";
        GUIStyle heading, body, small, button;
        Texture2D pixel;
        AudioSource sound;
        AudioClip ping;
        const string SaveKey="Tidebound.prototype.v1";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<TideboundGame>() == null)
                new GameObject("Tidebound").AddComponent<TideboundGame>();
        }
        void Start()
        {
            Application.targetFrameRate=60;
            try { save=JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey,"")); }
            catch { save=null; }
            if(save==null) save=new SaveData();
            save.rod=Mathf.Clamp(save.rod,0,3); save.zone=Mathf.Clamp(save.zone,0,2);
            if(save.caught==null) save.caught=new List<int>();
            if(save.bag==null) save.bag=new List<int>();
            save.caught.RemoveAll(i=>i<0||i>=fish.Length); save.bag.RemoveAll(i=>i<0||i>=fish.Length);
            BuildWorld(); Teleport(0); noticeUntil=12;
            sound=gameObject.AddComponent<AudioSource>();
            float[] samples=new float[6615];
            for(int i=0;i<samples.Length;i++) samples[i]=Mathf.Sin(i*2*Mathf.PI*660/44100f)*.12f*(1-i/(float)samples.Length);
            ping=AudioClip.Create("Sonar", samples.Length,1,44100,false); ping.SetData(samples,0);
        }
        Material Mat(Color c)
        {
            var m=new Material(Shader.Find("Standard")); m.color=c; m.SetFloat("_Glossiness",.12f); return m;
        }
        GameObject Shape(string name, PrimitiveType type, Vector3 p, Vector3 s, Color color, bool solid=true)
        {
            var g=GameObject.CreatePrimitive(type); g.name=name; g.transform.position=p; g.transform.localScale=s;
            g.GetComponent<Renderer>().sharedMaterial=Mat(color);
            if(!solid) Destroy(g.GetComponent<Collider>()); return g;
        }
        void BuildWorld()
        {
            RenderSettings.ambientLight=new Color(.70f,.79f,.80f);
            QualitySettings.shadows=ShadowQuality.All; QualitySettings.shadowDistance=90;
            RenderSettings.fog=true; RenderSettings.fogColor=new Color(.60f,.81f,.88f);
            RenderSettings.fogMode=FogMode.ExponentialSquared; RenderSettings.fogDensity=.004f;
            var sun=new GameObject("Sun").AddComponent<Light>(); sun.type=LightType.Directional;
            sun.color=new Color(1,.94f,.80f); sun.intensity=1.2f; sun.transform.rotation=Quaternion.Euler(42,-25,0); sun.shadows=LightShadows.Soft;
            var sea=Shape("Ocean",PrimitiveType.Plane,new Vector3(65,-.25f,0),new Vector3(65,1,65),new Color(.06f,.45f,.56f),false);
            var waterShader=Shader.Find("Tidebound/Coastal Water");
            if(waterShader!=null) sea.GetComponent<Renderer>().sharedMaterial=new Material(waterShader);
            water=sea.GetComponent<Renderer>().sharedMaterial;
            TideboundArt.Sky();
            for(int z=0;z<3;z++)
            {
                Vector3 o=zoneCenters[z];
                Color grass=z==1?new Color(.70f,.61f,.30f):z==2?new Color(.40f,.52f,.64f):new Color(.40f,.68f,.30f);
                Shape("Island rock",PrimitiveType.Cylinder,o+new Vector3(0,-1,0),new Vector3(23,1.4f,23),new Color(.45f,.49f,.45f));
                Shape("Island turf",PrimitiveType.Cylinder,o,new Vector3(22,.3f,22),grass);
                Shape("Sandy shore",PrimitiveType.Cylinder,o+new Vector3(0,-.1f,0),new Vector3(25,.15f,25),new Color(.84f,.76f,.53f));
                for(int j=0;j<12;j++)
                {
                    float a=j*2.39996f; Vector3 p=o+new Vector3(Mathf.Cos(a)*8,1,Mathf.Sin(a)*8);
                    if(p.z>3 && Mathf.Abs(p.x-o.x)<4) continue;
                    TideboundArt.Tree(p-Vector3.up,grass*.78f,j);
                }
                for(int j=0;j<12;j++) Shape("Dock plank",PrimitiveType.Cube,o+new Vector3(0,.5f,9+j*.7f),new Vector3(3,.22f,.63f),new Color(.48f,.32f,.21f));
                foreach(float x in new[]{-1.35f,1.35f})
                    foreach(float zz in new[]{10f,14f,17f}) Shape("Dock piling",PrimitiveType.Cylinder,o+new Vector3(x,0,zz),new Vector3(.22f,2,.22f),new Color(.31f,.24f,.18f));
                var hut=Shape("Harbor stall",PrimitiveType.Cube,o+new Vector3(-4,1.4f,3),new Vector3(3,2.2f,2.5f),new Color(.70f,.46f,.28f));
                Shape("Stall canopy",PrimitiveType.Cube,hut.transform.position+Vector3.up*1.3f,new Vector3(3.6f,.3f,3),new Color(.15f,.48f,.47f));
                TideboundArt.DressIsland(o,z);
                Label("MARA / HARBOR TRADER  [E]",o+new Vector3(-4,3.8f,3));
                Label(zoneNames[z].ToUpperInvariant(),o+new Vector3(0,4,0));
                // Buoys mark the fishing water and are visible from the end of each dock.
                for(int j=0;j<5;j++) Shape("Sonar buoy",PrimitiveType.Sphere,o+new Vector3((j-2)*4,0,23+j%2*3),new Vector3(.5f,.6f,.5f),new Color(1,.7f,.3f),false);
            }
            var actor=new GameObject("Angler"); player=actor.AddComponent<CharacterController>();
            player.height=1.8f; player.radius=.35f; player.center=Vector3.up*.9f; player.stepOffset=.4f;
            TideboundArt.DressAngler(actor.transform,new Color(.97f,.64f,.23f));
            var rod=Shape("Fishing rod",PrimitiveType.Cylinder,Vector3.zero,new Vector3(.055f,1.5f,.055f),new Color(.24f,.16f,.1f),false);
            rod.transform.SetParent(actor.transform,false); rod.transform.localPosition=new Vector3(.5f,1.8f,.7f); rod.transform.localRotation=Quaternion.Euler(40,0,-15);
            rodTip=new GameObject("Rod tip").transform; rodTip.SetParent(rod.transform,false); rodTip.localPosition=Vector3.up;
            bobber=Shape("Bobber",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.25f,new Color(1,.37f,.2f),false).transform; bobber.gameObject.SetActive(false);
            line=new GameObject("Fishing line").AddComponent<LineRenderer>(); line.sharedMaterial=Mat(new Color(.9f,.95f,1)); line.startWidth=.018f; line.endWidth=.012f; line.positionCount=2; line.enabled=false;
            if(Camera.main!=null) Camera.main.gameObject.SetActive(false);
            cam=new GameObject("Tidebound Camera").AddComponent<Camera>(); cam.tag="MainCamera"; cam.fieldOfView=55;
            cam.backgroundColor=new Color(.60f,.81f,.88f); cam.clearFlags=CameraClearFlags.SolidColor; cam.gameObject.AddComponent<AudioListener>();
        }
        void Label(string text, Vector3 p)
        {
            var g=new GameObject(text); g.transform.position=p; g.transform.rotation=Quaternion.Euler(0,180,0);
            var t=g.AddComponent<TextMesh>(); t.text=text; t.fontSize=48; t.characterSize=.065f; t.anchor=TextAnchor.MiddleCenter; t.color=Color.white;
        }
        void Teleport(int z)
        {
            Cancel(); currentZone=z; player.enabled=false; player.transform.position=zoneCenters[z]+new Vector3(0,.6f,11); player.enabled=true; yaw=0; pitch=18;
        }
        bool ActionDown() { return Input.GetMouseButtonDown(0)||Input.GetKeyDown(KeyCode.Space); }
        bool ActionHeld() { return Input.GetMouseButton(0)||Input.GetKey(KeyCode.Space); }
        void Update()
        {
            elapsed+=Time.deltaTime;
            if(Input.GetKeyDown(KeyCode.Escape)) { paused=!paused; journal=false; shop=false; }
            if(Input.GetKeyDown(KeyCode.J)&&phase==Phase.Explore&&!paused) { journal=!journal; shop=false; }
            if(Input.GetKeyDown(KeyCode.E)&&phase==Phase.Explore&&!paused)
            {
                if(Vector3.Distance(player.transform.position,zoneCenters[currentZone]+new Vector3(-4,1,3))<6) { shop=!shop; journal=false; }
                else Notify("The trader is in the wooden stall beside the dock.");
            }
            if(paused||journal||shop) return;
            if(Input.GetMouseButton(1)) { yaw+=Input.GetAxis("Mouse X")*3; pitch=Mathf.Clamp(pitch-Input.GetAxis("Mouse Y")*2,5,65); }
            if(Input.GetKeyDown(KeyCode.Q)&&Time.time>=sonarReady&&phase==Phase.Explore)
            { sonarUntil=Time.time+10; sonarReady=Time.time+18; sound.PlayOneShot(ping); Notify("Sonar active: your next cast attracts an unusual catch."); }
            if(phase==Phase.Explore)
            {
                Vector3 move=Quaternion.Euler(0,yaw,0)*new Vector3(Input.GetAxisRaw("Horizontal"),0,Input.GetAxisRaw("Vertical"));
                move=Vector3.ClampMagnitude(move,1)*5;
                if(move.sqrMagnitude>.1f) player.transform.rotation=Quaternion.Slerp(player.transform.rotation,Quaternion.LookRotation(move),Time.deltaTime*12);
                velocity=player.isGrounded?-2:velocity-22*Time.deltaTime;
                player.Move((move+Vector3.up*velocity)*Time.deltaTime);
                if(player.transform.position.y<-.8f) { Teleport(currentZone); Notify("Back on the dock. Your catch is safe."); }
                if(ActionDown()&&Input.mousePosition.y<Screen.height-92)
                {
                    Vector3 local=player.transform.position-zoneCenters[currentZone];
                    if(local.z<12||Mathf.Abs(local.x)>2.3f) Notify("Walk to the end of the dock to cast.");
                    else { phase=Phase.Charge; charge=0; chargeClock=0; }
                }
            }
            else if(phase==Phase.Charge)
            {
                chargeClock+=Time.deltaTime*.65f; charge=Mathf.PingPong(chargeClock,1);
                if(!ActionHeld())
                {
                    float power=Mathf.Clamp01(charge); sonarCast=Time.time<sonarUntil; sonarUntil=0;
                    bobber.position=zoneCenters[currentZone]+new Vector3(UnityEngine.Random.Range(-2f,2f),0,22+power*8);
                    bobber.gameObject.SetActive(true); line.enabled=true; timer=UnityEngine.Random.Range(2,5)-power; phase=Phase.Waiting;
                }
            }
            else if(phase==Phase.Waiting)
            {
                timer-=Time.deltaTime;
                if(timer<=0) { phase=Phase.Bite; timer=1.7f; sound.PlayOneShot(ping,1); }
            }
            else if(phase==Phase.Bite)
            {
                timer-=Time.deltaTime;
                if(ActionDown()) { ChooseFish(); phase=Phase.Reeling; reel=.2f; tension=.15f; cursor=.5f; }
                else if(timer<=0) { Cancel(); Notify("Missed the bite. Cast again!"); }
            }
            else if(phase==Phase.Reeling)
            {
                float d=fish[hooked].difficulty;
                fishPosition=.5f+Mathf.Sin(elapsed*(1.1f+d)*1.3f)*.29f+Mathf.Sin(elapsed*3.7f)*.12f;
                cursor=Mathf.Clamp01(cursor+(ActionHeld()?1:-1)*Time.deltaTime*.72f);
                bool aligned=Mathf.Abs(cursor-fishPosition)<.12f+save.rod*.025f;
                reel+=Time.deltaTime*(aligned?.22f:-.065f);
                tension=Mathf.Clamp01(tension+Time.deltaTime*(aligned?-.3f:.23f*d));
                if(reel>=1)
                {
                    save.bag.Add(hooked); if(!save.caught.Contains(hooked)) save.caught.Add(hooked);
                    Persist(); Notify("Caught a "+fish[hooked].name+"! Worth "+fish[hooked].value+" shells."); Cancel(); sound.PlayOneShot(ping,.7f);
                }
                else if(tension>=1||reel<=0) { Cancel(); Notify("The fish escaped. Keep the marker over the fish to land it."); }
            }
            if(Input.GetKeyDown(KeyCode.X)) Cancel();
            if(bobber.gameObject.activeSelf)
            {
                var p=bobber.position; p.y=Mathf.Sin(elapsed*3)*.07f; bobber.position=p;
                line.SetPosition(0,rodTip.position); line.SetPosition(1,bobber.position);
            }
            water.color=Color.Lerp(new Color(.045f,.53f,.58f),new Color(.08f,.65f,.66f),.5f+Mathf.Sin(elapsed*.1f)*.5f);
        }
        void LateUpdate()
        {
            if(cam==null||player==null) return;
            Vector3 target=player.transform.position+Vector3.up*1.5f;
            cam.transform.position=target+Quaternion.Euler(pitch,yaw,0)*new Vector3(0,0,-8.5f);
            cam.transform.LookAt(target);
        }
        void ChooseFish()
        {
            var choices=new List<int>();
            for(int i=0;i<fish.Length;i++) if(fish[i].zone==currentZone)
            {
                int weight=fish[i].rarity=="Common"?10:fish[i].rarity=="Unusual"?5:fish[i].rarity=="Rare"?2:1;
                if(sonarCast&&fish[i].rarity!="Common") weight*=3;
                for(int j=0;j<weight;j++) choices.Add(i);
            }
            hooked=choices[UnityEngine.Random.Range(0,choices.Count)];
        }
        void Cancel() { phase=Phase.Explore; if(bobber!=null) bobber.gameObject.SetActive(false); if(line!=null) line.enabled=false; }
        void Notify(string n) { notice=n; noticeUntil=Time.time+6; }
        void Persist() { PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(save)); PlayerPrefs.Save(); }
        void OnApplicationQuit() { if(save!=null) Persist(); }
        void Styles()
        {
            if(pixel!=null)return;
            pixel=new Texture2D(1,1); pixel.SetPixel(0,0,Color.white); pixel.Apply();
            heading=new GUIStyle(GUI.skin.label){fontSize=25,fontStyle=FontStyle.Bold}; heading.normal.textColor=new Color(.96f,.88f,.67f);
            body=new GUIStyle(GUI.skin.label){fontSize=17,wordWrap=true}; body.normal.textColor=Color.white;
            small=new GUIStyle(body){fontSize=13};
            button=new GUIStyle(GUI.skin.button){fontSize=16,padding=new RectOffset(10,10,8,8)};
        }
        void Box(Rect r,Color c) { GUI.color=c; GUI.DrawTexture(r,pixel); GUI.color=Color.white; }
        void Text(float x,float y,float w,string s,GUIStyle st=null) { GUI.Label(new Rect(x,y,w,65),s,st??body); }
        void Bar(float x,float y,float w,float value,Color c)
        { Box(new Rect(x,y,w,12),new Color(.07f,.15f,.19f)); Box(new Rect(x,y,w*Mathf.Clamp01(value),12),c); }
        void OnGUI()
        {
            if(save==null||player==null)return; Styles();
            float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f); GUI.matrix=Matrix4x4.Scale(new Vector3(scale,scale,1));
            float w=Screen.width/scale,h=Screen.height/scale;
            Box(new Rect(20,20,310,100),new Color(.07f,.12f,.15f,.88f));
            Text(36,28,290,"T I D E B O U N D",heading);
            Box(new Rect(20,20,4,100),new Color(.94f,.73f,.34f)); Text(36,70,290,zoneNames[currentZone]+"  /  "+(currentZone==2?"Moonlit waters":"Coastal waters"),small);
            Box(new Rect(w-305,20,285,100),new Color(.07f,.12f,.15f,.88f));
            Text(w-287,32,260,save.coins+" SHELLS    /    ROD "+(save.rod+1));
            Text(w-287,68,260,"Bag "+save.bag.Count+"  /  Journal "+save.caught.Count+" of "+fish.Length,small);
            Box(new Rect(20,h-64,w-40,44),new Color(.07f,.12f,.15f,.88f));
            Text(35,h-55,w-70,"WASD  Move     RMB + drag  Look     SPACE / LMB  Fish     Q  Sonar     E  Trader     J  Journal     X  Cancel     ESC  Pause",small);
            if(Time.time<noticeUntil) { Box(new Rect(w/2-315,135,630,65),new Color(.035f,.10f,.14f,.9f)); Text(w/2-300,147,600,notice); }
            if(!paused&&!journal&&!shop)
            {
                string msg=phase==Phase.Explore?"CAST INTO THE UNKNOWN\nWalk to the dock's end. Hold SPACE to cast.":phase==Phase.Charge?"CHOOSE YOUR CAST\nRelease SPACE or LMB to send your line.":phase==Phase.Waiting?"WATCH THE BOBBER\nWait for a bite...":phase==Phase.Bite?"BITE!  PRESS SPACE NOW":"HOLD TO RISE. RELEASE TO FALL.\nKeep your marker over the moving fish.";
                Box(new Rect(w/2-260,h-220,520,140),new Color(.07f,.12f,.15f,.88f)); Text(w/2-240,h-208,480,msg);
                if(phase==Phase.Charge) Bar(w/2-230,h-130,460,charge,new Color(1,.75f,.3f));
                if(phase==Phase.Bite)
                {
                    Bar(w/2-230,h-130,460,timer/1.7f,new Color(1,.65f,.2f));
                    Vector3 hit=cam.WorldToScreenPoint(bobber.position+Vector3.up*1.2f);
                    if(hit.z>0) Text(hit.x/scale-20,(Screen.height-hit.y)/scale-20,90,"!",heading);
                }
                if(phase==Phase.Reeling)
                {
                    float x=w/2-230,y=h-142;
                    Box(new Rect(x,y,460,20),new Color(.15f,.26f,.3f));
                    float band=.12f+save.rod*.025f;
                    Box(new Rect(x+Mathf.Max(0,fishPosition-band)*460,y,Mathf.Min(2*band,1-fishPosition+band)*460,20),new Color(.2f,.68f,.6f));
                    Box(new Rect(x+cursor*456,y-3,4,26),Color.white);
                    Bar(x,y+30,220,reel,new Color(.36f,.86f,.65f)); Bar(x+240,y+30,220,tension,new Color(1,.45f,.3f));
                    Text(x,y+43,460,"LANDED                                     LINE TENSION",small);
                }
                if(phase==Phase.Explore) Text(w/2-230,h-133,460,Time.time<sonarUntil?"SONAR ACTIVE  /  Cast now for improved rare odds":Time.time>=sonarReady?"SONAR READY  /  Press Q":"SONAR RECHARGING  /  "+Mathf.CeilToInt(sonarReady-Time.time)+"s",small);
            }
            if(journal) DrawJournal(w,h);
            if(shop) DrawShop(w,h);
            if(paused)
            {
                Panel(w,h,460,260); Text(w/2-200,h/2-110,400,"TAKE A BREATHER",heading);
                Text(w/2-200,h/2-55,400,"Your catches and upgrades save automatically.\nOriginal single-player Unity prototype.");
                if(GUI.Button(new Rect(w/2-200,h/2+35,400,40),"Continue",button))paused=false;
                if(GUI.Button(new Rect(w/2-200,h/2+85,400,35),"Save & quit",button)){Persist();Application.Quit();}
            }
        }
        void Panel(float w,float h,float pw,float ph)
        { Box(new Rect(0,0,w,h),new Color(0,0,0,.5f)); Box(new Rect(w/2-pw/2,h/2-ph/2,pw,ph),new Color(.07f,.12f,.15f,.98f)); }
        void DrawJournal(float w,float h)
        {
            Panel(w,h,780,510); float x=w/2-365,y=h/2-235;
            Text(x,y,700,"THE FIELD JOURNAL",heading); Text(x,y+40,700,"Discover nine species across three fishing grounds.  [J] Close",small);
            for(int i=0;i<fish.Length;i++)
            {
                float xx=x+i%3*248,yy=y+90+i/3*125; bool found=save.caught.Contains(i);
                Box(new Rect(xx,yy,232,110),new Color(.09f,.19f,.23f));
                Box(new Rect(xx+12,yy+15,7,75),found?fish[i].color:Color.gray);
                Text(xx+30,yy+10,195,found?fish[i].name:"Undiscovered");
                Text(xx+30,yy+44,195,found?fish[i].rarity+"  /  "+fish[i].value+" shells":zoneNames[fish[i].zone],small);
            }
        }
        void DrawShop(float w,float h)
        {
            Panel(w,h,700,510); float x=w/2-320,y=h/2-235;
            Text(x,y,640,"THE HARBOR TRADER",heading); Text(x,y+45,640,"Sell your haul. Improve your control. Follow the next current.  [E] Close",small);
            int total=0; foreach(int i in save.bag) total+=fish[i].value;
            if(GUI.Button(new Rect(x,y+90,640,48),"Sell "+save.bag.Count+" fish  /  +"+total+" shells",button)&&save.bag.Count>0)
            { save.coins+=total; save.bag.Clear(); Persist(); Notify("Haul sold for "+total+" shells."); }
            int cost=120*(save.rod+1);
            GUI.enabled=save.rod<3&&save.coins>=cost;
            if(GUI.Button(new Rect(x,y+150,640,48),save.rod>=3?"Rod fully upgraded":"Upgrade rod  /  "+cost+" shells  /  Wider control zone",button))
            { save.coins-=cost; save.rod++; Persist(); }
            GUI.enabled=true; Text(x,y+220,640,"CHART A NEW COURSE",body);
            for(int z=0;z<3;z++)
            {
                int unlock=z*250;
                GUI.enabled=z<=save.zone||(z==save.zone+1&&save.coins>=unlock);
                string caption=z<=save.zone?"Travel to "+zoneNames[z]:"Unlock "+zoneNames[z]+"  /  "+unlock+" shells";
                if(GUI.Button(new Rect(x,y+255+z*53,640,43),caption,button))
                {
                    if(z>save.zone) {save.coins-=unlock;save.zone=z;Persist();}
                    shop=false; Teleport(z); Notify("Welcome to "+zoneNames[z]+".");
                }
            }
            GUI.enabled=true;
        }
    }
}
