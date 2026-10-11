using UnityEngine;

namespace IdleClinic.Presentation
{
    /// <summary>Joined walls at one consistent height with explicit door apertures.</summary>
    internal static class ClinicArchitecture
    {
        internal const float WallHeight=2.05f;

        internal static void Build(ClinicArt art,Transform scene)
        {
            var root=art.Group("Joined clinic walls",scene);
            Wall(art,root,"West wall",new Vector2(-5.65f,-5.47f),new Vector2(-5.65f,5.05f),WallHeight,.16f);
            Wall(art,root,"North treatment cutaway",new Vector2(-5.73f,4.97f),new Vector2(-.235f,4.97f),WallHeight,.16f);
            Wall(art,root,"North waiting return",new Vector2(1.585f,4.97f),new Vector2(2.955f,4.97f),WallHeight,.16f);
            var staffEntry=art.Group("Staff corridor doorway",root);
            for(int side=0;side<2;side++)
                art.Box("Staff entry jamb",staffEntry,new Vector3(side==0?-.235f:1.585f,1.11f,4.97f),new Vector3(.10f,1.94f,.18f),"SageDark");
            art.Box("Staff entry lintel",staffEntry,new Vector3(.675f,2.15f,4.97f),new Vector3(1.92f,.14f,.18f),"Ivory");
            Wall(art,root,"North toilet return wall",new Vector2(3.92f,4.97f),new Vector2(5.765f,4.97f),WallHeight,.16f);
            Wall(art,root,"East cutaway wall",new Vector2(5.69f,-5.47f),new Vector2(5.69f,5.05f),WallHeight,.15f);
            // Slim front runs leave the authored queue rows a full visitor radius from the wall.
            GlassWall(art,root,"Reception front wall",-5.73f,-.235f);
            GlassWall(art,root,"Lobby front wall",1.585f,5.765f);
            // Independent nursing rooms, each entered directly from the open reception/waiting space.
            Wall(art,root,"Nursing front west",new Vector2(-5.73f,1.50f),new Vector2(-4.875f,1.50f),WallHeight,.15f);
            Wall(art,root,"Nursing front middle",new Vector2(-3.525f,1.50f),new Vector2(-2.175f,1.50f),WallHeight,.15f);
            Wall(art,root,"Nursing front east",new Vector2(-.825f,1.50f),new Vector2(-.235f,1.50f),WallHeight,.15f);
            Wall(art,root,"Nursing east wall",new Vector2(-.235f,1.50f),new Vector2(-.235f,4.97f),WallHeight,.16f);
            for(int side=0;side<2;side++)
                art.Box("Toilet doorway jamb",root,new Vector3(side==0?2.955f:3.92f,1.11f,4.97f),new Vector3(.10f,1.94f,.18f),"SageDark");
            art.Box("Toilet doorway lintel",root,new Vector3(3.4375f,2.15f,4.97f),new Vector3(1.065f,.14f,.18f),"Ivory");
            art.Box("Toilet doorway transom",root,new Vector3(3.4375f,1.91f,4.97f),new Vector3(.87f,.34f,.07f),"Sage");
        }
        private static void GlassWall(ClinicArt art,Transform parent,string name,float start,float end)
        {
            var root=art.Group(name,parent);
            float width=end-start,midpoint=(start+end)*.5f;
            var pane=art.Box(name+" glass",root,new Vector3(midpoint,.14f+WallHeight*.5f,-5.43f),new Vector3(width,WallHeight,.025f),"FacadeGlass");
            var renderer=pane.GetComponent<Renderer>();
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows=false;
            for(int edge=0;edge<2;edge++)
                art.Box(name+" frame rail",root,new Vector3(midpoint,.17f+edge*(WallHeight-.06f),-5.43f),new Vector3(width,.06f,.08f),"SageDark");
            int bays=Mathf.CeilToInt(width/1.4f);
            for(int i=0;i<=bays;i++)
                art.Box(name+" mullion",root,new Vector3(Mathf.Lerp(start,end,(float)i/bays),.14f+WallHeight*.5f,-5.43f),new Vector3(.045f,WallHeight,.08f),"SageDark");
        }
        private static void Wall(ClinicArt art,Transform root,string name,Vector2 start,Vector2 end,float height,float thickness)
        {
            bool horizontal=Mathf.Abs(end.x-start.x)>Mathf.Abs(end.y-start.y);
            var midpoint=(start+end)*.5f;
            var scale=horizontal?new Vector3(Mathf.Abs(end.x-start.x),height,thickness):new Vector3(thickness,height,Mathf.Abs(end.y-start.y));
            art.Box(name,root,new Vector3(midpoint.x,.14f+height*.5f,midpoint.y),scale,"Ivory");
            scale.y=.055f;
            art.Box(name+" cap",root,new Vector3(midpoint.x,.14f+height,midpoint.y),scale,"Sage");
            scale.y=.10f;
            art.Box(name+" skirting",root,new Vector3(midpoint.x,.19f,midpoint.y),scale,"SageDark");
        }
    }
}
