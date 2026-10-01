using UnityEngine;
using static IdleClinic.Presentation.ClinicFloorPlan;

namespace IdleClinic.Presentation
{
    /// <summary>The starter clinic's walls, from the floor plan: joined runs with explicit door openings. Walls facing the
    /// camera are cut low with a dark cap, as in an architectural cutaway; the west and north outer walls and the
    /// reception's back wall stand full height so pictures, screens and windows can hang on them.</summary>
    internal static class ClinicArchitecture
    {
        internal static void Build(ClinicArt art,Transform scene)
        {
            var root=art.Group("Joined clinic walls",scene);
            float o=OuterThickness*.5f;
            // Outer walls. The storefront leaves the entrance to its sliding doors.
            Wall(art,root,"West wall",new Vector2(West,Front-o),new Vector2(West,Back+o),TallWall,OuterThickness);
            Wall(art,root,"North wall",new Vector2(West-o,Back),new Vector2(-BackDoorWidth*.5f-.06f,Back),TallWall,OuterThickness);
            Wall(art,root,"North wall east",new Vector2(BackDoorWidth*.5f+.06f,Back),new Vector2(East+o,Back),TallWall,OuterThickness);
            Wall(art,root,"East cutaway wall",new Vector2(East,Front-o),new Vector2(East,Back+o),LowWall,OuterThickness);
            float entrance=EntranceWidth*.5f+.145f;
            Wall(art,root,"Lounge storefront",new Vector2(West-o,Front),new Vector2(-entrance,Front),FrontWall,.08f);
            Wall(art,root,"Reception storefront",new Vector2(entrance,Front),new Vector2(East+o,Front),FrontWall,.08f);
            // Across the middle: the lounge's back partition, and the reception's full-height back wall with the staff door.
            float i=InnerThickness*.5f;
            Wall(art,root,"Lounge back partition",new Vector2(West,Middle),new Vector2(HallWest+i,Middle),LowWall,InnerThickness);
            Wall(art,root,"Reception back wall",new Vector2(HallEast-i,Middle),new Vector2(StaffDoorX-RoomDoorWidth*.5f,Middle),TallWall,InnerThickness);
            Wall(art,root,"Reception back wall east",new Vector2(StaffDoorX+RoomDoorWidth*.5f,Middle),new Vector2(East,Middle),TallWall,InnerThickness);
            // The corridor's walls, with the treatment rooms' wide doors to the west and the office and WC to the east.
            Run(art,root,"Treatment corridor wall",HallWest,Middle,Back,new[]{TreatmentDoorZ(0),TreatmentDoorZ(1)},TreatmentDoorWidth);
            Run(art,root,"Service corridor wall",HallEast,Middle,Back,new[]{OfficeDoorZ,WashroomDoorZ},RoomDoorWidth);
            Wall(art,root,"Treatment room partition",new Vector2(West,TreatmentSplit),new Vector2(HallWest,TreatmentSplit),LowWall,InnerThickness);
            Wall(art,root,"Office and staff room partition",new Vector2(ServiceSplitX,Middle),new Vector2(ServiceSplitX,Back),LowWall,InnerThickness);
            Wall(art,root,"Office and WC partition",new Vector2(HallEast,ServiceSplitZ),new Vector2(ServiceSplitX,ServiceSplitZ),LowWall,InnerThickness);
            Wall(art,root,"Staff room and store partition",new Vector2(ServiceSplitX,ServiceSplitZ),new Vector2(StoreDoorX-RoomDoorWidth*.5f,ServiceSplitZ),LowWall,InnerThickness);
            Wall(art,root,"Staff room and store partition east",new Vector2(StoreDoorX+RoomDoorWidth*.5f,ServiceSplitZ),new Vector2(East,ServiceSplitZ),LowWall,InnerThickness);
            // High windows on the full-height outer walls, clear of the pictures and equipment that hang there.
            foreach(var z in new[]{-1.45f,-3.95f})Window(art,root,new Vector3(West+o+.01f,1.45f,z),90);
            foreach(var x in new[]{-3.4f,2.2f})Window(art,root,new Vector3(x,1.45f,Back-o-.01f),0);
            // The back door: a closed staff and fire exit.
            var back=art.Group("Back fire exit",root,new Vector3(0,Floor,Back));
            for(int side=-1;side<=1;side+=2)art.Box("Fire exit jamb",back,new Vector3(side*(BackDoorWidth*.5f+.03f),1.0f,0),new Vector3(.06f,2.0f,OuterThickness+.02f),"Charcoal");
            art.Box("Fire exit head",back,new Vector3(0,2.02f,0),new Vector3(BackDoorWidth+.12f,.06f,OuterThickness+.02f),"Charcoal");
            art.Box("Fire exit door",back,new Vector3(0,.98f,-.02f),new Vector3(BackDoorWidth,1.96f,.05f),"Sage");
            art.Box("Fire exit push bar",back,new Vector3(0,1.0f,-.07f),new Vector3(BackDoorWidth*.8f,.05f,.05f),"Chrome");
            art.Box("Fire exit sign",back,new Vector3(0,2.2f,-.1f),new Vector3(.34f,.13f,.03f),"Leaf");
        }

        /// <summary>A north-south wall run from z0 to z1 at x, broken by doorways of the given clear width.</summary>
        private static void Run(ClinicArt art,Transform root,string name,float x,float z0,float z1,float[] doors,float width)
        {
            float start=z0;int piece=0;
            foreach(var door in doors)
            {
                Wall(art,root,name+" "+piece++,new Vector2(x,start),new Vector2(x,door-width*.5f),LowWall,InnerThickness);
                start=door+width*.5f;
            }
            Wall(art,root,name+" "+piece,new Vector2(x,start),new Vector2(x,z1),LowWall,InnerThickness);
        }

        private static void Window(ClinicArt art,Transform root,Vector3 center,float yaw)
        {
            var window=art.Group("High window",root,center);window.localRotation=Quaternion.Euler(0,yaw,0);
            art.Box("High window frame",window,Vector3.zero,new Vector3(1.30f,.86f,.05f),"Charcoal");
            art.Box("Daylight window",window,new Vector3(0,0,-.012f),new Vector3(1.18f,.74f,.03f),"Blue");
            art.Box("Window mullion",window,new Vector3(0,0,-.03f),new Vector3(.04f,.74f,.03f),"Charcoal");
            art.Box("Window sill",window,new Vector3(0,-.46f,-.05f),new Vector3(1.40f,.05f,.12f),"Oak");
        }

        internal static void Wall(ClinicArt art,Transform root,string name,Vector2 start,Vector2 end,float height,float thickness)
        {
            bool horizontal=Mathf.Abs(end.x-start.x)>Mathf.Abs(end.y-start.y);
            if((horizontal?Mathf.Abs(end.x-start.x):Mathf.Abs(end.y-start.y))<.01f)return;
            var midpoint=(start+end)*.5f;
            var scale=horizontal?new Vector3(Mathf.Abs(end.x-start.x),height,thickness):new Vector3(thickness,height,Mathf.Abs(end.y-start.y));
            art.Box(name,root,new Vector3(midpoint.x,Floor+height*.5f,midpoint.y),scale,"Ivory");
            var cap=scale;cap.y=.05f;cap.x+=horizontal?0:.02f;cap.z+=horizontal?.02f:0;
            art.Box(name+" cap",root,new Vector3(midpoint.x,Floor+height,midpoint.y),cap,"Charcoal");
            var skirting=scale;skirting.y=.09f;skirting.x+=horizontal?0:.02f;skirting.z+=horizontal?.02f:0;
            art.Box(name+" skirting",root,new Vector3(midpoint.x,Floor+.045f,midpoint.y),skirting,"Oak");
        }
    }
}
