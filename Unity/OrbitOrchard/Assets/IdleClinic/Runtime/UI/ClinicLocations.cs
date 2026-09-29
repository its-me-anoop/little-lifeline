using System;
using System.Linq;
using IdleClinic.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdleClinic.App
{
    /// <summary>Read-only travel eligibility; the profile transaction repeats every gate before saving.</summary>
    public static class ClinicLocationReadout
    {
        public static bool CanOpen(ClinicState starter,long wallet,bool alreadyOpen)
            =>!alreadyOpen&&starter!=null&&wallet>=ClinicRules.DoctorsClinicUnlockCost&&ClinicRules.StarterCompletion(starter).Count==0;
        public static string Name(ClinicLocation location)=>location==ClinicLocation.DoctorsClinic?"Doctors clinic":"Starter clinic";
        public static string Income(ClinicLocation location)=>location==ClinicLocation.DoctorsClinic?"2× income":"1× income";
    }

    public sealed partial class ClinicApp
    {
        private bool locationsOpen;
        private Button locationControl;
        private Label locationName,locationMultiplier;

        /// <summary>A labelled chip says where you are and what it earns; tap it to travel.</summary>
        private void BuildLocationControl(VisualElement parent)
        {
            locationControl=IconButton(parent,ClinicGlyph.Locations,"Locations",ToggleLocations,"hud-chip location-control",
                ()=>ClinicLocationReadout.Name(State.Location)+", "+ClinicLocationReadout.Income(State.Location));
            locationControl.name="clinic-locations";
            locationName=Text(locationControl,"Starter clinic","chip-label",true);
            locationMultiplier=Text(locationControl,"1×","location-multiplier",true);
            locationControl.Add(new ClinicIcon(ClinicGlyph.Chevron,14){name="location-chevron"});
            locationControl.Q<ClinicIcon>("location-chevron").style.rotate=new Rotate(90);
        }
        private void UpdateLocationReadouts()
        {
            if(locationControl==null)return;
            locationControl.style.display=State.Tutorial==ClinicTutorialStep.Complete?DisplayStyle.Flex:DisplayStyle.None;
            locationName.text=ClinicLocationReadout.Name(State.Location);
            locationMultiplier.text=State.Location==ClinicLocation.DoctorsClinic?"2×":"1×";
        }
        private void ToggleLocations()
        {
            locationsOpen=!locationsOpen;selectedRoom=null;selectedObject=null;settingsOpen=false;gemsOpen=false;
            world.SelectRoom(default(IdleClinic.Presentation.ClinicHit));dockKey="";UpdateReadouts();
        }
        private void BuildLocationsDock()
        {
            dock.AddToClassList("locations-dock");
            var body=new ScrollView(ScrollViewMode.Vertical){name="clinic-locations-content",horizontalScrollerVisibility=ScrollerVisibility.Hidden};
            body.AddToClassList("bounded-dock-content");dock.Add(body);
            BindTouchCaptureLifecycle(body.contentContainer);BindTouchCaptureLifecycle(body.contentViewport);
            var unlocked=profile.doctorsState!=null;
            Text(body,"Both clinics keep caring while you travel.","room-detail");
            if(unlocked)
            {
                var row=Box(body,"location-destinations");
                LocationDestination(row,ClinicLocation.StarterClinic,ClinicGlyph.Home);
                LocationDestination(row,ClinicLocation.DoctorsClinic,ClinicGlyph.Doctor);
                return;
            }
            var starter=profile.state;
            var remaining=ClinicRules.StarterCompletion(starter).Count;
            // Where you are now, then what the next clinic takes and how close you are.
            var here=Box(body,"clinic-card clinic-card-current");
            var hereTile=Box(here,"icon-tile");hereTile.Add(new ClinicIcon(ClinicGlyph.Home,28,LeafInk));
            var hereWords=Box(here,"location-preview-copy");
            Display(Text(hereWords,"Starter clinic","location-name",true));
            Text(hereWords,"Reception, first aid, waiting room · 1× income","location-note");
            Text(here,"Here","here-pill",true);
            var next=Box(body,"clinic-card");
            var top=Box(next,"location-preview");
            var tile=Box(top,"icon-tile");tile.Add(new ClinicIcon(ClinicGlyph.Doctor,28,LeafInk));
            var lockBadge=Box(tile,"lock-badge");lockBadge.Add(new ClinicIcon(ClinicGlyph.Lock,16));
            var words=Box(top,"location-preview-copy");
            Display(Text(words,"Doctors clinic","location-name",true));
            Text(words,"2× income · 4 doctors · pharmacy","location-note");
            if(remaining>0)
            {
                Text(next,remaining+" improvements left to unlock","location-progress",true);
                var checklist=Box(next,"location-checklist");checklist.name="clinic-unlock-checklist";
                foreach(var room in starter.Rooms)
                {
                    var kind=room.Kind;
                    var complete=room.Built&&room.Tier==ClinicRules.MaximumTier(starter)
                        &&room.EquipmentLevel==6&&room.FacilitiesLevel==6&&room.DecorationLevel==6
                        &&!starter.Construction.Any(c=>c.Room==kind);
                    UnlockRequirement(checklist,RoomName(kind),complete,()=>Select(kind));
                }
                foreach(var role in new[]{ClinicStaffRole.Receptionist,ClinicStaffRole.Nurse})
                {
                    var capturedRole=role;
                    for(var station=0;station<ClinicRules.MaximumStaff(starter,role);station++)
                    {
                        var id=station;
                        var staff=starter.Staff.FirstOrDefault(s=>s.Role==role&&s.StationId==id);
                        var exists=StationIds(starter,role).Contains(id);
                        var complete=exists&&ClinicRules.StationLevel(starter,role,id)==6&&staff!=null&&staff.TrainingLevel==6;
                        UnlockRequirement(checklist,(role==ClinicStaffRole.Receptionist?"Reception":"Nursing")+" team "+(id+1),complete,
                            ()=>{if(exists)SelectObject(StationHit(capturedRole,id));else Select(ClinicRules.RoomForRole(capturedRole));});
                    }
                }
                foreach(var kind in new[]{ClinicAmenity.Parking,ClinicAmenity.Toilet,ClinicAmenity.Vending})
                {
                    var amenity=kind;
                    UnlockRequirement(checklist,kind==ClinicAmenity.Parking?"Car park":kind==ClinicAmenity.Toilet?"Toilet":"Vending machine",
                        starter.Amenity(kind).Level==ClinicRules.MaximumAmenityLevel(starter,kind),
                        ()=>{if(amenity==ClinicAmenity.Parking||starter.Room(ClinicRoom.Waiting).Built)SelectObject(AmenityHit(amenity));else Select(ClinicRoom.Waiting);});
                }
            }
            else Text(next,"Every improvement is done.","location-progress",true);
            // The coin requirement shows the gap as well as the goal.
            var coinsRow=Box(next,"coin-requirement");
            var coinIcon=new ClinicIcon(ClinicGlyph.Coin,22,CoinInk);coinsRow.Add(coinIcon);
            Text(coinsRow,ClinicRules.DoctorsClinicUnlockCost.ToString("N0",System.Globalization.CultureInfo.InvariantCulture)+" coins","unlock-name");
            var have=Text(coinsRow,"","unlock-status",true);
            var bar=Box(next,"meter meter-gold");var fill=Box(bar,"meter-fill");
            var open=IconButton(next,ClinicGlyph.Locations,"Open doctors clinic for 100,000 coins",()=>TryOpenDoctorsClinic(),"location-open primary-action");
            open.name="open-doctors-clinic";
            var purchaseWords=Box(open,"location-purchase-copy");
            Text(purchaseWords,remaining==0?"Open doctors clinic":"Complete the starter clinic","purchase-detail",true);
            var cost=Box(purchaseWords,"cost-row");cost.Add(new ClinicIcon(ClinicGlyph.Coin,17,CoinInk));Text(cost,"100,000","purchase-price",true);
            var need=Text(open,"","need-more",true);
            readouts.Add(()=>
            {
                var canOpen=ClinicLocationReadout.CanOpen(profile.state,State.Wallet,profile.doctorsState!=null);
                open.SetEnabled(canOpen);
                var wallet=Math.Min(State.Wallet,ClinicRules.DoctorsClinicUnlockCost);
                have.text=Money(State.Wallet);
                fill.style.width=Length.Percent(100f*wallet/ClinicRules.DoctorsClinicUnlockCost);
                need.text=State.Wallet>=ClinicRules.DoctorsClinicUnlockCost?"":"Need "+Money(ClinicRules.DoctorsClinicUnlockCost-State.Wallet)+" more";
            });
        }
        private void LocationDestination(VisualElement parent,ClinicLocation location,ClinicGlyph glyph)
        {
            var current=State.Location==location;
            var button=IconButton(parent,glyph,"Travel to "+ClinicLocationReadout.Name(location),()=>TrySelectLocation(location),"location-destination");
            button.name=location==ClinicLocation.StarterClinic?"travel-starter-clinic":"travel-doctors-clinic";
            Text(button,location==ClinicLocation.StarterClinic?"Starter clinic":"Doctors clinic","location-name",true);
            Text(button,ClinicLocationReadout.Income(location),"location-note");
            if(current)Text(button,"Here","here-pill",true);
            button.EnableInClassList("location-current",current);
            button.SetEnabled(!current);
        }
        private void UnlockRequirement(VisualElement parent,string name,bool complete,Action action)
        {
            var row=IconButton(parent,complete?ClinicGlyph.Check:ClinicGlyph.Upgrade,name+(complete?", complete":", improvements needed"),action,"unlock-requirement");
            Text(row,name,"unlock-name");
            Text(row,complete?"Done":"Improve","unlock-status",true);
            row.EnableInClassList("requirement-complete",complete);
        }

        // Called only after the complete campaign switch is saved and the active world is rebound.
        private void ResetLocationPresentation()
        {
            CancelWorldGesture();
            selectedRoom=null;selectedObject=null;settingsOpen=false;gemsOpen=false;locationsOpen=false;dockKey="";
            lastTutorial=State.Tutorial;wideWorldMarkers=false;
            ClearLocationMarkersAndFlights();
            world.Home(ReducedMotion);
            UpdateReadouts();UpdateAccessibilityFrames();
        }
        private void ClearLocationMarkersAndFlights()
        {
            foreach(var flight in flights){flight.Icon.RemoveFromHierarchy();coinPool.Push(flight.Icon);}flights.Clear();
            walletPulseUntil=0;if(wallet!=null)wallet.style.scale=new Scale(Vector3.one);
            foreach(var marker in cashMarkers.Values)marker.RemoveFromHierarchy();cashMarkers.Clear();
            foreach(var marker in patientRings.Values)marker.RemoveFromHierarchy();patientRings.Clear();
            foreach(var marker in constructionMarkers.Values)marker.RemoveFromHierarchy();constructionMarkers.Clear();
            foreach(var marker in roomTargets.Values)marker.RemoveFromHierarchy();roomTargets.Clear();
            foreach(var marker in objectTargets.Values)marker.RemoveFromHierarchy();objectTargets.Clear();objectHits.Clear();
            waitingMarker?.RemoveFromHierarchy();waitingMarker=null;
            vendingCashMarker?.RemoveFromHierarchy();vendingCashMarker=null;
            parkingCashMarker?.RemoveFromHierarchy();parkingCashMarker=null;
        }
    }
}
