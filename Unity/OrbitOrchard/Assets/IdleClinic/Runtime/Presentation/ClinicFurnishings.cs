using UnityEngine;
using static IdleClinic.Presentation.ClinicFloorPlan;

namespace IdleClinic.Presentation
{
    /// <summary>Permanent interior dressing: the hall's runner and wayfinding, and the basic furniture of the office, staff
    /// room and store. Everything stays outside the reserved walking lanes; equipment bought per room stands elsewhere.</summary>
    internal static class ClinicFurnishings
    {
        /// <summary>Builds the dressing; returns the fixed furniture of the office, staff room and store (in that order), which
        /// shows once each room is built.</summary>
        internal static GameObject[] Build(ClinicArt art,Transform parent)
        {
            var root=art.Group("Clinic interior details",parent);
            // The hall: a runner from the doors to the fire exit, and care dots guiding the way to the treatment rooms.
            art.Box("Corridor sage runner",root,new Vector3(-.1f,.15f,.9f),new Vector3(.5f,.008f,Back-Front-2.4f),"TileSage");
            for(int i=0;i<6;i++)art.Cylinder("Wayfinding care dot",root,new Vector3(-.1f,.16f,-3.4f+i*1.6f),new Vector3(.14f,.008f,.14f),"Gold");
            art.Box("Corridor sign",root,new Vector3(HallWest+.08f,1.35f,-.05f),new Vector3(.02f,.2f,.6f),"SageDark");
            return new[]{Office(art,root),StaffRoom(art,root),Store(art,root)};
        }
        private static GameObject Office(ClinicArt art,Transform root)
        {
            var office=art.Group("Office furniture",root);
            // The manager's desk faces the door, with a chair behind it and two for visitors in front.
            art.Box("Office desk top",office,new Vector3(2.25f,.89f,1.85f),new Vector3(1.3f,.05f,.7f),"Walnut");
            for(int side=-1;side<=1;side+=2)art.Box("Office desk pedestal",office,new Vector3(2.25f+side*.55f,.5f,1.85f),new Vector3(.18f,.72f,.64f),"Charcoal");
            art.Box("Office desk modesty panel",office,new Vector3(2.25f,.6f,1.55f),new Vector3(.95f,.4f,.03f),"Charcoal");
            Chair(art,office,new Vector3(2.25f,Floor,2.35f),0,"Leather");
            Chair(art,office,new Vector3(1.9f,Floor,1.15f),180,"Fabric");Chair(art,office,new Vector3(2.6f,Floor,1.15f),180,"Fabric");
            return office.gameObject;
        }
        private static GameObject StaffRoom(ClinicArt art,Transform root)
        {
            var staff=art.Group("Staff room furniture",root);
            // A kitchenette along the east wall, clear of the swing of the door from the reception.
            art.Box("Kitchenette base",staff,new Vector3(East-.38f,.58f,1.9f),new Vector3(.6f,.88f,1.4f),"Linen");
            art.Box("Kitchenette worktop",staff,new Vector3(East-.38f,1.04f,1.9f),new Vector3(.64f,.04f,1.44f),"Quartz");
            art.Box("Kitchenette plinth",staff,new Vector3(East-.67f,.19f,1.9f),new Vector3(.03f,.1f,1.36f),"Charcoal");
            for(int i=0;i<3;i++)art.Box("Kitchenette door",staff,new Vector3(East-.685f,.62f,1.37f+i*.46f),new Vector3(.01f,.66f,.42f),"Oak");
            return staff.gameObject;
        }
        private static GameObject Store(ClinicArt art,Transform root)
        {
            var store=art.Group("Store shelving",root);
            // Hooks and a labelled rail along the north wall; the shelving and stock arrive as equipment.
            art.Box("Store wall rail",store,new Vector3(4.55f,1.2f,Back-.1f),new Vector3(2.0f,.05f,.04f),"BrushedSteel");
            for(int i=0;i<5;i++)art.Box("Store label",store,new Vector3(3.75f+i*.4f,1.3f,Back-.1f),new Vector3(.14f,.08f,.01f),"Paper");
            return store.gameObject;
        }
        private static void Chair(ClinicArt art,Transform parent,Vector3 position,float yaw,string fabric)
        {
            var chair=art.Group("Chair",parent,position);chair.localRotation=Quaternion.Euler(0,yaw,0);
            art.Box("Chair seat",chair,new Vector3(0,.45f,0),new Vector3(.44f,.06f,.42f),fabric);
            art.Box("Chair back",chair,new Vector3(0,.72f,.2f),new Vector3(.44f,.48f,.05f),fabric);
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)art.Box("Chair leg",chair,new Vector3(x*.19f,.21f,z*.17f),new Vector3(.03f,.42f,.03f),"Charcoal");
        }
        internal static void Cabinet(ClinicArt art,Transform parent,Vector3 position,Vector3 size,string role)
        {
            art.Box("Cabinet body",parent,position+Vector3.up*size.y*.5f,size,role);
            art.Box("Cabinet split",parent,position+new Vector3(0,size.y*.5f,-size.z*.505f),new Vector3(.012f,size.y*.83f,.015f),"SageDark");
            for(int side=-1;side<=1;side+=2)art.Box("Cabinet brass handle",parent,position+new Vector3(side*.055f,size.y*.62f,-size.z*.53f),new Vector3(.025f,.12f,.035f),"Gold");
        }
        internal static void NoticeBoard(ClinicArt art,Transform parent,string name,Vector3 position,float rotation,float scale=1)
        {
            var board=art.Group(name,parent,position);board.localRotation=Quaternion.Euler(0,rotation,0);board.localScale=Vector3.one*scale;
            art.Box("Notice board oak frame",board,Vector3.zero,new Vector3(1.05f,.76f,.075f),"Wood");
            art.Box("Notice cork",board,new Vector3(0,0,-.044f),new Vector3(.94f,.65f,.02f),"Clay");
            for(int i=0;i<3;i++)
            {
                var poster=art.Group("Community pictogram poster",board,new Vector3(-.31f+i*.31f,i==1?-.05f:.04f,-.062f));
                poster.localRotation=Quaternion.Euler(0,0,i==1?-6:4);
                art.Box("Poster paper",poster,Vector3.zero,new Vector3(.25f,.40f,.014f),i==1?"TileBlue":"Linen");
                if(i==0)
                { art.Box("Poster care cross",poster,new Vector3(0,.06f,-.015f),new Vector3(.14f,.04f,.012f),"Rose");art.Box("Poster care cross",poster,new Vector3(0,.06f,-.02f),new Vector3(.04f,.14f,.012f),"Rose"); }
                else if(i==1)
                { art.Orb("Poster wellbeing circle",poster,new Vector3(0,.07f,-.015f),new Vector3(.15f,.15f,.015f),"Gold");art.Box("Poster wellbeing stem",poster,new Vector3(0,-.02f,-.02f),new Vector3(.03f,.11f,.012f),"SageDark"); }
                else
                { art.Orb("Poster person head",poster,new Vector3(0,.11f,-.02f),new Vector3(.07f,.07f,.014f),"Sage");art.Box("Poster person shoulders",poster,new Vector3(0,.02f,-.02f),new Vector3(.13f,.06f,.014f),"Sage"); }
                art.Box("Poster short caption",poster,new Vector3(0,-.11f,-.02f),new Vector3(.14f,.019f,.012f),"Wood");
                art.Orb("Brass notice pin",poster,new Vector3(0,.16f,-.027f),new Vector3(.032f,.032f,.015f),"Gold");
            }
        }
    }
}
