using UnityEngine;

namespace IdleClinic.Presentation
{
    /// <summary>Permanent staff rooms adjoining the starter clinic's rear corridor.</summary>
    internal static class ClinicSupportRooms
    {
        internal static readonly Rect Office = new Rect(-5.575f,6.10f,3.90f,3.45f);
        internal static readonly Rect Cleaning = new Rect(-1.525f,6.10f,3.10f,3.45f);
        internal static Vector3 Point(bool office) => new Vector3(office?Office.center.x:Cleaning.center.x,.14f,7.1f);

        internal static void Build(ClinicArt art,Transform scene)
        {
            var wing=art.Group("Clinic staff wing",scene);
            art.Box("Staff wing foundation",wing,new Vector3(-2,.005f,7.3f),new Vector3(7.45f,.27f,4.65f),"Clay");
            art.Box("Staff corridor floor",wing,new Vector3(-2,.12f,5.55f),new Vector3(7.30f,.03f,1.05f),"Ivory");
            Wall(art,wing,"Staff wing west wall",new Vector3(-5.65f,0,7.3f),.14f,4.65f,ClinicArchitecture.WallHeight);
            Wall(art,wing,"Staff wing back wall",new Vector3(-2,0,9.65f),7.45f,.14f,ClinicArchitecture.WallHeight);
            Wall(art,wing,"Staff wing east cutaway",new Vector3(1.67f,0,7.3f),.14f,4.65f,ClinicArchitecture.WallHeight);
            Wall(art,wing,"Office utilities partition",new Vector3(-1.60f,0,7.85f),.15f,3.60f,ClinicArchitecture.WallHeight);
            // Real apertures connect each room to the shared corridor; door leaves rest open inside.
            Wall(art,wing,"Office front left",new Vector3(-4.87f,0,6.02f),1.56f,.13f,ClinicArchitecture.WallHeight);
            Wall(art,wing,"Office front right",new Vector3(-2.35f,0,6.02f),1.50f,.13f,ClinicArchitecture.WallHeight);
            Wall(art,wing,"Cleaning front left",new Vector3(-1.04f,0,6.02f),1.12f,.13f,ClinicArchitecture.WallHeight);
            Wall(art,wing,"Cleaning front right",new Vector3(1.11f,0,6.02f),1.12f,.13f,ClinicArchitecture.WallHeight);
            Door(art,wing,"Office open door",new Vector3(-4.08f,.14f,6.04f));
            Door(art,wing,"Cleaning open door",new Vector3(-.47f,.14f,6.04f));
            BuildOffice(art,wing);BuildCleaning(art,wing);
        }

        private static void Wall(ClinicArt art,Transform root,string name,Vector3 p,float width,float depth,float height)
        {
            art.Box(name,root,p+Vector3.up*(.14f+height*.5f),new Vector3(width,height,depth),"Ivory");
            art.Box(name+" cap",root,p+Vector3.up*(.14f+height),new Vector3(width,.045f,depth),"Sage");
            art.Box(name+" skirting",root,p+Vector3.up*.19f,new Vector3(width,.10f,depth),"SageDark");
        }

        private static void Door(ClinicArt art,Transform parent,string name,Vector3 hinge)
        {
            var door=art.Group(name,parent,hinge);
            door.localRotation=Quaternion.Euler(0,-90,0);
            art.Box("Open staff door panel",door,new Vector3(.46f,.45f,0),new Vector3(.92f,.90f,.065f),"Sage");
            art.Box("Staff door handle",door,new Vector3(.78f,.49f,-.055f),new Vector3(.12f,.035f,.045f),"Gold");
        }

        private static void Floor(ClinicArt art,Transform root,Rect bounds,string role)
        {
            art.Box("Room floor",root,new Vector3(bounds.center.x,.14f,bounds.center.y),new Vector3(bounds.width,.035f,bounds.height),role);
            for(float x=bounds.xMin+.45f;x<bounds.xMax;x+=.55f)
                art.Box("Floor joint",root,new Vector3(x,.161f,bounds.center.y),new Vector3(.015f,.004f,bounds.height),"Ivory");
            for(float z=bounds.yMin+.45f;z<bounds.yMax;z+=.55f)
                art.Box("Floor joint",root,new Vector3(bounds.center.x,.162f,z),new Vector3(bounds.width,.004f,.015f),"Ivory");
        }

        private static void BuildOffice(ClinicArt art,Transform parent)
        {
            var root=art.Group("Boss office",parent);Floor(art,root,Office,"TilePeach");
            Window(art,root,-4.6f);Window(art,root,-2.6f);
            root=art.Group("Office level one furniture",root);
            var desk=art.Group("Boss desk",root,new Vector3(-3.50f,.17f,8.2f));
            art.Box("Desk oak top",desk,new Vector3(0,.77f,0),new Vector3(1.85f,.11f,.80f),"Wood");
            // The boss sits on the +Z side, so the drawers, the screen and the keyboard face +Z.
            ClinicFurnishings.Cabinet(art,desk,new Vector3(-.62f,0,.04f),new Vector3(.48f,.72f,.64f),"Sage",Vector3.forward);
            for(int side=-1;side<=1;side+=2)
                art.Box("Desk brass leg",desk,new Vector3(.72f,.37f,side*.29f),new Vector3(.06f,.74f,.06f),"Gold");
            art.Box("Computer monitor",desk,new Vector3(0,1.17f,.13f),new Vector3(.65f,.43f,.065f),"Ink");
            art.Box("Computer screen",desk,new Vector3(0,1.17f,.037f+.13f),new Vector3(.56f,.34f,.012f),"Blue");
            art.Box("Monitor stand",desk,new Vector3(0,.91f,.13f),new Vector3(.055f,.18f,.055f),"Chrome");
            art.Box("Monitor foot",desk,new Vector3(0,.83f,.13f),new Vector3(.30f,.03f,.18f),"Ink");
            art.Box("Keyboard",desk,new Vector3(0,.84f,.27f),new Vector3(.50f,.035f,.16f),"Linen");
            art.Box("Desk paperwork",desk,new Vector3(-.60f,.84f,.12f),new Vector3(.32f,.045f,.26f),"Paper");
            art.Cylinder("Desk coffee cup",desk,new Vector3(.59f,.90f,-.18f),new Vector3(.12f,.15f,.12f),"Linen");
            art.Box("Office rug border",root,new Vector3(-3.5f,.17f,7.45f),new Vector3(2.30f,.012f,1.1f),"Gold");
            art.Box("Office woven rug",root,new Vector3(-3.5f,.18f,7.45f),new Vector3(2.18f,.012f,.98f),"SageDark");
            Chair(art,root,"Boss swivel chair",new Vector3(-3.5f,.17f,8.96f),"Ink",0);
            Chair(art,root,"Office visitor chair",new Vector3(-3.10f,.17f,7.08f),"Sage",180);
            Chair(art,root,"Office visitor chair",new Vector3(-4.60f,.17f,7.25f),"Sage",180);
            var files=art.Group("Office filing cabinet",root,new Vector3(-5.10f,.17f,8.97f));
            ClinicFurnishings.Cabinet(art,files,Vector3.zero,new Vector3(.63f,1.05f,.50f),"Sage");
            for(int i=0;i<4;i++)art.Box("Office binder",files,new Vector3(-.21f+i*.14f,1.22f,0),new Vector3(.11f,.34f,.30f),i%2==0?"Apricot":"Blue");
            art.Model("Plant",root,new Vector3(-2.12f,.17f,8.99f));
            var plaque=art.Group("Boss office plaque",root,new Vector3(-2.54f,1.36f,9.55f));
            art.Box("Certificate gold frame",plaque,Vector3.zero,new Vector3(.67f,.48f,.055f),"Gold");
            art.Box("Certificate paper",plaque,new Vector3(0,0,-.035f),new Vector3(.57f,.38f,.012f),"Paper");
            art.Orb("Certificate seal",plaque,new Vector3(0,-.07f,-.05f),new Vector3(.12f,.12f,.012f),"Gold");
            for(int line=0;line<2;line++)art.Box("Certificate writing",plaque,new Vector3(0,.08f-line*.06f,-.05f),new Vector3(.32f,.016f,.012f),"SageDark");
            Sign(art,root,new Vector3(-2.30f,.17f,6.55f),true);
        }

        private static void Window(ClinicArt art,Transform root,float x)
        {
            art.Box("Staff window frame",root,new Vector3(x,1.50f,9.56f),new Vector3(1.35f,.94f,.06f),"Sage");
            art.Box("Staff window glass",root,new Vector3(x,1.50f,9.52f),new Vector3(1.20f,.80f,.025f),"Blue");
            art.Box("Staff window mullion",root,new Vector3(x,1.50f,9.49f),new Vector3(.04f,.80f,.025f),"Ivory");
        }

        private static void Chair(ClinicArt art,Transform root,string name,Vector3 p,string role,float yaw)
        {
            var chair=art.Group(name,root,p);chair.localRotation=Quaternion.Euler(0,yaw,0);
            art.Box("Chair cushion",chair,new Vector3(0,.46f,0),new Vector3(.54f,.12f,.54f),role);
            art.Box("Chair back",chair,new Vector3(0,.76f,.22f),new Vector3(.53f,.52f,.10f),role);
            art.Cylinder("Chair pedestal",chair,new Vector3(0,.24f,0),new Vector3(.07f,.40f,.07f),"Chrome");
            for(int i=0;i<3;i++)art.Box("Chair base",chair,new Vector3(0,.05f,0),new Vector3(.62f,.055f,.06f),"Ink").transform.localRotation=Quaternion.Euler(0,i*60,0);
        }

        private static void BuildCleaning(ClinicArt art,Transform parent)
        {
            var root=art.Group("Cleaning utilities room",parent);Floor(art,root,Cleaning,"TileBlue");
            root=art.Group("Cleaning utilities furniture",root);
            var rack=art.Group("Cleaning supply shelves",root,new Vector3(.75f,.17f,9.14f));
            for(int side=-1;side<=1;side+=2)art.Box("Shelf upright",rack,new Vector3(side*.52f,.83f,0),new Vector3(.055f,1.66f,.48f),"Chrome");
            for(int level=0;level<3;level++)
            {
                float y=.28f+level*.52f;
                art.Box("Supply shelf",rack,new Vector3(0,y,0),new Vector3(1.16f,.055f,.55f),"Wood");
                if(level==0)
                    for(int i=0;i<3;i++)art.Box("Folded cleaning towels",rack,new Vector3(-.32f+i*.32f,y+.10f,0),new Vector3(.26f,.13f,.35f),"Linen");
                else
                    for(int i=0;i<4;i++)Bottle(art,rack,new Vector3(-.39f+i*.26f,y+.04f,0),i%2==0?"Apricot":"Blue");
            }
            var sink=art.Group("Janitor utility sink",root,new Vector3(-.94f,.17f,8.94f));
            ClinicFurnishings.Cabinet(art,sink,Vector3.zero,new Vector3(.70f,.76f,.62f),"Sage");
            art.Box("Deep utility basin",sink,new Vector3(0,.86f,0),new Vector3(.79f,.22f,.69f),"Linen");
            art.Box("Utility sink hollow",sink,new Vector3(0,.978f,-.025f),new Vector3(.56f,.008f,.43f),"Ink");
            art.Cylinder("Utility sink tap",sink,new Vector3(0,1.07f,.20f),new Vector3(.05f,.28f,.05f),"Chrome");
            art.Box("Utility tap spout",sink,new Vector3(0,1.19f,.10f),new Vector3(.05f,.05f,.22f),"Chrome");
            var tools=art.Group("Wall mounted cleaning tools",root,new Vector3(1.18f,.17f,7.82f));
            art.Box("Cleaning tool wall rack",tools,new Vector3(.32f,1.36f,0),new Vector3(.06f,.14f,1.25f),"Wood");
            Tool(art,tools,"Stored mop",new Vector3(.08f,0,-.36f),false);
            Tool(art,tools,"Stored broom",new Vector3(.08f,0,.36f),true);
            var cart=art.Group("Janitor supply trolley",root,new Vector3(-.95f,.17f,7.22f));
            for(int side=-1;side<=1;side+=2)
            {
                art.Box("Trolley side",cart,new Vector3(side*.27f,.36f,0),new Vector3(.04f,.62f,.54f),"Sage");
                for(int end=-1;end<=1;end+=2)art.Orb("Trolley wheel",cart,new Vector3(side*.26f,.08f,end*.22f),new Vector3(.12f,.16f,.12f),"Ink");
            }
            art.Box("Trolley lower tray",cart,new Vector3(0,.17f,0),new Vector3(.57f,.07f,.57f),"Sage");
            art.Box("Trolley top tray",cart,new Vector3(0,.66f,0),new Vector3(.60f,.07f,.59f),"Sage");
            Bottle(art,cart,new Vector3(-.14f,.70f,0),"Apricot");Bottle(art,cart,new Vector3(.14f,.70f,0),"Blue");
            var bucket=art.Group("Mop bucket and wringer",root,new Vector3(.47f,.17f,7.08f));
            art.Box("Cleaning bucket",bucket,new Vector3(0,.23f,0),new Vector3(.65f,.40f,.53f),"PlateYellow");
            art.Box("Bucket water",bucket,new Vector3(-.13f,.44f,0),new Vector3(.30f,.008f,.38f),"Blue");
            art.Box("Bucket wringer",bucket,new Vector3(.20f,.52f,0),new Vector3(.25f,.22f,.48f),"Gold");
            art.Box("Wringer lever",bucket,new Vector3(.27f,.75f,0),new Vector3(.04f,.30f,.04f),"Ink");
            Tool(art,bucket,"Bucket mop",new Vector3(-.13f,.17f,0),false);
            var sign=art.Group("Folded wet floor sign",root,new Vector3(.99f,.17f,6.55f));
            for(int side=-1;side<=1;side+=2)
                art.Box("Caution sign panel",sign,new Vector3(0,.29f,side*.09f),new Vector3(.34f,.55f,.035f),"PlateYellow").transform.localRotation=Quaternion.Euler(side*-20,0,0);
            art.Box("Caution pictogram",sign,new Vector3(0,.31f,-.18f),new Vector3(.12f,.16f,.018f),"Ink");
            Sign(art,root,new Vector3(.14f,.17f,6.53f),false);
        }

        private static void Bottle(ClinicArt art,Transform root,Vector3 p,string role)
        {
            art.Box("Cleaning bottle",root,p+Vector3.up*.14f,new Vector3(.16f,.27f,.15f),role);
            art.Box("Bottle label",root,p+new Vector3(0,.13f,-.08f),new Vector3(.12f,.09f,.012f),"Paper");
            art.Box("Bottle cap",root,p+Vector3.up*.30f,new Vector3(.08f,.06f,.08f),"Ink");
        }

        private static void Tool(ClinicArt art,Transform root,string name,Vector3 p,bool broom)
        {
            var tool=art.Group(name,root,p);
            art.Cylinder("Cleaning tool handle",tool,new Vector3(0,.81f,0),new Vector3(.045f,1.45f,.045f),broom?"Wood":"Blue");
            art.Box(broom?"Broom head":"Mop head",tool,new Vector3(0,.10f,0),new Vector3(.40f,.10f,.19f),broom?"Wood":"Linen");
            for(int i=0;i<7;i++)art.Box(broom?"Broom bristle":"Mop strands",tool,new Vector3(-.17f+i*.055f,.035f,0),new Vector3(.035f,.07f,.22f),broom?"Gold":"Linen");
        }

        private static void Sign(ClinicArt art,Transform root,Vector3 p,bool office)
        {
            var sign=art.Group(office?"Office floor emblem":"Cleaning floor emblem",root,p);
            art.Box("Staff room emblem tile",sign,Vector3.zero,new Vector3(.62f,.018f,.48f),"SageDark");
            if(office)
            {
                art.Box("Briefcase pictogram",sign,new Vector3(0,.018f,0),new Vector3(.32f,.012f,.20f),"Gold");
                art.Box("Briefcase handle",sign,new Vector3(0,.018f,.13f),new Vector3(.14f,.012f,.07f),"Gold");
            }
            else
            {
                art.Box("Mop pictogram handle",sign,new Vector3(0,.018f,.05f),new Vector3(.035f,.012f,.26f),"Linen");
                art.Box("Mop pictogram head",sign,new Vector3(0,.018f,-.10f),new Vector3(.28f,.012f,.10f),"Linen");
            }
        }
    }
}
