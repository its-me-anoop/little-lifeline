using UnityEngine;

namespace IdleClinic.Presentation
{
    /// <summary>Static neighbourhood scenery outside the working clinic. All parts share the clinic's cached meshes.</summary>
    internal static class ClinicSurroundings
    {
        internal static void Build(ClinicArt art,Transform parent)
        {
            var town=art.Group("Clinic neighbourhood",parent);
            art.Box("Neighbourhood street",town,new Vector3(0,-.11f,-9.0f),new Vector3(130,.15f,3.2f),"AsphaltDark");
            art.Box("Neighbourhood side street",town,new Vector3(8.85f,-.11f,1.8f),new Vector3(3.2f,.15f,24.8f),"AsphaltDark");
            art.Box("Front pavement",town,new Vector3(0,-.035f,-6.95f),new Vector3(130,.16f,.90f),"Concrete");
            art.Box("Far pavement",town,new Vector3(0,-.035f,-11.05f),new Vector3(130,.16f,.90f),"Concrete");
            art.Box("Side pavement",town,new Vector3(6.73f,-.035f,1.3f),new Vector3(.95f,.16f,16.5f),"Concrete");
            art.Box("Front curb",town,new Vector3(0,.005f,-7.40f),new Vector3(130,.12f,.08f),"Render");
            art.Box("Side curb",town,new Vector3(7.19f,.005f,1.3f),new Vector3(.08f,.12f,16.5f),"Render");
            for(int i=0;i<7;i++)art.Box("Street centre stripe",town,new Vector3(-12+i*4,-.028f,-9.0f),new Vector3(1.5f,.005f,.08f),"Paint");
            for(int i=0;i<5;i++)art.Box("Clinic crossing",town,new Vector3(.72f,-.025f,-7.80f-i*.55f),new Vector3(1.85f,.01f,.25f),"Paint");
            for(int i=0;i<2;i++)art.Box("Crossing give-way line",town,new Vector3(.72f,-.026f,i==0?-7.52f:-10.55f),new Vector3(1.85f,.008f,.05f),"Paint");
            for(int i=0;i<14;i++)art.Box("Pavement joint",town,new Vector3(-11.5f+i*1.8f,.05f,-6.94f),new Vector3(.018f,.004f,.85f),"Slate");
            for(int i=0;i<14;i++)art.Box("Pavement joint",town,new Vector3(-11.5f+i*1.8f,.05f,-11.04f),new Vector3(.018f,.004f,.85f),"Slate");

            House(art,town,0,new Vector3(-10.3f,.05f,8.3f),new Vector3(2.6f,2.5f,3.2f),"Brick");
            House(art,town,1,new Vector3(-4.5f,.05f,8.5f),new Vector3(3.2f,2.9f,3.0f),"Sage");
            House(art,town,2,new Vector3(.3f,.05f,8.9f),new Vector3(3.4f,3.4f,3.5f),"Render");
            House(art,town,3,new Vector3(5.4f,.05f,10.3f),new Vector3(2.8f,2.6f,3.0f),"Blue");
            House(art,town,4,new Vector3(-5.3f,.05f,-13.3f),new Vector3(3.3f,2.1f,2.6f),"Render");
            House(art,town,5,new Vector3(11.8f,.05f,1.4f),new Vector3(2.1f,2.5f,3.2f),"Brick");
            House(art,town,6,new Vector3(-11.3f,.05f,-13.8f),new Vector3(3.0f,2.8f,3.3f),"Sage");
            House(art,town,7,new Vector3(.2f,.05f,-14.2f),new Vector3(3.5f,3.1f,3.0f),"Brick");
            House(art,town,8,new Vector3(10.8f,.05f,-13.8f),new Vector3(3.2f,2.6f,3.4f),"Blue");
            House(art,town,9,new Vector3(12.2f,.05f,8.0f),new Vector3(3.0f,3.3f,3.4f),"Sage");
            StreetFurniture(art,town);
            Tree(art,town,0,new Vector3(-16.4f,.05f,-4.2f),1.0f);
            Tree(art,town,1,new Vector3(-6.90f,.05f,5.9f),.95f);
            Tree(art,town,2,new Vector3(-6.95f,.05f,12.8f),.86f);
            Tree(art,town,3,new Vector3(6.8f,.05f,-3.8f),.82f);
            Tree(art,town,4,new Vector3(-2.9f,.05f,-11.45f),.90f);
            Tree(art,town,5,new Vector3(3.9f,.05f,-11.5f),.92f);
            var bench=art.Group("Street bench",town,new Vector3(-16.5f,.05f,3.6f));
            for(int side=-1;side<=1;side+=2)art.Box("Bench leg",bench,new Vector3(side*.50f,.22f,0),new Vector3(.08f,.44f,.48f),"SageDark");
            for(int slat=0;slat<3;slat++)art.Box("Bench oak slat",bench,new Vector3(0,.46f,-.18f+slat*.18f),new Vector3(1.35f,.055f,.13f),"Wood");
        }

        private static void House(ClinicArt art,Transform town,int index,Vector3 position,Vector3 size,string facade)
        {
            var house=art.Group("Town house "+index,town,position);
            bool upstairs=size.y>=2.45f;
            art.Box("House walls",house,new Vector3(0,size.y*.5f,0),size,facade);
            art.Box("House plinth",house,new Vector3(0,.15f,0),new Vector3(size.x+.06f,.30f,size.z+.06f),facade=="Brick"?"Slate":"Brick");
            art.Box("Roof cornice",house,new Vector3(0,size.y-.04f,0),new Vector3(size.x+.10f,.10f,size.z+.10f),"Paint");
            PitchedRoof(art,house,new Vector3(0,size.y,0),new Vector2(size.x,size.z),facade,index%2==0?"RoofTile":"Slate");
            art.Box("Chimney",house,new Vector3(size.x*.28f,size.y+size.z*.36f,size.z*.12f),new Vector3(.32f,.95f,.36f),"Brick");
            art.Box("Chimney cap",house,new Vector3(size.x*.28f,size.y+size.z*.36f+.49f,size.z*.12f),new Vector3(.40f,.06f,.44f),"Render");
            float front=-size.z*.5f;
            art.Box("House door",house,new Vector3(0,.72f,front-.02f),new Vector3(.54f,1.12f,.05f),index%3==0?"CarNavy":index%3==1?"CarSage":"Timber");
            art.Box("Door frame",house,new Vector3(0,.76f,front-.012f),new Vector3(.68f,1.26f,.03f),"Paint");
            art.Box("Door lintel",house,new Vector3(0,1.40f,front-.04f),new Vector3(.76f,.10f,.10f),"Render");
            art.Box("Door step",house,new Vector3(0,.09f,front-.20f),new Vector3(.80f,.14f,.34f),"Concrete");
            art.Box("Door knocker",house,new Vector3(0,.95f,front-.05f),new Vector3(.06f,.06f,.02f),"Gold");
            foreach(var height in upstairs?new[]{size.y*.36f,size.y*.74f}:new[]{size.y*.48f})
                for(int side=-1;side<=1;side+=2)Window(art,house,new Vector3(side*size.x*.29f,height,front-.03f),new Vector2(.58f,.70f));
            if(upstairs)Window(art,house,new Vector3(0,size.y*.74f,front-.03f),new Vector2(.40f,.56f));
            if(index%2==1)return;
            var shopfront=art.Group("Neighbourhood shopfront",house,new Vector3(0,.46f,front-.08f));
            art.Box("Shop canopy",shopfront,new Vector3(0,1.24f,-.18f),new Vector3(size.x*.85f,.10f,.60f),index%4==0?"SageDark":"Rose");
            for(int stripe=0;stripe<5;stripe++)art.Box("Canopy stripe",shopfront,new Vector3(-size.x*.35f+stripe*size.x*.175f,1.245f,-.18f),new Vector3(size.x*.085f,.11f,.61f),"Linen");
            for(int side=-1;side<=1;side+=2)
            {
                art.Box("Shop planter",shopfront,new Vector3(side*size.x*.37f,-.27f,-.17f),new Vector3(.36f,.37f,.36f),"Timber");
                art.Orb("Shop planter leaves",shopfront,new Vector3(side*size.x*.37f,.06f,-.17f),new Vector3(.47f,.50f,.43f),"Leaf");
            }
        }

        private static void Window(ClinicArt art,Transform parent,Vector3 center,Vector2 size)
        {
            art.Box("House window frame",parent,center,new Vector3(size.x+.10f,size.y+.10f,.05f),"Paint");
            art.Box("House window glass",parent,center+new Vector3(0,0,-.03f),new Vector3(size.x,size.y,.02f),"Glass");
            art.Box("Window glazing bar",parent,center+new Vector3(0,0,-.045f),new Vector3(.03f,size.y,.015f),"Paint");
            art.Box("Window glazing bar",parent,center+new Vector3(0,size.y*.1f,-.045f),new Vector3(size.x,.03f,.015f),"Paint");
            art.Box("House window sill",parent,center+new Vector3(0,-size.y*.5f-.07f,-.05f),new Vector3(size.x+.20f,.06f,.14f),"Render");
        }

        /// <summary>A 45° gabled roof over a rectangular footprint, ridge along x. The gable ends are filled in
        /// the facade material so the roof reads as a solid building from the high camera.</summary>
        internal static void PitchedRoof(ClinicArt art,Transform parent,Vector3 eaves,Vector2 footprint,string gable,string covering,float overhang=.22f)
        {
            float half=footprint.y*.5f,rise=half;
            art.Box("Roof gable",parent,eaves,new Vector3(footprint.x,half*1.4142f,half*1.4142f),gable).transform.localRotation=Quaternion.Euler(45,0,0);
            float slope=(half+overhang)*1.4142f;
            for(int side=-1;side<=1;side+=2)
            {
                var plane=art.Box("Roof slope",parent,eaves+new Vector3(0,rise-(half+overhang)*.5f+.03f,side*(half+overhang)*.5f),new Vector3(footprint.x+overhang*2,.08f,slope),covering);
                plane.transform.localRotation=Quaternion.Euler(side*45,0,0);
                for(int course=1;course<4;course++)
                {
                    float t=course/4f;
                    var line=art.Box("Roof tile course",parent,eaves+new Vector3(0,rise-(half+overhang)*t+.075f,side*(half+overhang)*t),new Vector3(footprint.x+overhang*2,.02f,.035f),covering=="Slate"?"CarGraphite":"Brick");
                    line.transform.localRotation=Quaternion.Euler(side*45,0,0);
                }
            }
            art.Box("Roof ridge",parent,eaves+new Vector3(0,rise+.06f,0),new Vector3(footprint.x+overhang*2,.09f,.16f),covering=="Slate"?"CarGraphite":"Brick");
        }

        private static void StreetFurniture(ClinicArt art,Transform town)
        {
            for(int i=0;i<5;i++)
            {
                float x=-12+i*5.4f;
                var lamp=art.Group("Neighbourhood lantern "+i,town,new Vector3(x,.05f,-11.65f));
                art.Cylinder("Lantern post",lamp,new Vector3(0,1.25f,0),new Vector3(.075f,2.5f,.075f),"SageDark");
                art.Box("Lantern top",lamp,new Vector3(0,2.62f,0),new Vector3(.36f,.45f,.36f),"Gold");
                art.Box("Lantern glass",lamp,new Vector3(0,2.62f,-.19f),new Vector3(.24f,.31f,.025f),"Linen");
            }
            var bus=art.Group("Bus shelter",town,new Vector3(5.1f,.05f,-11.92f));
            for(int side=-1;side<=1;side+=2)art.Box("Bus shelter upright",bus,new Vector3(side*.88f,1.1f,0),new Vector3(.075f,2.2f,.075f),"Gold");
            art.Box("Bus shelter back",bus,new Vector3(0,1.2f,-.24f),new Vector3(1.88f,1.25f,.035f),"Blue");
            art.Box("Bus shelter roof",bus,new Vector3(0,2.28f,0),new Vector3(2.15f,.16f,1.25f),"SageDark");
            art.Box("Bus shelter seat",bus,new Vector3(0,.55f,0),new Vector3(1.70f,.12f,.38f),"Wood");
            var stop=art.Group("Bus stop sign",town,new Vector3(6.4f,.05f,-11.65f));
            art.Cylinder("Bus stop pole",stop,new Vector3(0,1.25f,0),new Vector3(.07f,2.5f,.07f),"Gold");
            art.Box("Bus stop tile",stop,new Vector3(0,2.22f,0),new Vector3(.50f,.47f,.08f),"Blue");
            art.Box("Bus pictogram",stop,new Vector3(0,2.22f,-.055f),new Vector3(.30f,.19f,.02f),"Linen");
            var rack=art.Group("Street bicycle rack",town,new Vector3(6.77f,.05f,5.7f));
            for(int i=0;i<3;i++)
            {
                float z=i*.48f;art.Box("Bicycle rack top",rack,new Vector3(0,.67f,z),new Vector3(.50f,.055f,.055f),"Gold");
                for(int side=-1;side<=1;side+=2)art.Cylinder("Bicycle rack leg",rack,new Vector3(side*.25f,.34f,z),new Vector3(.055f,.67f,.055f),"Gold");
            }
            var bicycle=art.Group("Neighbourhood bicycle",town,new Vector3(6.72f,.05f,6.13f));
            bicycle.localRotation=Quaternion.Euler(0,90,0);
            for(int side=-1;side<=1;side+=2)
            {
                var wheel=art.Cylinder("Bicycle wheel",bicycle,new Vector3(side*.49f,.39f,0),new Vector3(.58f,.035f,.58f),"Ink");wheel.transform.localRotation=Quaternion.Euler(90,0,0);
                var inset=art.Cylinder("Bicycle wheel inset",bicycle,new Vector3(side*.49f,.39f,-.025f),new Vector3(.46f,.008f,.46f),"Meadow");inset.transform.localRotation=Quaternion.Euler(90,0,0);
            }
            art.Box("Bicycle frame",bicycle,new Vector3(0,.54f,0),new Vector3(.83f,.045f,.045f),"Apricot").transform.localRotation=Quaternion.Euler(0,0,22);
            art.Box("Bicycle saddle",bicycle,new Vector3(-.12f,.85f,0),new Vector3(.28f,.065f,.18f),"Wood");
            art.Box("Bicycle handlebars",bicycle,new Vector3(.42f,.98f,0),new Vector3(.08f,.055f,.32f),"SageDark");
            var board=art.Group("Community board posts",town,new Vector3(-7.02f,.05f,5.85f));
            for(int side=-1;side<=1;side+=2)art.Box("Community notice post",board,new Vector3(side*.45f,.7f,0),new Vector3(.075f,1.4f,.075f),"Wood");
            ClinicFurnishings.NoticeBoard(art,board,"Community notice board",new Vector3(0,1.38f,0),0,1.16f);
            for(int bed=0;bed<6;bed++)
            {
                var flower=art.Group("Flower border",town,new Vector3(bed<3?-16.0f:11.25f,.05f,-5.6f+(bed%3)*4.0f));
                art.Box("Flower bed edging",flower,new Vector3(0,.15f,0),new Vector3(.68f,.25f,2.5f),"Clay");
                art.Box("Flower bed soil",flower,new Vector3(0,.29f,0),new Vector3(.55f,.03f,2.32f),"Wood");
                for(int bloom=0;bloom<4;bloom++)
                {
                    art.Orb("Border leaves",flower,new Vector3(0,.48f,-.87f+bloom*.58f),new Vector3(.47f,.43f,.48f),"Leaf");
                    art.Orb("Border flower",flower,new Vector3(0,.73f,-.87f+bloom*.58f),new Vector3(.18f,.17f,.18f),bloom%2==0?"Rose":"Gold");
                }
            }
            for(int side=-1;side<=1;side+=2)
            {
                var park=art.Group("Outer neighbourhood pocket park",town,new Vector3(side*21.7f,0,0));
                art.Box("Pocket park lawn",park,new Vector3(0,-.18f,-.6f),new Vector3(6.3f,.08f,39.7f),"TileSage");
                art.Box("Pocket park walking path",park,new Vector3(side*.6f,-.14f,-.6f),new Vector3(.85f,.05f,39.7f),"TilePeach");
                for(int row=0;row<4;row++)
                {
                    float z=row==0?-17:row==1?-3:row==2?6:15;
                    Tree(art,park,20+row+(side+1)*2,new Vector3(-side*1.7f,.04f,z),1.2f);
                    var bench=art.Group("Pocket park bench",park,new Vector3(side*1.8f,.05f,z));
                    art.Box("Pocket park bench seat",bench,new Vector3(0,.46f,0),new Vector3(1.35f,.12f,.42f),"Wood");
                    art.Box("Pocket park bench back",bench,new Vector3(0,.75f,.22f),new Vector3(1.35f,.48f,.075f),"Sage");
                    for(int foot=-1;foot<=1;foot+=2)art.Box("Pocket park bench leg",bench,new Vector3(foot*.45f,.22f,0),new Vector3(.075f,.44f,.38f),"Gold");
                }
            }
            for(int parcel=0;parcel<8;parcel++)
            {
                float x=parcel%4*9f-13.5f,z=parcel<4?16.4f:-19.2f;
                art.Box("Neighbourhood lawn parcel",town,new Vector3(x,-.17f,z),new Vector3(7.8f,.07f,5.1f),parcel%2==0?"TileSage":"Sage");
                art.Box("Garden path",town,new Vector3(x,-.11f,z),new Vector3(.65f,.07f,5.3f),"TilePeach");
                Tree(art,town,10+parcel,new Vector3(x-2.2f,-.08f,z),1.15f);
            }
        }

        private static void Tree(ClinicArt art,Transform town,int index,Vector3 position,float scale)
        {
            var tree=art.Group("Street tree "+index,town,position);tree.localScale=Vector3.one*scale;
            art.Cylinder("Tree planting bed",tree,new Vector3(0,.025f,0),new Vector3(1.30f,.05f,1.30f),"Timber");
            art.Cylinder("Tree grille",tree,new Vector3(0,.055f,0),new Vector3(1.0f,.02f,1.0f),"CarGraphite");
            art.Cylinder("Tree trunk",tree,new Vector3(0,.70f,0),new Vector3(.16f,1.40f,.16f),"Wood");
            art.Orb("Tree crown",tree,new Vector3(0,1.70f,0),new Vector3(1.35f,1.70f,1.25f),"Leaf");
            art.Orb("Tree side leaves",tree,new Vector3(-.34f,1.40f,.12f),new Vector3(.88f,1.15f,.92f),"Sage");
            art.Orb("Tree upper leaves",tree,new Vector3(.23f,2.17f,-.04f),new Vector3(.77f,.81f,.77f),"Leaf");
            art.Orb("Tree front leaves",tree,new Vector3(.18f,1.55f,-.38f),new Vector3(.80f,.86f,.70f),"CarSage");
            art.Orb("Tree back leaves",tree,new Vector3(-.10f,1.95f,.40f),new Vector3(.78f,.82f,.72f),"Leaf");
        }
    }
}
