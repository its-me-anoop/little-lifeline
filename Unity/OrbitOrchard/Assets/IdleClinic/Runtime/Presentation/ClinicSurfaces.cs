using System.Collections.Generic;
using UnityEngine;

namespace IdleClinic.Presentation
{
    /// <summary>Procedural textures for the real-world surfaces of the reception furniture: walnut and oak grain, quartz,
    /// brushed steel, woven fabric, leather, cork and a clinic software screen. Models carry world-scale UVs (one UV unit
    /// per metre) for the grained surfaces and a whole-face UV for screens. Painted once per material and cached.</summary>
    internal static class ClinicSurfaces
    {
        private const int Size=256;

        internal static void Apply(Material material,string role,List<Object> owned)
        {
            Texture2D texture;
            switch(role)
            {
                case "Walnut":texture=Wood(new Color(.25f,.15f,.09f),new Color(.45f,.30f,.19f),11,role);Finish(material,.34f,0);break;
                case "Oak":texture=Wood(new Color(.60f,.45f,.29f),new Color(.80f,.65f,.46f),8,role);Finish(material,.30f,0);break;
                case "Quartz":texture=Quartz();Finish(material,.62f,0);break;
                case "BrushedSteel":texture=Brushed();Finish(material,.58f,.72f);break;
                case "Fabric":texture=Weave(new Color(.42f,.53f,.49f));Finish(material,.08f,0);break;
                case "Leather":texture=Pebbled(new Color(.33f,.21f,.14f),.07f,"Leather");Finish(material,.36f,0);break;
                case "Cork":texture=Pebbled(new Color(.70f,.53f,.34f),.16f,"Cork");Finish(material,.05f,0);break;
                case "ScreenUI":texture=Screen();Finish(material,.7f,0);
                    material.EnableKeyword("_EMISSION");material.SetTexture("_EmissionMap",texture);
                    material.SetColor("_EmissionColor",new Color(.62f,.62f,.62f));material.globalIlluminationFlags=MaterialGlobalIlluminationFlags.None;break;
                case "Brass":Finish(material,.52f,.62f);return;
                case "Acrylic":Finish(material,.82f,0);return;
                case "Rubber":case "Charcoal":Finish(material,.12f,0);return;
                default:return;
            }
            owned.Add(texture);material.mainTexture=texture;material.color=Color.white;
        }

        private static void Finish(Material material,float gloss,float metal)
        { material.SetFloat("_Glossiness",gloss);material.SetFloat("_Metallic",metal); }

        private static Texture2D Make(string name,Color[] pixels,int width=Size,int height=Size)
        {
            var texture=new Texture2D(width,height,TextureFormat.RGB24,true){name=name,wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
            texture.SetPixels(pixels);texture.Apply(true,true);return texture;
        }

        private static float Hash(int x,int y,int seed)
        {
            unchecked
            {
                uint h=(uint)(x*374761393+y*668265263+seed*2147483647);
                h=(h^(h>>13))*1274126177u;return ((h^(h>>16))&65535)/65535f;
            }
        }

        /// <summary>Flat-sawn grain: growth rings warped by noise, fine pores and the odd darker streak.</summary>
        private static Texture2D Wood(Color dark,Color light,float rings,string role)
        {
            var pixels=new Color[Size*Size];
            for(int y=0;y<Size;y++)for(int x=0;x<Size;x++)
            {
                float u=x/(float)Size,v=y/(float)Size;
                float warp=Mathf.PerlinNoise(u*3.1f,v*.9f)*2.2f+Mathf.PerlinNoise(u*9f+7,v*2f)*.35f;
                float ring=Mathf.Repeat(u*rings+warp,1f);
                float band=Mathf.SmoothStep(0,1,Mathf.Abs(ring-.5f)*2);
                float pore=Hash(x,y/3,11)*.10f;
                float streak=Mathf.PerlinNoise(u*40,v*1.5f)*.12f;
                var c=Color.Lerp(dark,light,.35f+band*.55f-pore-streak*.5f);
                pixels[y*Size+x]=c;
            }
            return Make(role+" grain",pixels);
        }

        /// <summary>Engineered quartz: a soft cloud, fine mineral speckle and a few pale grey veins.</summary>
        private static Texture2D Quartz()
        {
            var pixels=new Color[Size*Size];var basis=new Color(.95f,.945f,.92f);
            for(int y=0;y<Size;y++)for(int x=0;x<Size;x++)
            {
                float u=x/(float)Size,v=y/(float)Size;
                float cloud=(Mathf.PerlinNoise(u*4,v*4)-.5f)*.05f;
                float vein=Mathf.Abs(Mathf.Sin((u*2.2f+v*.8f+Mathf.PerlinNoise(u*5,v*5)*1.3f)*Mathf.PI*2));
                float veinShade=vein<.035f?.12f*(1-vein/.035f):0;
                float speck=Hash(x,y,5);
                var c=basis*(1+cloud-veinShade);
                if(speck>.985f)c*=.78f;else if(speck>.97f)c=Color.Lerp(c,new Color(.80f,.74f,.64f),.5f);
                c.a=1;pixels[y*Size+x]=c;
            }
            return Make("Quartz stone",pixels);
        }

        /// <summary>Brushed stainless steel: long fine streaks along one direction.</summary>
        private static Texture2D Brushed()
        {
            var pixels=new Color[Size*Size];
            for(int y=0;y<Size;y++)
            {
                float row=Hash(0,y,3);
                for(int x=0;x<Size;x++)
                {
                    float streak=(row-.5f)*.09f+(Hash(x/24,y,9)-.5f)*.05f+(Mathf.PerlinNoise(x*.01f,y*.2f)-.5f)*.06f;
                    float g=.70f+streak;pixels[y*Size+x]=new Color(g,g*1.01f,g*1.02f);
                }
            }
            return Make("Brushed steel",pixels);
        }

        /// <summary>A plain weave: alternating over-under threads with slight colour variation.</summary>
        private static Texture2D Weave(Color basis)
        {
            var pixels=new Color[Size*Size];
            for(int y=0;y<Size;y++)for(int x=0;x<Size;x++)
            {
                bool over=((x/3)+(y/3))%2==0;
                float thread=over?Mathf.Sin((x%3)/3f*Mathf.PI):Mathf.Sin((y%3)/3f*Mathf.PI);
                float shade=.84f+thread*.16f+(Hash(x/3,y/3,21)-.5f)*.08f;
                var c=basis*shade;c.a=1;pixels[y*Size+x]=c;
            }
            return Make("Woven fabric",pixels);
        }

        /// <summary>Leather or cork: a mottled base with small raised grains.</summary>
        private static Texture2D Pebbled(Color basis,float contrast,string name)
        {
            var pixels=new Color[Size*Size];
            for(int y=0;y<Size;y++)for(int x=0;x<Size;x++)
            {
                float u=x/(float)Size,v=y/(float)Size;
                float mottle=(Mathf.PerlinNoise(u*6,v*6)-.5f)*contrast;
                float grain=(Mathf.PerlinNoise(u*48,v*48)-.5f)*contrast*.9f+(Hash(x,y,31)-.5f)*contrast*.5f;
                var c=basis*(1+mottle+grain);c.a=1;pixels[y*Size+x]=c;
            }
            return Make(name+" grain",pixels);
        }

        /// <summary>Clinic front-desk software: a teal header, a sidebar, appointment rows and a green action button.</summary>
        private static Texture2D Screen()
        {
            const int w=256,h=160;
            var pixels=new Color[w*h];
            var page=new Color(.93f,.95f,.96f);var header=new Color(.18f,.47f,.50f);var side=new Color(.84f,.89f,.90f);
            var ink=new Color(.30f,.36f,.40f);var faint=new Color(.72f,.77f,.80f);var go=new Color(.36f,.68f,.46f);var warm=new Color(.93f,.66f,.40f);
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                int top=h-1-y;Color c=page;
                if(top<22)c=header;
                else if(x<46)c=side;
                if(top>=6&&top<14&&x>=8&&x<60)c=new Color(.92f,.97f,.97f);
                if(x<46&&top>=34&&(top-34)%18<8&&x>=8&&x<38)c=faint;
                if(x>=56&&x<246&&top>=32)
                {
                    int row=(top-32)/24,inRow=(top-32)%24;
                    if(row<5&&inRow<19)
                    {
                        c=Color.white;
                        if(inRow>=5&&inRow<9&&x>=64&&x<150)c=ink;
                        if(inRow>=12&&inRow<15&&x>=64&&x<120)c=faint;
                        if(inRow>=6&&inRow<14&&x>=214&&x<238)c=row==0?go:row==2?warm:faint;
                    }
                }
                pixels[y*w+x]=c;
            }
            return Make("Reception screen",pixels,w,h);
        }
    }
}
