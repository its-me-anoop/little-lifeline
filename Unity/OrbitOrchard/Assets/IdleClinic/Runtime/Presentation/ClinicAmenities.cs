using IdleClinic.Core;
using UnityEngine;
using static IdleClinic.Presentation.ClinicFloorPlan;

namespace IdleClinic.Presentation
{
    /// <summary>Amenity tiers and occupancy are projections of simulation state, never an income source. The toilet is the
    /// accessible WC off the corridor; the vending machine stands at the back of the lounge.</summary>
    internal sealed class ClinicAmenities
    {
        internal static readonly Vector3 ParkingPoint=ClinicParkingLayout.SignPoint;
        internal static readonly Vector3 ToiletPoint=new Vector3(Washroom.center.x,1.3f,Washroom.center.y);
        internal static readonly Vector3 VendingPoint=new Vector3(VendingMachine.x,1.15f,VendingMachine.z);
        internal static readonly Vector3 VendingCashPoint=VendingTips;
        private readonly ClinicParkingPresentation parkingWorld;
        private readonly GameObject[,] tierDetails=new GameObject[3,3];
        private readonly GameObject toilet,vending,toiletPlot,vendingPlot,toiletClosure,cash,tipCup;
        private readonly Transform vendingButton,vendingTip;
        internal long VendingTill { get; private set; }
        internal static Vector3 BayPatient(int bay)=>ClinicParkingLayout.BayDoor(bay);
        internal ClinicAmenities(ClinicArt art,Transform parent)
        {
            var root=art.Group("Clinic amenities",parent);
            parkingWorld=new ClinicParkingPresentation(art,root);
            var washroom=art.Group("Accessible WC",root);
            toiletPlot=art.Group("Toilet build marker",washroom,ToiletPoint).gameObject;
            art.Box("Toilet plot placard",toiletPlot.transform,Vector3.zero,new Vector3(.7f,.55f,.07f),"Sage");
            art.Orb("Toilet pictogram head",toiletPlot.transform,new Vector3(0,.12f,-.055f),new Vector3(.11f,.11f,.035f),"Linen");
            art.Box("Toilet pictogram body",toiletPlot.transform,new Vector3(0,-.04f,-.055f),new Vector3(.16f,.18f,.035f),"Linen");
            // Until the toilet is fitted, a boarded panel closes the WC doorway.
            var closure=art.Group("Future toilet doorway closure",washroom,new Vector3(HallEast-.12f,Floor,WashroomDoorZ));toiletClosure=closure.gameObject;
            art.Box("Toilet closure board",closure,new Vector3(0,LowWall*.48f,0),new Vector3(.05f,LowWall*.95f,RoomDoorWidth),"Wood");
            art.Box("Toilet construction crossbar",closure,new Vector3(-.04f,LowWall*.55f,0),new Vector3(.03f,.1f,RoomDoorWidth+.05f),"Apricot");
            toilet=art.Group("Patient toilet",washroom).gameObject;
            var seat=ToiletSeat;
            art.Cylinder("Toilet pedestal",toilet.transform,new Vector3(seat.x,.34f,seat.z+.05f),new Vector3(.40f,.40f,.53f),"Linen");
            art.Orb("Toilet bowl",toilet.transform,new Vector3(seat.x,.52f,seat.z),new Vector3(.62f,.25f,.72f),"Linen");
            art.Orb("Toilet seat inset",toilet.transform,new Vector3(seat.x,.646f,seat.z-.05f),new Vector3(.43f,.014f,.47f),"Blue");
            art.Box("Toilet cistern",toilet.transform,new Vector3(seat.x,.78f,seat.z+.4f),new Vector3(.62f,.72f,.2f),"Linen");
            art.Box("Toilet flush",toilet.transform,new Vector3(seat.x+.17f,1.16f,seat.z+.4f),new Vector3(.12f,.035f,.08f),"Chrome");
            // Accessible fittings: a fold-down rail beside the pan and a fixed rail on the wall.
            art.Box("Grab rail",toilet.transform,new Vector3(seat.x+.42f,.86f,seat.z),new Vector3(.04f,.04f,.62f),"BrushedSteel");
            art.Box("Grab rail post",toilet.transform,new Vector3(seat.x+.42f,.52f,seat.z+.3f),new Vector3(.04f,.68f,.04f),"BrushedSteel");
            art.Box("Wall grab rail",toilet.transform,new Vector3(seat.x-.45f,1.0f,Back-.12f),new Vector3(.6f,.04f,.04f),"BrushedSteel");
            var wash=art.Group("Toilet washstand",toilet.transform,new Vector3(1.55f,Floor,Back-.35f));
            art.Box("Toilet vanity",wash,new Vector3(0,.4f,0),new Vector3(.7f,.8f,.46f),"Oak");
            art.Box("Toilet sink",wash,new Vector3(0,.82f,0),new Vector3(.62f,.08f,.44f),"Quartz");
            art.Box("Toilet sink water",wash,new Vector3(0,.865f,-.03f),new Vector3(.42f,.01f,.28f),"Blue");
            art.Cylinder("Toilet sink tap",wash,new Vector3(0,.97f,.16f),new Vector3(.04f,.2f,.04f),"Chrome");
            art.Box("Toilet mirror",toilet.transform,new Vector3(1.55f,1.5f,Back-.1f),new Vector3(.6f,.7f,.03f),"Blue");
            // The rest of an accessible WC: a hand dryer, a fold-down baby changer, a bin and a red emergency pull cord.
            art.Box("Hand dryer",toilet.transform,new Vector3(2.2f,1.25f,Back-.14f),new Vector3(.28f,.3f,.16f),"BrushedSteel");
            art.Box("Hand dryer outlet",toilet.transform,new Vector3(2.2f,1.09f,Back-.2f),new Vector3(.12f,.03f,.06f),"Charcoal");
            art.Box("Baby changer",toilet.transform,new Vector3(HallEast+.14f,1.0f,3.2f),new Vector3(.16f,.5f,.8f),"Ivory");
            art.Box("Baby changer strap",toilet.transform,new Vector3(HallEast+.23f,1.0f,3.2f),new Vector3(.02f,.06f,.6f),"Sage");
            art.Cylinder("WC bin",toilet.transform,new Vector3(ServiceSplitX-.3f,.36f,3.05f),new Vector3(.3f,.44f,.3f),"BrushedSteel");
            art.Box("Emergency pull cord",toilet.transform,new Vector3(ServiceSplitX-.12f,.9f,4.9f),new Vector3(.01f,1.3f,.01f),"Crimson");
            art.Box("Emergency pull handle",toilet.transform,new Vector3(ServiceSplitX-.12f,.3f,4.9f),new Vector3(.04f,.06f,.04f),"Crimson");
            art.Box("Accessible WC floor mat",toilet.transform,new Vector3(seat.x,.152f,seat.z-.7f),new Vector3(.9f,.01f,.6f),"TileBlue");
            for(int level=1;level<=3;level++)
            {
                var detail=art.Group("Toilet tier "+level,toilet.transform);tierDetails[1,level-1]=detail.gameObject;
                if(level==1)art.Box("Toilet towel",detail,new Vector3(2.05f,1.05f,Back-.1f),new Vector3(.30f,.36f,.035f),"Linen");
                if(level==2)art.Box("Toilet soap dispenser",detail,new Vector3(1.2f,1.18f,Back-.1f),new Vector3(.14f,.25f,.1f),"Apricot");
                if(level==3)art.Model("Plant",detail,new Vector3(3.1f,Floor,3.0f));
            }
            var machine=art.Group("Waiting vending amenity",root,VendingMachine);
            vendingPlot=art.Group("Vending build marker",machine).gameObject;
            art.Box("Future vending crate",vendingPlot.transform,new Vector3(0,.38f,0),new Vector3(.90f,.74f,.70f),"Wood");
            art.Box("Future vending drink icon",vendingPlot.transform,new Vector3(0,.55f,-.36f),new Vector3(.19f,.30f,.025f),"Blue");
            vending=art.Group("Vending machine",machine).gameObject;
            art.Box("Vending cabinet",vending.transform,new Vector3(0,.83f,0),new Vector3(.93f,1.65f,.70f),"Apricot");
            art.Box("Vending window",vending.transform,new Vector3(-.13f,1.05f,-.365f),new Vector3(.56f,.84f,.025f),"SageDark");
            art.Box("Vending delivery slot",vending.transform,new Vector3(-.1f,.30f,-.375f),new Vector3(.63f,.24f,.05f),"Ink");
            vendingButton=art.Group("Vending purchase button",vending.transform,new Vector3(.32f,.94f,-.385f));
            art.Box("Vending button",vendingButton,Vector3.zero,new Vector3(.12f,.12f,.035f),"Gold");
            art.Box("Vending crown",vending.transform,new Vector3(0,1.72f,0),new Vector3(.99f,.13f,.76f),"Wood");
            for(int level=1;level<=3;level++)
            {
                var detail=art.Group("Vending tier "+level,vending.transform);tierDetails[2,level-1]=detail.gameObject;
                for(int bottle=0;bottle<3;bottle++)
                {
                    float x=-.30f+bottle*.18f,y=.72f+(level-1)*.27f;
                    art.Cylinder("Vending refreshment",detail,new Vector3(x,y,-.386f),new Vector3(.105f,.17f,.09f),level==1?"Blue":level==2?"Gold":"Rose");
                    art.Box("Refreshment label",detail,new Vector3(x,y,-.439f),new Vector3(.08f,.05f,.015f),"Linen");
                }
            }
            // The tip cup stands on the lounge's back partition beside the machine.
            var tipRoot=art.Group("Vending tip cup",root,VendingCashPoint-new Vector3(0,.05f,0));tipCup=tipRoot.gameObject;
            art.Cylinder("Tip cup",tipRoot,Vector3.zero,new Vector3(.23f,.29f,.23f),"Sage");
            cash=art.Group("Collectable vending tips",tipRoot).gameObject;
            for(int i=0;i<3;i++)art.Cylinder("Vending tip coin",cash.transform,new Vector3((i-1)*.045f,.105f+i*.025f,0),new Vector3(.12f,.03f,.12f),"Gold");
            vendingTip=art.Cylinder("Patient leaves a tip",root,VendingCashPoint,new Vector3(.14f,.035f,.14f),"Gold").transform;
            toilet.SetActive(false);vending.SetActive(false);cash.SetActive(false);vendingTip.gameObject.SetActive(false);
        }
        internal void Render(ClinicState state,bool reducedMotion)
        {
            int parking=0,toiletLevel=0,vendingLevel=0;VendingTill=0;
            for(int i=0;i<state.Amenities.Count;i++)
            {
                var amenity=state.Amenities[i];int level=Mathf.Clamp(amenity.Level,0,3);
                if(amenity.Kind==ClinicAmenity.Parking)parking=level;
                else if(amenity.Kind==ClinicAmenity.Toilet)toiletLevel=level;
                else if(amenity.Kind==ClinicAmenity.Vending){ vendingLevel=level;VendingTill=amenity.Till; }
            }
            for(int kind=1;kind<3;kind++)for(int tier=1;tier<=3;tier++)ClinicUpgradeEffects.Show(tierDetails[kind,tier-1],tier<=(kind==1?toiletLevel:vendingLevel));
            parkingWorld.Render(state,parking,reducedMotion);
            toiletClosure.SetActive(toiletLevel==0);
            ClinicUpgradeEffects.Show(toilet,toiletLevel>0);toiletPlot.SetActive(toiletLevel==0);ClinicUpgradeEffects.Show(vending,vendingLevel>0);vendingPlot.SetActive(vendingLevel==0);
            tipCup.SetActive(vendingLevel>0);cash.SetActive(vendingLevel>0&&VendingTill>0);
            ClinicPatientState vendingUser=null;
            for(int i=0;i<state.Patients.Count;i++)
            {
                var patient=state.Patients[i];
                if(patient.VisitingAmenity==ClinicAmenity.Vending&&patient.Phase==ClinicPatientPhase.UsingAmenity)vendingUser=patient;
            }
            bool tipping=vendingUser!=null&&!reducedMotion;vendingTip.gameObject.SetActive(tipping);
            if(tipping)
            {
                float t=Mathf.Clamp01((float)((state.Tick+state.SubTick-vendingUser.PhaseStartedTick)/Mathf.Max(1,vendingUser.PhaseEndsTick-vendingUser.PhaseStartedTick)));
                vendingTip.position=Vector3.Lerp(new Vector3(VendingPatient.x,1.06f,VendingPatient.z),VendingCashPoint,t)+Vector3.up*Mathf.Sin(t*Mathf.PI)*.25f;
            }
            vendingButton.localScale=Vector3.one*(vendingUser==null||reducedMotion?1:.91f+.09f*Mathf.Cos((float)(state.Tick+state.SubTick)*.25f));
        }
    }
}
