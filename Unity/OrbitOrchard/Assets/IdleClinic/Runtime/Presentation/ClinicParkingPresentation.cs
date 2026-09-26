using IdleClinic.Core;
using UnityEngine;

namespace IdleClinic.Presentation
{
    /// <summary>Six pooled vehicles follow durable trip phases; presentation never releases a bay.</summary>
    internal sealed class ClinicParkingPresentation
    {
        /// <summary>Where the car park's cash box marker floats: just above the pay machine.</summary>
        internal static Vector3 CashPoint(bool doctors)=>PayMachinePoint+new Vector3(0,1.62f,0)+(doctors?DoctorsParkingLayout.Offset:Vector3.zero);
        private static readonly Vector3 PayMachinePoint=new Vector3(-6.30f,.14f,-2.1f);
        private readonly GameObject[] bayMarks,futureBays,tierDetails;
        private readonly Vehicle[] vehicles;private readonly Transform entryArm,exitArm;
        internal ClinicParkingPresentation(ClinicArt art,Transform parent,bool doctors=false)
        {
            int count=doctors?12:6,tiers=doctors?6:3;bayMarks=new GameObject[count];futureBays=new GameObject[count];vehicles=new Vehicle[count];tierDetails=new GameObject[tiers];
            Vector3 offset=doctors?DoctorsParkingLayout.Offset:Vector3.zero;float length=doctors?18.9f:10.2f,mid=doctors?4.35f:0;
            var lot=art.Group("Clinic car park",parent,offset);
            art.Box("Car park surface",lot,new Vector3(-10.325f,-.02f,mid),new Vector3(8.35f,.25f,length),"AsphaltDark");
            art.Box("Parking drive aisle",lot,new Vector3(-10.6f,.108f,mid),new Vector3(3.0f,.013f,length-.1f),"AsphaltDark");
            for(int side=-1;side<=1;side+=2)art.Box("Parking aisle edge line",lot,new Vector3(-10.6f+side*1.46f,.118f,mid),new Vector3(.05f,.006f,length-.4f),"Paint");
            art.Box("Parking pedestrian path",lot,new Vector3(-6.65f,.105f,mid-.5f),new Vector3(.85f,.07f,length+.9f),"Concrete");
            for(float z=mid-length*.5f;z<mid+length*.5f;z+=.9f)art.Box("Parking path joint",lot,new Vector3(-6.65f,.142f,z),new Vector3(.84f,.004f,.018f),"Slate");
            if(doctors)art.Box("Parking street connection",lot,new Vector3(-10.2f,-.075f,-8.0f),new Vector3(5.65f,.07f,2.4f),"AsphaltDark");
            art.Box("Parking entrance apron",lot,new Vector3(-10.2f,.04f,-6.25f),new Vector3(5.65f,.12f,2.50f),"AsphaltDark");
            for(int stripe=0;stripe<8;stripe++)art.Box("Parking speed hump",lot,new Vector3(-12.95f+stripe*.36f,.108f,-5.15f),new Vector3(.36f,.03f,.34f),stripe%2==0?"PlateYellow":"Tyre");
            art.Box("Parking west kerb",lot,new Vector3(-14.5f,.17f,mid),new Vector3(.16f,.22f,length),"Render");
            art.Box("Parking north kerb",lot,new Vector3(-10.3f,.17f,mid+length*.5f-.03f),new Vector3(8.4f,.22f,.16f),"Render");
            art.Box("Parking entrance island",lot,new Vector3(-10.05f,.18f,-7.30f),new Vector3(.22f,.20f,.85f),"Render");
            art.Orb("Entrance island shrub",lot,new Vector3(-10.05f,.36f,-7.30f),new Vector3(.26f,.22f,.62f),"Leaf");
            PayMachine(art,lot,PayMachinePoint);
            Arrow(art,lot,"Parking entrance arrow",new Vector3(-11.35f,.121f,-5.65f),0);
            Arrow(art,lot,"Parking exit arrow",new Vector3(-8.7f,.121f,-5.65f),180);
            Arrow(art,lot,"Parking aisle arrow",new Vector3(-10.6f,.121f,.3f),0);
            art.Box("Parking slow marking",lot,new Vector3(-11.35f,.121f,-4.55f),new Vector3(.62f,.006f,.14f),"Paint");
            for(int row=0;row<tiers;row++)
            {
                float crossing=-4.42f+row*2.9f;
                for(int stripe=0;stripe<10;stripe++)art.Box("Parking passenger crossing",lot,new Vector3(-12.75f+stripe*.64f,.119f,crossing),new Vector3(.30f,.009f,.52f),"Paint");
            }
            entryArm=Gate(art,lot,"Parking entrance gate",new Vector3(-12.80f,.14f,-6.68f),true);
            exitArm=Gate(art,lot,"Parking exit gate",new Vector3(-10.05f,.28f,-7.05f),false);
            for(int i=0;i<bayMarks.Length;i++)
            {
                Vector3 center=ClinicParkingLayout.BayCenter(i);
                var bay=art.Group("Parking bay "+i,lot,new Vector3(center.x,.12f,center.z));bayMarks[i]=bay.gameObject;
                // White T-marked bays on asphalt; the bay nearest the clinic path is accessible, one has a charger.
                float nose=i%2==0?-1.10f:1.10f;
                if(i==1)AccessibleBay(art,bay);
                if(i==2)ChargingBay(art,bay,nose);
                for(int side=-1;side<=1;side+=2)art.Box("Parking bay stripe",bay,new Vector3(0,.01f,side*1.23f),new Vector3(2.25f,.008f,.07f),"Paint");
                art.Box("Parking bay end line",bay,new Vector3(nose*1.03f,.01f,0),new Vector3(.07f,.008f,2.46f),"Paint");
                art.Box("Parking stop",bay,new Vector3(nose,.055f,0),new Vector3(.14f,.10f,1.10f),"Concrete");
                for(int reflector=-1;reflector<=1;reflector+=2)art.Box("Parking stop reflector",bay,new Vector3(nose-Mathf.Sign(nose)*.071f,.07f,reflector*.34f),new Vector3(.01f,.04f,.14f),"PlateYellow");
                var car=ClinicStreetLife.Car(art,lot,"Parked patient car "+i,center,i);vehicles[i]=new Vehicle(car,i,offset,doctors?-1:0);
                car.SetActive(false);bayMarks[i].SetActive(false);futureBays[i]=BuildFutureBay(art,lot,i,new Vector3(center.x,.12f,center.z));
            }
            var sign=art.Group("Parking expansion marker",lot,ClinicParkingLayout.SignPoint);
            art.Box("Parking sign",sign,Vector3.zero,new Vector3(.88f,.70f,.08f),"SignBlue");
            art.Box("Parking sign border",sign,new Vector3(0,0,-.045f),new Vector3(.80f,.62f,.01f),"Paint");
            art.Box("Parking sign field",sign,new Vector3(0,0,-.05f),new Vector3(.74f,.56f,.01f),"SignBlue");
            art.Box("Parking letter stem",sign,new Vector3(-.12f,0,-.058f),new Vector3(.09f,.44f,.018f),"Paint");
            art.Box("Parking letter top",sign,new Vector3(.03f,.17f,-.058f),new Vector3(.32f,.09f,.018f),"Paint");
            art.Box("Parking letter middle",sign,new Vector3(.03f,0,-.058f),new Vector3(.32f,.09f,.018f),"Paint");
            art.Box("Parking letter curve",sign,new Vector3(.16f,.085f,-.058f),new Vector3(.09f,.22f,.018f),"Paint");
            art.Box("Parking sign post",lot,ClinicParkingLayout.SignPoint+new Vector3(0,-.40f,.04f),new Vector3(.07f,1.0f,.07f),"Chrome");
            for(int level=1;level<=tiers;level++)
            {
                var detail=art.Group("Parking tier "+level,lot);tierDetails[level-1]=detail.gameObject;float z=-3+(level-1)*2.9f;
                art.Box("Parking planting strip",detail,new Vector3(-14.9f,.23f,z),new Vector3(.38f,.35f,2.5f),"Render");
                art.Box("Parking planting soil",detail,new Vector3(-14.9f,.41f,z),new Vector3(.30f,.02f,2.42f),"Timber");
                for(int n=0;n<3;n++)art.Orb("Parking hedge",detail,new Vector3(-14.9f,.60f,z-.8f+n*.8f),new Vector3(.58f,.66f,.83f),n%2==0?"Leaf":"CarSage");
                // Column street light: tapered pole, outreach arm and a flat LED head over the bays.
                art.Cylinder("Parking lamp post",detail,new Vector3(-14.65f,1.6f,z),new Vector3(.08f,3.2f,.08f),"CarGraphite");
                art.Cylinder("Parking lamp base",detail,new Vector3(-14.65f,.30f,z),new Vector3(.16f,.40f,.16f),"CarGraphite");
                art.Box("Parking lamp arm",detail,new Vector3(-14.25f,3.17f,z),new Vector3(.80f,.05f,.05f),"CarGraphite");
                art.Box("Parking lamp head",detail,new Vector3(-13.85f,3.14f,z),new Vector3(.46f,.07f,.20f),"CarGraphite");
                art.Box("Parking lamp lens",detail,new Vector3(-13.85f,3.10f,z),new Vector3(.40f,.02f,.15f),"LampLight");
            }
        }
        internal void Render(ClinicState state,int level,bool reducedMotion)
        {
            bool entering=false,exiting=false;
            for(int i=0;i<bayMarks.Length;i++)
            {
                bayMarks[i].SetActive(i<level*2);futureBays[i].SetActive(i>=level*2);ClinicPatientState owner=null;
                for(int p=0;p<state.Patients.Count;p++)if(state.Patients[p].ParkingBayId==i){owner=state.Patients[p];break;}
                vehicles[i].Render(owner,state.Tick+state.SubTick,i<level*2);
                entering|=owner!=null&&owner.Phase==ClinicPatientPhase.DrivingToParking;
                exiting|=owner!=null&&owner.Phase==ClinicPatientPhase.DrivingFromParking;
            }
            for(int i=0;i<tierDetails.Length;i++)tierDetails[i].SetActive(i<level);
            // The barrier changes immediately, before the authoritative trip reaches its gate.
            entryArm.localRotation=Quaternion.Euler(0,0,entering?82:0);exitArm.localRotation=Quaternion.Euler(0,0,exiting?82:0);
        }
        private static Transform Gate(ClinicArt art,Transform parent,string name,Vector3 position,bool entrance)
        {
            var gate=art.Group(name,parent,position);
            art.Box("Parking gate pedestal",gate,new Vector3(0,.48f,0),new Vector3(.24f,.96f,.26f),"CarGraphite");
            art.Box("Parking gate band",gate,new Vector3(0,.78f,0),new Vector3(.25f,.07f,.27f),"PlateYellow");
            art.Box("Parking ticket reader",gate,new Vector3(entrance?.16f:-.16f,.70f,0),new Vector3(.10f,.16f,.14f),"LampLight");
            var hinge=art.Group("Parking gate arm hinge",gate,new Vector3(0,.83f,0));
            float armLength=entrance?2.40f:2.70f;
            art.Box("Parking boom barrier",hinge,new Vector3(armLength*.5f,0,0),new Vector3(armLength,.09f,.075f),"Paint");
            for(int i=0;i<5;i++)art.Box("Parking barrier stripe",hinge,new Vector3(.25f+i*(armLength-.40f)/4,0,-.042f),new Vector3(.16f,.09f,.012f),"TailLight");
            art.Box("Parking gate direction sign",gate,new Vector3(0,1.17f,0),new Vector3(.48f,.35f,.06f),entrance?"SignBlue":"TailLight");
            art.Box("Parking sign direction stem",gate,new Vector3(0,1.17f,-.04f),new Vector3(.055f,.22f,.015f),"Paint");
            for(int side=-1;side<=1;side+=2)art.Box("Parking sign arrow tip",gate,new Vector3(side*.055f,1.17f+(entrance?.07f:-.07f),-.04f),new Vector3(.15f,.045f,.015f),"Paint").transform.localRotation=Quaternion.Euler(0,0,side*(entrance?-45:45));
            return hinge;
        }
        private static void Arrow(ClinicArt art,Transform parent,string name,Vector3 position,float angle)
        {
            var arrow=art.Group(name,parent,position);arrow.localRotation=Quaternion.Euler(0,angle,0);
            art.Box("Drive direction shaft",arrow,new Vector3(0,0,-.14f),new Vector3(.11f,.008f,.62f),"Paint");
            for(int side=-1;side<=1;side+=2)art.Box("Drive direction head",arrow,new Vector3(side*.14f,0,.12f),new Vector3(.10f,.008f,.42f),"Paint").transform.localRotation=Quaternion.Euler(0,-side*45,0);
        }
        private sealed class Vehicle
        {
            private readonly GameObject root;private readonly int bay;private readonly Transform[] wheels;private readonly bool[] front;
            private readonly ClinicParkingPath entry,reverse,exit;private readonly Vector3 offset;private readonly float roadShift;
            internal Vehicle(GameObject root,int bay,Vector3 offset,float roadShift)
            {
                this.root=root;this.bay=bay;this.offset=offset;this.roadShift=roadShift;entry=ClinicParkingPath.Enter(bay,offset,roadShift);reverse=ClinicParkingPath.Reverse(bay,offset);exit=ClinicParkingPath.Exit(bay,offset,roadShift);
                var parts=root.GetComponentsInChildren<Transform>();var list=new System.Collections.Generic.List<Transform>();
                foreach(var part in parts)if(part.name=="Car tyre"||part.name=="Car hubcap")list.Add(part);wheels=list.ToArray();front=new bool[wheels.Length];
                for(int i=0;i<wheels.Length;i++)front[i]=wheels[i].localPosition.z>0;
            }
            internal void Render(ClinicPatientState owner,double tick,bool available)
            {
                root.SetActive(available&&owner!=null);if(owner==null)return;
                Vector3 position=ClinicParkingLayout.BayCenter(bay)+offset,direction=ClinicParkingLayout.BayFacing(bay);float distance=0,steer=0;
                double elapsed=System.Math.Max(0,tick-owner.PhaseStartedTick);ClinicParkingPath route=null;float progress=0;bool backwards=false;
                if(owner.Phase==ClinicPatientPhase.WaitingToPark)position=new Vector3(-32-bay*3,-.11f,-8.2f+roadShift)+offset;
                else if(owner.Phase==ClinicPatientPhase.DrivingToParking){route=entry;progress=(float)(elapsed/ClinicRules.ParkingEntryTicks);}
                else if(owner.Phase==ClinicPatientPhase.DrivingFromParking)
                {
                    if(elapsed<=ClinicRules.ParkingReverseTicks){route=reverse;progress=(float)(elapsed/ClinicRules.ParkingReverseTicks);backwards=true;}
                    else if(elapsed<ClinicRules.ParkingReverseTicks+ClinicRules.ParkingGearChangeTicks){position=reverse.Sample(1,out var heading,out _);direction=-heading;distance=-reverse.Length;}
                    else {route=exit;progress=(float)((elapsed-ClinicRules.ParkingReverseTicks-ClinicRules.ParkingGearChangeTicks)/ClinicRules.ParkingExitTravelTicks);}
                }
                if(route!=null)
                {
                    position=route.Sample(progress,out direction,out distance);route.Sample(Mathf.Min(1,progress+.025f),out var next,out _);
                    steer=Mathf.Clamp(Vector3.SignedAngle(direction,next,Vector3.up)*(backwards?-1:1),-26,26);
                    if(backwards){direction=-direction;distance=-distance;}
                    else if(route==exit)distance-=reverse.Length;
                }
                root.transform.SetPositionAndRotation(position,Quaternion.LookRotation(direction));
                float roll=distance/.175f*Mathf.Rad2Deg;
                for(int i=0;i<wheels.Length;i++)wheels[i].localRotation=Quaternion.Euler(0,front[i]?steer:0,0)*Quaternion.Euler(roll,0,0)*Quaternion.Euler(0,0,90);
            }
        }
        private static void AccessibleBay(ClinicArt art,Transform bay)
        {
            art.Box("Accessible bay surface",bay,new Vector3(0,.004f,0),new Vector3(2.2f,.008f,2.36f),"SignBlue");
            var symbol=art.Group("Accessible parking symbol",bay,new Vector3(0,.012f,0));
            art.Cylinder("Wheelchair wheel",symbol,new Vector3(-.08f,0,-.12f),new Vector3(.62f,.006f,.62f),"Paint");
            art.Cylinder("Wheelchair wheel centre",symbol,new Vector3(-.08f,.002f,-.12f),new Vector3(.46f,.006f,.46f),"SignBlue");
            art.Orb("Wheelchair user head",symbol,new Vector3(.06f,.004f,.42f),new Vector3(.16f,.006f,.16f),"Paint");
            art.Box("Wheelchair user back",symbol,new Vector3(.02f,.004f,.20f),new Vector3(.09f,.006f,.32f),"Paint");
            art.Box("Wheelchair user seat",symbol,new Vector3(.14f,.004f,.02f),new Vector3(.30f,.006f,.09f),"Paint");
            art.Box("Wheelchair user leg",symbol,new Vector3(.30f,.004f,-.12f),new Vector3(.08f,.006f,.30f),"Paint");
        }
        private static void ChargingBay(ClinicArt art,Transform bay,float nose)
        {
            art.Box("Charging bay surface",bay,new Vector3(0,.004f,0),new Vector3(2.2f,.008f,2.36f),"CarSage");
            var charger=art.Group("Parking EV charger",bay,new Vector3(nose*1.18f,0,.95f));
            art.Box("Charger plinth",charger,new Vector3(0,.06f,0),new Vector3(.30f,.12f,.30f),"Concrete");
            art.Box("Charger post",charger,new Vector3(0,.62f,0),new Vector3(.20f,1.04f,.16f),"CarGraphite");
            art.Box("Charger screen",charger,new Vector3(-Mathf.Sign(nose)*.105f,.86f,0),new Vector3(.01f,.20f,.12f),"LampLight");
            art.Box("Charger status light",charger,new Vector3(-Mathf.Sign(nose)*.105f,1.06f,0),new Vector3(.01f,.06f,.14f),"CarSage");
            art.Box("Charger cable",charger,new Vector3(-Mathf.Sign(nose)*.11f,.52f,0),new Vector3(.02f,.36f,.02f),"Tyre");
        }
        private static void PayMachine(ClinicArt art,Transform parent,Vector3 position)
        {
            var machine=art.Group("Parking pay machine",parent,position);machine.localRotation=Quaternion.Euler(0,-90,0);
            art.Box("Pay machine body",machine,new Vector3(0,.72f,0),new Vector3(.42f,1.44f,.30f),"SignBlue");
            art.Box("Pay machine hood",machine,new Vector3(0,1.47f,-.02f),new Vector3(.48f,.07f,.38f),"CarGraphite");
            art.Box("Pay machine screen",machine,new Vector3(0,1.12f,-.155f),new Vector3(.26f,.18f,.01f),"LampLight");
            art.Box("Pay machine keypad",machine,new Vector3(0,.86f,-.155f),new Vector3(.20f,.14f,.01f),"Chrome");
            art.Box("Pay machine ticket slot",machine,new Vector3(0,.62f,-.155f),new Vector3(.16f,.03f,.01f),"Tyre");
        }
        private static GameObject BuildFutureBay(ClinicArt art,Transform parent,int index,Vector3 position)
        {
            var root=art.Group("Future parking bay "+index,parent,position);
            art.Box("Parking staging pad",root,new Vector3(0,.008f,0),new Vector3(1.95f,.018f,2.30f),index%2==0?"TilePeach":"TileSage");
            for(int corner=-1;corner<=1;corner+=2)
            {
                art.Box("Parking survey corner",root,new Vector3(corner*.79f,.029f,corner*.94f),new Vector3(.30f,.012f,.04f),"Linen");
                art.Box("Parking survey corner",root,new Vector3(corner*.92f,.029f,corner*.81f),new Vector3(.04f,.012f,.30f),"Linen");
            }
            var supplies=art.Group("Covered parking pavers",root,new Vector3(-.15f,0,-.10f));
            art.Box("Paver delivery pallet",supplies,new Vector3(0,.09f,0),new Vector3(.92f,.07f,.70f),"Wood");
            art.Box("Stacked paving slabs",supplies,new Vector3(0,.22f,0),new Vector3(.82f,.20f,.60f),"Clay");
            art.Box("Paver weather cover",supplies,new Vector3(0,.38f,0),new Vector3(.90f,.12f,.67f),index%2==0?"TileBlue":"Apricot");
            art.Box("Paver securing strap",supplies,new Vector3(0,.383f,0),new Vector3(.065f,.135f,.70f),"Gold");
            art.Box("Portable parking planter",root,new Vector3(.37f,.18f,.63f),new Vector3(.46f,.28f,.46f),"Wood");
            art.Orb("Portable planter foliage",root,new Vector3(.37f,.45f,.63f),new Vector3(.63f,.40f,.62f),"Leaf");
            return root.gameObject;
        }

    }
}
