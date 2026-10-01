using System.Collections.Generic;
using IdleClinic.Core;
using UnityEngine;

namespace IdleClinic.Presentation
{
    /// <summary>The twenty pieces of equipment in each room. A piece arrives with a room size and stands in its own place; every
    /// model carries ten versions, basic to advanced, and only the owned one shows, so each upgrade visibly replaces the
    /// piece. A projection of saved state, never an income source.</summary>
    internal sealed class ClinicRoomGear
    {
        /// <summary>Development previews: when above zero every piece in every room shows at this version.</summary>
        internal static int PreviewVersion;
        private const float Floor=.14f;

        /// <summary>A row of pieces from a to b (room frame), each facing yaw (0 faces the room from the north wall, 90 west, 180 south, -90 east).</summary>
        private struct Lane
        {
            internal Vector2 A,B;internal float Yaw;internal int Count;
            internal Lane(float ax,float az,float bx,float bz,float yaw,int count){A=new Vector2(ax,az);B=new Vector2(bx,bz);Yaw=yaw;Count=count;}
        }
        private sealed class Layout
        {
            internal readonly Vector4[] Slots=new Vector4[ClinicGear.ItemCount];
            /// <summary>Height above the floor of each piece's origin: a counter top, or the mount of a piece hung on a wall.</summary>
            internal readonly float[] Heights=new float[ClinicGear.ItemCount];
            internal Layout(bool[] wall,Lane[] floor,Lane[] hung,float scale)
            {
                var f=Expand(floor);var w=Expand(hung);int fi=0,wi=0;
                for(int i=0;i<ClinicGear.ItemCount;i++)
                {
                    var p=wall[i]?w[wi++]:f[fi++];
                    Slots[i]=new Vector4(p.x,p.y,p.z,scale);
                }
            }
            /// <summary>Explicit places, one row per piece: x, height, z, yaw, scale.</summary>
            internal Layout(float[,] places)
            {
                for(int i=0;i<ClinicGear.ItemCount;i++){Slots[i]=new Vector4(places[i,0],places[i,2],places[i,3],places[i,4]);Heights[i]=places[i,1];}
            }
            private static List<Vector3> Expand(Lane[] lanes)
            {
                var list=new List<Vector3>();
                foreach(var lane in lanes)for(int i=0;i<lane.Count;i++)
                {
                    float t=(i+.5f)/lane.Count;var p=Vector2.Lerp(lane.A,lane.B,t);list.Add(new Vector3(p.x,p.y,lane.Yaw));
                }
                return list;
            }
        }
        private static bool[] Wall(params int[] items){var w=new bool[ClinicGear.ItemCount];foreach(var i in items)w[i]=true;return w;}
        private static readonly Dictionary<int,Layout> Layouts=BuildLayouts();
        private static int Key(bool doctors,ClinicRoom room)=>(doctors?10:0)+(int)room;
        private static Dictionary<int,Layout> BuildLayouts()
        {
            var d=new Dictionary<int,Layout>();
            // Starter clinic, from the floor plan. Reception: six pieces on the counter (the visitor ledge at 1.115 m and the staff
            // work surface at 0.96 m), pictures and screens on the full-height back wall, and the rest along the east wall and
            // behind the counter, clear of the queue rows, the counter lane and the staff lane.
            d[Key(false,ClinicRoom.Reception)]=new Layout(new float[,]{
                {1.63f,1.115f,-2.30f,0,1},{2.96f,1.115f,-2.30f,0,1},{2.45f,1.115f,-2.30f,0,1},{2.38f,.96f,-1.83f,180,1},{2.82f,.96f,-1.83f,180,1},
                {2.07f,1.115f,-2.30f,0,1},{5.40f,0,-3.62f,90,1},{4.25f,1.62f,-.19f,0,.8f},{1.55f,1.55f,-.19f,0,.85f},{5.35f,0,-4.4f,90,1},
                {1.15f,0,-2.75f,90,1},{1.10f,2.0f,-.19f,-45,1},{3.0f,0,-4.41f,0,1},{-.9f,0,-4.88f,0,1},{5.30f,0,-4.85f,0,1},
                {3.0f,1.66f,-.19f,0,.75f},{5.35f,0,-2.80f,90,1},{1.95f,0,-.50f,0,1},{1.40f,0,-.50f,0,1},{5.35f,0,-2.15f,90,1}});
            // First aid: both treatment rooms. Wall pieces hang on treatment room 2's full-height north wall; the floor pieces
            // stand west of each bay and in the corners, clear of the lanes from the door to the chair and of the nurse.
            d[Key(false,ClinicRoom.FirstAid)]=new Layout(new float[,]{
                {-4.65f,0,5.69f,0,1},{-1.50f,0,5.69f,0,1},{-4.60f,0,1.50f,0,1},{-5.30f,0,3.30f,-90,1},{-1.40f,0,2.50f,0,1},
                {-4.80f,0,5.35f,0,1},{-2.20f,0,.20f,180,1},{-1.60f,0,3.18f,180,1},{-4.65f,0,2.55f,0,1},{-5.30f,0,5.69f,0,1},
                {-1.90f,0,2.50f,0,1},{-3.30f,0,5.69f,0,1},{-5.25f,0,2.60f,0,1},{-5.30f,0,4.35f,-90,1},{-2.30f,0,5.69f,0,1},
                {-4.70f,0,3.30f,180,1},{-4.75f,0,.45f,90,1},{-4.60f,0,4.60f,0,1},{-5.30f,0,1.35f,-90,1},{-4.00f,0,3.25f,180,1}});
            // The waiting lounge: pieces along its back partition and storefront, clear of the seats and aisles, with the rest
            // along the hall's walls outside the treatment rooms and beside the fire exit.
            d[Key(false,ClinicRoom.Waiting)]=new Layout(new float[,]{
                {-.90f,0,1.95f,-90,.62f},{-3.20f,0,-.45f,0,.62f},{-.85f,0,.15f,-90,.62f},{-3.85f,0,-.45f,0,.62f},{-5.40f,0,-.50f,0,.62f},
                {-3.90f,0,-4.75f,180,.62f},{-1.95f,0,-4.75f,180,.62f},{-2.55f,0,-4.80f,180,.62f},{.78f,0,5.45f,90,.62f},{-.86f,0,2.75f,-90,.62f},
                {-.86f,0,-.55f,-90,.62f},{-.73f,.62f,5.78f,0,.55f},{.73f,.59f,5.78f,0,.55f},{-.85f,0,5.65f,-90,.62f},{.80f,0,-1.60f,90,.62f},
                {-3.20f,0,-4.75f,180,.62f},{-.88f,0,5.10f,-90,.62f},{-4.70f,0,-.45f,0,.62f},{-1.40f,0,-.50f,0,.62f},{-1.35f,0,-4.80f,180,.62f}});
            // The office: eight pieces on the manager's desk (0.915 m up, facing the manager), the rest around the walls, clear
            // of the swing of the door from the corridor.
            d[Key(false,ClinicRoom.Office)]=new Layout(new float[,]{
                {2.25f,.915f,1.95f,180,1},{2.75f,.915f,2.05f,180,1},{1.80f,.915f,1.95f,180,1},{2.62f,.915f,1.72f,180,1},{1.95f,.915f,1.65f,180,1},
                {2.25f,.915f,1.60f,0,1},{2.50f,.915f,2.10f,180,1},{1.70f,.915f,1.66f,0,1},{3.20f,0,2.18f,90,.85f},{3.12f,0,1.30f,90,.85f},
                {3.05f,0,.30f,90,.85f},{1.25f,0,2.40f,0,.85f},{1.25f,0,.15f,0,.85f},{2.25f,0,1.20f,0,.85f},{2.45f,0,.20f,180,.8f},
                {1.25f,0,1.85f,-90,.7f},{2.95f,0,.95f,0,.8f},{1.75f,0,.20f,180,.8f},{1.60f,0,2.45f,0,.8f},{2.95f,0,2.45f,0,.85f}});
            // The staff room: the appliances on the kitchenette worktop (1.06 m up), a bistro table with what stands on it,
            // and seating and storage around the walls, clear of the door from the reception.
            d[Key(false,ClinicRoom.StaffRoom)]=new Layout(new float[,]{
                {5.36f,1.06f,2.40f,90,1},{5.36f,1.06f,2.05f,90,1},{5.36f,1.06f,1.70f,90,.85f},{5.36f,1.06f,1.35f,90,.9f},{3.64f,0,1.48f,-90,.75f},
                {4.40f,0,1.45f,0,1},{4.55f,.75f,1.60f,0,.9f},{4.30f,.75f,1.42f,0,.85f},{4.48f,.75f,1.28f,20,.8f},{3.72f,0,2.30f,0,.85f},
                {4.35f,0,.25f,180,.75f},{3.64f,.33f,1.20f,-90,.75f},{4.85f,0,1.00f,0,.7f},{4.60f,0,2.45f,0,.6f},{4.00f,0,.95f,-90,.6f},
                {5.42f,0,.95f,90,.6f},{4.20f,0,2.40f,0,.7f},{4.40f,0,1.45f,0,.8f},{3.80f,0,.45f,-90,.7f},{3.65f,0,.92f,-90,.7f}});
            // The store: shelving and cold storage along the north wall, carts and racks down the sides, clear of the door.
            d[Key(false,ClinicRoom.Store)]=new Layout(new float[,]{
                {3.78f,0,3.05f,-90,.6f},{4.95f,0,4.55f,90,.6f},{3.72f,0,3.65f,-90,.6f},{4.45f,0,4.60f,0,.6f},{5.55f,0,3.90f,90,.6f},
                {3.72f,0,4.25f,-90,.6f},{4.55f,0,3.95f,0,.6f},{5.40f,0,4.45f,90,.6f},{3.75f,0,4.90f,-90,.6f},{3.75f,0,5.45f,0,.6f},
                {5.43f,0,5.45f,0,.6f},{4.30f,0,5.10f,0,.55f},{5.45f,0,3.30f,90,.5f},{5.35f,0,5.00f,90,.6f},{4.95f,0,5.05f,0,.55f},
                {4.95f,0,3.95f,0,.6f},{4.35f,0,5.45f,0,.65f},{5.00f,0,5.45f,0,.6f},{4.15f,0,3.95f,0,.6f},{4.45f,0,4.10f,0,.5f}});
            // Doctors clinic: rooms twice the size, so the same pieces spread along longer walls.
            d[Key(true,ClinicRoom.Reception)]=new Layout(Wall(7,8,11,16),
                new[]{new Lane(-11.6f,-3.6f,-2.3f,-3.6f,0,11),new Lane(-11.8f,-4.2f,-11.8f,-7.0f,-90,5)},
                new[]{new Lane(-12.15f,-4.5f,-12.15f,-7.2f,-90,4)},1f);
            d[Key(true,ClinicRoom.FirstAid)]=new Layout(Wall(0,1,9,11,14),
                new[]{new Lane(-11.6f,2.55f,-2.3f,2.55f,0,10),new Lane(-11.8f,-1.2f,-11.8f,2.0f,-90,5)},
                new[]{new Lane(-12.15f,-1.4f,-12.15f,2.4f,-90,5)},1f);
            d[Key(true,ClinicRoom.Waiting)]=new Layout(Wall(11),
                new[]{new Lane(2.2f,-1.75f,9.6f,-1.75f,0,10),new Lane(2.2f,-6.85f,9.6f,-6.85f,180,9)},
                new[]{new Lane(9.95f,-4.5f,9.95f,-4.5f,90,1)},1f);
            d[Key(true,ClinicRoom.Consultation)]=new Layout(Wall(3,4,6,8,9),
                new[]{new Lane(-11.6f,8.55f,-7.2f,8.55f,0,4),new Lane(-6.0f,8.55f,-1.6f,8.55f,0,4),new Lane(-.4f,8.55f,4.0f,8.55f,0,4),new Lane(5.2f,8.55f,9.6f,8.55f,0,3)},
                new[]{new Lane(-11.6f,8.98f,-7.6f,8.98f,0,2),new Lane(-6.0f,8.98f,-6.0f,8.98f,0,1),new Lane(-.4f,8.98f,-.4f,8.98f,0,1),new Lane(5.2f,8.98f,5.2f,8.98f,0,1)},1f);
            // The pharmacy's front strip is where patients stand at the counters, so its pieces stand in the gaps between them.
            d[Key(true,ClinicRoom.Pharmacy)]=new Layout(Wall(),
                new[]{new Lane(1.95f,.14f,3.35f,.14f,180,3),new Lane(4.75f,.14f,6.85f,.14f,180,4),new Lane(8.25f,.14f,9.45f,.14f,180,2),
                      new Lane(9.9f,.5f,9.9f,2.5f,90,6),new Lane(1.75f,.5f,1.75f,2.5f,-90,5)},
                new Lane[0],.85f);
            return d;
        }
        internal static Vector3 Point(bool doctors,ClinicRoom room,int item)
        {
            var layout=Layouts[Key(doctors,room)];int i=Mathf.Clamp(item,0,ClinicGear.ItemCount-1);var slot=layout.Slots[i];
            return new Vector3(slot.x,Floor+Mathf.Max(.55f,layout.Heights[i]),slot.y);
        }
        internal static string Folder(ClinicRoom room)=>room+"Gear";

        private readonly ClinicArt art;private readonly Transform root;private readonly bool doctors;
        private readonly Dictionary<int,GameObject[]> items=new Dictionary<int,GameObject[]>();
        private readonly Dictionary<int,GameObject[][]> versions=new Dictionary<int,GameObject[][]>();
        internal ClinicRoomGear(ClinicArt art,Transform parent,bool doctors)
        {
            this.art=art;this.doctors=doctors;root=art.Group("Room equipment",parent);
        }
        private void Ensure(ClinicRoom room,int item)
        {
            int key=Key(doctors,room);
            if(!items.TryGetValue(key,out var list)){list=new GameObject[ClinicGear.ItemCount];items[key]=list;versions[key]=new GameObject[ClinicGear.ItemCount][];}
            if(list[item]!=null)return;
            var slot=Layouts[key].Slots[item];
            var model=art.Model(Folder(room)+"/Gear"+(item+1).ToString("00"),root,new Vector3(slot.x,Floor+Layouts[key].Heights[item],slot.y),Quaternion.Euler(0,slot.z,0));
            model.name=room+" "+ClinicGear.ItemName(room,item);model.transform.localScale=Vector3.one*slot.w;
            var found=new GameObject[ClinicGear.MaximumVersion];var stem="Gear"+(item+1).ToString("00")+"_V";
            foreach(Transform child in model.GetComponentsInChildren<Transform>(true))
                for(int v=1;v<=ClinicGear.MaximumVersion;v++)if(child.name==stem+v&&found[v-1]==null){found[v-1]=child.gameObject;child.gameObject.SetActive(false);}
            list[item]=model;versions[key][item]=found;model.SetActive(false);
        }
        internal void Render(ClinicState state)
        {
            for(int r=0;r<8;r++)
            {
                var room=(ClinicRoom)r;
                if(!Layouts.ContainsKey(Key(doctors,room)))continue;
                var data=state.Room(room);
                bool active=PreviewVersion>0||ClinicGear.Active(state,room)&&data!=null&&data.Built;
                for(int item=0;item<ClinicGear.ItemCount;item++)
                {
                    bool shown=active&&(PreviewVersion>0||data.Tier>=ClinicGear.UnlockTier(state,item));
                    int key=Key(doctors,room);
                    if(!shown&&(!items.TryGetValue(key,out var have)||have[item]==null))continue;
                    if(shown)Ensure(room,item);
                    int version=PreviewVersion>0?Mathf.Clamp(PreviewVersion,1,ClinicGear.MaximumVersion):shown?Mathf.Clamp(ClinicGear.Version(state,room,item),1,ClinicGear.MaximumVersion):1;
                    ClinicUpgradeEffects.Show(items[key][item],shown);
                    var found=versions[key][item];
                    for(int v=1;v<=ClinicGear.MaximumVersion;v++)if(found[v-1]!=null)ClinicUpgradeEffects.Show(found[v-1],shown&&v==version);
                }
            }
        }
    }
}
