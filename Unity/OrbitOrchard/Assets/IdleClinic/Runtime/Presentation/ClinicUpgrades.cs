using IdleClinic.Core;
using UnityEngine;
using static IdleClinic.Presentation.ClinicFloorPlan;

namespace IdleClinic.Presentation
{
    /// <summary>Small, cumulative fittings make each purchased level visible without moving work sockets. Levels 2–6
    /// add a row of fittings; levels 7–10 build onto that row (a second shelf, ropes between posts, stock on top).
    /// Decor lives in each room's style.</summary>
    internal sealed class ClinicUpgrades
    {
        private const int TopLevel=10,Levels=TopLevel-1;
        private readonly GameObject[,,] details=new GameObject[3,2,Levels];
        private readonly GameObject[,,] stationDetails=new GameObject[2,2,Levels];
        private readonly ClinicRoomGear roomGear;
        internal ClinicUpgrades(ClinicArt art,Transform parent)
        {
            roomGear=new ClinicRoomGear(art,parent,false);
            for(int kind=0;kind<2;kind++)for(int station=0;station<2;station++)for(int level=2;level<=TopLevel;level++)
            {
                var root=art.Group((kind==0?"Desk ":"Station ")+station+" equipment "+level,parent);stationDetails[kind,station,level-2]=root.gameObject;
                var at=kind==0?Desk(station):Bay(station);int n=level-2,slot=n%5;bool upper=n>=5;
                if(kind==0)
                {
                    // On the visitor ledge, left of the payment tray.
                    float px=at.x-.65f+slot*.13f,z=at.z-.27f;
                    if(!upper)
                    {
                        art.Box("Individual desk terminal",root,new Vector3(px,1.34f,z),new Vector3(.11f,.17f,.16f),level%2==0?"SageDark":"Gold");
                        art.Box("Individual terminal display",root,new Vector3(px,1.43f,z),new Vector3(.08f,.015f,.10f),"Blue");
                    }
                    else art.Box("Desk accessory tray",root,new Vector3(px,1.44f,z),new Vector3(.10f,.035f,.12f),level%2==0?"Apricot":"Linen");
                }
                else
                {
                    // On top of the bay's cabinet.
                    float px=at.x-.84f+slot*.10f;
                    if(!upper)
                    {
                        art.Box("Individual care instrument",root,new Vector3(px,1.27f,at.z+.77f),new Vector3(.08f,.22f,.20f),level%2==0?"Blue":"Apricot");
                        art.Box("Instrument clean label",root,new Vector3(px,1.29f,at.z+.663f),new Vector3(.052f,.06f,.013f),"Linen");
                    }
                    else art.Orb("Instrument status light",root,new Vector3(px,1.40f,at.z+.69f),new Vector3(.05f,.05f,.05f),level%2==0?"Gold":"Leaf");
                }
                root.gameObject.SetActive(false);
            }
            for(int room=0;room<3;room++)for(int track=0;track<2;track++)for(int level=2;level<=TopLevel;level++)
            {
                var root=art.Group(((ClinicRoom)room)+" "+((UpgradeTrack)track)+" level "+level,parent);
                details[room,track,level-2]=root.gameObject;int n=level-2;
                if(room==0)Reception(art,root,track,n);
                else if(room==1)Treatment(art,root,track,n);
                else Waiting(art,root,track,n);
                root.gameObject.SetActive(false);
            }
        }
        internal void Render(ClinicState state)
        {
            roomGear.Render(state);
            for(int kind=0;kind<2;kind++)for(int station=0;station<2;station++)
            {
                int equipment=0;
                if(kind==0){for(int i=0;i<state.ReceptionDesks.Count;i++)if(state.ReceptionDesks[i].Id==station)equipment=state.ReceptionDesks[i].EquipmentLevel;}
                else {for(int i=0;i<state.TreatmentStations.Count;i++)if(state.TreatmentStations[i].Id==station)equipment=state.TreatmentStations[i].EquipmentLevel;}
                for(int level=2;level<=TopLevel;level++)ClinicUpgradeEffects.Show(stationDetails[kind,station,level-2],level<=equipment);
            }
            for(int room=0;room<3;room++)
            {
                ClinicRoomState data=null;for(int i=0;i<state.Rooms.Count;i++)if((int)state.Rooms[i].Kind==room){data=state.Rooms[i];break;}
                for(int track=0;track<2;track++)for(int level=2;level<=TopLevel;level++)
                {
                    // Equipment is shown piece by piece by ClinicRoomGear once it is priced per item.
                    bool active=data!=null&&data.Built&&data.Level((UpgradeTrack)track)>=level&&!(track==0&&ClinicGear.Active(state,(ClinicRoom)room));
                    ClinicUpgradeEffects.Show(details[room,track,level-2],active);
                }
            }
        }
        private static void Reception(ClinicArt art,Transform root,int track,int n)
        {
            int slot=n%5;bool second=n>=5;
            if(track==0)
            {
                // A larger register suite on the first desk's staff work surface, clear of both actor sockets.
                float x=2.25f+slot*.17f;
                if(!second)
                {
                    art.Box("Register accessory base",root,new Vector3(x,1.175f,-1.8f),new Vector3(.15f,.15f,.18f),n%2==0?"Ivory":"SageDark");
                    art.Box("Receipt and appointment slips",root,new Vector3(x,1.26f,-1.8f),new Vector3(.11f,.015f,.14f),"Linen");
                }
                else
                {
                    art.Box("Appointment screen",root,new Vector3(x,1.38f,-1.72f),new Vector3(.15f,.11f,.02f),"Ink");
                    art.Box("Appointment screen glow",root,new Vector3(x,1.38f,-1.73f),new Vector3(.12f,.08f,.01f),"Blue");
                }
            }
            else
            {
                // Guide posts between the two queue rows, ropes joining them, and a sign at the entrance end.
                float x=QueueWest+.15f+slot*.62f,z=(QueueRowA+QueueRowB)*.5f;
                if(!second)
                {
                    art.Cylinder("Queue guide post",root,new Vector3(x,.47f,z),new Vector3(.055f,.65f,.055f),"Gold");
                    art.Cylinder("Queue guide foot",root,new Vector3(x,.16f,z),new Vector3(.19f,.045f,.19f),"SageDark");
                    art.Box("Queue floor arrow",root,new Vector3(x,.146f,QueueRowA+.35f),new Vector3(.28f,.008f,.06f),"Gold");
                }
                else if(slot<4)art.Box("Queue guide rope",root,new Vector3(x+.31f,.70f,z),new Vector3(.56f,.03f,.03f),"Rose");
                else art.Box("Queue welcome sign",root,new Vector3(QueueWest-.3f,.95f,z),new Vector3(.03f,.22f,.34f),"SignBlue");
            }
        }
        private static void Treatment(ClinicArt art,Transform root,int track,int n)
        {
            int slot=n%5;bool second=n>=5;
            if(track==0)
            {
                // Shelves on treatment room 2's north wall, east of the bay.
                float x=-2.75f+slot*.33f;float y=second?1.49f:1.04f,z=Back-.25f;
                art.Box("Treatment instrument shelf",root,new Vector3(x,y,z),new Vector3(.3f,.075f,.3f),"Gold");
                art.Cylinder("Treatment supply bottle",root,new Vector3(x,y+.19f,z),new Vector3(.13f,.32f,.13f),n%2==0?"Blue":"Linen");
                art.Box("Supply label",root,new Vector3(x,y+.19f,z-.072f),new Vector3(.08f,.09f,.018f),"Apricot");
            }
            else
            {
                // Linen cupboards along treatment room 1's south wall, west of the lanes to the chair.
                float x=-5.35f+slot*.45f,z=Middle+.36f;
                if(!second)
                {
                    art.Box("Clean linen cupboard",root,new Vector3(x,.46f,z),new Vector3(.42f,.60f,.42f),"Sage");
                    art.Box("Fresh linen stack",root,new Vector3(x,.80f,z),new Vector3(.32f,.07f,.28f),"Linen");
                    art.Box("Cupboard pull",root,new Vector3(x,.53f,z+.22f),new Vector3(.12f,.03f,.025f),"Gold");
                }
                else
                {
                    art.Box("Supply basket",root,new Vector3(x,.90f,z),new Vector3(.30f,.12f,.24f),"Wood");
                    art.Box("Rolled towels",root,new Vector3(x,.98f,z),new Vector3(.24f,.06f,.18f),n%2==0?"TileBlue":"Linen");
                }
            }
        }
        private static void Waiting(ClinicArt art,Transform root,int track,int n)
        {
            int slot=n%5;bool second=n>=5;
            if(track==0)
            {
                // Reading shelves along the lounge's back partition.
                float x=-5.2f+slot*.57f,z=Middle-.3f;
                if(!second)
                {
                    art.Box("Reading shelf",root,new Vector3(x,.43f,z),new Vector3(.43f,.08f,.32f),"Wood");
                    art.Cylinder("Reading shelf foot",root,new Vector3(x,.28f,z),new Vector3(.075f,.28f,.075f),"Gold");
                    art.Box("Lounge reading book",root,new Vector3(x,.49f,z),new Vector3(.25f,.045f,.27f),n%2==0?"Apricot":"Blue");
                }
                else
                {
                    art.Box("Standing magazine",root,new Vector3(x-.1f,.60f,z),new Vector3(.03f,.18f,.20f),n%2==0?"Rose":"Mustard");
                    art.Box("Standing magazine",root,new Vector3(x+.1f,.58f,z),new Vector3(.03f,.15f,.20f),"Linen");
                }
            }
            else
            {
                // Seat pairs themselves expand in ClinicWorld; these matching lamps on the back partition identify each addition.
                float x=-5.3f+slot*.8f,z=Middle-.1f;
                if(!second)
                {
                    art.Box("Lounge comfort sconce",root,new Vector3(x,1.1f,z),new Vector3(.19f,.2f,.09f),"Gold");
                    art.Orb("Sconce shade",root,new Vector3(x,1.23f,z-.05f),new Vector3(.19f,.24f,.19f),"Linen");
                }
                else art.Orb("Sconce warm glow",root,new Vector3(x,1.23f,z-.08f),new Vector3(.12f,.12f,.12f),"LampLight");
            }
        }
    }
}
