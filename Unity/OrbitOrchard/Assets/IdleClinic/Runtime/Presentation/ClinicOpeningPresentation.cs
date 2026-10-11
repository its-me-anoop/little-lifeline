using System.Collections.Generic;
using IdleClinic.Core;
using UnityEngine;

namespace IdleClinic.Presentation
{
    /// <summary>Saved opening stages control room clutter and the level-one furnishings.</summary>
    internal sealed class ClinicOpeningPresentation
    {
        private readonly GameObject[] rubbish=new GameObject[6];
        private readonly List<(GameObject item,int zone)> furnishings=new List<(GameObject,int)>();
        internal ClinicOpeningPresentation(ClinicArt art,Transform scene)
        {
            foreach(var item in scene.GetComponentsInChildren<Transform>(true))
            {
                if(item.name=="Office level one furniture") furnishings.Add((item.gameObject,3));
                else if(item.name=="Cleaning utilities furniture") furnishings.Add((item.gameObject,4));
                else if(item.parent!=null && item.parent.name=="Clinic interior details" && item.name!="Terrazzo aggregate" && item.name!="Corridor sage runner" && item.name!="Wayfinding care dot")
                    furnishings.Add((item.gameObject,Zone(Centre(item))));
                else if(item.parent==scene && item.name=="Plant") furnishings.Add((item.gameObject,Zone(item.position)));
            }
            var centres=new[]{new Vector3(-3.25f,.14f,-3.65f),new Vector3(-4.10f,.14f,3.65f),new Vector3(3.35f,.14f,1.6f),new Vector3(-3.65f,.14f,8.1f),new Vector3(.05f,.14f,8.1f),new Vector3(-1.5f,.14f,3.65f)};
            for(int zone=0;zone<6;zone++)
            {
                var root=art.Group("Opening clutter "+zone,scene);rubbish[zone]=root.gameObject;
                for(int i=0;i<3;i++)
                {
                    var box=art.Group("Empty cardboard box",root,centres[zone]+new Vector3((i-1)*.65f,0,(i%2)*.7f));
                    box.localRotation=Quaternion.Euler(0,i*31,0);
                    float size=.47f;
                    art.Box("Cardboard bottom",box,new Vector3(0,.035f,0),new Vector3(size,.045f,size),"Clay");
                    for(int side=-1;side<=1;side+=2)
                    {
                        art.Box("Cardboard side",box,new Vector3(side*size*.5f,.21f,0),new Vector3(.025f,.40f,size),"Wood");
                        art.Box("Cardboard end",box,new Vector3(0,.21f,side*size*.5f),new Vector3(size,.40f,.025f),"Wood");
                        var flap=art.Box("Open box flap",box,new Vector3(side*.34f,.38f,0),new Vector3(.22f,.018f,size),"Clay");
                        flap.transform.localRotation=Quaternion.Euler(0,0,side*30);
                    }
                }
                for(int i=0;i<9;i++)
                {
                    var litter=art.Box("Discarded paper",root,centres[zone]+new Vector3((i%3-1)*.55f,.025f,(i/3-1)*.48f-.65f),new Vector3(.14f,.012f,.21f),i%3==0?"Timber":"Paper");
                    litter.transform.localRotation=Quaternion.Euler(0,i*47,0);
                    art.Orb("Dust patch",root,centres[zone]+new Vector3((i%3-1)*.6f,.007f,(i/3-1)*.52f),new Vector3(.40f,.008f,.29f),"Concrete");
                }
            }
        }
        private static Vector3 Centre(Transform item)
        {
            var renderers=item.GetComponentsInChildren<Renderer>(true);
            if(renderers.Length==0)return item.position;
            var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
            return bounds.center;
        }
        private static int Zone(Vector3 p)=>p.z>6?3:p.x>1.25f?2:p.z>1.50f?1:0;
        internal void Render(ClinicState state)
        {
            if(!state.OpeningEnabled)
            {
                foreach(var item in rubbish)item.SetActive(false);
                foreach(var entry in furnishings)entry.item.SetActive(true);
                return;
            }
            for(int zone=0;zone<6;zone++)
            {
                bool clean=zone==5?state.Room(ClinicRoom.FirstAid).StationCount>=2:zone==3?state.OfficeCleaned:zone<3&&state.Room((ClinicRoom)zone).Cleaned;
                rubbish[zone].SetActive(state.OpeningEnabled&&!clean);
            }
            foreach(var entry in furnishings)
            {
                bool built=entry.zone==3?state.OfficeLevel>0:entry.zone<3&&state.Room((ClinicRoom)entry.zone).Built;
                entry.item.SetActive(!state.OpeningEnabled||built);
            }
        }
        internal static Vector3 BossPoint(int zone)=>zone==3?new Vector3(-4.08f,.14f,7.15f):zone==2?new Vector3(3.15f,.14f,.4f):zone==1?new Vector3(-4.20f,.14f,2.05f):new Vector3(-3.85f,.14f,.15f);
        internal static List<Vector3> BossRoute(int from,int to)
        {
            var route=new List<Vector3>{BossPoint(from)};
            if(from==to){route.Add(BossPoint(to));return route;}
            void Out(int zone)
            {
                if(zone==3){route.Add(new Vector3(-4.08f,.14f,5.55f));route.Add(new Vector3(.675f,.14f,5.55f));}
                if(zone==1){route.Add(new Vector3(-4.20f,.14f,1.50f));route.Add(new Vector3(-4.20f,.14f,.4f));}
                route.Add(new Vector3(.675f,.14f,.4f));
            }
            Out(from);
            if(to==3){route.Add(new Vector3(.675f,.14f,5.55f));route.Add(new Vector3(-4.08f,.14f,5.55f));}
            if(to==1){route.Add(new Vector3(-4.20f,.14f,.4f));route.Add(new Vector3(-4.20f,.14f,1.50f));}
            route.Add(BossPoint(to));return route;
        }
    }
}
