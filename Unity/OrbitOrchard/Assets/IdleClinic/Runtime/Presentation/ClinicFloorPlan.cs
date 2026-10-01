using UnityEngine;

namespace IdleClinic.Presentation
{
    /// <summary>The starter clinic's floor plan, in world metres (x east, z north; the street is to the south). An 11.4 by
    /// 11 m building: across the front, the waiting lounge, the entrance lobby and the reception; behind, two treatment
    /// rooms west of a central corridor, and the office, staff room, accessible WC and store to the east. Walls facing the
    /// camera are cut low so every room reads from above; the west and north outer walls and the reception's feature wall
    /// stand full height. Everything that places walls, doors, furniture, sockets or walking routes reads from here.</summary>
    internal static class ClinicFloorPlan
    {
        internal const float Floor=.14f;
        // Wall centre lines.
        internal const float West=-5.72f,East=5.72f,Front=-5.13f,Back=5.87f;
        /// <summary>Between the front rooms (lounge, lobby, reception) and the back rooms.</summary>
        internal const float Middle=-.13f;
        /// <summary>The lobby and the corridor behind it run between these two lines.</summary>
        internal const float HallWest=-1f,HallEast=1f;
        /// <summary>Between treatment room 1 (south) and treatment room 2 (north).</summary>
        internal const float TreatmentSplit=2.87f;
        /// <summary>The east back rooms: office | staff room, and WC | store.</summary>
        internal const float ServiceSplitX=3.4f,ServiceSplitZ=2.67f;
        internal const float TallWall=2.05f,LowWall=.9f,FrontWall=.6f;
        internal const float OuterThickness=.16f,InnerThickness=.12f;

        internal static readonly Rect Lounge=new Rect(West,Front,HallWest-West,Middle-Front);
        internal static readonly Rect Reception=new Rect(HallEast,Front,East-HallEast,Middle-Front);
        internal static readonly Rect Hall=new Rect(HallWest,Front,HallEast-HallWest,Back-Front);
        internal static readonly Rect TreatmentOne=new Rect(West,Middle,HallWest-West,TreatmentSplit-Middle);
        internal static readonly Rect TreatmentTwo=new Rect(West,TreatmentSplit,HallWest-West,Back-TreatmentSplit);
        internal static readonly Rect Office=new Rect(HallEast,Middle,ServiceSplitX-HallEast,ServiceSplitZ-Middle);
        internal static readonly Rect StaffRoom=new Rect(ServiceSplitX,Middle,East-ServiceSplitX,ServiceSplitZ-Middle);
        internal static readonly Rect Washroom=new Rect(HallEast,ServiceSplitZ,ServiceSplitX-HallEast,Back-ServiceSplitZ);
        internal static readonly Rect Store=new Rect(ServiceSplitX,ServiceSplitZ,East-ServiceSplitX,Back-ServiceSplitZ);
        internal static Rect Treatment(int station)=>station==0?TreatmentOne:TreatmentTwo;
        /// <summary>The office, staff room and store (ClinicRoom order).</summary>
        internal static readonly Rect[] ServiceRoomRects={Office,StaffRoom,Store};
        internal static Rect ServiceRoomRect(IdleClinic.Core.ClinicRoom room)=>ServiceRoomRects[Mathf.Clamp((int)room-(int)IdleClinic.Core.ClinicRoom.Office,0,2)];

        // Doorways: centre along the wall and clear width.
        internal const float EntranceWidth=1.8f;
        internal const float TreatmentDoorWidth=1.4f,RoomDoorWidth=1f;
        internal static float TreatmentDoorZ(int station)=>station==0?1f:4f;
        internal const float OfficeDoorZ=.9f,WashroomDoorZ=4.2f,StaffDoorX=5.2f,StoreDoorX=4.6f,BackDoorWidth=.9f;

        // Walking lanes through the lobby and corridor: visitors keep right, so two can pass in any doorway.
        internal const float NorthLane=.25f,SouthLane=-.45f;
        /// <summary>Across the front of the reception counter, between the desks' visitor spots and the queue.</summary>
        internal const float CounterLane=-3.55f;
        /// <summary>Behind the counter, where staff walk to their desks.</summary>
        internal const float StaffLane=-.9f;
        /// <summary>The lounge's cross aisle (in front of the seat island) and its west aisle (between the wall seats and the island).</summary>
        internal const float LoungeCrossAisle=-4.25f,LoungeWestAisle=-4.45f,LoungeEastAisle=-1.9f;

        // The reception counter: two desks side by side facing the street, under the feature wall.
        internal static Vector3 Desk(int id)=>new Vector3(id==0?2.2f:4.1f,Floor,-2.03f);
        internal static readonly Vector3 FeatureWall=new Vector3(3.0f,Floor,Middle-InnerThickness*.5f-.1f);
        /// <summary>The feature wall is built 2 m wide; the reception's back wall gives it 1.6 m between the notice board and clock.</summary>
        internal const float FeatureWallScale=.8f;

        // The queue snakes in two rows between the counter lane and the storefront: row A (nearer the counter) walks west
        // to the head at its west end; row B, which newcomers join from the entrance, walks east and turns into row A.
        internal const int QueueRowLength=7,QueuePlaces=14;
        internal const float QueueRowA=-4.1f,QueueRowB=-4.72f,QueueWest=1.6f,QueueSpacing=.52f;
        internal static float QueueTurnX=>QueueWest+(QueueRowLength-1)*QueueSpacing;
        internal static Vector3 QueuePlace(int index)
        {
            bool rowA=index<QueueRowLength;int step=rowA?index:index-QueueRowLength;
            float x=rowA?QueueWest+step*QueueSpacing:QueueTurnX-step*QueueSpacing;
            return new Vector3(x,Floor,rowA?QueueRowA:QueueRowB);
        }
        internal static Vector3 QueueFacing(int index)=>index==0?Vector3.forward:index<QueueRowLength?Vector3.left:Vector3.right;
        internal static bool QueueRowAOf(int index)=>index<QueueRowLength;

        // Treatment rooms: one bay each, the chair facing the room with its cabinet against the north wall.
        internal static Vector3 Bay(int station)=>new Vector3(-3.4f,Floor,station==0?1.78f:4.78f);
        /// <summary>The walking line into (south of the door centre) or out of (north of it) a treatment room; the nurse
        /// stands beside the chair, clear of both.</summary>
        internal static float TreatmentLane(int station,bool entering)=>TreatmentDoorZ(station)+(entering?-.33f:.33f);

        // Waiting lounge seats: the island's east side (facing the lobby), its west side, then the row along the west wall.
        // Places 14-17 are corridor chairs that the store adds (see the store's capacity).
        internal const int LoungeSeats=14,Seats=17;
        internal static Vector3 Seat(int index)
        {
            if(index<4)return new Vector3(-2.95f,Floor,-1.75f-index*.64f);
            if(index<8)return new Vector3(-3.6f,Floor,-1.75f-(index-4)*.64f);
            if(index<14)return new Vector3(-5.3f,Floor,-1.3f-(index-8)*.66f);
            return new Vector3(.72f,Floor,1.9f+(index-14)*.64f);
        }
        internal static Vector3 SeatFacing(int index)=>index<4||index>=8&&index<14?Vector3.right:Vector3.left;
        /// <summary>Seats reached from the lounge's west aisle; the rest are reached straight from the lobby or corridor.</summary>
        internal static bool SeatUsesWestAisle(int index)=>index>=4&&index<14;

        /// <summary>Standing places by the corridor mouth before the lounge is built.</summary>
        internal static Vector3 Standing(int index)=>new Vector3(.62f,Floor,index==0?.25f:-.55f);

        // Amenities.
        internal static readonly Vector3 ToiletSeat=new Vector3(2.9f,Floor,5.3f);
        internal static readonly Vector3 VendingMachine=new Vector3(-2.35f,Floor,-.55f);
        internal static readonly Vector3 VendingPatient=new Vector3(-2.35f,Floor,-1.2f);
        internal static readonly Vector3 VendingTips=new Vector3(-1.62f,Floor+LowWall+.23f,Middle);

        internal static readonly Vector3 Entrance=new Vector3(0,Floor,-5.8f);
        internal static readonly Vector3 Exit=new Vector3(-.6f,Floor,-6.15f);
        /// <summary>The centre of the crossing and of the walk from the car park to the door.</summary>
        internal const float StreetCrossingX=0;
    }
}
