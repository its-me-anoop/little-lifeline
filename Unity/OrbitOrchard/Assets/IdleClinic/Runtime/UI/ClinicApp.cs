using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IdleClinic.Core;
using IdleClinic.Presentation;
using IdleClinic.Services;
using OrbitOrchard.App;
using OrbitOrchard.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdleClinic.App
{
    /// <summary>The persistent world stays mounted while contextual controls change.</summary>
    public sealed partial class ClinicApp : MonoBehaviour
    {
        private ClinicSimulation simulation, otherSimulation;
        private ClinicProfileStore saves;
        private ClinicProfile profile;
        private ClinicWorld world;
        private AppleServices apple;
        private static readonly Color CoinInk = new Color(.54f, .39f, .09f);
        private static readonly Color LeafInk = new Color(.18f, .37f, .28f);
        private VisualElement root, overlay, header, dock, cameraTools, hint, wallet, particles, hudChips, scrim;
        private Image board;
        private Label walletLabel, hintLabel, toast;
        private Font bodyFont, boldFont, displayFont;
        private PanelSettings runtimePanel;
        private Camera backdrop;
        private ClinicAudio clinicAudio;
        private ClinicRoom? selectedRoom;
        private ClinicHit? selectedObject;
        private bool settingsOpen, ready, inactive, skipNextDelta=true, restoreRequested, restoreObservedBusy;
        private double saveClock, readoutClock, toastUntil;
        private string dockKey = "";
        private ClinicTutorialStep lastTutorial = (ClinicTutorialStep)(-1);
        private readonly List<Action> readouts = new List<Action>();
        private readonly Dictionary<int, VisualElement> cashMarkers = new Dictionary<int, VisualElement>();
        private readonly Dictionary<int, ClinicProgress> patientRings = new Dictionary<int, ClinicProgress>();
        private readonly Dictionary<int, VisualElement> constructionMarkers = new Dictionary<int, VisualElement>();
        private readonly Dictionary<ClinicRoom, VisualElement> roomTargets = new Dictionary<ClinicRoom, VisualElement>();
        private VisualElement waitingMarker,vendingCashMarker,parkingCashMarker,taxiCashMarker;
        private readonly VisualElement[] pharmacyCashMarkers=new VisualElement[2];
        private Button parkingControl;
        private readonly Dictionary<string,VisualElement> objectTargets=new Dictionary<string,VisualElement>();
        private readonly Dictionary<VisualElement,ClinicHit> objectHits=new Dictionary<VisualElement,ClinicHit>();
        private bool ReducedMotion => profile != null && (profile.preferences.reducedMotion || (apple != null && apple.IsReduceMotionEnabled));
        private ClinicState State => simulation.State;
        private ClinicState ActiveState => State;

        private void Awake()
        {
            OrchardInput.Initialize(gameObject);
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount = 0;
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            apple = AppleServices.Instance;
            if (apple == null) apple = new GameObject("AppleServices").AddComponent<AppleServices>();
            apple.StateChanged+=AppleChanged;
            backdrop = new GameObject("Clinic backdrop").AddComponent<Camera>();
            backdrop.depth = -100; backdrop.cullingMask = 0;
            backdrop.clearFlags = CameraClearFlags.SolidColor;
            backdrop.backgroundColor = new Color(.91f,.92f,.85f);
            gameObject.AddComponent<AudioListener>();
            clinicAudio = gameObject.AddComponent<ClinicAudio>();
        }

        private IEnumerator Start()
        {
            var doc = GetComponent<UIDocument>() ?? gameObject.AddComponent<UIDocument>();
            runtimePanel=Instantiate(Resources.Load<PanelSettings>("ClinicPanel"));
            runtimePanel.scaleMode=PanelScaleMode.ConstantPixelSize;
            runtimePanel.scale=Screen.width/Math.Max(1,apple.ScreenWidthPoints);
            doc.panelSettings = runtimePanel;
            root = doc.rootVisualElement;
            root.styleSheets.Add(Resources.Load<StyleSheet>("ClinicStyle"));
            root.AddToClassList("clinic-root");
            bodyFont = Resources.Load<Font>("Fonts/LifelineBody");
            boldFont = Resources.Load<Font>("Fonts/LifelineBodyBold");
            root.style.unityFontDefinition = FontDefinition.FromFont(bodyFont);
            var loading = Box(root,"loading");
            loading.Add(new ClinicIcon(ClinicGlyph.Equipment,52));
            Text(loading,"Opening your clinic","loading-title");
            var loadBar = new ProgressBar { lowValue=0, highValue=1, title="" };
            loading.Add(loadBar);
            var request = Resources.LoadAsync<Font>("Fonts/ClinicDisplay");
            while(!request.isDone) { loadBar.value=request.progress; yield return null; }
            displayFont = request.asset as Font;
            saves = new ClinicProfileStore(Application.persistentDataPath);
            profile = saves.LoadClinic(DateTimeOffset.UtcNow);
            BindSimulations();
            apple.GemTransactionReceived+=OnGemTransaction;
            world = new GameObject("Idle Clinic World").AddComponent<ClinicWorld>();
            world.Initialize();
            world.ConfigureLocation(profile.activeLocation);
            RefreshAudioPreferences();
            root.Clear();
            BuildInterface();
            ready = true;
            ProcessGemTransactions();
            CheckPurchasedUnlocks();
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            gameObject.AddComponent<ClinicPerformanceRecorder>();
            ApplyQALaunchArguments();
#endif
            if(saves.LastOfflineReport.applied && saves.LastOfflineReport.tillEarned>0)
                ShowWelcome(saves.LastOfflineReport);
            if(!string.IsNullOrEmpty(saves.Error)) Notify(saves.Error,10);
        }

        private void BuildInterface()
        {
            board = new Image { name="clinic-world", scaleMode=ScaleMode.StretchToFill };
            board.AddToClassList("world"); root.Add(board);
            BindWorldInput();
            overlay=Box(root,"overlay");overlay.pickingMode=PickingMode.Ignore;
            particles=Box(overlay,"overlay");particles.pickingMode=PickingMode.Ignore;
            // Sheets (gems, clinics, settings) dim the world behind them; tapping the dimmed world closes them.
            scrim=Box(root,"sheet-scrim");scrim.style.display=DisplayStyle.None;
            scrim.RegisterCallback<ClickEvent>(_=>CloseContext());
            header=Box(root,"header");header.pickingMode=PickingMode.Ignore;
            // One capsule holds both balances: coins, a hairline, then the gem button with its badge beside the number.
            var balances=Box(header,"balances");balances.name="clinic-balances";balances.pickingMode=PickingMode.Ignore;
            wallet=Box(balances,"wallet");wallet.pickingMode=PickingMode.Ignore;
            wallet.Add(new ClinicIcon(ClinicGlyph.Coin,30,CoinInk));
            walletLabel=Text(wallet,"0","wallet-value",true);walletLabel.name="clinic-wallet";Display(walletLabel);
            Box(balances,"balance-divider").pickingMode=PickingMode.Ignore;
            BuildGemControl(balances);
            var settings=IconButton(header,ClinicGlyph.Settings,"Settings",ToggleSettings,"round-control");settings.name="clinic-settings";
            // Where you are, and any running speed-up, sit under the balances.
            hudChips=Box(root,"hud-chips");hudChips.pickingMode=PickingMode.Ignore;
            BuildLocationControl(hudChips);
            BuildBoostChip(hudChips);
            hint=Box(root,"tutorial-hint");hint.pickingMode=PickingMode.Ignore;
            hintLabel=Text(hint,"","hint-label");
            // Camera and car park live on the thumb side, grouped, above the guide.
            cameraTools=Box(root,"camera-tools");cameraTools.pickingMode=PickingMode.Ignore;
            parkingControl=IconButton(cameraTools,ClinicGlyph.Parking,"Manage car park",()=>SelectObject(new ClinicHit(ClinicHitKind.Parking)),"round-control");
            var cameraStack=Box(cameraTools,"camera-stack");cameraStack.pickingMode=PickingMode.Ignore;
            IconButton(cameraStack,ClinicGlyph.Home,"Return to reception",()=>world.Home(ReducedMotion),"camera-button").name="camera-home";
            Box(cameraStack,"camera-divider").pickingMode=PickingMode.Ignore;
            IconButton(cameraStack,ClinicGlyph.Plus,"Zoom in",()=>world.Zoom(.82f,new Vector2(.5f,.5f)),"camera-button").name="camera-zoom-in";
            IconButton(cameraStack,ClinicGlyph.Minus,"Zoom out",()=>world.Zoom(1.22f,new Vector2(.5f,.5f)),"camera-button").name="camera-zoom-out";
            dock=Box(root,"context-dock");dock.style.display=DisplayStyle.None;
            dock.RegisterCallback<GeometryChangedEvent>(_=>ApplySafeArea());
            BuildGuide();
            BuildWelcome();
            toast=Text(root,"","toast");toast.style.display=DisplayStyle.None;toast.pickingMode=PickingMode.Ignore;
            root.RegisterCallback<GeometryChangedEvent>(OnViewportGeometryChanged);
            ApplySafeArea();RebuildDock();UpdateReadouts();
            Unstyle(root);
            InitializeAccessibility();
        }

        private void Update()
        {
            if(!ready || inactive) return;
            var delta=skipNextDelta?0:Time.unscaledDeltaTime;skipNextDelta=false;
            if(!saves.HasPendingOfflineProgress)
            {
                var report=simulation.Advance(delta);
                HandleEvents(report.Events);
                otherSimulation?.Advance(delta, false);
            }
            saveClock+=delta;readoutClock+=delta;
            if(saveClock>=5){saveClock=0;if(saves.HasPendingOfflineProgress)ResumeClinic();else{SaveNow();ProcessGemTransactions();}}
            if(readoutClock>=.15){readoutClock=0;UpdateReadouts();}
            UpdateWorld(delta);UpdateFlights(delta);UpdateJuice(delta);UpdateGuidePointer();UpdateAccessibilityFrames();
            if(toast.style.display==DisplayStyle.Flex && Time.unscaledTimeAsDouble>toastUntil)toast.style.display=DisplayStyle.None;
        }

        private void UpdateReadouts()
        {
            UpdateLocationReadouts();
            walletLabel.text=WalletText();
            UpdateGemReadout();
            if(parkingControl!=null)parkingControl.style.display=State.Tutorial==ClinicTutorialStep.Complete?DisplayStyle.Flex:DisplayStyle.None;
            if(lastTutorial!=State.Tutorial)
            {
                lastTutorial=State.Tutorial;
                if(lastTutorial==ClinicTutorialStep.HireFirstNurse){selectedObject=null;selectedRoom=ClinicRoom.FirstAid;settingsOpen=false;gemsOpen=false;locationsOpen=false;world.SelectRoom(ClinicRoom.FirstAid);}
                if(lastTutorial==ClinicTutorialStep.Complete){selectedObject=null;selectedRoom=null;world.SelectRoom(default(ClinicHit));Notify("Your clinic is open",3);}
                dockKey="";
            }
            hintLabel.text=State.Tutorial==ClinicTutorialStep.FirstArrival ? "Your first patient is checking in"
                : State.Tutorial==ClinicTutorialStep.CollectFirstPayment ? "Tap the payment on the counter"
                : State.Tutorial==ClinicTutorialStep.HireFirstNurse ? "A nurse makes all the difference"
                : State.Tutorial==ClinicTutorialStep.FirstTreatment ? "A little care. A fresh start." : "";
            hint.style.display=string.IsNullOrEmpty(hintLabel.text)?DisplayStyle.None:DisplayStyle.Flex;
            var key=simulation.ConstructionSlots+":"+simulation.BuildersBusy+":"+(gemsOpen?"gems:"+GemDockKey():locationsOpen?"locations":settingsOpen?"settings":selectedObject.HasValue?selectedObject.Value.Kind+":"+selectedObject.Value.Id:selectedRoom+":"+upgradeTrack)+":"+State.Location+":"+profile.state.DoctorsClinicUnlocked+":"+State.Tutorial+":"+State.Staff.Count+":"+State.WaitingRoomUnlocked+":"+
                string.Join(";",State.Rooms.Select(r=>$"{r.Built}:{r.Tier}:{r.EquipmentLevel}:{r.FacilitiesLevel}:{r.DecorationLevel}:{r.StationCount}:{(r.GearLevels==null?"":string.Join(",",r.GearLevels))}"))+":"+
                string.Join(";",State.Construction.Select(c=>c.Id))+":"+
                string.Join(";",State.ReceptionDesks.Select(d=>$"{d.Id}:{d.EquipmentLevel}"))+":"+
                string.Join(";",State.TreatmentStations.Select(s=>$"{s.Id}:{s.EquipmentLevel}"))+":"+
                string.Join(";",State.ConsultationStations.Select(s=>$"{s.Id}:{s.EquipmentLevel}"))+":"+
                string.Join(";",State.PharmacyStations.Select(s=>$"{s.Id}:{s.EquipmentLevel}"))+":"+
                string.Join(";",State.Staff.Select(s=>$"{s.Id}:{s.TrainingLevel}"))+":"+
                string.Join(";",State.Amenities.Select(a=>$"{a.Kind}:{a.Level}"));
            if(key!=dockKey){dockKey=key;RebuildDock();}
            foreach(var update in readouts.ToArray())update();
            root.EnableInClassList("reduced-motion",ReducedMotion);
            UpdateGuide();
            UpdateWelcome();
            UpdateAccessibilityValues();
        }

        private void UpdateWorld(float delta)
        {
            var rect=board.worldBound;
            if(rect.width<5 || rect.height<5 || root.resolvedStyle.width<1)return;
            board.image=world.SetRenderSize(Mathf.CeilToInt(rect.width/root.resolvedStyle.width*Screen.width),
                Mathf.CeilToInt(rect.height/root.resolvedStyle.height*Screen.height));
            world.Render(State,delta,ReducedMotion);
            if (!saves.HasPendingOfflineProgress)
                clinicAudio.ObserveWorld(world.MovingActorCount, world.ActiveServiceCount, world.DoorOpeningCount, delta);
            else clinicAudio.ResetWorldObservation();
            UpdateWorldMarkers();
        }

        private void Select(ClinicRoom room)
        {
            if(!ClinicSelectionPolicy.CanSelectRoom(State,room))return;
            settingsOpen=false;gemsOpen=false;locationsOpen=false;selectedObject=null;selectedRoom=room;upgradeTrackRoom=null;world.SelectRoom(room);dockKey="";UpdateReadouts();
        }
        private void CloseContext()
        {
            selectedRoom=null;selectedObject=null;settingsOpen=false;gemsOpen=false;locationsOpen=false;world.SelectRoom(default(ClinicHit));dockKey="";UpdateReadouts();
        }

        private void Run(Func<ClinicCommandResult> action)
        {
            if(saves.HasPendingOfflineProgress){Notify("Saving your return first…");return;}
            var result=action();
            if(!result.Success){Notify(result.Message);return;}
            HandleEvents(simulation.DrainEvents());
            SaveNow();UpdateReadouts();
        }

        private void Collect(int desk)
        {
            Run(()=>simulation.Collect(desk));
        }

        private void HandleEvents(IEnumerable<ClinicEvent> events)
        {
            foreach(var e in events)
            {
                clinicAudio.PlayEvent(e.Kind);
                AnnounceEvent(e);
                if(e.Kind==ClinicEventKind.CashCollected&&saves.BoostActive(DateTimeOffset.UtcNow))
                {
                    // Double collections: the same amount again, paid as a reward. Commit, then bind to the saved profile.
                    var bonus=saves.GrantBoostBonus(e.Amount,DateTimeOffset.UtcNow);
                    if(bonus>0){profile=saves.Profile;BindSimulations();FloatText(CashPoint(e),"×2 +"+Money(bonus),"float-coins");}
                }
                if(e.Kind==ClinicEventKind.CashCollected)
                {
                    if(ClinicCashPresentation.IsVendingCollection(e))LaunchCoinsFrom(world.GetVendingCashPoint());
                    else if(ClinicCashPresentation.IsParkingCollection(e))LaunchCoinsFrom(world.GetParkingCashPoint());
                    else if(ClinicCashPresentation.IsTaxiCollection(e))LaunchCoinsFrom(world.GetTaxiCashPoint());
                    else if(ClinicCashPresentation.IsPharmacyCollection(e))LaunchCoinsFrom(world.GetPharmacyCashPoint(e.DeskId));
                    else LaunchCoins(e.DeskId,e.Amount);
                    Feedback(0);
                }
                else if(e.Kind==ClinicEventKind.TreatmentCompleted)Feedback(-1);
                else if(e.Kind==ClinicEventKind.NurseHired || e.Kind==ClinicEventKind.ReceptionistHired || e.Kind==ClinicEventKind.DoctorHired || e.Kind==ClinicEventKind.PharmacistHired || e.Kind==ClinicEventKind.EquipmentUpgraded || e.Kind==ClinicEventKind.StationAdded || e.Kind==ClinicEventKind.StaffTrained || e.Kind==ClinicEventKind.StationUpgraded || e.Kind==ClinicEventKind.AmenityUpgraded)
                    Feedback(1);
                else if(e.Kind==ClinicEventKind.ConstructionCompleted){Feedback(1);ShowRoomReady(e.Room);}
                else if(e.Kind==ClinicEventKind.WaitingRoomUnlocked)Notify("A waiting room is ready to build",5);
            }
        }

        private void Feedback(int haptic)
        {
            if(haptic>=0 && profile.preferences.haptics && apple!=null)apple.PlayHaptic(haptic);
        }

        private void OnViewportGeometryChanged(GeometryChangedEvent change)
        {
            if(change.oldRect.size!=change.newRect.size)CancelWorldGesture();
            ApplySafeArea();
        }

        private void ApplySafeArea()
        {
            if(root==null || header==null || Screen.width<1)return;
            var w=root.resolvedStyle.width;var h=root.resolvedStyle.height;
            if(float.IsNaN(w)||float.IsNaN(h))return;
            if(runtimePanel!=null)runtimePanel.scale=Screen.width/Math.Max(1,apple.ScreenWidthPoints);
            var safe=ClinicViewportLayout.SafePanelArea(new Vector2(w,h),new Vector2(Screen.width,Screen.height),Screen.safeArea);
            var top=safe.yMin;
            var bottom=h-safe.yMax;
            var dockArea=ClinicViewportLayout.DockArea(safe);
            var dockOpen=dock.style.display==DisplayStyle.Flex;
            // Gems, clinics and settings rise as a full-width sheet over a dimmed world; rooms and objects stay a floating card.
            var sheet=dockOpen&&(gemsOpen||locationsOpen||settingsOpen);
            header.style.left=safe.xMin+16;header.style.right=w-safe.xMax+16;
            header.style.top=top+12;hint.style.top=top+72;
            hint.style.left=safe.xMin+20;hint.style.right=w-safe.xMax+20;
            hudChips.style.left=safe.xMin+16;hudChips.style.top=top+76;
            hudChips.style.display=dockOpen?DisplayStyle.None:DisplayStyle.Flex;
            scrim.style.display=sheet?DisplayStyle.Flex:DisplayStyle.None;
            dock.EnableInClassList("sheet-dock",sheet);
            if(sheet)
            {
                var sheetWidth=Math.Min(560,w);
                dock.style.left=(w-sheetWidth)/2;dock.style.right=StyleKeyword.Auto;dock.style.width=sheetWidth;
                dock.style.bottom=0;dock.style.paddingBottom=bottom+20;
            }
            else
            {
                dock.style.left=dockArea.xMin;dock.style.right=StyleKeyword.Auto;dock.style.width=dockArea.width;
                dock.style.bottom=bottom+12;dock.style.paddingBottom=StyleKeyword.Null;
            }
            var guideWidth=Math.Min(520,safe.width-32);
            if(guideCard!=null){guideCard.style.bottom=bottom+16;guideCard.style.left=safe.xMin+(safe.width-guideWidth)/2;guideCard.style.width=guideWidth;}
            // The camera tools hide while a panel is open, so a panel may rise to just below the balances.
            LimitDockContent(sheet?h-(top+76)-bottom-20:h-bottom-12-(top+76));
            cameraTools.style.right=w-safe.xMax+16;
            cameraTools.style.display=dockOpen?DisplayStyle.None:DisplayStyle.Flex;
            var guideHeight=guideCard==null||guideCard.style.display==DisplayStyle.None?0:
                float.IsNaN(guideCard.resolvedStyle.height)||guideCard.resolvedStyle.height<1?84:guideCard.resolvedStyle.height;
            cameraTools.style.bottom=bottom+(guideHeight>0?guideHeight+32:20);
            LayoutWelcome(safe,top);
            toast.style.top=top+132;
            root.EnableInClassList("compact",w<370 || h<700);
            root.EnableInClassList("narrow",w<440);
        }

        private void LimitDockContent(float availableHeight)
        {
            // Heading, padding and border remain fixed while the one body scrolls. A room's heading carries a second line.
            var fixedHeadingAndInsets=dock.ClassListContains("room-dock")?92f:70f;
            // Docks with a tab row (Gems & goals) keep it fixed above the body too.
            var tabs=dock.Q(className:"gem-tabs");
            var tabHeight=tabs==null?0:float.IsNaN(tabs.resolvedStyle.height)||tabs.resolvedStyle.height<1?56:tabs.resolvedStyle.height+tabs.resolvedStyle.marginTop+tabs.resolvedStyle.marginBottom;
            dock.Query<ScrollView>(className:"bounded-dock-content").ForEach(body=>
                body.style.maxHeight=Mathf.Max(44,availableHeight-fixedHeadingAndInsets-tabHeight));
        }

        private void Notify(string message,double seconds=4)
        {
            if(toast==null)return;
            toast.text=message;toastUntil=Time.unscaledTimeAsDouble+seconds;toast.style.display=DisplayStyle.Flex;
        }
        private void SaveNow()
        {
            if(saves==null || saves.HasPendingOfflineProgress)return;
            if(!saves.Save(profile,DateTimeOffset.UtcNow))Notify(saves.Error??"Your progress could not be saved",8);
        }
        private void BindSimulations()
        {
            simulation = new ClinicSimulation(profile.ActiveState);
            var other = profile.activeLocation == ClinicLocation.StarterClinic ? profile.doctorsState : profile.state;
            otherSimulation = other == null ? null : new ClinicSimulation(other);
            var builders = saves?.ConstructionSlots ?? 1;
            simulation.ConstructionSlots = builders;
            if (otherSimulation != null) otherSimulation.ConstructionSlots = builders;
        }

        private void TryOpenDoctorsClinic()
        {
            if (!saves.OpenDoctorsClinic(DateTimeOffset.UtcNow)) { Notify(saves.Error, 7); return; }
            PublishLocation();
            clinicAudio.PlayEvent(ClinicEventKind.DoctorsClinicUnlocked);
            Feedback(1);
            Notify("Welcome to your doctors’ clinic", 5);
        }

        private void TrySelectLocation(ClinicLocation destination)
        {
            if (!saves.SelectLocation(destination, DateTimeOffset.UtcNow)) { Notify(saves.Error, 7); return; }
            PublishLocation();
        }

        private void PublishLocation()
        {
            CancelWorldGesture();
            profile = saves.Profile;
            BindSimulations();
            world.ConfigureLocation(profile.activeLocation);
            clinicAudio.ResetWorldObservation();
            ResetLocationPresentation();
            skipNextDelta = true;
            saveClock = 0;
            RefreshAudioPreferences();
            UpdateReadouts();
        }

        private void RefreshAudioPreferences()
        {
            if (profile != null && clinicAudio != null) clinicAudio.ApplyPreferences(profile.preferences);
        }

        private void ResumeClinic()
        {
            var report=saves.ApplyOffline(DateTimeOffset.UtcNow);profile=saves.Profile;BindSimulations();
            world.ConfigureLocation(profile.activeLocation);world.ResetActorPlacement();RefreshAudioPreferences();
            skipNextDelta=true;
            if(report.applied && report.tillEarned>0)ShowWelcome(report);
            if(!string.IsNullOrEmpty(saves.Error))Notify(saves.Error,10);
            dockKey="";if(ready)UpdateReadouts();
        }
        private void AppleChanged()
        {
            CheckPurchasedUnlocks();
            if(!ready||!restoreRequested)return;
            if(apple.IsRestoring){restoreObservedBusy=true;return;}
#if UNITY_IOS && !UNITY_EDITOR
            if(!restoreObservedBusy)return;
#endif
            restoreRequested=false;
            Notify(apple.IsPassOwned?"Your existing purchase is restored":
                string.IsNullOrEmpty(apple.StoreStatus)?"No existing purchase was found":apple.StoreStatus,7);
        }
        private void RequestRestore()
        {
            if(restoreRequested||apple.IsRestoring)return;
            restoreObservedBusy=false;restoreRequested=true;
            Notify("Checking your existing purchases");apple.RestorePurchases();
        }
        private void OnApplicationPause(bool paused)
        {
            CancelWorldGesture();
            if(!ready)return;
            if(paused&&!inactive)SaveNow();
            var wasInactive=inactive;inactive=paused;
            clinicAudio.SetPaused(paused);
            if(!paused&&wasInactive)ResumeClinic();
        }
        private void OnApplicationFocus(bool focus){if(!focus)CancelWorldGesture();}
        private void OnApplicationQuit(){if(ready&&!inactive)SaveNow();}
        private void OnDestroy()
        {
            DisposeAccessibility();
            if(apple!=null){apple.StateChanged-=AppleChanged;apple.GemTransactionReceived-=OnGemTransaction;}
            if(world!=null)Destroy(world.gameObject);
            if(backdrop!=null)Destroy(backdrop.gameObject);
            if(runtimePanel!=null)Destroy(runtimePanel);
        }

        private string OfflineMessage(ClinicOfflineReport report)
            => report.coinCapped ? "Your tills filled up while you were away: "+Money(report.tillEarned)+" ready to collect. Hold more with offline upgrades in the shop."
                : report.wasCapped ? "While away: "+Money(report.tillEarned)+" ready to collect ("+report.limitHours+"-hour limit)."
                : "While away: "+Money(report.tillEarned)+" ready to collect";
        private double displayedWallet=-1;
        /// <summary>The wallet counts toward its new value, so earning and spending read as movement.</summary>
        private string WalletText()
        {
            var target=(double)State.Wallet;
            if(displayedWallet<0||ReducedMotion||Math.Abs(target-displayedWallet)<1)displayedWallet=target;
            else displayedWallet+=(target-displayedWallet)*.45;
            return Money((long)Math.Round(displayedWallet));
        }
        private static string Money(long value)
        {
            if(value>=1000000000)return (value/1000000000d).ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+"B";
            if(value>=1000000)return (value/1000000d).ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+"M";
            if(value>=10000)return (value/1000d).ToString("0.#",System.Globalization.CultureInfo.InvariantCulture)+"K";
            return value.ToString("N0",System.Globalization.CultureInfo.InvariantCulture);
        }
        private static string RoomName(ClinicRoom room)=>room==ClinicRoom.Reception?"Reception":room==ClinicRoom.FirstAid?"First aid":room==ClinicRoom.Consultation?"Consultations":room==ClinicRoom.Pharmacy?"Pharmacy"
            :room==ClinicRoom.Office?"Office":room==ClinicRoom.StaffRoom?"Staff room":room==ClinicRoom.Store?"Store":"Waiting room";
        private static string TimeLabel(double seconds)
        {
            if(seconds<60)return Math.Ceiling(Math.Max(0,seconds))+"s";
            var minutes=(long)Math.Ceiling(seconds/60);
            if(minutes<60)return minutes+"m";
            return minutes%60==0?minutes/60+"h":minutes/60+"h "+minutes%60+"m";
        }
        private static VisualElement Box(VisualElement parent,string classes)
        {
            var box=new VisualElement();foreach(var name in classes.Split(' '))box.AddToClassList(name);parent.Add(box);return box;
        }
        /// <summary>The clinic draws its own buttons; the default theme's grey fill, border and pressed tint would fight every colour here.</summary>
        private static void Unstyle(VisualElement scope)
        {
            if(scope!=null)scope.Query<Button>().ForEach(button=>button.RemoveFromClassList(Button.ussClassName));
        }
        /// <summary>Numbers and headings use the rounded display face once it has loaded.</summary>
        private Label Display(Label label)
        {
            if(displayFont!=null)label.style.unityFontDefinition=FontDefinition.FromFont(displayFont);
            return label;
        }
        private Label Text(VisualElement parent,string value,string classes,bool bold=false)
        {
            var label=new Label(value);foreach(var name in classes.Split(' '))label.AddToClassList(name);
            label.pickingMode=PickingMode.Ignore;
            if(bold && boldFont!=null)label.style.unityFontDefinition=FontDefinition.FromFont(boldFont);
            parent.Add(label);return label;
        }
        private Button IconButton(VisualElement parent,ClinicGlyph glyph,string label,Action action,string classes,Func<string> value=null)
        {
            Action feedbackAction=()=>{ clinicAudio.PlayTap(); action(); };
            var button=new Button(feedbackAction){tooltip=label,name=label.ToLowerInvariant().Replace(' ','-')};
            foreach(var name in classes.Split(' '))button.AddToClassList(name);
            button.Add(new ClinicIcon(glyph));parent.Add(button);RegisterAccessibleButton(button,label,feedbackAction,value);return button;
        }
    }
}
