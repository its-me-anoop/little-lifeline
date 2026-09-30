using System.Collections.Generic;
using IdleClinic.Core;
using UnityEngine;

namespace IdleClinic.Presentation
{
    /// <summary>A small offscreen stage that renders one piece of first aid equipment at one version, for the equipment
    /// panel. It shares the clinic's light and materials, and sits far from the clinic so the main camera never sees it.</summary>
    internal sealed class ClinicGearPreview
    {
        private const int Size=384;
        private readonly ClinicArt art;
        private readonly Transform stage;
        private readonly Camera camera;
        private readonly Dictionary<int,GameObject> models=new Dictionary<int,GameObject>();
        internal RenderTexture Texture { get; private set; }
        internal ClinicGearPreview(ClinicArt art,Transform parent)
        {
            this.art=art;
            stage=art.Group("Equipment preview stage",parent,new Vector3(300,0,300));
            Texture=new RenderTexture(Size,Size,16){name="Equipment preview",antiAliasing=4};
            var rig=art.Group("Equipment preview camera",stage);
            camera=rig.gameObject.AddComponent<Camera>();
            camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.98f,.976f,.94f,0);
            camera.cullingMask=1<<ClinicArt.Layer;camera.nearClipPlane=.1f;camera.farClipPlane=40;camera.targetTexture=Texture;camera.enabled=false;
        }
        /// <summary>Show a piece of a room's equipment at a version and return the picture.</summary>
        internal RenderTexture Show(ClinicRoom room,int item,int version)
        {
            item=Mathf.Clamp(item,0,ClinicGear.ItemCount-1);version=Mathf.Clamp(version,1,ClinicGear.MaximumVersion);
            int key=(int)room*100+item;
            foreach(var pair in models)if(pair.Value!=null)pair.Value.SetActive(pair.Key==key);
            if(!models.TryGetValue(key,out var model)||model==null)
            {
                model=art.Model(ClinicRoomGear.Folder(room)+"/Gear"+(item+1).ToString("00"),stage,Vector3.zero);
                model.name=ClinicGear.ItemName(room,item);models[key]=model;
            }
            model.SetActive(true);
            var stem="Gear"+(item+1).ToString("00")+"_V";
            GameObject shown=null;
            foreach(Transform child in model.GetComponentsInChildren<Transform>(true))
                if(child.name.StartsWith(stem,System.StringComparison.Ordinal)&&int.TryParse(child.name.Substring(stem.Length),out var v)){child.gameObject.SetActive(v==version);if(v==version)shown=child.gameObject;}
            var bounds=new Bounds(stage.position+Vector3.up*.7f,Vector3.zero);var first=true;
            if(shown!=null)foreach(var renderer in shown.GetComponentsInChildren<Renderer>())
            { if(first){bounds=renderer.bounds;first=false;}else bounds.Encapsulate(renderer.bounds); }
            var direction=new Vector3(-.62f,.55f,-.9f).normalized;
            camera.transform.position=bounds.center+direction*12;camera.transform.rotation=Quaternion.LookRotation(-direction,Vector3.up);
            camera.orthographicSize=Mathf.Max(.45f,bounds.extents.magnitude*.78f);
            camera.Render();
            return Texture;
        }
        internal void Dispose(){if(Texture!=null){Texture.Release();ClinicArt.Destroy(Texture);Texture=null;}}
    }
}
