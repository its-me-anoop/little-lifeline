using System;
using UnityEngine;
using IdleClinic.Core;

namespace IdleClinic.Presentation
{
    /// <summary>Twelve repeatable wardrobe silhouettes applied afresh whenever a patient rig is reused.</summary>
    internal sealed class ClinicAppearance
    {
        private static readonly string[] Clothes={"Apricot","Denim","Sage","Mustard","Rose","Blue","Apricot","Denim","Sage","Rose","Mustard","Blue"};
        private static readonly string[] Skins={"Skin","SkinDeep","SkinLight","SkinBrown","SkinOlive","SkinDeep","SkinLight","SkinBrown","SkinLight","SkinDeep","SkinOlive","SkinBrown"};
        private static readonly float[] Heights={1,1.06f,.94f,1.03f,.97f,1.02f,.84f,1.10f,.92f,.96f,1.01f,.95f};
        private static readonly float[] Widths={1,1.05f,.94f,1.14f,.96f,1.07f,.87f,1.04f,1.05f,.98f,1.12f,.93f};
        private readonly Transform root,head,chest,headwear,bodywear;
        private readonly GameObject[] variants=new GameObject[12],bodyVariants=new GameObject[12];
        private readonly Renderer skin;
        private readonly Material[][] materials=new Material[12][];
        // Training shows on the uniform: a dot for each level within a band of five, a star for each full band,
        // and shoulder boards whose colour steps up every ten levels, so every one of the fifty levels looks different.
        private readonly GameObject[] trainingDots=new GameObject[4],trainingStars=new GameObject[9];
        private readonly GameObject[] shoulderRanks=new GameObject[4];
        private static readonly string[] RankColours={"Chrome","Gold","SignBlue","Rose"};
        private readonly bool patient;
        private int current=-1;
        internal ClinicAppearance(ClinicArt art,Transform root,bool patient,ClinicStaffRole role=ClinicStaffRole.Nurse)
        {
            this.root=root;this.patient=patient;skin=root.GetComponentInChildren<SkinnedMeshRenderer>();
            var rig=skin as SkinnedMeshRenderer;if(rig!=null){head=Array.Find(rig.bones,b=>b.name=="head");chest=Array.Find(rig.bones,b=>b.name=="chest");}
            headwear=art.Group("Character head accessories",root);bodywear=art.Group("Character body accessories",root);
            var styles=ClinicCharacterStyles.Current;
            var originals=skin==null?Array.Empty<Material>():skin.sharedMaterials;
            // Staff trousers follow the role: matching scrubs for nurses, dark for everyone else.
            string staffTrousers=role==ClinicStaffRole.Nurse?"Blue":role==ClinicStaffRole.Doctor?"Slate":"Ink";
            for(int appearance=0;appearance<12;appearance++)
            {
                var hair=styles.Hair(appearance);
                materials[appearance]=new Material[originals.Length];
                for(int slot=0;slot<originals.Length;slot++)
                {
                    string name=originals[slot]==null?"":originals[slot].name;
                    bool uniform=name=="Clinic Sage"||name=="Clinic Apricot"||name=="Clinic Blue";
                    materials[appearance][slot]=name=="Clinic Skin"?art.Material(Skins[appearance])
                        :name=="Clinic Hair"?art.Material(hair)
                        :name=="Clinic Trousers"?art.Material(patient?styles.Trousers(appearance):staffTrousers)
                        :patient&&name=="Clinic Apricot"?art.Material(Clothes[appearance])
                        :!patient&&uniform&&role==ClinicStaffRole.Doctor?art.Material("Linen")
                        :!patient&&uniform&&role==ClinicStaffRole.Pharmacist?art.Material("SageDark"):originals[slot];
                }
                var variant=art.Group("Appearance silhouette "+appearance,headwear);variants[appearance]=variant.gameObject;
                foreach(var group in styles.headwear)
                {
                    if((group.patientsOnly&&!patient)||!ClinicCharacterStyles.Includes(group,appearance))continue;
                    foreach(var shape in group.shapes)ClinicCharacterStyles.Build(art,variant,shape,hair);
                }
                variant.gameObject.SetActive(false);
                if(patient)
                {
                    var outfit=art.Group("Appearance outfit "+appearance,bodywear);bodyVariants[appearance]=outfit.gameObject;
                    foreach(var group in styles.bodywear)
                        if(group.who=="Patient"&&group.appearances!=null&&Array.IndexOf(group.appearances,appearance)>=0)
                            foreach(var shape in group.shapes)ClinicCharacterStyles.Build(art,outfit,shape,hair);
                    outfit.gameObject.SetActive(false);
                }
            }
            if(!patient)
            {
                // Training marks lie on the uniform: their depth follows the torso surface they sit on.
                for(int i=0;i<trainingDots.Length;i++)
                {
                    float x=-.100f+i*.025f,y=1.120f;
                    trainingDots[i]=art.Box("Staff training dot",bodywear,new Vector3(x,y,styles.TorsoFront(x,y)+.008f),new Vector3(.020f,.020f,.014f),"Gold");
                }
                for(int i=0;i<trainingStars.Length;i++)
                {
                    float x=.028f+(i%5)*.021f,y=1.135f-(i/5)*.030f;
                    var star=art.Box("Staff training star",bodywear,new Vector3(x,y,styles.TorsoFront(x,y)+.007f),new Vector3(.018f,.018f,.012f),"Gold");
                    star.transform.localRotation=Quaternion.Euler(0,0,45);trainingStars[i]=star;
                }
                for(int rank=0;rank<shoulderRanks.Length;rank++)
                {
                    var boards=art.Group("Shoulder boards rank "+(rank+1),bodywear);shoulderRanks[rank]=boards.gameObject;
                    for(int side=-1;side<=1;side+=2)art.Box("Shoulder board",boards,new Vector3(side*.150f,1.200f,-.004f),new Vector3(.070f,.016f,.110f),RankColours[rank]);
                    boards.gameObject.SetActive(false);
                }
                var who=role==ClinicStaffRole.Doctor?"Doctor":role==ClinicStaffRole.Pharmacist?"Pharmacist":null;
                if(who!=null)
                    foreach(var group in styles.bodywear)
                        if(group.who==who)foreach(var shape in group.shapes)ClinicCharacterStyles.Build(art,bodywear,shape,"Hair");
            }
        }
        internal void Apply(int appearance,int trainingLevel=1)
        {
            appearance=Math.Max(0,appearance)%12;
            if(current!=appearance)
            {
                current=appearance;if(skin!=null)skin.sharedMaterials=materials[appearance];
                for(int i=0;i<variants.Length;i++)variants[i].SetActive(i==appearance);
                root.localScale=patient?new Vector3(Widths[appearance],Heights[appearance],Widths[appearance]):Vector3.one;
                if(patient)for(int i=0;i<bodyVariants.Length;i++)if(bodyVariants[i]!=null)bodyVariants[i].SetActive(i==appearance);
            }
            if(!patient)
            {
                int earned=Math.Max(0,trainingLevel-1),dots=earned%5,stars=Math.Min(trainingStars.Length,earned/5),rank=Math.Min(shoulderRanks.Length,earned/10);
                for(int i=0;i<trainingDots.Length;i++)trainingDots[i].SetActive(i<dots);
                for(int i=0;i<trainingStars.Length;i++)trainingStars[i].SetActive(i<stars);
                for(int i=0;i<shoulderRanks.Length;i++)shoulderRanks[i].SetActive(i==rank-1);
            }
        }
        internal void AfterPose(bool seated)
        {
            // Seated pelvis sits 0.55m above the actor origin. Offset just that scale
            // difference so a shorter or taller visitor still rests on the same chair.
            if(seated&&patient&&current>=0)root.position+=Vector3.up*(.55f*(1-Heights[current]));
            if(head!=null){headwear.position=head.position;headwear.rotation=root.rotation;}
            if(chest!=null)bodywear.localPosition=Vector3.up*(root.InverseTransformPoint(chest.position).y-1.10f);
        }
    }
}
