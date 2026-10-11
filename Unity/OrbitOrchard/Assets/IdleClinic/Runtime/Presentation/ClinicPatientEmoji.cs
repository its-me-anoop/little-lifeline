using UnityEngine;
using UnityEngine.Rendering;

namespace IdleClinic.Presentation
{
    /// <summary>Small camera-facing emoji, with a private random clock independent of gameplay randomness.</summary>
    internal sealed class ClinicPatientEmoji
    {
        private static readonly string[] Names={"Smile","Grin","Heart eyes","Surprised","Sleepy","Worried","Angry"};
        private readonly Transform root,head;
        private readonly GameObject angryFace;
        private readonly GameObject[] eyes=new GameObject[2],hearts=new GameObject[2];
        private readonly Transform[] smile=new Transform[7];
        private readonly GameObject openMouth,teeth,sleepMark;
        private uint random;
        private double next,until;
        internal bool Showing(double seconds)=>root.gameObject.activeSelf&&seconds<until;

        internal ClinicPatientEmoji(ClinicArt art,Transform actor,Transform head)
        {
            this.head=head;root=art.Group("Patient emoji bubble",actor);
            var yellow=art.Instance("Emoji yellow","Paper");yellow.color=new Color(1,.78f,.18f);
            var red=art.Instance("Emoji angry","Paper");red.color=new Color(.98f,.34f,.19f);
            var ink=art.Instance("Emoji ink","Paper");ink.color=new Color(.24f,.19f,.12f);
            var pink=art.Instance("Emoji heart","Paper");pink.color=new Color(.91f,.26f,.36f);
            var blue=art.Instance("Emoji sleepy","Paper");blue.color=new Color(.25f,.49f,.70f);
            GameObject Orb(string name,Transform parent,Vector3 point,Vector3 scale,Material material)
            {
                var shape=art.Orb(name,parent,point,scale,"Paper");var renderer=shape.GetComponent<MeshRenderer>();
                renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;return shape;
            }
            var paper=art.Material("Paper");
            Orb("Emoji speech bubble",root,Vector3.zero,new Vector3(.62f,.59f,.065f),paper);
            Orb("Emoji bubble tail",root,new Vector3(-.085f,-.29f,0),new Vector3(.09f,.12f,.045f),paper);
            Orb("Emoji face",root,new Vector3(0,0,-.05f),new Vector3(.46f,.46f,.04f),yellow);
            angryFace=Orb("Emoji angry face",root,new Vector3(0,0,-.055f),new Vector3(.46f,.46f,.045f),red);
            for(int side=0;side<2;side++)
            {
                float x=side==0?-.083f:.083f;
                eyes[side]=Orb("Emoji eye",root,new Vector3(x,.055f,-.081f),new Vector3(.045f,.055f,.018f),ink);
                var heart=art.Group("Emoji heart eye",root,new Vector3(x,.055f,-.087f));hearts[side]=heart.gameObject;
                for(int lobe=0;lobe<2;lobe++)Orb("Heart lobe",heart,new Vector3(lobe==0?-.018f:.018f,.011f,0),new Vector3(.057f,.055f,.012f),pink);
                var tip=Orb("Heart tip",heart,new Vector3(0,-.023f,0),new Vector3(.057f,.065f,.012f),pink);
                tip.transform.localRotation=Quaternion.Euler(0,0,45);
            }
            for(int i=0;i<smile.Length;i++)smile[i]=Orb("Emoji smile",root,Vector3.zero,new Vector3(.037f,.029f,.018f),ink).transform;
            openMouth=Orb("Emoji open mouth",root,new Vector3(0,-.09f,-.085f),new Vector3(.09f,.115f,.02f),ink);
            teeth=Orb("Emoji grin teeth",root,new Vector3(0,-.073f,-.098f),new Vector3(.11f,.032f,.008f),paper);
            var sleepy=art.Group("Emoji sleepy Z",root,new Vector3(.17f,.22f,-.10f));sleepMark=sleepy.gameObject;
            for(int i=0;i<3;i++)
            {
                var stroke=art.Box("Sleep Z stroke",sleepy,new Vector3(0,i==0?.03f:i==1?0:-.03f,0),new Vector3(.08f,.018f,.012f),"Paper");
                stroke.GetComponent<MeshRenderer>().sharedMaterial=blue;stroke.GetComponent<MeshRenderer>().shadowCastingMode=ShadowCastingMode.Off;
                if(i==1)stroke.transform.localRotation=Quaternion.Euler(0,0,38);
            }
            root.gameObject.SetActive(false);
        }
        internal void Assign(ulong clinicSeed,int patientId,double seconds)
        {
            random=(uint)(clinicSeed^(clinicSeed>>32))^((uint)patientId*2654435761u)^0x9e3779b9u;
            if(random==0)random=1;
            Hide();next=seconds+1+Roll()*8;
        }
        private float Roll()
        {random^=random<<13;random^=random>>17;random^=random<<5;return (random&0xFFFFFF)/16777216f;}
        internal void Angry(double seconds) { Expression(6);until=seconds+3;root.gameObject.SetActive(true); }
        internal void Hide()=>root.gameObject.SetActive(false);
        internal void InVehicle(double seconds){Hide();next=System.Math.Max(next,seconds+3);}
        internal bool Render(double seconds,Vector3 fallback,Quaternion cameraRotation,bool reducedMotion,bool mayStart)
        {
            bool started=false;
            if(root.gameObject.activeSelf&&seconds>=until){Hide();next=seconds+8+Roll()*12;}
            if(!root.gameObject.activeSelf&&seconds>=next&&mayStart)
            {
                Expression(Mathf.Min(5,(int)(Roll()*6)));until=seconds+3;root.gameObject.SetActive(true);started=true;
            }
            if(!root.gameObject.activeSelf)return started;
            root.position=(head!=null?head.position:fallback+Vector3.up*1.45f)+Vector3.up*.62f;
            root.rotation=cameraRotation;
            // Optional bob and gentle pop are removed under reduced motion.
            float age=(float)(3-(until-seconds));
            if(!reducedMotion)root.position+=Vector3.up*(.025f*Mathf.Sin(age*3));
            float scale=reducedMotion?1:Mathf.Lerp(.85f,1,Mathf.Clamp01(age/.18f));root.localScale=Vector3.one*scale;
            return started;
        }
        private void Expression(int kind)
        {
            root.name="Patient emoji bubble "+Names[kind];angryFace.SetActive(kind==6);
            for(int i=0;i<2;i++)
            {
                eyes[i].SetActive(kind!=2);hearts[i].SetActive(kind==2);
                eyes[i].transform.localScale=kind==4?new Vector3(.065f,.015f,.018f):new Vector3(.045f,.055f,.018f);
                eyes[i].transform.localRotation=Quaternion.Euler(0,0,kind==6?(i==0?-35:35):kind==5?(i==0?18:-18):0);
            }
            bool round=kind==3||kind==4;openMouth.SetActive(round||kind==1);teeth.SetActive(kind==1);sleepMark.SetActive(kind==4);
            openMouth.transform.localScale=kind==1?new Vector3(.16f,.11f,.02f):new Vector3(.09f,.115f,.02f);
            for(int i=0;i<smile.Length;i++)
            {
                smile[i].gameObject.SetActive(!round&&kind!=1);
                float x=(i-3)*.029f;float curve=.0055f*(i-3)*(i-3);
                smile[i].localPosition=new Vector3(x,kind>=5?-.055f-curve:-.115f+curve,-.09f);
            }
        }
    }
}
