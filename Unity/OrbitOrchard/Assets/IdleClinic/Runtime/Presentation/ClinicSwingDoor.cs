using UnityEngine;

namespace IdleClinic.Presentation
{
    /// <summary>An interior hinged door: a slim frame, a threshold and one or two leaves that swing into the room as someone
    /// walks up, then close behind them. In the low cutaway walls the leaves are cut at the wall height, as a plan drawing
    /// shows them. Presentation only.</summary>
    internal sealed class ClinicSwingDoor
    {
        private readonly Vector3 center;
        private readonly bool alongZ;
        private readonly float width;
        private readonly Transform[] hinges;
        private readonly float[] sign;
        private float openness,hold;
        private bool rendered;
        internal int OpeningCount { get; private set; }
        internal string Name { get; }

        /// <param name="alongZ">The wall runs north-south (people pass through east-west).</param>
        /// <param name="into">+1 to swing towards +x (or +z), -1 towards -x (or -z).</param>
        internal ClinicSwingDoor(ClinicArt art,Transform parent,string name,Vector3 center,bool alongZ,float width,float height,int into,bool pair=false)
        {
            this.center=center;this.alongZ=alongZ;this.width=width;Name=name;
            var root=art.Group(name,parent,center);root.localRotation=Quaternion.Euler(0,alongZ?90:0,0);
            // Local frame: x runs along the wall, z through it.
            for(int side=-1;side<=1;side+=2)
                art.Box("Doorway jamb",root,new Vector3(side*(width*.5f+.025f),height*.5f,0),new Vector3(.05f,height+.04f,.15f),"Charcoal");
            if(height>1.5f)art.Box("Doorway head",root,new Vector3(0,height+.02f,0),new Vector3(width+.1f,.05f,.15f),"Charcoal");
            art.Box("Door threshold",root,new Vector3(0,.004f,0),new Vector3(width,.008f,.16f),"Oak");
            int leaves=pair?2:1;float leaf=width/leaves;
            hinges=new Transform[leaves];sign=new float[leaves];
            // Local +z is world +x for a north-south wall and world +z otherwise. A positive turn swings a leaf hinged on the
            // left (extending to +x) towards local -z, and one hinged on the right towards local +z.
            for(int i=0;i<leaves;i++)
            {
                float hingeX=pair?(i==0?-width*.5f:width*.5f):-width*.5f;float dir=hingeX<0?1:-1;
                hinges[i]=art.Group("Door hinge",root,new Vector3(hingeX,0,0));
                sign[i]=-dir*into;
                var panel=art.Group("Door leaf",hinges[i],new Vector3(dir*leaf*.5f,0,0));
                art.Box("Door leaf panel",panel,new Vector3(0,height*.5f,0),new Vector3(leaf-.02f,height-.02f,.04f),"Oak");
                art.Box("Door leaf edge",panel,new Vector3(0,height-.01f,0),new Vector3(leaf-.02f,.02f,.045f),"Charcoal");
                art.Box("Door handle",panel,new Vector3(-dir*(leaf*.5f-.09f),Mathf.Min(height-.12f,1.0f),0),new Vector3(.02f,.14f,.07f),"BrushedSteel");
            }
        }

        internal void Render(ClinicActors actors,float deltaTime,bool reducedMotion)
        {
            bool near=alongZ?actors.ApproachesDoor(center,1.3f,width*.5f+.35f):actors.ApproachesDoor(center,width*.5f+.35f,1.3f);
            if(near)hold=.5f;else hold=Mathf.Max(0,hold-Mathf.Max(0,deltaTime));
            float target=hold>0?1:0;
            if(rendered&&target>0&&openness<=0)OpeningCount++;
            openness=reducedMotion||!rendered?target:Mathf.MoveTowards(openness,target,Mathf.Max(0,deltaTime)*(target>openness?6f:2.2f));rendered=true;
            for(int i=0;i<hinges.Length;i++)hinges[i].localRotation=Quaternion.Euler(0,sign[i]*88*openness,0);
        }
    }
}
