using System;
using System.Collections.Generic;
using IdleClinic.Core;
using UnityEngine;
using UnityEngine.Rendering;
using static IdleClinic.Presentation.ClinicFloorPlan;

namespace IdleClinic.Presentation
{
    public enum ClinicHitKind { None,Cash,Reception,Treatment,Waiting,Expansion,Desk,Station,Parking,Toilet,Vending,VendingCash,Consultation,Pharmacy,DoctorStation,PharmacyStation,Taxi,Office,StaffRoom,Store }
    public readonly struct ClinicHit
    {
        public ClinicHitKind Kind { get; }
        public int Id { get; }
        public ClinicHit(ClinicHitKind kind,int id=0) { Kind=kind;Id=id; }
    }

    /// <summary>Persistent miniature hospital scene. Gesture ownership and financial effects live in the UI/core.</summary>
    public sealed class ClinicWorld : MonoBehaviour
    {
        private static readonly Vector3 CameraOffset=new Vector3(1.2f,15,-12);
        private readonly Plane floor=new Plane(Vector3.up,new Vector3(0,Floor,0));
        private readonly Dictionary<string,Transform> anchors=new Dictionary<string,Transform>();
        private readonly GameObject[] desks=new GameObject[2],stations=new GameObject[2],seats=new GameObject[Seats];
        private readonly GameObject[,] cash=new GameObject[2,6];
        private readonly Transform[] roomRoots=new Transform[8];
        private readonly GameObject[,] tierDetails=new GameObject[3,2];
        // Reception, treatment room 1, lounge, treatment room 2 (both treatment rooms follow the first aid room).
        private readonly ClinicRoomStyle[] styles=new ClinicRoomStyle[4];
        // The office, staff room and store: their styles, fixed furniture and the plots shown before they are built.
        private readonly ClinicRoomStyle[] serviceStyles=new ClinicRoomStyle[3];
        private readonly GameObject[] servicePlots=new GameObject[3];
        private GameObject[] serviceFurniture;
        private bool serviceRoomsOffered;
        private readonly GameObject[] renovations=new GameObject[3];
        private readonly long[] tills=new long[2];
        private static readonly string[] CashAnchors={"reception.desk.0.cash","reception.desk.1.cash"};
        private readonly ClinicRoomState[] roomState=new ClinicRoomState[3];
        private ClinicArt art;
        private DoctorsClinicWorld doctors;
        public ClinicLocation Location { get; private set; }
        public int MovingActorCount=>actors==null?0:actors.MovingCount;
        public int ActiveServiceCount { get; private set; }
        public int DoorOpeningCount
        {
            get
            {
                if(doctors!=null)return doctors.DoorOpeningCount;
                int count=entranceDoor?.OpeningCount??0;foreach(var door in roomDoors)count+=door.OpeningCount;return count;
            }
        }
        private ClinicActors actors;
        private ClinicUpgrades upgrades;
        private ClinicAmenities amenities;
        private ClinicStreetLife streetLife;
        private ClinicConstruction construction;
        private ClinicDoor entranceDoor;
        private readonly List<ClinicSwingDoor> roomDoors=new List<ClinicSwingDoor>();
        private Transform scene,selection;
        private GameObject waitingClosed;
        private Vector3 center=new Vector3(0,0,-.65f),homeTarget,velocity;
        private float size=6.9f,homeSize,sizeVelocity;
        private bool initialized,homing,waitingBuilt,userCamera;

        public Camera SceneCamera { get; private set; }
        public RenderTexture Texture { get; private set; }
        public RenderTexture SceneTexture=>Texture;
        public Vector3 CashPoint=>GetCashPoint(0);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        /// <summary>Development QA only: show every room styled as this room size and decor level (zero keeps the real ones).</summary>
        public static int PreviewTier,PreviewDecor;
        /// <summary>Development previews: every room's equipment at this version when above zero.</summary>
        public static int PreviewGear{get=>ClinicRoomGear.PreviewVersion;set=>ClinicRoomGear.PreviewVersion=value;}
#endif
        internal static int StyleTier(int tier,int maximum,bool doctors)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if(PreviewTier>0)return Mathf.Min(PreviewTier,StyleMaximum(maximum,doctors));
#endif
            return tier;
        }
        /// <summary>A preview always uses the full rules 5 range, whatever rules the save is on.</summary>
        internal static int StyleMaximum(int maximum,bool doctors)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if(PreviewTier>0)return doctors?40:20;
#endif
            return maximum;
        }
        internal static int StyleDecor(int level)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if(PreviewDecor>0)return PreviewDecor;
#endif
            return level;
        }
        public void Initialize(ClinicLocation location) { Initialize();ConfigureLocation(location); }
        public void ResetActorPlacement() => actors?.ResetPlacement();
        public void ConfigureLocation(ClinicLocation location)
        {
            Initialize();if(Location==location)return;
            gearPreview?.Dispose();gearPreview=null;scene.gameObject.SetActive(false);ClinicArt.Destroy(scene.gameObject);anchors.Clear();Location=location;
            scene=art.Group(location==ClinicLocation.DoctorsClinic?"Small doctors clinic":"Fixed hospital",transform);
            doctors=null;effects=new ClinicUpgradeEffects(art,scene);
            if(location==ClinicLocation.DoctorsClinic)doctors=new DoctorsClinicWorld(art,scene,this,Anchor,RegisterSockets);
            else BuildStarter();
            actors=new ClinicActors(art,scene,this);Home(true);
        }

        public void Initialize()
        {
            if(initialized)return;initialized=true;art=new ClinicArt();scene=art.Group("Fixed hospital",transform);
            var camera=art.Group("Clinic camera",transform);SceneCamera=camera.gameObject.AddComponent<Camera>();
            SceneCamera.orthographic=true;SceneCamera.transform.rotation=Quaternion.LookRotation(-CameraOffset,Vector3.up);
            SceneCamera.clearFlags=CameraClearFlags.SolidColor;SceneCamera.backgroundColor=ClinicArt.Color("Meadow");
            SceneCamera.cullingMask=1<<ClinicArt.Layer;SceneCamera.nearClipPlane=.1f;SceneCamera.farClipPlane=100;
            SceneCamera.allowHDR=false;SceneCamera.allowMSAA=true;
            BuildLighting();effects=new ClinicUpgradeEffects(art,scene);BuildStarter();
            actors=new ClinicActors(art,scene,this);
            SetRenderSize(393,852);Home(true);
        }
        private void BuildStarter()
        {
            BuildArchitecture();BuildFurniture();BuildAnchors();ClinicSurroundings.Build(art,scene);serviceFurniture=ClinicFurnishings.Build(art,scene);BuildServicePlots();
            upgrades=new ClinicUpgrades(art,scene);amenities=new ClinicAmenities(art,scene);streetLife=new ClinicStreetLife(art,scene);
            // Scaffolds: the reception's outside its east wall, the lounge's and treatment rooms' outside the west wall, and the
            // back rooms' inside each room (the consultation and pharmacy places are the doctors clinic's and stay unused here).
            var unused=new Vector3(0,-40,0);
            construction=new ClinicConstruction(art,scene,new[]{new Vector3(6.07f,Floor,-2.6f),new Vector3(-6.02f,Floor,2.87f),new Vector3(-6.02f,Floor,-2.6f),unused,unused,
                new Vector3(Office.center.x,Floor,Office.center.y),new Vector3(StaffRoom.center.x,Floor,StaffRoom.center.y),new Vector3(Store.center.x,Floor,Store.center.y)});
            BuildDoors();BuildRoomStyles();
        }
        private void BuildDoors()
        {
            entranceDoor=new ClinicDoor(art,scene,true,new Vector3(0,Floor,Front),openingWidth:EntranceWidth);
            roomDoors.Clear();
            for(int i=0;i<2;i++)
                roomDoors.Add(new ClinicSwingDoor(art,scene,"Treatment room "+(i+1)+" doorway",new Vector3(HallWest,Floor,TreatmentDoorZ(i)),true,TreatmentDoorWidth,LowWall,-1,true));
            roomDoors.Add(new ClinicSwingDoor(art,scene,"Office doorway",new Vector3(HallEast,Floor,OfficeDoorZ),true,RoomDoorWidth,LowWall,1));
            roomDoors.Add(new ClinicSwingDoor(art,scene,"WC doorway",new Vector3(HallEast,Floor,WashroomDoorZ),true,RoomDoorWidth,LowWall,1));
            roomDoors.Add(new ClinicSwingDoor(art,scene,"Staff room doorway",new Vector3(StaffDoorX,Floor,Middle),false,RoomDoorWidth,2.0f,1));
            roomDoors.Add(new ClinicSwingDoor(art,scene,"Store doorway",new Vector3(StoreDoorX,Floor,ServiceSplitZ),false,RoomDoorWidth,LowWall,1));
        }

        public RenderTexture SetRenderSize(int width,int height)
        {
            Initialize();width=Mathf.Max(1,width);height=Mathf.Max(1,height);
            float factor=Mathf.Min(1,1536f/Mathf.Max(width,height));width=Mathf.Max(1,Mathf.RoundToInt(width*factor));height=Mathf.Max(1,Mathf.RoundToInt(height*factor));
            if(Texture!=null && Texture.width==width && Texture.height==height) { if(!Texture.IsCreated())Texture.Create();return Texture; }
            ReleaseTexture();var descriptor=new RenderTextureDescriptor(width,height,RenderTextureFormat.ARGB32,16){msaaSamples=2,sRGB=QualitySettings.activeColorSpace==ColorSpace.Linear};
            descriptor.msaaSamples=Mathf.Max(1,SystemInfo.GetRenderTextureSupportedMSAASampleCount(descriptor));
            Texture=new RenderTexture(descriptor){name="Idle Clinic world",filterMode=FilterMode.Bilinear};Texture.Create();
            SceneCamera.targetTexture=Texture;SceneCamera.aspect=width/(float)height;
            if(!userCamera)Home(true);else {size=BoundedSize(size);ClampCenter();ApplyCamera();}return Texture;
        }

        public void Render(ClinicState state,float deltaTime,bool reducedMotion=false)
        {
            Initialize();if(state==null)return;ConfigureLocation(state.Location);effects.Reduced=reducedMotion;
            ActiveServiceCount=0;foreach(var patient in state.Patients)if(patient.Phase==ClinicPatientPhase.CheckingIn||patient.Phase==ClinicPatientPhase.Consulting||patient.Phase==ClinicPatientPhase.Treating||patient.Phase==ClinicPatientPhase.Dispensing)ActiveServiceCount++;
            if(doctors!=null)
            {
                actors.Render(state,reducedMotion);doctors.Render(state,actors,deltaTime,reducedMotion);
                effects.Armed=true;effects.Update(deltaTime);
                UpdateHome(deltaTime,reducedMotion);return;
            }
            for(int i=0;i<3;i++)roomState[i]=null;
            for(int i=0;i<state.Rooms.Count;i++)if((int)state.Rooms[i].Kind<3)roomState[(int)state.Rooms[i].Kind]=state.Rooms[i];
            for(int i=0;i<2;i++)
            {
                ReceptionDeskState desk=null;for(int j=0;j<state.ReceptionDesks.Count;j++)if(state.ReceptionDesks[j].Id==i)desk=state.ReceptionDesks[j];
                ClinicUpgradeEffects.Show(desks[i],desk!=null);tills[i]=desk==null?0:desk.Till;
                int stacks=tills[i]==0?0:Mathf.Clamp(1+(int)Math.Log10(Math.Max(1,tills[i])),1,6);
                for(int j=0;j<6;j++)cash[i,j].SetActive(j<stacks && desk!=null);
            }
            var treatment=roomState[1];int stationCount=treatment==null?1:treatment.StationCount;
            for(int i=0;i<2;i++)ClinicUpgradeEffects.Show(stations[i],i<stationCount);
            var waiting=roomState[2];waitingBuilt=waiting!=null&&waiting.Built;
            waitingClosed.SetActive(!waitingBuilt);
            int capacity=waitingBuilt?Mathf.Clamp(ClinicRules.WaitingCapacity(state),0,Seats):0;
            for(int i=0;i<Seats;i++)ClinicUpgradeEffects.Show(seats[i],i<capacity);
            int maximumTier=ClinicRules.MaximumTier(state);
            for(int i=0;i<3;i++)
            {
                var room=roomState[i];int tier=room==null?1:room.Tier;
                var style=StyleTier(tier,maximumTier,false);bool built=room!=null&&room.Built;int decor=StyleDecor(room==null?1:room.DecorationLevel);
                styles[i]?.Render(style,StyleMaximum(maximumTier,false),built,decor);
                if(i==1)styles[3]?.Render(style,StyleMaximum(maximumTier,false),built,decor);
                // The older room-size fittings stand in until a room prices its equipment piece by piece.
                bool gear=ClinicGear.Active(state,(ClinicRoom)i);
                for(int j=0;j<2;j++)ClinicUpgradeEffects.Show(tierDetails[i,j],(i!=2||waitingBuilt)&&tier>=j+2&&!gear);
                bool building=false;for(int j=0;j<state.Construction.Count;j++)if((int)state.Construction[j].Room==i)building=true;
                renovations[i].SetActive(building);
            }
            serviceRoomsOffered=false;
            for(int i=0;i<3;i++)
            {
                var kind=(ClinicRoom)((int)ClinicRoom.Office+i);var room=state.Room(kind);bool built=room!=null&&room.Built;
                bool building=false;for(int j=0;j<state.Construction.Count;j++)if(state.Construction[j].Room==kind)building=true;
                serviceRoomsOffered|=room!=null;
                ClinicUpgradeEffects.Show(serviceFurniture[i],built);servicePlots[i].SetActive(room!=null&&!built&&!building);
                serviceStyles[i]?.Render(StyleTier(room==null?1:room.Tier,maximumTier,false),StyleMaximum(maximumTier,false),built,StyleDecor(room==null?1:room.DecorationLevel));
            }
            actors.Render(state,reducedMotion);upgrades.Render(state);amenities.Render(state,reducedMotion);streetLife.Render(state,reducedMotion);construction.Render(state,reducedMotion);
            entranceDoor.Render(actors,deltaTime,reducedMotion);foreach(var door in roomDoors)door.Render(actors,deltaTime,reducedMotion);
            effects.Armed=true;effects.Update(deltaTime);
            UpdateHome(deltaTime,reducedMotion);
        }
        private ClinicUpgradeEffects effects;
        /// <summary>A ring of sparkles on the floor around an improved room or object.</summary>
        public void CelebrateUpgrade(Vector3 point,float radius)=>effects?.Celebrate(new Vector3(point.x,Floor,point.z),radius);
        private void UpdateHome(float deltaTime,bool reducedMotion)
        {
            if(homing)
            {
                if(reducedMotion) { center=homeTarget;size=homeSize; }
                else { center=Vector3.SmoothDamp(center,homeTarget,ref velocity,.22f,100,deltaTime);size=Mathf.SmoothDamp(size,homeSize,ref sizeVelocity,.22f,100,deltaTime); }
                if((center-homeTarget).sqrMagnitude<.00001f&&Mathf.Abs(size-homeSize)<.001f)homing=false;
                ApplyCamera();
            }
        }

        public void Home(bool immediate=false)
        {
            Initialize();FitHome(out homeTarget,out homeSize);userCamera=false;homing=!immediate;velocity=Vector3.zero;sizeVelocity=0;
            var previousCenter=center;float previousSize=size;
            center=homeTarget;size=BoundedSize(homeSize);ClampCenter();homeTarget=center;homeSize=size;
            if(!immediate){center=previousCenter;size=previousSize;}ApplyCamera();
        }
        public void Pan(Vector2 fromUV,Vector2 toUV)
        {
            if(!TryViewportToGround(fromUV,out var before)||!TryViewportToGround(toUV,out var after))return;
            homing=false;userCamera=true;center+=before-after;ClampCenter();ApplyCamera();
        }
        /// <param name="factor">Values below one zoom in. The anchor remains under the finger/cursor.</param>
        public void Zoom(float factor,Vector2 anchorUV)
        {
            if(float.IsNaN(factor)||float.IsInfinity(factor)||factor<=0||!TryViewportToGround(anchorUV,out var before))return;
            homing=false;userCamera=true;size=BoundedSize(size*factor);ApplyCamera();
            if(TryViewportToGround(anchorUV,out var after))center+=before-after;
            ClampCenter();ApplyCamera();
        }
        public bool TryViewportToGround(Vector2 uv,out Vector3 point)
        {
            Initialize();var ray=SceneCamera.ViewportPointToRay(uv);
            if(floor.Raycast(ray,out float distance)) { point=ray.GetPoint(distance);return true; }
            point=default;return false;
        }
        public Vector2 WorldToViewport(Vector3 point)
        { var p=SceneCamera.WorldToViewportPoint(point);return p.z>0?new Vector2(p.x,p.y):new Vector2(-10,-10); }
        public bool TryGetAnchorViewport(string name,out Vector2 uv)
        { if(!anchors.TryGetValue(name,out var anchor)){uv=default;return false;}uv=WorldToViewport(anchor.position);return uv.x>=0&&uv.x<=1&&uv.y>=0&&uv.y<=1; }
        public Vector3 GetCashPoint(int deskId)=>doctors!=null?GetAnchorPoint("reception.desk."+Mathf.Clamp(deskId,0,3)+".cash"):GetAnchorPoint(CashAnchors[Mathf.Clamp(deskId,0,1)]);
        public Vector3 GetDeskPoint(int id)=>doctors!=null?doctors.WorkstationPoint(ClinicStaffRole.Receptionist,id):desks[Mathf.Clamp(id,0,1)].transform.position+new Vector3(-.36f,1.2f,.1f);
        public Vector3 GetStationPoint(int id)=>doctors!=null?doctors.WorkstationPoint(ClinicStaffRole.Nurse,id):stations[Mathf.Clamp(id,0,1)].transform.position+new Vector3(-.20f,.80f,.27f);
        public Vector3 GetWorkstationPoint(ClinicStaffRole role,int id)=>doctors!=null?doctors.WorkstationPoint(role,id):role==ClinicStaffRole.Receptionist?GetDeskPoint(id):GetStationPoint(id);
        private ClinicGearPreview gearPreview;
        /// <summary>A picture of one first aid equipment piece at one version, for the equipment panel.</summary>
        public RenderTexture GearPreview(ClinicRoom room,int item,int version){Initialize();if(gearPreview==null)gearPreview=new ClinicGearPreview(art,scene);return gearPreview.Show(room,item,version);}
        /// <summary>Where a piece of a room's equipment stands, for celebrations and floating text.</summary>
        public Vector3 GetGearPoint(ClinicRoom room,int item)=>ClinicRoomGear.Point(doctors!=null,room,item);
        public Vector3 GetRoomPoint(ClinicRoom room)=>doctors!=null?doctors.RoomPoint(room):(roomRoots[Mathf.Clamp((int)room,0,roomRoots.Length-1)]?.position??Entrance)+Vector3.up;
        public Vector3 GetAmenityPoint(ClinicAmenity kind)=>doctors!=null?doctors.AmenityPoint(kind):kind==ClinicAmenity.Parking?ClinicAmenities.ParkingPoint:
            kind==ClinicAmenity.Toilet?ClinicAmenities.ToiletPoint:ClinicAmenities.VendingPoint;
        public Vector3 GetVendingCashPoint()=>doctors!=null?doctors.VendingCashPoint:ClinicAmenities.VendingCashPoint;
        public Vector3 GetTaxiCashPoint()=>doctors!=null?doctors.AmenityPoint(ClinicAmenity.Taxi)-Vector3.up*.15f:Vector3.zero;
        public Vector3 GetPharmacyCashPoint(int station)=>doctors!=null?doctors.WorkstationPoint(ClinicStaffRole.Pharmacist,station)+Vector3.up*.1f:Vector3.zero;
        public Vector3 GetParkingCashPoint()=>ClinicParkingPresentation.CashPoint(doctors!=null);
        public Vector3 GetAnchorPoint(string name)
        {
            if(anchors.TryGetValue(name??"",out var anchor))return anchor.position;
            if(doctors!=null)throw new ArgumentException("Missing doctors clinic anchor: "+name,nameof(name));
            return Entrance;
        }
        internal Vector3 Facing(string name)=>anchors.TryGetValue(name??"",out var anchor)?anchor.forward:Vector3.forward;
        internal bool HasSeatAt(string name)
        {
            if(string.IsNullOrEmpty(name))return false;
            bool chair=name.StartsWith("consultation.station.",StringComparison.Ordinal)&&name.EndsWith(".patient",StringComparison.Ordinal)||name.StartsWith("waiting.seat.",StringComparison.Ordinal)||
                name.StartsWith("firstaid.station.",StringComparison.Ordinal)&&name.EndsWith(".patient",StringComparison.Ordinal);
            return chair&&anchors.TryGetValue(name,out var anchor)&&anchor.gameObject.activeInHierarchy;
        }
        private static bool Inside(Rect rect,Vector3 p)=>p.x>=rect.xMin&&p.x<=rect.xMax&&p.z>=rect.yMin&&p.z<=rect.yMax;
        public ClinicHit Pick(Vector2 uv)
        {
            if(uv.x<0||uv.x>1||uv.y<0||uv.y>1)return default;
            if(doctors!=null)return doctors.Pick(uv);
            var ray=SceneCamera.ViewportPointToRay(uv);
            for(int i=0;i<2;i++)if(tills[i]>0&&new Bounds(GetCashPoint(i),new Vector3(.65f,.65f,.55f)).IntersectRay(ray))return new ClinicHit(ClinicHitKind.Cash,i);
            if(amenities.VendingTill>0&&new Bounds(GetVendingCashPoint(),new Vector3(.40f,.42f,.36f)).IntersectRay(ray))return new ClinicHit(ClinicHitKind.VendingCash);
            for(int i=0;i<2;i++)
            {
                if(desks[i].activeSelf&&new Bounds(GetDeskPoint(i),new Vector3(.56f,.50f,.48f)).IntersectRay(ray))return new ClinicHit(ClinicHitKind.Desk,i);
                if(stations[i].activeSelf&&new Bounds(GetStationPoint(i),new Vector3(.66f,.64f,.67f)).IntersectRay(ray))return new ClinicHit(ClinicHitKind.Station,i);
            }
            for(int i=0;i<3;i++)if(new Bounds(GetAmenityPoint((ClinicAmenity)i),i==0?new Vector3(1.2f,1.3f,.8f):new Vector3(.85f,.82f,.7f)).IntersectRay(ray))
                return new ClinicHit(i==0?ClinicHitKind.Parking:i==1?ClinicHitKind.Toilet:ClinicHitKind.Vending,i);
            if(!TryViewportToGround(uv,out var p))return default;
            if(p.x>=-14.5f&&p.x<=-6.15f&&p.z>=-5.1f&&p.z<=5.1f)return new ClinicHit(ClinicHitKind.Parking,(int)ClinicAmenity.Parking);
            if(Inside(Washroom,p))return new ClinicHit(ClinicHitKind.Toilet,(int)ClinicAmenity.Toilet);
            if(Mathf.Abs(p.x-VendingMachine.x)<=.6f&&p.z>=VendingPatient.z-.2f&&p.z<=Middle)return new ClinicHit(ClinicHitKind.Vending,(int)ClinicAmenity.Vending);
            if(Inside(Reception,p))return new ClinicHit(ClinicHitKind.Reception,(int)ClinicRoom.Reception);
            if(Inside(TreatmentOne,p)||Inside(TreatmentTwo,p))return new ClinicHit(ClinicHitKind.Treatment,(int)ClinicRoom.FirstAid);
            if(Inside(Lounge,p))return new ClinicHit(waitingBuilt?ClinicHitKind.Waiting:ClinicHitKind.Expansion,(int)ClinicRoom.Waiting);
            if(serviceRoomsOffered)
            {
                if(Inside(Office,p))return new ClinicHit(ClinicHitKind.Office,(int)ClinicRoom.Office);
                if(Inside(StaffRoom,p))return new ClinicHit(ClinicHitKind.StaffRoom,(int)ClinicRoom.StaffRoom);
                if(Inside(Store,p))return new ClinicHit(ClinicHitKind.Store,(int)ClinicRoom.Store);
            }
            return default;
        }
        private static Rect SelectionRect(ClinicRoom room)=>room==ClinicRoom.Reception?Reception:room==ClinicRoom.FirstAid
            ?Rect.MinMaxRect(West,Middle,HallWest,Back):ClinicRules.IsServiceRoom(room)?ServiceRoomRect(room):Lounge;
        private void Frame(Rect rect,float y)
        { selection.gameObject.SetActive(true);selection.position=new Vector3(rect.center.x,y,rect.center.y);selection.localScale=new Vector3(rect.width,1,rect.height); }
        public void SelectRoom(ClinicRoom room)
        { if(doctors!=null){doctors.Select(new ClinicHit(room==ClinicRoom.Reception?ClinicHitKind.Reception:room==ClinicRoom.FirstAid?ClinicHitKind.Treatment:room==ClinicRoom.Waiting?ClinicHitKind.Waiting:room==ClinicRoom.Consultation?ClinicHitKind.Consultation:ClinicHitKind.Pharmacy,(int)room));return;}Frame(SelectionRect(room),.17f); }
        public void SelectRoom(ClinicHit hit)
        {
            if(doctors!=null){doctors.Select(hit);return;}
            if(hit.Kind==ClinicHitKind.None||hit.Kind==ClinicHitKind.Cash||hit.Kind==ClinicHitKind.VendingCash){selection.gameObject.SetActive(false);return;}
            if(hit.Kind==ClinicHitKind.Desk||hit.Kind==ClinicHitKind.Station)
            {
                selection.gameObject.SetActive(true);selection.position=(hit.Kind==ClinicHitKind.Desk?desks[Mathf.Clamp(hit.Id,0,1)]:stations[Mathf.Clamp(hit.Id,0,1)]).transform.position+Vector3.up*.025f;
                selection.localScale=hit.Kind==ClinicHitKind.Desk?new Vector3(1.95f,1,1.2f):new Vector3(1.9f,1,1.9f);return;
            }
            if(hit.Kind==ClinicHitKind.Parking){selection.gameObject.SetActive(true);selection.position=new Vector3(-10.325f,.17f,0);selection.localScale=new Vector3(8.35f,1,10.2f);return;}
            if(hit.Kind==ClinicHitKind.Toilet){Frame(Washroom,.19f);return;}
            if(hit.Kind==ClinicHitKind.Vending){selection.gameObject.SetActive(true);selection.position=new Vector3(VendingMachine.x,.17f,VendingMachine.z);selection.localScale=new Vector3(1.05f,1,.86f);return;}
            SelectRoom((ClinicRoom)hit.Id);
        }
        private float BoundedSize(float requested)
        {
            var coverage=GroundCoverage();float reference=Mathf.Max(.001f,SceneCamera.orthographicSize);
            float width=doctors!=null?80:56,depth=doctors!=null?66:52;
            float limit=Mathf.Min(doctors!=null?24:16,width*reference/Mathf.Max(.001f,coverage.size.x),depth*reference/Mathf.Max(.001f,coverage.size.z));
            return Mathf.Clamp(requested,2.4f,Mathf.Max(2.4f,limit));
        }
        private Bounds GroundCoverage()
        {
            TryViewportToGround(Vector2.zero,out var first);var coverage=new Bounds(first,Vector3.zero);
            for(int corner=1;corner<4;corner++)if(TryViewportToGround(new Vector2(corner&1,(corner>>1)&1),out var point))coverage.Encapsulate(point);
            return coverage;
        }
        private void ClampCenter()
        {
            ApplyCamera();var coverage=GroundCoverage();
            float minimum=(doctors!=null?-40:-28)-coverage.min.x,maximum=(doctors!=null?40:28)-coverage.max.x;
            center.x+=minimum>maximum?(minimum+maximum)*.5f:Mathf.Clamp(0,minimum,maximum);
            minimum=(doctors!=null?-32:-27)-coverage.min.z;maximum=(doctors!=null?34:25)-coverage.max.z;
            center.z+=minimum>maximum?(minimum+maximum)*.5f:Mathf.Clamp(0,minimum,maximum);center.y=0;
        }
        private void ApplyCamera() { SceneCamera.transform.position=center+CameraOffset;SceneCamera.orthographicSize=size; }
        private void FitHome(out Vector3 target,out float fittedSize)
        {
            // The whole starter clinic, from the queue at the storefront to the back wall, fits below the top HUD.
            // Projection-space fitting works for every render-target aspect without resetting a user camera.
            var right=SceneCamera.transform.right;var up=SceneCamera.transform.up;
            var minimum=new Vector2(float.MaxValue,float.MaxValue);var maximum=new Vector2(float.MinValue,float.MinValue);
            for(int corner=0;corner<8;corner++)
            {
                var point=doctors!=null?new Vector3((corner&1)==0?-12.35f:10.35f,(corner&2)==0?0:2.20f,(corner&4)==0?-10.9f:9.2f):new Vector3((corner&1)==0?West-.1f:East+.1f,(corner&2)==0?0:2.20f,(corner&4)==0?Front-.9f:Back);
                var projected=new Vector2(Vector3.Dot(right,point),Vector3.Dot(up,point));minimum=Vector2.Min(minimum,projected);maximum=Vector2.Max(maximum,projected);
            }
            const float visibleWidth=.84f,visibleHeight=.64f,verticalCenter=.46f;
            fittedSize=Mathf.Max((maximum.x-minimum.x)/(2*SceneCamera.aspect*visibleWidth),(maximum.y-minimum.y)/(2*visibleHeight));
            var middle=(minimum+maximum)*.5f;
            float desiredRight=middle.x,desiredUp=middle.y-(verticalCenter-.5f)*2*fittedSize;
            float determinant=right.x*up.z-right.z*up.x;
            target=new Vector3((desiredRight*up.z-right.z*desiredUp)/determinant,0,(right.x*desiredUp-desiredRight*up.x)/determinant);
        }

        private void BuildArchitecture()
        {
            art.Box("Meadow ground",scene,new Vector3(0,-.30f,0),new Vector3(100,.15f,100),"Meadow");
            art.Box("Clinic foundation",scene,new Vector3(0,-.01f,(Front+Back)*.5f),new Vector3(East-West+.3f,.27f,Back-Front+.3f),"Clay");
            roomRoots[0]=art.Group("Reception",scene,new Vector3(Reception.center.x,0,Reception.center.y));
            roomRoots[1]=art.Group("First aid",scene,new Vector3((West+HallWest)*.5f,0,(Middle+Back)*.5f));
            roomRoots[2]=art.Group("Waiting room",scene,new Vector3(Lounge.center.x,0,Lounge.center.y));
            foreach(var kind in ClinicRules.ServiceRooms){var rect=ServiceRoomRect(kind);roomRoots[(int)kind]=art.Group(kind+" room",scene,new Vector3(rect.center.x,0,rect.center.y));}
            // Renovation supplies wait on the forecourt, clear of the walk to the door, while the rooms keep working.
            var workAreas=new[]{new Vector3(3.2f,0,-5.85f),new Vector3(-2.7f,0,-5.85f),new Vector3(-4.5f,0,-5.85f)};
            for(int room=0;room<3;room++)
            {
                renovations[room]=art.Group("Renovation at work",scene,workAreas[room]).gameObject;
                art.Box("Renovation toolbox",renovations[room].transform,new Vector3(0,.31f,0),new Vector3(.55f,.34f,.37f),"Apricot");
                for(int i=0;i<3;i++)art.Box("Stacked fresh panels",renovations[room].transform,new Vector3(.38f,.19f+i*.09f,.18f),new Vector3(.5f,.08f,.6f),"Wood");
                renovations[room].SetActive(false);
            }
            // Older rules show a room's size with a few fittings; rooms whose equipment is priced per piece show that instead.
            var fittings=new[]
            {
                new[]{new Vector3(5.3f,Floor,-4.85f),new Vector3(1.9f,Floor,-.5f),new Vector3(1.3f,Floor,-.55f)},
                new[]{new Vector3(-5.3f,Floor,.35f),new Vector3(-4.9f,Floor,5.5f),new Vector3(-5.3f,Floor,3.35f)},
                new[]{new Vector3(-5.35f,Floor,-.55f),Vector3.zero,new Vector3(-1.35f,Floor,-4.8f)}
            };
            for(int room=0;room<3;room++)for(int level=0;level<2;level++)
            {
                var details=art.Group("Tier "+(level+2)+" fittings",roomRoots[room]);tierDetails[room,level]=details.gameObject;
                if(level==0){art.Model("Plant",details,fittings[room][0]-roomRoots[room].position);if(room!=2)art.Model("Cupboard",details,fittings[room][1]-roomRoots[room].position);}
                else art.Model("Plant",details,fittings[room][2]-roomRoots[room].position);
                details.gameObject.SetActive(false);
            }
            // The lobby and corridor: one continuous hall floor from the doors to the fire exit.
            art.Box("Main circulation floor",scene,new Vector3(Hall.center.x,.11f,Hall.center.y),new Vector3(Hall.width,.05f,Hall.height),"Ivory");
            foreach(var room in new[]{Office,StaffRoom,Washroom,Store})
                art.Box("Service room floor",scene,new Vector3(room.center.x,.11f,room.center.y),new Vector3(room.width,.05f,room.height),"TileSage");
            art.Box("Forecourt paving",scene,new Vector3(0,.11f,-5.85f),new Vector3(East-West+.3f,.06f,1.4f),"Ivory");
            ClinicArchitecture.Build(art,scene);
            art.Box("Entry mat",scene,new Vector3(0,.145f,-4.55f),new Vector3(1.4f,.012f,1.0f),"CarGraphite");
            art.Box("Entry mat border",scene,new Vector3(0,.143f,-4.55f),new Vector3(1.48f,.01f,1.08f),"Chrome");
            for(float x=West+.3f;x<East;x+=.72f)art.Box("Front path paver",scene,new Vector3(x,.12f,-6.25f),new Vector3(.68f,.07f,.55f),"Concrete");
            // Planters flank the entrance; a freestanding totem marks the clinic from the street.
            foreach(var x in new[]{-1.55f,1.55f})
            {
                var planter=art.Group("Entrance planter",scene,new Vector3(x,Floor,-5.62f));
                art.Box("Planter box",planter,new Vector3(0,.25f,0),new Vector3(.62f,.5f,.62f),"Charcoal");
                art.Box("Planter soil",planter,new Vector3(0,.5f,0),new Vector3(.54f,.02f,.54f),"Soil");
                art.Model("Plant",planter,new Vector3(0,.5f,0));
            }
            var entrance=art.Group("Entrance care emblem",scene,new Vector3(-2.6f,0,-5.75f));
            art.Box("Emblem post",entrance,new Vector3(0,.14f,0),new Vector3(.42f,.28f,.22f),"Concrete");
            art.Box("Totem panel",entrance,new Vector3(0,1.00f,0),new Vector3(.36f,1.46f,.12f),"SignBlue");
            art.Box("Totem cap",entrance,new Vector3(0,1.75f,0),new Vector3(.40f,.05f,.16f),"CarGraphite");
            art.Box("Care mark horizontal",entrance,new Vector3(0,1.42f,-.066f),new Vector3(.24f,.07f,.02f),"Paint");
            art.Box("Care mark vertical",entrance,new Vector3(0,1.42f,-.066f),new Vector3(.07f,.24f,.02f),"Paint");
            for(int line=0;line<3;line++)art.Box("Totem directory line",entrance,new Vector3(0,1.10f-line*.13f,-.066f),new Vector3(.22f,.035f,.012f),"Paint");
            // Street edge: tactile paving at the crossing, bollards kept clear of the crossing and walking routes.
            art.Box("Crossing tactile paving",scene,new Vector3(StreetCrossingX,.035f,-7.12f),new Vector3(1.85f,.02f,.40f),"PlateYellow");
            foreach(var x in new[]{-5.2f,-4.1f,-3.0f,-1.9f,1.9f,2.9f,3.9f,5.0f})
            {
                var bollard=art.Group("Kerbside bollard",scene,new Vector3(x,0,-7.22f));
                art.Cylinder("Bollard post",bollard,new Vector3(0,.42f,0),new Vector3(.13f,.84f,.13f),"CarGraphite");
                art.Cylinder("Bollard band",bollard,new Vector3(0,.72f,0),new Vector3(.135f,.06f,.135f),"Paint");
            }
            var seat=art.Group("Entrance bench",scene,new Vector3(3.9f,0,-5.75f));
            art.Box("Entrance bench seat",seat,new Vector3(0,.46f,0),new Vector3(1.40f,.07f,.40f),"Timber");
            art.Box("Entrance bench back",seat,new Vector3(0,.74f,.19f),new Vector3(1.40f,.34f,.05f),"Timber");
            for(int side=-1;side<=1;side+=2)art.Box("Entrance bench frame",seat,new Vector3(side*.60f,.30f,.04f),new Vector3(.06f,.60f,.44f),"CarGraphite");
            var bin=art.Group("Entrance litter bin",scene,new Vector3(5.2f,0,-5.8f));
            art.Cylinder("Litter bin body",bin,new Vector3(0,.42f,0),new Vector3(.40f,.84f,.40f),"CarGraphite");
            art.Cylinder("Litter bin lid",bin,new Vector3(0,.86f,0),new Vector3(.44f,.06f,.44f),"Chrome");
            selection=art.Group("Room selection",scene);
            for(int side=-1;side<=1;side+=2)
            {
                art.Box("Selection edge",selection,new Vector3(side*.49f,0,0),new Vector3(.007f,.014f,.98f),"Gold");
                art.Box("Selection edge",selection,new Vector3(0,0,side*.49f),new Vector3(.98f,.014f,.007f),"Gold");
            }
            selection.gameObject.SetActive(false);
        }
        /// <summary>Room styles: every room size restyles the floor, walls and furniture; decor adds pieces per level.</summary>
        private void BuildRoomStyles()
        {
            const float top=.135f;float o=OuterThickness*.5f,i=InnerThickness*.5f;
            float w=West+o,e=East-o,f=Front+.04f,m=Middle-i,mN=Middle+i,h=HallWest-i,hE=HallEast+i,split=TreatmentSplit-i,splitN=TreatmentSplit+i,b=Back-o;
            float entrance=EntranceWidth*.5f+.145f;
            styles[0]=new ClinicRoomStyle(art,scene,"Reception",Reception,top,new[]{
                new RoomWall(hE,m,StaffDoorX-RoomDoorWidth*.5f,m,TallWall),new RoomWall(StaffDoorX+RoomDoorWidth*.5f,m,e,m,TallWall),
                new RoomWall(e,f,e,m,LowWall),new RoomWall(entrance,f,e,f,FrontWall)},0,"TilePeach",10,
                // Decor keeps clear of the feature wall, the pictures and screens around it, and the staff door.
                new[]{new Vector3(FeatureWall.x,1.2f,m),new Vector3(1.4f,.6f,m),new Vector3(StaffDoorX,.7f,m),new Vector3(5.5f,.4f,m)});
            styles[1]=new ClinicRoomStyle(art,scene,"First aid",TreatmentOne,top,new[]{
                new RoomWall(w,mN,w,split,TallWall),new RoomWall(w,mN,h,mN,LowWall),new RoomWall(w,split,h,split,LowWall),
                new RoomWall(h,mN,h,TreatmentDoorZ(0)-TreatmentDoorWidth*.5f,LowWall),new RoomWall(h,TreatmentDoorZ(0)+TreatmentDoorWidth*.5f,h,split,LowWall)},1,"TileBlue",10,
                new[]{new Vector3(w,.5f,.6f),new Vector3(w,.5f,2.1f)});
            styles[3]=new ClinicRoomStyle(art,scene,"First aid 2",TreatmentTwo,top,new[]{
                new RoomWall(w,splitN,w,b,TallWall),new RoomWall(w,b,h,b,TallWall),new RoomWall(w,splitN,h,splitN,LowWall),
                new RoomWall(h,splitN,h,TreatmentDoorZ(1)-TreatmentDoorWidth*.5f,LowWall),new RoomWall(h,TreatmentDoorZ(1)+TreatmentDoorWidth*.5f,h,b,LowWall)},1,"TileBlue",10,
                new[]{new Vector3(w,.5f,3.65f),new Vector3(-4.9f,.5f,b),new Vector3(-3.4f,.8f,b),new Vector3(-2.0f,.5f,b)});
            styles[2]=new ClinicRoomStyle(art,scene,"Waiting room",Lounge,top,new[]{
                new RoomWall(w,f,w,m,TallWall),new RoomWall(w,m,h,m,LowWall),new RoomWall(w,f,-entrance,f,FrontWall)},2,"TileSage",10,
                // Decor keeps clear of the windows, the lounge picture and the vending machine's tip cup.
                new[]{new Vector3(w,.75f,-1.45f),new Vector3(w,.75f,-3.95f),new Vector3(w,.5f,-2.6f),new Vector3(VendingTips.x,.5f,m),new Vector3(VendingMachine.x,.6f,m)});
            serviceStyles[0]=new ClinicRoomStyle(art,scene,"Office",Office,top,new[]{
                new RoomWall(hE,mN,ServiceSplitX-i,mN,LowWall),new RoomWall(ServiceSplitX-i,mN,ServiceSplitX-i,ServiceSplitZ-i,LowWall),new RoomWall(hE,ServiceSplitZ-i,ServiceSplitX-i,ServiceSplitZ-i,LowWall)},0,"TileSage",10);
            serviceStyles[1]=new ClinicRoomStyle(art,scene,"Staff room",StaffRoom,top,new[]{
                new RoomWall(ServiceSplitX+i,mN,StaffDoorX-RoomDoorWidth*.5f,mN,LowWall),new RoomWall(ServiceSplitX+i,ServiceSplitZ-i,StoreDoorX-RoomDoorWidth*.5f,ServiceSplitZ-i,LowWall)},2,"TilePeach",10);
            serviceStyles[2]=new ClinicRoomStyle(art,scene,"Store",Store,top,new[]{
                new RoomWall(ServiceSplitX+i,b,e,b,TallWall),new RoomWall(ServiceSplitX+i,ServiceSplitZ+i,ServiceSplitX+i,b,LowWall)},1,"TileBlue",10);
            foreach(var style in styles)style.Adopt(scene);
        }
        private void BuildFurniture()
        {
            for(int i=0;i<2;i++)
            {
                desks[i]=art.Model("ReceptionDesk",scene,Desk(i));
                RegisterSockets(desks[i].transform,"reception.desk."+i+".",Vector3.forward,Vector3.back);
                var point=GetCashPoint(i);
                for(int j=0;j<6;j++)cash[i,j]=art.Box("Collectable cash",scene,point+new Vector3((j%2)*.13f-.06f,(j/2)*.055f,0),new Vector3(.20f,.043f,.16f),j%2==0?"Gold":"Apricot");
                desks[i].SetActive(i==0);for(int j=0;j<6;j++)cash[i,j].SetActive(false);
                stations[i]=art.Model("TreatmentBay",scene,Bay(i));
                RegisterSockets(stations[i].transform,"firstaid.station."+i+".",Vector3.back,Vector3.left);stations[i].SetActive(i==0);
            }
            art.Model("ReceptionFeature",scene,FeatureWall).transform.localScale=new Vector3(FeatureWallScale,1,1);
            for(int i=0;i<Seats;i++)
            {
                var facing=SeatFacing(i);var rotation=Quaternion.Euler(0,facing.x>0?-90:90,0);
                seats[i]=art.Model("Seat",scene,Seat(i),rotation);
                foreach(var node in seats[i].GetComponentsInChildren<Transform>(true))if(node.name.Contains("__patient"))
                { anchors["waiting.seat."+i]=node;node.rotation=Quaternion.LookRotation(facing);break; }
                seats[i].SetActive(false);
            }
            waitingClosed=art.Group("Future waiting lounge",scene,new Vector3(Lounge.center.x,0,Lounge.center.y)).gameObject;
            art.Box("Covered seating crate",waitingClosed.transform,new Vector3(-.3f,.38f,0),new Vector3(1.70f,.48f,1.05f),"Wood");
            for(int i=-1;i<=1;i++)art.Box("Crate strap",waitingClosed.transform,new Vector3(-.3f+i*.54f,.39f,0),new Vector3(.075f,.52f,1.09f),"Gold");
            art.Box("Future lounge inset",waitingClosed.transform,new Vector3(0,.15f,0),new Vector3(Lounge.width-.6f,.015f,Lounge.height-.6f),"Sage");
        }
        /// <summary>Before the office, staff room or store is built: dust sheets over stacked boxes and a placard naming the room.</summary>
        private void BuildServicePlots()
        {
            for(int i=0;i<3;i++)
            {
                var rect=ServiceRoomRects[i];var plot=art.Group("Future "+ClinicRules.ServiceRooms[i]+" plot",scene,new Vector3(rect.center.x,0,rect.center.y));
                art.Box("Future room inset",plot,new Vector3(0,.15f,0),new Vector3(rect.width-.4f,.012f,rect.height-.4f),"Sage");
                art.Box("Covered stock",plot,new Vector3(-.2f,.36f,.2f),new Vector3(.9f,.44f,.7f),"Linen");
                art.Box("Covered stock",plot,new Vector3(.45f,.3f,-.35f),new Vector3(.6f,.32f,.5f),"Linen");
                for(int s=-1;s<=1;s+=2)art.Box("Crate strap",plot,new Vector3(-.2f+s*.25f,.37f,.2f),new Vector3(.06f,.46f,.72f),"Gold");
                art.Box("Room placard post",plot,new Vector3(.5f,.55f,.45f),new Vector3(.05f,.9f,.05f),"Charcoal");
                art.Box("Room placard",plot,new Vector3(.5f,1.05f,.44f),new Vector3(.5f,.3f,.04f),"SageDark");
                art.Box("Room placard mark",plot,new Vector3(.5f,1.05f,.415f),new Vector3(.3f,.06f,.01f),"Paper");
                plot.gameObject.SetActive(false);servicePlots[i]=plot.gameObject;
            }
        }
        private void RegisterSockets(Transform root,string prefix,Vector3 patientFacing,Vector3 staffFacing)
        {
            foreach(var node in root.GetComponentsInChildren<Transform>(true))
            {
                int split=node.name.IndexOf("__",StringComparison.Ordinal);if(split<0)continue;
                var part=node.name.Substring(split+2).Split('.')[0];anchors[prefix+part]=node;
                if(part=="patient"||part=="staff")node.rotation=Quaternion.LookRotation(part=="patient"?patientFacing:staffFacing);
            }
        }
        private void Anchor(string name,Vector3 position,Vector3 forward=default)
        { var item=art.Group(name,scene,position);item.rotation=Quaternion.LookRotation(forward==default?Vector3.forward:forward);anchors[name]=item; }
        private void BuildAnchors()
        {
            for(int i=0;i<6;i++)Anchor("parking.bay."+i+".patient",ClinicAmenities.BayPatient(i),Vector3.right);
            Anchor("waiting.toilet.patient",ToiletSeat,Vector3.back);
            Anchor("waiting.vending.patient",VendingPatient,Vector3.forward);
            Anchor("waiting.vending.cash",ClinicAmenities.VendingCashPoint);
            Anchor("entrance",Entrance);Anchor("exit",Exit,Vector3.back);
            // The queue snakes in two rows inside the reception, between the counter lane and the storefront.
            for(int i=0;i<QueuePlaces;i++)Anchor("reception.queue."+i,QueuePlace(i),QueueFacing(i));
            for(int i=0;i<2;i++)Anchor("firstaid.standing."+i,Standing(i),Vector3.left);
            Anchor("reception.progress",new Vector3(3.15f,1.65f,-1.1f));Anchor("firstaid.progress",new Vector3(-3.4f,1.9f,2.87f));Anchor("waiting.progress",new Vector3(-3.4f,1.5f,-2.6f));
        }
        private void BuildLighting()
        {
            RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.75f,.78f,.75f);
            RenderSettings.ambientEquatorColor=new Color(.48f,.53f,.49f);RenderSettings.ambientGroundColor=new Color(.31f,.28f,.24f);
            var light=art.Group("Soft clinic daylight",transform).gameObject.AddComponent<Light>();light.type=LightType.Directional;
            light.transform.rotation=Quaternion.Euler(48,-35,0);light.intensity=1.02f;light.color=new Color(.98f,.96f,.90f);
            light.shadows=LightShadows.Soft;light.shadowStrength=.28f;light.shadowBias=.025f;light.shadowNormalBias=.12f;light.cullingMask=1<<ClinicArt.Layer;
        }
        private void ReleaseTexture() { if(Texture==null)return;SceneCamera.targetTexture=null;Texture.Release();ClinicArt.Destroy(Texture);Texture=null; }
        private void OnDestroy() { gearPreview?.Dispose();ReleaseTexture();art?.Dispose(); }
    }
}
