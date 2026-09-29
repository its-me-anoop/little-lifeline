using System;
using System.Linq;
using IdleClinic.Core;
using IdleClinic.Services;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdleClinic.App
{
    public sealed partial class ClinicApp
    {
        // The room panel shows one upgrade track at a time; it opens on the track the guide points at.
        private UpgradeTrack upgradeTrack = UpgradeTrack.Equipment;
        private ClinicRoom? upgradeTrackRoom;

        private void RebuildDock()
        {
            BuildDockContent();
            Unstyle(dock);
        }

        private void BuildDockContent()
        {
            dock.Clear();readouts.Clear();
            dock.RemoveFromClassList("upgrade-dock");dock.RemoveFromClassList("management-dock");dock.RemoveFromClassList("locations-dock");dock.RemoveFromClassList("room-dock");
            if(!gemsOpen&&!locationsOpen&&!settingsOpen && !selectedRoom.HasValue&&!selectedObject.HasValue){dock.style.display=DisplayStyle.None;ApplySafeArea();return;}
            dock.style.display=DisplayStyle.Flex;
            Box(dock,"dock-handle").pickingMode=PickingMode.Ignore;
            var heading=Box(dock,"dock-heading");
            var titles=Box(heading,"dock-titles");titles.pickingMode=PickingMode.Ignore;
            var title=Text(titles,gemsOpen?"Gems & goals":locationsOpen?"Your clinics":settingsOpen?"Settings":selectedObject.HasValue?ObjectName(selectedObject.Value):RoomName(selectedRoom.Value),"dock-title",true);
            Display(title);
            var roomPanel=!gemsOpen&&!locationsOpen&&!settingsOpen&&!selectedObject.HasValue;
            if(roomPanel&&State.Tutorial==ClinicTutorialStep.Complete&&State.Room(selectedRoom.Value).Built)BuildRoomMeta(titles,State.Room(selectedRoom.Value));
            if(roomPanel&&State.Tutorial==ClinicTutorialStep.Complete)BuildRoomShortcuts(heading,selectedRoom.Value);
            IconButton(heading,ClinicGlyph.Close,"Close controls",CloseContext,"round-control close-control");
            if(gemsOpen){BuildGemsDock();ApplySafeArea();return;}
            if(locationsOpen){BuildLocationsDock();ApplySafeArea();return;}
            if(settingsOpen){BuildSettings();ApplySafeArea();return;}
            if(selectedObject.HasValue){BuildManagementDock(selectedObject.Value);ApplySafeArea();return;}
            var room=State.Room(selectedRoom.Value);
            if(!room.Built)
            {
                var job=State.Construction.FirstOrDefault(c=>c.Room==ClinicRoom.Waiting);
                if(job!=null){BuildConstructionReadout(job);ApplySafeArea();return;}
                Text(dock,State.WaitingRoomUnlocked?"Four seats. A softer wait.":"Opens when two paid patients are waiting","room-detail");
                var line=Box(dock,"action-row");
                Purchase(line,ClinicGlyph.Chair,"Build waiting room",ClinicRules.WaitingRoomCost,
                    ()=>State.WaitingRoomUnlocked && State.Tutorial==ClinicTutorialStep.Complete,
                    ()=>simulation.BuildWaitingRoom(),"4 seats · 20s","primary-action");
                ApplySafeArea();return;
            }
            if(State.Tutorial!=ClinicTutorialStep.Complete)
            {
                if(State.Tutorial==ClinicTutorialStep.HireFirstNurse && room.Kind==ClinicRoom.FirstAid)
                {
                    Text(dock,"Your first patient is ready for care","room-detail");
                    var row=Box(dock,"action-row");
                    Purchase(row,ClinicGlyph.Nurse,"Hire first nurse",50,()=>true,()=>simulation.HireNurse(),"First aid","primary-action");
                }
                else Text(dock,State.Tutorial==ClinicTutorialStep.CollectFirstPayment?"Collect the coins on the counter":
                    State.Tutorial==ClinicTutorialStep.FirstTreatment?"Watch your nurse help the first patient":"Your first patient is on the way","room-detail");
                ApplySafeArea();return;
            }
            dock.AddToClassList("room-dock");
            if(upgradeTrackRoom!=room.Kind){upgradeTrackRoom=room.Kind;upgradeTrack=GuideTrack(room.Kind)??UpgradeTrack.Equipment;}
            var body=new ScrollView(ScrollViewMode.Vertical){name="clinic-room-content",horizontalScrollerVisibility=ScrollerVisibility.Hidden,verticalScrollerVisibility=ScrollerVisibility.Hidden};
            body.AddToClassList("bounded-dock-content");dock.Add(body);
            BindTouchCaptureLifecycle(body.contentContainer);BindTouchCaptureLifecycle(body.contentViewport);
            var tabs=Box(body,"segmented track-tabs");
            BuildTrackTab(tabs,room,UpgradeTrack.Equipment,ClinicGlyph.Equipment);
            BuildTrackTab(tabs,room,UpgradeTrack.Facilities,ClinicGlyph.Facility);
            BuildTrackTab(tabs,room,UpgradeTrack.Decoration,ClinicGlyph.Plant);
            BuildUpgrade(body,room,upgradeTrack);
            var construction=State.Construction.FirstOrDefault(c=>c.Room==room.Kind);
            if(construction!=null)BuildConstructionReadout(construction,body);
            else if(room.Tier<ClinicRules.MaximumTier(State)&&simulation.BuildersBusy)BuildBuilderBusy(body);
            else if(room.Tier<ClinicRules.MaximumTier(State))BuildRenovation(body,room);
            else Text(body,"Room complete. Every size is built.","max-room");
            var actions=Box(body,"action-row");
            BuildStaffAction(actions,room);
            if(actions.childCount==0)actions.RemoveFromHierarchy();
            ApplySafeArea();
        }

        private static string TrackName(UpgradeTrack track)=>track==UpgradeTrack.Equipment?"Equipment":track==UpgradeTrack.Facilities?"Facilities":"Decor";

        /// <summary>"Room 2 of 3" and the upgrade limit it allows, under the room's name. While building it reads "Room 2 → 3".</summary>
        private void BuildRoomMeta(VisualElement parent,ClinicRoomState room)
        {
            var meta=Box(parent,"dock-meta");meta.pickingMode=PickingMode.Ignore;
            var building=State.Construction.Any(c=>c.Room==room.Kind);
            var chip=Text(meta,building?"Room "+room.Tier+" → "+(room.Tier+1):"Room "+room.Tier+" of "+ClinicRules.MaximumTier(State),"meta-chip",true);
            chip.EnableInClassList("meta-chip-building",building);
            Text(meta,"Upgrade limit "+ClinicRules.ComponentCap(State,room.Kind),"meta-note");
        }

        private UpgradeTrack? GuideTrack(ClinicRoom room)
        {
            var control=CurrentGuide()?.Control;
            var prefix="upgrade-"+room.ToString().ToLowerInvariant()+"-";
            if(control==null||!control.StartsWith(prefix,StringComparison.Ordinal))return null;
            foreach(UpgradeTrack track in Enum.GetValues(typeof(UpgradeTrack)))
                if(control==prefix+track.ToString().ToLowerInvariant())return track;
            return null;
        }

        private void BuildTrackTab(VisualElement parent,ClinicRoomState room,UpgradeTrack track,ClinicGlyph glyph)
        {
            var level=room.Level(track);var cap=ClinicRules.ComponentCap(State,room.Kind);
            var selected=track==upgradeTrack;
            var tab=IconButton(parent,glyph,TrackName(track)+", level "+level+" of "+cap,()=>{upgradeTrack=track;dockKey="";UpdateReadouts();},"segment track-tab");
            tab.name="upgrade-tab-"+room.Kind.ToString().ToLowerInvariant()+"-"+track.ToString().ToLowerInvariant();
            tab.EnableInClassList("segment-selected",selected);
            var icon=tab.Q<ClinicIcon>();icon.style.width=18;icon.style.height=18;
            Text(tab,TrackName(track),"segment-label",true);
            var levelLabel=Display(Text(tab,level+"/"+cap,"level-pill",true));
            levelLabel.EnableInClassList("level-pill-full",level>=cap);
        }

        private void BuildStaffAction(VisualElement actions,ClinicRoomState room)
        {
            var role=RoomRole(room.Kind);if(!role.HasValue)return;
            var staff=State.Staff.Count(s=>s.Role==role.Value);
            var stations=ClinicRules.StationCount(State,role.Value);
            if(staff>=ClinicRules.MaximumStaff(State,role.Value))return;
            if(role.Value==ClinicStaffRole.Receptionist||staff<stations)
            {
                Purchase(actions,RoleGlyph(role.Value),"Hire "+RoleName(role.Value),ClinicRules.HireCost(State,role.Value),()=>true,
                    ()=>simulation.HireStaff(role.Value),"+1 "+RoleName(role.Value),"minor-action");
                return;
            }
            if(stations<ClinicRules.StationCap(State,role.Value))
                Purchase(actions,RoleGlyph(role.Value),"Add "+(role.Value==ClinicStaffRole.Doctor?"consultation room":role.Value==ClinicStaffRole.Pharmacist?"pharmacy counter":"treatment station"),
                    ClinicRules.AddStationCost(State,role.Value),()=>true,()=>simulation.AddStation(role.Value),"+1 station","minor-action");
            else if(room.Tier<ClinicRules.MaximumTier(State))
            {
                var next=IconButton(actions,RoleGlyph(role.Value),"Renovate to add another "+RoleName(role.Value),()=>FocusRenovation(room),"room-shortcut");
                Text(next,"Room "+(room.Tier+1)+" · +1 station","purchase-detail");
            }
        }

        /// <summary>The selected track: what the next level adds, the change it makes, how far the room can go, and the price.</summary>
        private void BuildUpgrade(VisualElement parent,ClinicRoomState room,UpgradeTrack track)
        {
            var level=room.Level(track);var cap=ClinicRules.ComponentCap(State,room.Kind);var capped=level>=cap;
            var maxTier=ClinicRules.MaximumTier(State);var nextRoom=room.Tier<maxTier;
            var building=State.Construction.Any(c=>c.Room==room.Kind);
            var price=ClinicRules.UpgradeCost(State,room.Kind,track);
            var panel=Box(parent,"track-panel");
            var intro=Box(panel,"track-intro");intro.pickingMode=PickingMode.Ignore;
            var tile=Box(intro,"icon-tile icon-tile-large");tile.pickingMode=PickingMode.Ignore;
            tile.Add(new ClinicIcon(track==UpgradeTrack.Equipment?ClinicGlyph.Equipment:track==UpgradeTrack.Facilities?ClinicGlyph.Facility:ClinicGlyph.Plant,34,LeafInk));
            var words=Box(intro,"track-words");words.pickingMode=PickingMode.Ignore;
            Text(words,(capped?(nextRoom?"ROOM "+(room.Tier+1)+" ADDS":"FULLY IMPROVED"):"LEVEL "+(level+1)+" ADDS"),"eyebrow",true);
            Display(Text(words,TrackHeadline(room.Kind,track),"track-title",true));
            Text(words,TrackDescription(room.Kind,track),"track-description");

            var effect=Box(panel,"effect-row");effect.pickingMode=PickingMode.Ignore;
            Text(effect,EffectLabel(room.Kind,track),"effect-label",true);
            var values=Box(effect,"effect-values");values.pickingMode=PickingMode.Ignore;
            var before=EffectBefore(room,track);
            if(before!=null)
            {
                Display(Text(values,before,"effect-before",true));
                values.Add(new ClinicIcon(ClinicGlyph.Arrow,14,new Color(.34f,.41f,.35f)));
            }
            Display(Text(values,capped&&!nextRoom?"Max":NextBenefit(room,track),"effect-after",true));

            // One pip per level this room can ever reach: filled, next, open now, or waiting on a bigger room.
            var most=ClinicRules.TrackCap(maxTier);
            var pips=Box(panel,"level-pips");pips.pickingMode=PickingMode.Ignore;
            pips.tooltip="Level "+level+" of "+cap+" in this room"+(cap<most?". Levels "+(cap+1)+" to "+most+" open with bigger rooms.":".");
            for(var l=2;l<=most;l++)
            {
                var pip=Box(pips,"level-pip");pip.pickingMode=PickingMode.Ignore;
                pip.EnableInClassList("pip-have",l<=level);
                pip.EnableInClassList("pip-next",l==level+1&&l<=cap);
                pip.EnableInClassList("pip-locked",l>cap);
            }

            var controlName="upgrade-"+room.Kind.ToString().ToLowerInvariant()+"-"+track.ToString().ToLowerInvariant();
            Action activate=()=>{if(capped)FocusRenovation(room);else Run(()=>simulation.Upgrade(room.Kind,track));};
            var button=new Button(activate){name=controlName};
            button.AddToClassList("track-action");
            button.tooltip=TrackName(track)+" level "+level+". "+(capped?(nextRoom?"Requires room "+(room.Tier+1):"Fully improved"):
                Money(price)+" coins. "+UpgradeBenefit(room.Kind,track));
            if(capped)
            {
                button.AddToClassList("blocked-action");
                button.Add(new ClinicIcon(nextRoom?ClinicGlyph.Lock:ClinicGlyph.Check,18));
                Text(button,!nextRoom?"Fully improved":building?"Opens when the renovation finishes":"Renovate to room "+(room.Tier+1)+" first","blocked-label",true);
            }
            else
            {
                button.AddToClassList("primary-action");
                Text(button,"Upgrade "+TrackName(track).ToLowerInvariant(),"action-label",true);
                button.Add(new ClinicIcon(ClinicGlyph.Coin,18,new Color(.98f,.86f,.45f)));
                Display(Text(button,Money(price),"action-price",true));
                readouts.Add(()=>button.SetEnabled(State.Wallet>=price));
            }
            panel.Add(button);
            RegisterAccessibleButton(button,button.tooltip,activate);
        }

        private string TrackHeadline(ClinicRoom room,UpgradeTrack track)
        {
            if(track==UpgradeTrack.Equipment)return room==ClinicRoom.Waiting?"Faster calls":UpgradeBenefit(room,track);
            if(track==UpgradeTrack.Decoration)return "A warmer welcome";
            return room==ClinicRoom.Waiting?"Two more seats":room==ClinicRoom.Reception?"One more queue place":"Better care";
        }
        private static string TrackDescription(ClinicRoom room,UpgradeTrack track)
        {
            if(track==UpgradeTrack.Equipment)return room==ClinicRoom.Waiting?"Better equipment by the seats. Patients answer the call sooner."
                :"Better equipment at every station. Staff serve each patient faster.";
            if(track==UpgradeTrack.Decoration)return "A more welcoming room raises every visit fee.";
            return room==ClinicRoom.Waiting?"More paid patients wait inside, not out the door."
                :room==ClinicRoom.Reception?"One more patient can queue before new arrivals turn away."
                :"Improved care raises the visit fee.";
        }
        private static string EffectLabel(ClinicRoom room,UpgradeTrack track)
        {
            if(track==UpgradeTrack.Equipment)return room==ClinicRoom.Waiting?"Call from a seat":room==ClinicRoom.Reception?"Check-in time"
                :room==ClinicRoom.Consultation?"Consultation time":room==ClinicRoom.Pharmacy?"Dispensing time":"Treatment time";
            if(track==UpgradeTrack.Facilities)return room==ClinicRoom.Waiting?"Seats":room==ClinicRoom.Reception?"Queue places":"Care fee bonus";
            return "Decor fee bonus";
        }
        /// <summary>The current value, where the rules can state it; fee bonuses show only the gain.</summary>
        private string EffectBefore(ClinicRoomState room,UpgradeTrack track)
        {
            var inv=System.Globalization.CultureInfo.InvariantCulture;
            if(track==UpgradeTrack.Equipment)return ServiceRange(room,0);
            if(track==UpgradeTrack.Facilities&&room.Kind==ClinicRoom.Waiting)return ClinicRules.WaitingCapacity(State).ToString(inv);
            if(track==UpgradeTrack.Facilities&&room.Kind==ClinicRoom.Reception)return ClinicRules.UnpaidQueueCapacity(State).ToString(inv);
            return null;
        }

        private void FocusRenovation(ClinicRoomState room)
        {
            dock.Q<Button>("expand-room")?.Focus();
            Notify(room.Tier<ClinicRules.MaximumTier(State)?"Room "+(room.Tier+1)+" unlocks more improvements":"Fully improved");
        }

        private string NextBenefit(ClinicRoomState room,UpgradeTrack track)
        {
            if(track==UpgradeTrack.Decoration)return "+5% fee";
            if(track==UpgradeTrack.Facilities)return room.Kind==ClinicRoom.Waiting?"+2 seats":room.Kind==ClinicRoom.Reception?"+1 place":room.Kind==ClinicRoom.Consultation||room.Kind==ClinicRoom.Pharmacy?"+10% fee":"+15% fee";
            return ServiceRange(room,1)??"Faster service";
        }

        private string ServiceRange(ClinicRoomState room,int levelsAdded)
        {
            if(room.Kind==ClinicRoom.Waiting)return ServiceTime(ClinicRules.WaitingCallTicks(State,equipmentLevelsAdded:levelsAdded));
            var role=RoomRole(room.Kind)??ClinicStaffRole.Nurse;
            var ids=StationIds(State,role);
            var times=ids.Select(id=>ClinicRules.StationServiceTicks(State,role,id,roomEquipmentLevelsAdded:levelsAdded)).OrderBy(t=>t).ToArray();
            if(times.Length==0)return null;
            return times.First()==times.Last()?ServiceTime(times.First()):ServiceTime(times.First()).TrimEnd('s')+"–"+ServiceTime(times.Last());
        }

        private static string UpgradeBenefit(ClinicRoom room,UpgradeTrack track)
        {
            if(track==UpgradeTrack.Decoration)return "More welcoming furnishings increase the visit fee.";
            if(track==UpgradeTrack.Equipment)return room==ClinicRoom.FirstAid?"Faster first aid":room==ClinicRoom.Reception?"Faster check-in":room==ClinicRoom.Consultation?"Faster consultations":room==ClinicRoom.Pharmacy?"Faster dispensing":"Faster calls from the waiting room";
            return room==ClinicRoom.Waiting?"Two more seats":room==ClinicRoom.Reception?"One more queue place":"Improved care increases the visit fee";
        }

        private Button Purchase(VisualElement parent,ClinicGlyph glyph,string label,long price,Func<bool> unlocked,
            Func<ClinicCommandResult> action,string detail,string classes)
        {
            var button=new Button(()=>Run(action)){tooltip=label+" for "+Money(price)+" coins",name=label.ToLowerInvariant().Replace(' ','-')};
            button.AddToClassList("purchase-button");button.AddToClassList(classes);
            button.Add(new ClinicIcon(glyph,26));
            var words=Box(button,"purchase-words");
            Text(words,detail,"purchase-detail",true);
            var cost=Box(words,"cost-row");cost.Add(new ClinicIcon(ClinicGlyph.Coin,15,CoinInk));Display(Text(cost,Money(price),"purchase-price",true));
            parent.Add(button);
            readouts.Add(()=>button.SetEnabled(unlocked()&&State.Wallet>=price));
            RegisterAccessibleButton(button,label+", "+Money(price)+" coins. "+detail,()=>Run(action));
            return button;
        }

        /// <summary>Renovating shows what it unlocks, how long it takes, and how far the wallet is from the price.</summary>
        private void BuildRenovation(VisualElement parent,ClinicRoomState room)
        {
            var price=ClinicRules.RenovationCost(State,room.Kind);
            var nextTier=room.Tier+1;
            Func<ClinicCommandResult> action=()=>simulation.Renovate(room.Kind);
            var button=new Button(()=>Run(action)){name="expand-room",tooltip="Renovate to room "+nextTier+" for "+Money(price)+" coins"};
            button.AddToClassList("renovate-card");
            var tile=Box(button,"icon-tile icon-tile-small tile-light");tile.Add(new ClinicIcon(ClinicGlyph.Upgrade,22,BuildInk));
            var words=Box(button,"renovate-words");
            Display(Text(words,"Renovate to room "+nextTier,"renovate-title",true));
            Text(words,"Upgrade limit "+ClinicRules.TrackCap(nextTier)+" · "+TimeLabel(ClinicRules.RenovationSeconds(State,room.Kind))+" build","renovate-detail");
            var bar=Box(words,"meter meter-gold");var fill=Box(bar,"meter-fill");
            var cost=Box(button,"renovate-cost");
            var priceRow=Box(cost,"cost-row");priceRow.Add(new ClinicIcon(ClinicGlyph.Coin,18,CoinInk));Display(Text(priceRow,Money(price),"purchase-price",true));
            var need=Text(cost,"","need-more",true);
            parent.Add(button);
            readouts.Add(()=>
            {
                var ready=State.Wallet>=price;
                button.SetEnabled(ready);
                fill.style.width=Length.Percent(price<=0?100:100f*Math.Min(State.Wallet,price)/price);
                need.text=ready?"Ready":"Need "+Money(price-State.Wallet)+" more";
                need.EnableInClassList("need-ready",ready);
            });
            RegisterAccessibleButton(button,button.tooltip+". Upgrade limit "+ClinicRules.TrackCap(nextTier),()=>Run(action),
                ()=>State.Wallet>=price?"Ready":"Need "+Money(price-State.Wallet)+" more coins");
        }

        /// <summary>Every builder is on another room: say where and when it frees up, finish it with gems, or add a second builder.</summary>
        private void BuildBuilderBusy(VisualElement parent)
        {
            var job=State.Construction.OrderBy(c=>c.EndsTick).First();
            var card=Box(parent,"build-card builder-busy");
            var top=Box(card,"build-top");top.pickingMode=PickingMode.Ignore;
            var tile=Box(top,"icon-tile icon-tile-small tile-light");tile.Add(new ClinicIcon(ClinicGlyph.Clock,20,BuildInk));
            var words=Box(top,"build-words");words.pickingMode=PickingMode.Ignore;
            Display(Text(words,"Builder busy on "+RoomName(job.Room).ToLowerInvariant(),"build-title",true));
            var when=Text(words,"","build-detail");
            var bar=Box(card,"meter meter-gold");var fill=Box(bar,"meter-fill");
            readouts.Add(()=>
            {
                when.text="Free in "+TimeLabel((job.EndsTick-State.Tick)/(double)ClinicRules.TicksPerSecond)+" · then renovate here";
                fill.style.width=Length.Percent(100f*Mathf.Clamp01((State.Tick-job.StartedTick)/(float)Math.Max(1,job.EndsTick-job.StartedTick)));
            });
            var choices=Box(card,"build-choices");
            var id=job.Id;
            GemActionButton(choices,"Finish "+RoomName(job.Room)+" now with gems","Finish",()=>SkipConstruction(id),()=>SkipCost(id),"gem-action",readouts);
            if(saves==null||!saves.HasUnlock(ClinicUnlocks.ExtraBuilder))
            {
                var builder=IconButton(choices,ClinicGlyph.Upgrade,"Add a second builder, in the shop",OpenShop,"soft-action");
                builder.name="add-second-builder";
                builder.Q<ClinicIcon>().Tint=GemInk;
                Text(builder,"2nd builder","soft-action-label",true);
            }
        }

        private void BuildConstructionReadout(ClinicConstructionState job,VisualElement parent=null)
        {
            var room=State.Room(job.Room);
            var card=Box(parent??dock,"build-card construction-readout");
            var top=Box(card,"build-top");top.pickingMode=PickingMode.Ignore;
            var words=Box(top,"build-words");words.pickingMode=PickingMode.Ignore;
            Display(Text(words,room.Built?"Renovating to room "+(room.Tier+1):"Building the "+RoomName(job.Room).ToLowerInvariant(),"build-title",true));
            Text(words,room.Built?"Care continues while you build":"Seats arrive when it opens","build-detail");
            var time=Display(Text(top,"","construction-time",true));
            var bar=Box(card,"meter meter-gold meter-thick");var fill=Box(bar,"meter-fill");
            readouts.Add(()=>
            {
                fill.style.width=Length.Percent(100f*Mathf.Clamp01((State.Tick-job.StartedTick)/(float)Math.Max(1,job.EndsTick-job.StartedTick)));
                var left=Math.Max(0,(job.EndsTick-State.Tick)/(double)ClinicRules.TicksPerSecond);
                // Minutes and seconds under an hour, so the count visibly moves.
                var whole=(long)Math.Ceiling(left);
                time.text=whole>=3600||whole<60?TimeLabel(left):whole/60+"m "+(whole%60).ToString("00")+"s";
            });
            BuildSkipButton(card,job);
        }

        private void ToggleSettings(){locationsOpen=false;gemsOpen=false;settingsOpen=!settingsOpen;selectedRoom=null;selectedObject=null;dockKey="";UpdateReadouts();}
        private void BuildSettings()
        {
            var content=new ScrollView(ScrollViewMode.Vertical){name="clinic-settings-content",horizontalScrollerVisibility=ScrollerVisibility.Hidden,verticalScrollerVisibility=ScrollerVisibility.Hidden};
            content.AddToClassList("bounded-dock-content");dock.Add(content);
            BindTouchCaptureLifecycle(content.contentContainer);BindTouchCaptureLifecycle(content.contentViewport);
            var row=Box(content,"settings-list");
            Preference(row,ClinicGlyph.Sound,"Effects",null,()=>profile.preferences.sound,v=>{profile.preferences.sound=v;RefreshAudioPreferences();});
            Preference(row,ClinicGlyph.Music,"Music",null,()=>profile.preferences.music,v=>{profile.preferences.music=v;RefreshAudioPreferences();});
            Preference(row,ClinicGlyph.Haptic,"Haptics",null,()=>profile.preferences.haptics,v=>profile.preferences.haptics=v);
            Preference(row,ClinicGlyph.Motion,"Less motion","Calmer camera and effects",()=>profile.preferences.reducedMotion,v=>profile.preferences.reducedMotion=v,last:true);
            if(saves!=null)
            {
                var note=Box(content,"settings-note");note.pickingMode=PickingMode.Ignore;
                note.Add(new ClinicIcon(ClinicGlyph.Clock,20,BuildInk));
                Text(note,"Offline: up to "+saves.OfflineLimitHours+" hours of earnings. Building continues the whole time you are away.","help-text");
            }
            var help=Box(content,"help-row");
            Text(help,"Drag to explore · Pinch to zoom\nTap a room to improve it. Tap cash to collect.","help-text");
            var links=Box(content,"settings-links");
            if(apple!=null)
            {
                var restore=IconButton(links,ClinicGlyph.Restore,"Restore existing purchases",RequestRestore,"settings-link restore-button");
                restore.name="restore-purchases";
                Text(restore,"Restore purchases","settings-link-label",true);
                restore.Add(new ClinicIcon(ClinicGlyph.Chevron,16));
                readouts.Add(()=>restore.SetEnabled(!restoreRequested&&!apple.IsRestoring));
            }
            SettingsLink(links,ClinicGlyph.Help,"Privacy policy","https://github.com/its-me-anoop/gravitile-support/blob/main/privacy.md");
            SettingsLink(links,ClinicGlyph.Help,"Contact support","https://github.com/its-me-anoop/gravitile-support");
        }
        private void SettingsLink(VisualElement parent,ClinicGlyph glyph,string label,string destination)
        {
            var button=IconButton(parent,glyph,label+", opens in browser",()=>{SaveNow();Application.OpenURL(destination);},"settings-link");
            button.name=label.ToLowerInvariant().Replace(' ','-');
            Text(button,label,"settings-link-label",true);
            button.Add(new ClinicIcon(ClinicGlyph.External,16));
        }
        /// <summary>A switch row. "On" or "Off" is written beside the switch, so state never rests on colour alone.</summary>
        private void Preference(VisualElement list,ClinicGlyph glyph,string label,string detail,Func<bool> get,Action<bool> set,bool last=false)
        {
            var on=get();
            var button=IconButton(list,glyph,label,()=>{set(!get());SaveNow();dockKey="";UpdateReadouts();},"switch-row",()=>get()?"On":"Off");
            var words=Box(button,"switch-words");words.pickingMode=PickingMode.Ignore;
            Text(words,label,"preference-label",true);
            if(detail!=null)Text(words,detail,"switch-detail");
            Text(button,on?"On":"Off","preference-value",true);
            var track=Box(button,"switch-track");track.pickingMode=PickingMode.Ignore;
            var knob=Box(track,"switch-knob");knob.pickingMode=PickingMode.Ignore;
            if(on)knob.Add(new ClinicIcon(ClinicGlyph.Check,14,LeafInk));
            button.EnableInClassList("preference-on",on);
            button.EnableInClassList("switch-last",last);
        }
    }
}
