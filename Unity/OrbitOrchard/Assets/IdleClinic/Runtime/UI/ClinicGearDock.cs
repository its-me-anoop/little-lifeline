using System;
using System.Linq;
using IdleClinic.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdleClinic.App
{
    public sealed partial class ClinicApp
    {
        // The piece the panel's picture shows: the one last tapped or upgraded, else the first that can still be upgraded.
        private int gearFocus=-1;

        private bool GearList(ClinicRoomState room,UpgradeTrack track)=>track==UpgradeTrack.Equipment&&ClinicGear.Active(State,room.Kind);

        /// <summary>Upgrades owned and the most the room's unlocked pieces allow, for the Equipment tab.</summary>
        private static void GearProgress(ClinicState state,ClinicRoomState room,out int owned,out int allowed)
        {
            owned=ClinicGear.Steps(state,room.Kind);
            allowed=ClinicGear.UnlockedCount(state,room.Kind)*ClinicGear.StepsPerItem;
        }

        private static string GearTask(ClinicRoom kind)=>kind==ClinicRoom.Office?"raises the visit fee":kind==ClinicRoom.StaffRoom?"makes every member of staff quicker"
            :kind==ClinicRoom.Store?"makes room for more waiting patients":"shortens "+(kind==ClinicRoom.Reception?"check-in":kind==ClinicRoom.FirstAid?"treatment":kind==ClinicRoom.Waiting?"the wait to be called":kind==ClinicRoom.Consultation?"consultations":"dispensing");
        private static string GearTimeLabel(ClinicRoom kind)=>kind==ClinicRoom.Office?"Visit fee":kind==ClinicRoom.StaffRoom?"Staff speed":kind==ClinicRoom.Store?"Places to wait"
            :kind==ClinicRoom.Reception?"Check-in time":kind==ClinicRoom.FirstAid?"Treatment time"
            :kind==ClinicRoom.Waiting?"Time to call a patient":kind==ClinicRoom.Consultation?"Consultation time":"Dispensing time";
        private string GearTime(ClinicRoom kind)
        {
            var inv=System.Globalization.CultureInfo.InvariantCulture;
            if(kind==ClinicRoom.Office)return Money(ClinicRules.VisitFee(State))+" (+"+ClinicRules.OfficeFeePercent(State).ToString(inv)+"%)";
            if(kind==ClinicRoom.StaffRoom)return "+"+ClinicRules.StaffRoomSpeedPercent(State).ToString(inv)+"%";
            if(kind==ClinicRoom.Store)return "+"+ClinicRules.StoreQueuePlaces(State).ToString(inv)+" queue · +"+ClinicRules.StoreSeats(State).ToString(inv)+" seats";
            var role=RoomRole(kind);
            var ticks=kind==ClinicRoom.Waiting||!role.HasValue?ClinicRules.WaitingCallTicks(State):ClinicRules.StationServiceTicks(State,role.Value,0);
            return (ticks/10d).ToString("0.0",System.Globalization.CultureInfo.InvariantCulture)+"s";
        }

        /// <summary>First aid equipment: one row per piece, each with ten versions. Pieces arrive with room size.</summary>
        private void BuildGearList(VisualElement parent,ClinicRoomState room)
        {
            var kind=room.Kind;var unlocked=ClinicGear.UnlockedCount(State,kind);
            var panel=Box(parent,"track-panel gear-panel");
            var intro=Box(panel,"track-intro");intro.pickingMode=PickingMode.Ignore;
            var focus=gearFocus>=0&&gearFocus<ClinicGear.ItemCount?gearFocus:-1;
            if(focus<0)focus=Enumerable.Range(0,ClinicGear.ItemCount).FirstOrDefault(i=>ClinicGear.Unlocked(State,kind,i)&&!ClinicGear.AtTop(State,kind,i));
            var focusOpen=ClinicGear.Unlocked(State,kind,focus);var focusVersion=ClinicGear.Version(State,kind,focus);
            var shownVersion=focusOpen?Math.Min(ClinicGear.MaximumVersion,focusVersion+(focusVersion<ClinicGear.MaximumVersion?1:0)):1;
            var tile=Box(intro,"gear-preview");tile.pickingMode=PickingMode.Ignore;
            var picture=world==null?null:world.GearPreview(kind,focus,shownVersion);
            if(picture!=null)tile.style.backgroundImage=new StyleBackground(Background.FromRenderTexture(picture));
            var words=Box(intro,"track-words");words.pickingMode=PickingMode.Ignore;
            Text(words,!focusOpen?"ARRIVES WITH ROOM SIZE "+ClinicGear.UnlockTier(State,focus):focusVersion>=ClinicGear.MaximumVersion?"FULLY UPGRADED":"NEXT VERSION · "+ClinicGear.VersionName(shownVersion).ToUpperInvariant(),"eyebrow",true);
            Display(Text(words,ClinicGear.ItemName(kind,focus),"track-title",true));
            Text(words,unlocked+" of "+ClinicGear.ItemCount+" pieces unlocked. Every piece has ten versions, basic to advanced. Each upgrade "+GearTask(kind)+" and helps the clinic grow.","track-description");
            var effect=Box(panel,"effect-row");effect.pickingMode=PickingMode.Ignore;
            Text(effect,GearTimeLabel(kind),"effect-label",true);
            var values=Box(effect,"effect-values");values.pickingMode=PickingMode.Ignore;
            Display(Text(values,GearTime(kind),"effect-after",true));
            GearProgress(State,room,out var owned,out var allowed);
            var total=ClinicGear.ItemCount*ClinicGear.StepsPerItem;
            var meter=Box(panel,"level-pips");meter.pickingMode=PickingMode.Ignore;
            meter.tooltip=owned+" of "+total+" equipment upgrades owned";
            for(var i=0;i<ClinicGear.ItemCount;i++)
            {
                var pip=Box(meter,"level-pip");pip.pickingMode=PickingMode.Ignore;
                var version=ClinicGear.Version(State,kind,i);
                pip.EnableInClassList("pip-have",version>=ClinicGear.MaximumVersion);
                pip.EnableInClassList("pip-next",i<unlocked&&version<ClinicGear.MaximumVersion);
                pip.EnableInClassList("pip-locked",i>=unlocked);
            }
            var list=Box(parent,"gear-list");
            var guideItem=Enumerable.Range(0,ClinicGear.ItemCount).FirstOrDefault(i=>ClinicGear.Unlocked(State,kind,i)&&!ClinicGear.AtTop(State,kind,i));
            for(var item=0;item<ClinicGear.ItemCount;item++)BuildGearRow(list,kind,item,item==guideItem);
        }

        private void BuildGearRow(VisualElement list,ClinicRoom kind,int item,bool guideTarget)
        {
            var open=ClinicGear.Unlocked(State,kind,item);var version=ClinicGear.Version(State,kind,item);var top=version>=ClinicGear.MaximumVersion;
            var row=Box(list,"gear-row");
            row.EnableInClassList("gear-focus",item==gearFocus);
            row.RegisterCallback<ClickEvent>(e=>{if(gearFocus==item)return;gearFocus=item;dockKey="";UpdateReadouts();});
            row.EnableInClassList("gear-locked",!open);row.EnableInClassList("gear-top",open&&top);
            var words=Box(row,"gear-words");words.pickingMode=PickingMode.Ignore;
            Display(Text(words,ClinicGear.ItemName(kind,item),"gear-name",true));
            Text(words,open?"Version "+version+" of "+ClinicGear.MaximumVersion+" · "+ClinicGear.VersionName(version):"Arrives with room size "+ClinicGear.UnlockTier(State,item),"gear-version");
            var pips=Box(words,"gear-pips");pips.pickingMode=PickingMode.Ignore;
            for(var v=1;v<=ClinicGear.MaximumVersion;v++)
            {
                var pip=Box(pips,"gear-pip");pip.pickingMode=PickingMode.Ignore;
                pip.EnableInClassList("pip-have",open&&v<=version);
                pip.EnableInClassList("pip-next",open&&v==version+1);
            }
            var name=guideTarget?"upgrade-"+kind.ToString().ToLowerInvariant()+"-equipment":"gear-upgrade-"+item;
            if(!open)
            {
                var lockedButton=new Button(){name=name,tooltip=ClinicGear.ItemName(kind,item)+" arrives with room size "+ClinicGear.UnlockTier(State,item)};
                lockedButton.AddToClassList("gear-action");lockedButton.AddToClassList("blocked-action");
                lockedButton.Add(new ClinicIcon(ClinicGlyph.Lock,16));
                row.Add(lockedButton);lockedButton.SetEnabled(false);
                return;
            }
            if(top)
            {
                var done=new Button(){name=name,tooltip=ClinicGear.ItemName(kind,item)+" is fully upgraded"};
                done.AddToClassList("gear-action");done.AddToClassList("blocked-action");
                done.Add(new ClinicIcon(ClinicGlyph.Check,16));Text(done,"Advanced","blocked-label",true);
                row.Add(done);RegisterAccessibleButton(done,done.tooltip,()=>{});
                return;
            }
            var price=ClinicGear.UpgradeCost(State,kind,item);var next=ClinicGear.VersionName(version+1);
            Func<ClinicCommandResult> action=()=>simulation.UpgradeGear(kind,item);
            var buy=new Button(()=>{gearFocus=item;Run(action);}){name=name,tooltip="Upgrade the "+ClinicGear.ItemName(kind,item).ToLowerInvariant()+" to "+next.ToLowerInvariant()+" for "+Money(price)+" coins"};
            buy.AddToClassList("gear-action");buy.AddToClassList("primary-action");
            buy.Add(new ClinicIcon(ClinicGlyph.Coin,15,new Color(.98f,.86f,.45f)));
            Display(Text(buy,Money(price),"gear-price",true));
            row.Add(buy);
            readouts.Add(()=>buy.SetEnabled(State.Wallet>=price));
            RegisterAccessibleButton(buy,buy.tooltip,()=>Run(action));
        }
    }
}
