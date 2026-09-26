using IdleClinic.Core;
using UnityEngine;

namespace IdleClinic.Presentation
{
    /// <summary>Small fixed ambient pools. The crossing owns the road for its whole pedestrian interval.</summary>
    internal sealed class ClinicStreetLife
    {
        private readonly Vector3 offset;private readonly float northPavementOffset,roadOffset;
        private readonly Transform[] cars=new Transform[4],pedestrians=new Transform[6],leftLegs=new Transform[6],rightLegs=new Transform[6];
        private readonly Transform[][] trafficWheels=new Transform[4][];
        private static readonly string[] CarColors={"Apricot","Sage","Blue","Mustard","Rose","Denim"}; // pedestrian clothing
        internal ClinicStreetLife(ClinicArt art,Transform parent,Vector3 offset=default,float northPavementOffset=0,float roadOffset=0)
        {
            this.offset=offset;this.northPavementOffset=northPavementOffset;this.roadOffset=roadOffset;var root=art.Group("Neighbourhood street life",parent);
            for(int i=0;i<4;i++)
            {
                cars[i]=Car(art,root,"Traffic car "+i,Vector3.zero,i==3?TaxiAppearance:i).transform;
                var wheelParts=new System.Collections.Generic.List<Transform>();
                foreach(var part in cars[i].GetComponentsInChildren<Transform>())if(part.name=="Car tyre"||part.name=="Car hubcap")wheelParts.Add(part);
                trafficWheels[i]=wheelParts.ToArray();
            }
            for(int i=0;i<6;i++)
            {
                var person=art.Group("Street pedestrian "+i,root);pedestrians[i]=person;
                person.localScale=Vector3.one*(i==4?.90f:i==5?.96f:1f);
                // Same adult proportions as the clinic's characters: a smaller head, neck, tapered coat and slim limbs.
                string skin=i%3==0?"SkinDeep":i%3==1?"SkinLight":"SkinBrown",coat=CarColors[i];
                art.Orb("Pedestrian head",person,new Vector3(0,1.42f,.01f),new Vector3(.21f,.25f,.22f),skin);
                art.Orb("Pedestrian hair",person,new Vector3(0,1.49f,-.015f),new Vector3(.22f,.15f,.23f),i==5?"HairSilver":i==2?"HairGold":"HairChestnut");
                art.Cylinder("Pedestrian neck",person,new Vector3(0,1.27f,0),new Vector3(.09f,.10f,.09f),skin);
                art.Box("Pedestrian coat",person,new Vector3(0,1.02f,0),new Vector3(.36f,.46f,.22f),coat);
                art.Box("Pedestrian coat hem",person,new Vector3(0,.76f,0),new Vector3(.33f,.12f,.21f),coat);
                art.Orb("Pedestrian shoulders",person,new Vector3(0,1.21f,0),new Vector3(.40f,.10f,.23f),coat);
                art.Box("Pedestrian arm",person,new Vector3(-.21f,.98f,0),new Vector3(.08f,.50f,.09f),coat);
                art.Box("Pedestrian arm",person,new Vector3(.21f,.98f,0),new Vector3(.08f,.50f,.09f),coat);
                for(int side=-1;side<=1;side+=2)art.Orb("Pedestrian hand",person,new Vector3(side*.21f,.70f,0),new Vector3(.06f,.08f,.06f),skin);
                leftLegs[i]=art.Group("Pedestrian left leg",person,new Vector3(-.085f,.72f,0));
                rightLegs[i]=art.Group("Pedestrian right leg",person,new Vector3(.085f,.72f,0));
                foreach(var leg in new[]{leftLegs[i],rightLegs[i]})
                {
                    art.Box("Pedestrian trousers",leg,new Vector3(0,-.33f,0),new Vector3(.11f,.66f,.12f),i%2==0?"Denim":"Ink");
                    art.Box("Pedestrian shoe",leg,new Vector3(0,-.68f,.04f),new Vector3(.10f,.07f,.22f),i==1?"Linen":"Tyre");
                }
                if(i%2==0)art.Box("Pedestrian shoulder bag",person,new Vector3(.24f,.84f,.06f),new Vector3(.07f,.22f,.20f),"Timber");
                if(i==3)art.Orb("Pedestrian hair bun",person,new Vector3(0,1.50f,-.12f),new Vector3(.12f,.12f,.12f),"HairChestnut");
            }
        }
        internal void Render(ClinicState state,bool reducedMotion)
        {
            double seconds=reducedMotion?0:(ClinicRules.TrafficTick(state)+state.SubTick)*.1;
            float cycle=(float)(seconds%40);
            // Ambient cars use the far lane; the near lane belongs to patient vehicles.
            // Far-lane traffic yields throughout the zebra crossing interval.
            float west=cycle<10?Mathf.Lerp(55,2.90f,cycle/10):cycle<20?2.90f:Mathf.Lerp(2.90f,-55,(cycle-20)/20);
            for(int i=0;i<4;i++)
            {
                float x=west+i*4.5f;
                cars[i].position=new Vector3(x,-.11f,-9.75f+roadOffset)+offset;cars[i].rotation=Quaternion.Euler(0,-90,0);
                var roll=Quaternion.Euler((55-x)/.175f*Mathf.Rad2Deg,0,0)*Quaternion.Euler(0,0,90);
                for(int wheel=0;wheel<trafficWheels[i].Length;wheel++)trafficWheels[i][wheel].localRotation=roll;
            }
            for(int i=0;i<6;i++)
            {
                float step=(float)(seconds*.78+i*1.2);Vector3 point;Vector3 direction;
                if(i==0)
                {
                    float journey=(float)(seconds%80);float half=journey%40;bool returning=journey>=40;
                    float t=Mathf.Clamp01((half-11)/7);
                    float north=-6.97f+northPavementOffset,south=-11.08f+roadOffset;
                    point=new Vector3(1.38f,.14f,Mathf.Lerp(returning?north:south,returning?south:north,t));
                    direction=returning?Vector3.back:Vector3.forward;
                    if(half>=18&&half<32)
                    {
                        float stroll=half-18;bool outwards=stroll<7;
                        point.x+=(returning?-1:1)*(outwards?stroll:14-stroll)*.45f;
                        direction=(outwards!=returning)?Vector3.right:Vector3.left;
                    }
                }
                else
                {
                    // The reception-side pavement stays available for arriving parking passengers.
                    float lower=i%2==0?-12f:2.6f,span=11-lower;
                    float distance=(float)((seconds*.47+i*4.1)%(span*2));bool forward=distance<span;
                    float x=lower+(forward?distance:span*2-distance);
                    point=new Vector3(x,.14f,i%2==0?-11.06f+roadOffset:-6.95f+northPavementOffset);direction=forward?Vector3.right:Vector3.left;
                }
                pedestrians[i].position=point+offset;pedestrians[i].rotation=Quaternion.LookRotation(direction);
                float swing=reducedMotion?0:Mathf.Sin(step*5)*24;
                if(i==0&&(cycle<11||cycle>=32))swing=0;
                leftLegs[i].localRotation=Quaternion.Euler(swing,0,0);rightLegs[i].localRotation=Quaternion.Euler(-swing,0,0);
            }
        }
        /// <summary>Top of the roof panel; roof signs and racks sit on it.</summary>
        internal const float CarRoofHeight=.88f;
        /// <summary>Pass as the appearance for a licensed taxi; only taxis are painted taxi yellow.</summary>
        internal const int TaxiAppearance=-1;
        private static readonly string[] Paints={"CarWhite","CarSilver","CarNavy","CarGraphite","CarRed","CarSage","CarWhite"};

        /// <summary>A small car in real proportions: sedan, hatchback or SUV. Wheels keep a 0.175 m radius,
        /// which the parking animation uses to roll them, and the "Car tyre"/"Car hubcap" names it steers.</summary>
        internal static GameObject Car(ClinicArt art,Transform parent,string name,Vector3 position,int appearance)
        {
            var root=art.Group(name,parent,position);bool taxi=appearance==TaxiAppearance;
            string paint=taxi?"CarTaxi":Paints[appearance%Paints.Length];
            int body=taxi?0:appearance%3;bool suv=body==2,hatch=body==1;
            float lift=suv?.07f:0,length=hatch?1.92f:2.08f,cabinBack=hatch?-.66f:-.46f,cabinFront=suv?.44f:.36f;
            // Lower body with a gentle waist, sills and a raised bonnet.
            art.Box("Car rounded body",root,new Vector3(0,.37f+lift,0),new Vector3(1.0f,.30f,length),paint);
            art.Box("Car bonnet",root,new Vector3(0,.535f+lift,length*.5f-.33f),new Vector3(.96f,.05f,.62f),paint).transform.localRotation=Quaternion.Euler(4,0,0);
            art.Box("Car boot",root,new Vector3(0,.535f+lift,-length*.5f+.22f),new Vector3(.96f,.05f,.40f),paint);
            art.Box("Car sill",root,new Vector3(0,.215f+lift,0),new Vector3(1.02f,.05f,length-.62f),"Tyre");
            // Glasshouse: pillars in body colour, tinted glass all round, and a roof panel.
            float cabinCenter=(cabinFront+cabinBack)*.5f,cabinLength=cabinFront-cabinBack;
            // Tinted glass cabin under a shorter roof panel: from above, the uncovered glass reads as the
            // windscreen and rear window without panels standing proud of the roofline.
            art.Box("Car cabin",root,new Vector3(0,.70f+lift,cabinCenter),new Vector3(.86f,.30f,cabinLength),"Glass");
            art.Box("Car roof",root,new Vector3(0,CarRoofHeight-.02f+lift,cabinCenter-.03f),new Vector3(.86f,.05f,cabinLength-(hatch?.16f:.26f)),paint);
            for(int side=-1;side<=1;side+=2)
            {
                art.Box("Car side window",root,new Vector3(side*.432f,.70f+lift,cabinCenter),new Vector3(.02f,.24f,cabinLength-.08f),"Glass");
                art.Box("Car pillar",root,new Vector3(side*.436f,.70f+lift,cabinCenter-.02f),new Vector3(.03f,.30f,.07f),paint);
                art.Box("Car door line",root,new Vector3(side*.502f,.40f+lift,cabinCenter-.02f),new Vector3(.006f,.24f,.012f),"Tyre");
                art.Box("Car door handle",root,new Vector3(side*.504f,.49f+lift,cabinCenter+.22f),new Vector3(.01f,.02f,.09f),"Chrome");
                art.Box("Car mirror",root,new Vector3(side*.54f,.62f+lift,cabinFront-.02f),new Vector3(.09f,.07f,.05f),paint);
                for(int wheel=-1;wheel<=1;wheel+=2)
                {
                    float z=wheel*(length*.5f-.40f);
                    art.Box("Car wheel arch",root,new Vector3(side*.496f,.31f+lift*.5f,z),new Vector3(.02f,.25f,.46f),"Tyre");
                    var tire=art.Cylinder("Car tyre",root,new Vector3(side*.46f,.175f,z),new Vector3(.35f,.13f,.35f),"Tyre");tire.transform.localRotation=Quaternion.Euler(0,0,90);
                    var hub=art.Cylinder("Car hubcap",root,new Vector3(side*.528f,.175f,z),new Vector3(.21f,.012f,.21f),"Chrome");hub.transform.localRotation=Quaternion.Euler(0,0,90);
                    art.Box("Car wheel spoke",hub.transform,new Vector3(0,-side*1.2f,0),new Vector3(.82f,.6f,.14f),"CarGraphite");
                    art.Box("Car wheel spoke",hub.transform,new Vector3(0,-side*1.2f,0),new Vector3(.14f,.6f,.82f),"CarGraphite");
                }
                art.Box("Car headlamp",root,new Vector3(side*.33f,.46f+lift,length*.5f+.005f),new Vector3(.24f,.08f,.02f),"LampLight");
                art.Box("Car indicator",root,new Vector3(side*.45f,.46f+lift,length*.5f),new Vector3(.06f,.06f,.02f),"Mustard");
                art.Box("Car tail lamp",root,new Vector3(side*.36f,.47f+lift,-length*.5f-.005f),new Vector3(.20f,.09f,.02f),"TailLight");
            }
            art.Box("Car grille",root,new Vector3(0,.37f+lift,length*.5f+.005f),new Vector3(.40f,.10f,.02f),"Tyre");
            art.Box("Car front bumper",root,new Vector3(0,.25f+lift,length*.5f+.01f),new Vector3(.98f,.11f,.08f),paint);
            art.Box("Car rear bumper",root,new Vector3(0,.25f+lift,-length*.5f-.01f),new Vector3(.98f,.11f,.08f),paint);
            art.Box("Car number plate",root,new Vector3(0,.27f+lift,length*.5f+.055f),new Vector3(.30f,.07f,.01f),"Paint");
            art.Box("Car number plate",root,new Vector3(0,.35f+lift,-length*.5f-.055f),new Vector3(.30f,.07f,.01f),"PlateYellow");
            if(suv)for(int side=-1;side<=1;side+=2)art.Box("Car roof rail",root,new Vector3(side*.34f,CarRoofHeight+.03f+lift,cabinCenter-.03f),new Vector3(.03f,.03f,cabinLength-.3f),"Chrome");
            return root.gameObject;
        }
    }
}
