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
            internal Layout(bool[] wall,Lane[] floor,Lane[] hung,float scale)
            {
                var f=Expand(floor);var w=Expand(hung);int fi=0,wi=0;
                for(int i=0;i<ClinicGear.ItemCount;i++)
                {
                    var p=wall[i]?w[wi++]:f[fi++];
                    Slots[i]=new Vector4(p.x,p.y,p.z,scale);
                }
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
            // Starter clinic. Wall-hung pieces use the high west and north walls; the rest keep off the walked lanes.
            d[Key(false,ClinicRoom.Reception)]=new Layout(Wall(7,8,11,16),
                new[]{new Lane(-5.1f,-.70f,-.70f,-.70f,0,8),new Lane(-.55f,-1.3f,-.55f,-4.0f,90,4),new Lane(-5.05f,-1.4f,-5.05f,-4.0f,-90,4)},
                new[]{new Lane(-5.47f,-4.3f,-5.47f,-1.7f,-90,4)},1f);
            d[Key(false,ClinicRoom.FirstAid)]=new Layout(Wall(0,1,9,11,14),
                new[]{new Lane(-5.0f,4.40f,-.75f,4.40f,0,5),new Lane(-5.15f,.75f,-5.15f,3.7f,-90,3),new Lane(-.50f,2.9f,-.50f,4.1f,90,3),new Lane(-4.9f,.50f,-2.4f,.50f,180,4)},
                new[]{new Lane(-5.0f,4.79f,-2.4f,4.79f,0,4),new Lane(-5.42f,1.5f,-5.42f,1.5f,-90,1)},1f);
            // The waiting room reaches the front wall. Seats keep both side walls and the walking spine down the middle; the pieces
            // stand in four rows of the lobby end (clear of the second west doorway, the walk to the vending machine at -3.4 and
            // the vending patient's spot) and by the back wall beside the toilet door.
            d[Key(false,ClinicRoom.Waiting)]=new Layout(Wall(11),
                new[]{new Lane(2.95f,-4.55f,5.30f,-4.55f,180,5),new Lane(2.95f,-3.95f,4.50f,-3.95f,180,3),new Lane(2.30f,-2.85f,4.20f,-2.85f,180,4),
                      new Lane(2.30f,-2.05f,4.20f,-2.05f,180,4),new Lane(4.25f,4.50f,5.30f,4.50f,0,3)},
                new[]{new Lane(4.65f,4.79f,4.65f,4.79f,0,1)},.62f);
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
            var slot=Layouts[Key(doctors,room)].Slots[Mathf.Clamp(item,0,ClinicGear.ItemCount-1)];
            return new Vector3(slot.x,Floor+.55f,slot.y);
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
            var model=art.Model(Folder(room)+"/Gear"+(item+1).ToString("00"),root,new Vector3(slot.x,Floor,slot.y),Quaternion.Euler(0,slot.z,0));
            model.name=room+" "+ClinicGear.ItemName(room,item);model.transform.localScale=Vector3.one*slot.w;
            var found=new GameObject[ClinicGear.MaximumVersion];var stem="Gear"+(item+1).ToString("00")+"_V";
            foreach(Transform child in model.GetComponentsInChildren<Transform>(true))
                for(int v=1;v<=ClinicGear.MaximumVersion;v++)if(child.name==stem+v&&found[v-1]==null){found[v-1]=child.gameObject;child.gameObject.SetActive(false);}
            list[item]=model;versions[key][item]=found;model.SetActive(false);
        }
        internal void Render(ClinicState state)
        {
            for(int r=0;r<5;r++)
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
