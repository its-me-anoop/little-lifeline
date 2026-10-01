using System;
using System.Collections.Generic;
using IdleClinic.Core;
using UnityEngine;
using static IdleClinic.Presentation.ClinicFloorPlan;

namespace IdleClinic.Presentation
{
    /// <summary>Walking routes through the starter clinic's floor plan, never a straight line through walls or furniture.
    /// Every place a person stands has a short way out to the hall (the lobby and corridor spine); between those, people
    /// keep right on the hall's two lanes, so visitors walking opposite ways pass each other in any doorway. The queue,
    /// the counter and the car park have their own short paths.</summary>
    internal sealed class ClinicRoute
    {
        private readonly Vector3[] points=new Vector3[64];
        private readonly List<Vector3> fromWay=new List<Vector3>(8),toWay=new List<Vector3>(8);
        private int count;
        private float length;
        internal float Length=>length;

        internal void Build(ClinicWorld world,string from,string to,bool staff,Vector3? visibleStart=null)
        {
            count=0;length=0;var start=visibleStart??world.GetAnchorPoint(from);var end=world.GetAnchorPoint(to);Add(start);
            if(from==to&&(end-start).sqrMagnitude<.000001f){Add(end);return;}
            if(world.Location==ClinicLocation.DoctorsClinic){DoctorsClinicRoute.Build(world,from,to,staff,start,Add);return;}
            float y=end.y;
            int fromQueue=Index(from,"reception.queue."),toQueue=Index(to,"reception.queue.");
            if(fromQueue>=0&&toQueue>=0)
            {
                // Along the row, turning at the east end between the rows.
                if(QueueRowAOf(fromQueue)!=QueueRowAOf(toQueue))
                { Add(new Vector3(QueueTurnX,y,start.z));Add(new Vector3(QueueTurnX,y,end.z)); }
                Add(end);return;
            }
            if(fromQueue>=0&&Starts(to,"reception.desk."))
            {
                // Out of the head of the queue (or round the west end of the rows) to the counter lane, then up to the desk.
                if(!QueueRowAOf(fromQueue)){Add(new Vector3(QueueWest-.5f,y,start.z));Add(new Vector3(QueueWest-.5f,y,CounterLane));}
                else Add(new Vector3(start.x,y,CounterLane));
                Add(new Vector3(end.x,y,CounterLane));Add(end);return;
            }
            if(toQueue>=0)
            {
                // In through the entrance on the arrivals lane, then along row B (empty until row A is full) to the place.
                Way(world,from,NorthLane,false,fromWay,y);Resume(world,from,start,NorthLane,fromWay,y);foreach(var point in fromWay)Add(point);
                Add(new Vector3(NorthLane,y,QueueRowB));Add(new Vector3(end.x,y,QueueRowB));Add(end);return;
            }
            Way(world,from,0,false,fromWay,y);Way(world,to,0,true,toWay,y);
            float fromZ=fromWay.Count>0?fromWay[fromWay.Count-1].z:start.z,toZ=toWay.Count>0?toWay[toWay.Count-1].z:end.z;
            float lane=toZ>=fromZ?NorthLane:SouthLane;
            Way(world,from,lane,false,fromWay,y);Way(world,to,lane,true,toWay,y);Resume(world,from,start,lane,fromWay,y);
            foreach(var point in fromWay)Add(point);
            for(int i=toWay.Count-1;i>=0;i--)Add(toWay[i]);
            Add(end);
        }

        /// <summary>The way from a place out to the hall lane at <paramref name="lane"/> (the last point is on the lane).
        /// <paramref name="arriving"/> picks the entry line of a treatment room rather than its exit line.</summary>
        private static void Way(ClinicWorld world,string anchor,float lane,bool arriving,List<Vector3> way,float y)
        {
            way.Clear();var at=world.GetAnchorPoint(anchor);int id;
            if(anchor=="entrance"||anchor=="exit"){way.Add(new Vector3(lane,y,at.z));return;}
            if((id=Index(anchor,"reception.desk."))>=0)
            {
                float z=anchor.EndsWith(".staff",StringComparison.Ordinal)?StaffLane:CounterLane;
                way.Add(new Vector3(at.x,y,z));way.Add(new Vector3(lane,y,z));return;
            }
            if((id=Index(anchor,"firstaid.station."))>=0)
            {
                bool nurse=anchor.EndsWith(".staff",StringComparison.Ordinal);float z=TreatmentLane(Mathf.Clamp(id,0,1),arriving||nurse);
                way.Add(new Vector3(at.x,y,z));way.Add(new Vector3(lane,y,z));return;
            }
            if((id=Index(anchor,"waiting.seat."))>=0)
            {
                if(SeatUsesWestAisle(id))
                { way.Add(new Vector3(LoungeWestAisle,y,at.z));way.Add(new Vector3(LoungeWestAisle,y,LoungeCrossAisle));way.Add(new Vector3(lane,y,LoungeCrossAisle));return; }
                if(id<4)way.Add(new Vector3(at.x+.6f,y,at.z));
                way.Add(new Vector3(lane,y,at.z));return;
            }
            if((id=Index(anchor,"reception.queue."))>=0)
            { way.Add(new Vector3(QueueWest-.5f,y,at.z));way.Add(new Vector3(lane,y,at.z));return; }
            if(Starts(anchor,"parking.bay."))
            {
                // Out beside the car, along the pedestrian lane at the car park's edge, then the front pavement to the door.
                float z=at.z-.58f;
                way.Add(new Vector3(at.x,y,z));way.Add(new Vector3(-6.65f,y,z));way.Add(new Vector3(-6.65f,y,-6.90f));way.Add(new Vector3(lane,y,-6.90f));return;
            }
            if(Starts(anchor,"waiting.toilet.")){way.Add(new Vector3(at.x,y,WashroomDoorZ));way.Add(new Vector3(lane,y,WashroomDoorZ));return;}
            way.Add(new Vector3(lane,y,at.z));
        }

        /// <summary>When a walk is re-aimed partway (a new destination, or a restored save), carry on from where the person
        /// is rather than walking back to where they set out. Someone already indoors never goes back out through the car park.</summary>
        private static void Resume(ClinicWorld world,string from,Vector3 start,float lane,List<Vector3> way,float y)
        {
            if(way.Count==0||(start-world.GetAnchorPoint(from)).sqrMagnitude<=.09f)return;
            int nearest=0;float best=float.MaxValue;
            for(int i=0;i<way.Count;i++){float d=(way[i]-start).sqrMagnitude;if(d<best){best=d;nearest=i;}}
            if(nearest>0)way.RemoveRange(0,nearest);
            if(start.z>Front&&way[0].z<Front){way.Clear();way.Add(new Vector3(lane,y,start.z));}
        }

        internal void BuildSavedPath(IList<ClinicMovementPoint> path,float y)
        {count=0;length=0;foreach(var point in path)Add(new Vector3(point.X,y,point.Z));}
        private static bool Starts(string value,string prefix)=>value!=null&&value.StartsWith(prefix,StringComparison.Ordinal);
        /// <summary>The number after a prefix ("reception.queue.4" gives 4), or -1.</summary>
        private static int Index(string value,string prefix)
        {
            if(!Starts(value,prefix))return -1;
            int result=0,i=prefix.Length;bool any=false;
            for(;i<value.Length&&char.IsDigit(value[i]);i++){result=result*10+(value[i]-'0');any=true;}
            return any?result:-1;
        }
        private void Add(Vector3 point)
        { if(count>0){float distance=Vector3.Distance(points[count-1],point);if(distance<.001f)return;length+=distance;}if(count<points.Length)points[count++]=point; }
        internal Vector3 Sample(float progress,out Vector3 direction)
        {
            direction=Vector3.forward;if(count==0)return Vector3.zero;if(count==1)return points[0];
            float remaining=length*Mathf.Clamp01(progress);
            for(int i=1;i<count;i++)
            { var delta=points[i]-points[i-1];float distance=delta.magnitude;if(remaining<=distance||i==count-1){direction=delta.normalized;return Vector3.Lerp(points[i-1],points[i],distance==0?1:remaining/distance);}remaining-=distance; }
            return points[count-1];
        }
    }
}
