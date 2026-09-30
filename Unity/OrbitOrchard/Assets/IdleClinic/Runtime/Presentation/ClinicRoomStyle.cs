using System.Collections.Generic;
using UnityEngine;

namespace IdleClinic.Presentation
{
    /// <summary>The inner face of a wall a room style may line, from A to B at floor level, and the wall's height.</summary>
    internal readonly struct RoomWall
    {
        internal readonly Vector2 A,B;internal readonly float Height;
        internal RoomWall(float x1,float z1,float x2,float z2,float height){A=new Vector2(x1,z1);B=new Vector2(x2,z2);Height=height;}
    }

    /// <summary>One room's look. Every room size restyles everything in the room: the floor colour and pattern, the wall
    /// finish, the trim, and the colours of the furniture already standing there. Larger rooms add crown moulding,
    /// metal trim and a floor inlay. Decor bought with gems adds framed pieces to the tall walls and plants to the
    /// partitions, one piece per level. Presentation only.</summary>
    internal sealed class ClinicRoomStyle
    {
        private sealed class Palette
        {
            internal Color Floor,Pattern,Wall,Wainscot,Trim,Accent,AccentDark,Warm,Cool,Wood,Fabric,Rug,Metal;
        }
        private const int Patterns=7,Treatments=5;
        private static readonly Palette[] Library=BuildLibrary();
        // Furniture colour roles a style repaints; everything else (skin, leaves, screens, cash) keeps its colour.
        private static readonly string[] Themed={"Sage","SageDark","Apricot","Blue","Wood","Timber","Linen","Ivory","TileBlue","TileSage","TilePeach","Gold"};

        private readonly ClinicArt art;private readonly Transform root;private readonly Rect rect;private readonly float floorTop;
        private readonly int room;private readonly string key;private readonly Color baseFloor;
        private readonly Material floor,pattern,alternate,wall,wainscot,trim,rug,rugBorder,metal;
        private readonly Dictionary<string,Material> furniture=new Dictionary<string,Material>();
        private readonly GameObject[] patterns=new GameObject[Patterns],treatments=new GameObject[Treatments];
        private readonly GameObject rugGroup,crown,inlay;
        private readonly List<GameObject> decor=new List<GameObject>();
        // keepClear entries are (x, radius, z): wall pieces never hang within that distance of the point.
        private int shownTier=-1,shownMaximum=-1;

        internal ClinicRoomStyle(ClinicArt art,Transform parent,string name,Rect rect,float floorTop,IList<RoomWall> walls,int room,
            string floorRole,int decorLevels,IList<Vector3> keepClear=null)
        {
            this.art=art;this.rect=rect;this.floorTop=floorTop;this.room=room;key=name;baseFloor=ClinicArt.Color(floorRole);
            root=art.Group(name+" style",parent);
            floor=Tinted("floor",floorRole);pattern=Tinted("pattern","Ivory");alternate=Tinted("alternate",floorRole);
            wall=Tinted("wall","Ivory");wainscot=Tinted("wainscot","TileSage");trim=Tinted("trim","SageDark");
            rug=Tinted("rug",floorRole);rugBorder=Tinted("rug border","SageDark");metal=Tinted("metal","Gold");
            foreach(var role in Themed)furniture[role]=Tinted("furniture "+role,role);

            var floorRoot=art.Group("Room floor",root);
            Flat(floorRoot,"Styled floor",rect.center,rect.size,floorTop-.0125f,.025f,floor);
            for(int p=0;p<Patterns;p++){patterns[p]=art.Group("Floor pattern "+p,floorRoot).gameObject;BuildPattern(p,patterns[p].transform);}
            rugGroup=art.Group("Room rug",floorRoot).gameObject;
            var rugSize=new Vector2(Mathf.Min(rect.width*.52f,4.2f),Mathf.Min(rect.height*.45f,2.6f));
            Flat(rugGroup.transform,"Rug border",rect.center,rugSize,floorTop+.006f,.004f,rugBorder);
            Flat(rugGroup.transform,"Rug field",rect.center,rugSize-new Vector2(.16f,.16f),floorTop+.009f,.004f,rug);
            inlay=art.Group("Floor inlay",floorRoot).gameObject;
            foreach(var edge in Edges(rect,.12f))Flat(inlay.transform,"Metal floor inlay",edge.center,edge.size,floorTop+.005f,.004f,metal);

            var wallRoot=art.Group("Room walls",root);
            for(int t=0;t<Treatments;t++)treatments[t]=art.Group("Wall finish "+t,wallRoot).gameObject;
            crown=art.Group("Crown moulding",wallRoot).gameObject;
            foreach(var w in walls)BuildWall(wallRoot,w);
            BuildDecor(walls,decorLevels,keepClear??new Vector3[0]);
            Render(1,20,true,1);
        }

        private Material Tinted(string part,string role)=>art.Instance(key+" "+part,role);

        /// <summary>Repaint the furniture already standing in this room with the room's own colours.</summary>
        internal void Adopt(Transform scope)
        {
            foreach(var renderer in scope.GetComponentsInChildren<Renderer>(true))
            {
                if(renderer.transform.IsChildOf(root)||Skip(renderer.transform))continue;
                var c=renderer.bounds.center;if(!rect.Contains(new Vector2(c.x,c.z)))continue;
                var slots=renderer.sharedMaterials;bool changed=false;
                for(int i=0;i<slots.Length;i++)
                {
                    if(slots[i]==null)continue;var name=slots[i].name.Replace("Clinic ","");
                    if(furniture.TryGetValue(name,out var themed)){slots[i]=themed;changed=true;}
                }
                if(changed)renderer.sharedMaterials=slots;
            }
        }
        private static bool Skip(Transform node)
        {
            for(var t=node;t!=null;t=t.parent)
            {
                var n=t.name;
                if(n.Contains("cash")||n.Contains("Cash")||n.Contains("sparkle")||n.Contains("selection")||n.Contains("Selection")
                    ||n.Contains("wall")||n.Contains("Wall")||n.Contains("door")||n.Contains("Door")||n.Contains("Character")||n.Contains("Actor")
                    ||n.Contains("Renovation")||n.Contains("delivery")||n.Contains("Tip coin"))return true;
            }
            return false;
        }

        /// <summary>Show the look for this room size and decor level; repaints only when something changed.</summary>
        internal void Render(int tier,int maximumTier,bool built,int decorLevel)
        {
            tier=built?Mathf.Max(1,tier):1;maximumTier=Mathf.Max(1,maximumTier);
            for(int i=0;i<decor.Count;i++)ClinicUpgradeEffects.Show(decor[i],built&&i+2<=decorLevel);
            if(tier==shownTier&&maximumTier==shownMaximum)return;
            shownTier=tier;shownMaximum=maximumTier;
            float progress=tier/(float)maximumTier;
            var p=PaletteFor(tier,maximumTier);
            floor.color=p.Floor;pattern.color=p.Pattern;alternate.color=Color.Lerp(p.Floor,p.Pattern,.45f);
            wall.color=p.Wall;wainscot.color=p.Wainscot;rug.color=p.Rug;rugBorder.color=p.Trim;metal.color=p.Metal;
            trim.color=progress>=.6f?p.Metal:p.Trim;
            floor.SetFloat("_Glossiness",.12f+.45f*progress);
            SetFurniture(p,tier==1);
            int patternIndex=tier==1?0:(tier+room*3)%Patterns;
            for(int i=0;i<Patterns;i++)patterns[i].SetActive(i==patternIndex);
            int treatment=tier==1?0:(tier+room)%Treatments;
            for(int i=0;i<Treatments;i++)treatments[i].SetActive(i==treatment);
            rugGroup.SetActive(tier>1&&(tier+room)%3==0);
            crown.SetActive(progress>=.25f);
            inlay.SetActive(progress>=.8f);
        }

        private void SetFurniture(Palette p,bool original)
        {
            foreach(var pair in furniture)
            {
                if(original){pair.Value.color=ClinicArt.Color(pair.Key);continue;}
                pair.Value.color=pair.Key switch
                {
                    "Sage"=>p.Accent,"SageDark"=>p.AccentDark,"Apricot"=>p.Warm,"Blue"=>p.Cool,
                    "Wood"=>p.Wood,"Timber"=>Color.Lerp(p.Wood,Color.black,.2f),"Linen"=>p.Fabric,"Ivory"=>Color.Lerp(p.Fabric,p.Wall,.5f),
                    "TileBlue"=>Color.Lerp(p.Rug,p.Cool,.35f),"TileSage"=>Color.Lerp(p.Rug,p.Accent,.35f),"TilePeach"=>Color.Lerp(p.Rug,p.Warm,.35f),
                    "Gold"=>p.Metal,_=>pair.Value.color
                };
            }
        }

        private Palette PaletteFor(int tier,int maximumTier)
        {
            if(tier<=1)return new Palette{Floor=baseFloor,Pattern=ClinicArt.Color("Ivory"),Wall=ClinicArt.Color("Ivory"),Wainscot=ClinicArt.Color("TileSage"),
                Trim=ClinicArt.Color("SageDark"),Accent=ClinicArt.Color("Sage"),AccentDark=ClinicArt.Color("SageDark"),Warm=ClinicArt.Color("Apricot"),
                Cool=ClinicArt.Color("Blue"),Wood=ClinicArt.Color("Wood"),Fabric=ClinicArt.Color("Linen"),Rug=baseFloor,Metal=ClinicArt.Color("Gold")};
            var source=Library[(tier-2+room*7)%Library.Length];
            if(tier<=20)return source;
            // Beyond the twentieth size (the doctors clinic), the same moods return richer: deeper walls, brass trim.
            return new Palette{Floor=Color.Lerp(source.Floor,source.Pattern,.25f),Pattern=source.Pattern,Wall=Color.Lerp(source.Wall,source.Wainscot,.22f),
                Wainscot=Color.Lerp(source.Wainscot,Color.black,.12f),Trim=source.Trim,Accent=source.Accent,AccentDark=source.AccentDark,Warm=source.Warm,
                Cool=source.Cool,Wood=Color.Lerp(source.Wood,Color.black,.15f),Fabric=source.Fabric,Rug=Color.Lerp(source.Rug,source.AccentDark,.2f),
                Metal=Color.Lerp(source.Metal,ClinicArt.Color("Gold"),.6f)};
        }

        // ---------- Floor ----------
        private void BuildPattern(int index,Transform parent)
        {
            const float step=.65f;float y=floorTop+.002f;
            switch(index)
            {
                case 0: // square tiles
                    for(float x=rect.xMin+step;x<rect.xMax-.05f;x+=step)Flat(parent,"Tile seam",new Vector2(x,rect.center.y),new Vector2(.012f,rect.height),y,.004f,pattern);
                    for(float z=rect.yMin+step;z<rect.yMax-.05f;z+=step)Flat(parent,"Tile seam",new Vector2(rect.center.x,z),new Vector2(rect.width,.012f),y,.004f,pattern);
                    break;
                case 1: // checkerboard
                    int cx=0;for(float x=rect.xMin;x<rect.xMax-.05f;x+=step,cx++){int cz=0;for(float z=rect.yMin;z<rect.yMax-.05f;z+=step,cz++)
                        if((cx+cz)%2==0){var w=Mathf.Min(step,rect.xMax-x);var d=Mathf.Min(step,rect.yMax-z);Flat(parent,"Checker tile",new Vector2(x+w*.5f,z+d*.5f),new Vector2(w-.01f,d-.01f),y,.004f,alternate);}}
                    break;
                case 2: // staggered planks
                    int row=0;for(float z=rect.yMin;z<rect.yMax-.03f;z+=.24f,row++)
                    {
                        var d=Mathf.Min(.24f,rect.yMax-z);if(row%2==0)Flat(parent,"Plank",new Vector2(rect.center.x,z+d*.5f),new Vector2(rect.width,d-.012f),y,.004f,alternate);
                        for(float x=rect.xMin+(row%3)*.4f+.6f;x<rect.xMax;x+=1.2f)Flat(parent,"Plank end",new Vector2(x,z+d*.5f),new Vector2(.012f,d),y+.001f,.004f,pattern);
                    }
                    break;
                case 3: // diagonal lattice
                    for(int dir=-1;dir<=1;dir+=2)for(float c=-(rect.width+rect.height);c<rect.width+rect.height;c+=.9f)DiagonalLine(parent,c,dir,y);
                    break;
                case 4: // bordered field with a central diamond
                    foreach(var edge in Edges(Inset(rect,.18f),.22f))Flat(parent,"Border band",edge.center,edge.size,y,.004f,alternate);
                    foreach(var edge in Edges(Inset(rect,.52f),.03f))Flat(parent,"Border line",edge.center,edge.size,y,.004f,pattern);
                    {var s=Mathf.Min(rect.width,rect.height)*.32f;var diamond=Flat(parent,"Centre diamond",rect.center,new Vector2(s,s),y,.004f,pattern);diamond.transform.localRotation=Quaternion.Euler(0,45,0);}
                    break;
                case 5: // terrazzo chips
                    var random=new System.Random(room*97+11);int chips=Mathf.Clamp((int)(rect.width*rect.height*3),20,420);
                    for(int i=0;i<chips;i++)
                    {
                        var at=new Vector2(rect.xMin+.05f+(float)random.NextDouble()*(rect.width-.1f),rect.yMin+.05f+(float)random.NextDouble()*(rect.height-.1f));
                        var size=.05f+(float)random.NextDouble()*.08f;
                        var chip=art.Orb("Terrazzo chip",parent,new Vector3(at.x,y,at.y),new Vector3(size,.004f,size*(.6f+(float)random.NextDouble()*.6f)),"Ivory");
                        chip.GetComponent<Renderer>().sharedMaterial=i%3==0?trim:pattern;
                    }
                    break;
                default: // wide bands
                    int band=0;for(float z=rect.yMin;z<rect.yMax-.05f;z+=.6f,band++)
                        if(band%2==0){var d=Mathf.Min(.6f,rect.yMax-z);Flat(parent,"Floor band",new Vector2(rect.center.x,z+d*.5f),new Vector2(rect.width,d),y,.004f,alternate);}
                    break;
            }
        }
        private void DiagonalLine(Transform parent,float c,int dir,float y)
        {
            // Line through the rect in local terms: z - zMin = dir*(x - xMin) + c. Clip it to the rect.
            var points=new List<Vector2>();
            void Try(float x,float z){if(x>=rect.xMin-.001f&&x<=rect.xMax+.001f&&z>=rect.yMin-.001f&&z<=rect.yMax+.001f)points.Add(new Vector2(x,z));}
            Try(rect.xMin,rect.yMin+c);Try(rect.xMax,rect.yMin+c+dir*rect.width);
            Try(rect.xMin+(0-c)/dir,rect.yMin);Try(rect.xMin+(rect.height-c)/dir,rect.yMax);
            if(points.Count<2)return;
            var a=points[0];var b=points[0];float best=0;
            foreach(var p in points)if((p-a).sqrMagnitude>best){best=(p-a).sqrMagnitude;b=p;}
            if(best<.04f)return;
            var line=Flat(parent,"Lattice line",(a+b)*.5f,new Vector2(Mathf.Sqrt(best),.014f),y,.004f,pattern);
            line.transform.localRotation=Quaternion.Euler(0,-Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg,0);
        }

        // ---------- Walls ----------
        private void BuildWall(Transform parent,RoomWall w)
        {
            var along=w.B-w.A;float length=along.magnitude;if(length<.1f)return;
            var dir=along/length;var inward=new Vector2(-dir.y,dir.x);
            if(Vector2.Dot(rect.center-(w.A+w.B)*.5f,inward)<0)inward=-inward;
            float height=Mathf.Max(.2f,w.Height-.03f);bool tall=w.Height>=1.8f;
            var yaw=-Mathf.Atan2(dir.y,dir.x)*Mathf.Rad2Deg;
            GameObject Panel(Transform p,string name,float from,float to,float bottom,float top,float depth,Material m)
            {
                var mid=w.A+dir*((from+to)*.5f)+inward*depth;
                var box=art.Box(name,p,new Vector3(mid.x,.14f+(bottom+top)*.5f,mid.y),new Vector3(Mathf.Max(.01f,to-from),Mathf.Max(.005f,top-bottom),.012f),"Ivory");
                box.transform.localRotation=Quaternion.Euler(0,yaw,0);box.GetComponent<Renderer>().sharedMaterial=m;return box;
            }
            Panel(parent,"Painted wall lining",0,length,0,height,.014f,wall);
            Panel(parent,"Styled skirting",0,length,0,.11f,.026f,trim);
            float dado=Mathf.Min(.92f,height*.46f);
            // 1: wainscot and dado rail.
            Panel(treatments[1].transform,"Wainscot panel",0,length,0,dado,.022f,wainscot);
            Panel(treatments[1].transform,"Dado rail",0,length,dado-.02f,dado+.03f,.03f,trim);
            // 2: striped paper above a plain dado on tall walls; a low partition gets a panel below the rail instead.
            Panel(treatments[2].transform,"Dado rail",0,length,dado-.02f,dado+.02f,.03f,trim);
            if(tall)for(float s=.14f;s<length-.05f;s+=.3f)Panel(treatments[2].transform,"Wallpaper stripe",s,Mathf.Min(length,s+.11f),dado+.03f,height-.02f,.02f,wainscot);
            else Panel(treatments[2].transform,"Partition panel",0,length,0,dado,.022f,wainscot);
            // 3: tiled lower half.
            Panel(treatments[3].transform,"Wall tile field",0,length,0,dado,.02f,wainscot);
            for(float r=.2f;r<dado;r+=.2f)Panel(treatments[3].transform,"Wall tile grout",0,length,r-.005f,r+.005f,.024f,pattern);
            for(float s=.2f;s<length;s+=.2f)Panel(treatments[3].transform,"Wall tile grout",s-.005f,s+.005f,0,dado,.024f,pattern);
            // 4: panel moulding.
            float top=tall?Mathf.Min(height-.35f,1.55f):height-.08f,bottom=.22f;
            for(float s=.18f;s+.5f<length;s+=.72f)
            {
                Panel(treatments[4].transform,"Moulding frame",s,s+.5f,top-.025f,top,.028f,trim);Panel(treatments[4].transform,"Moulding frame",s,s+.5f,bottom,bottom+.025f,.028f,trim);
                Panel(treatments[4].transform,"Moulding frame",s,s+.025f,bottom,top,.028f,trim);Panel(treatments[4].transform,"Moulding frame",s+.475f,s+.5f,bottom,top,.028f,trim);
                Panel(treatments[4].transform,"Moulding inset",s+.025f,s+.475f,bottom+.025f,top-.025f,.02f,wainscot);
            }
            if(tall)Panel(crown.transform,"Crown moulding",0,length,height-.08f,height,.04f,trim);
            else Panel(crown.transform,"Partition cap trim",0,length,height-.04f,height,.03f,metal);
        }

        // ---------- Decor ----------
        private void BuildDecor(IList<RoomWall> walls,int levels,IList<Vector3> keepClear)
        {
            var wallSlots=new List<(Vector3 at,float yaw)>();var capSlots=new List<Vector3>();
            foreach(var w in walls)
            {
                var along=w.B-w.A;float length=along.magnitude;if(length<.6f)continue;var dir=along/length;var inward=new Vector2(-dir.y,dir.x);
                if(Vector2.Dot(rect.center-(w.A+w.B)*.5f,inward)<0)inward=-inward;
                float yaw=-Mathf.Atan2(dir.y,dir.x)*Mathf.Rad2Deg;bool tall=w.Height>=1.8f;
                for(float s=.45f;s<=length-.35f;s+=tall?.7f:.9f)
                {
                    var at=w.A+dir*s;
                    if(Blocked(at,keepClear))continue;
                    if(tall)wallSlots.Add((new Vector3(at.x+inward.x*.03f,.14f+w.Height-.44f,at.y+inward.y*.03f),yaw));
                    else capSlots.Add(new Vector3(at.x-inward.x*.05f,.17f+w.Height,at.y-inward.y*.05f));
                }
            }
            int wallIndex=0,capIndex=0;
            for(int level=2;level<=levels;level++)
            {
                var item=art.Group("Decor level "+level,root);int n=level-2;
                bool onWall=(n%2==0&&wallIndex<wallSlots.Count)||capIndex>=capSlots.Count;
                if(onWall&&wallIndex<wallSlots.Count){var s=wallSlots[wallIndex++];WallPiece(item,s.at,s.yaw,n);}
                else if(capIndex<capSlots.Count)CapPiece(item,capSlots[capIndex++],n);
                else CapPiece(item,new Vector3(rect.xMin+.4f+(n%4)*.3f,.14f,rect.yMin+.4f),n);
                item.gameObject.SetActive(false);decor.Add(item.gameObject);
            }
        }
        private static bool Blocked(Vector2 at,IList<Vector3> keepClear)
        {
            foreach(var c in keepClear)if((new Vector2(c.x,c.z)-at).sqrMagnitude<c.y*c.y)return true;
            return false;
        }
        private void WallPiece(Transform item,Vector3 at,float yaw,int n)
        {
            item.localPosition=at;item.localRotation=Quaternion.Euler(0,yaw,0);
            string[] prints={"Sage","Apricot","Blue","Rose","Mustard","Denim"};
            switch(n/2%3)
            {
                case 0:
                    art.Box("Decor frame",item,Vector3.zero,new Vector3(.40f,.32f,.035f),n%4==0?"Wood":"Gold");
                    art.Box("Decor print",item,new Vector3(0,0,.022f),new Vector3(.33f,.25f,.012f),"Paper");
                    art.Orb("Decor print motif",item,new Vector3(0,0,.03f),new Vector3(.16f,.16f,.01f),prints[n%prints.Length]);
                    break;
                case 1:
                    art.Orb("Decor mirror frame",item,Vector3.zero,new Vector3(.38f,.38f,.035f),"Gold");
                    art.Orb("Decor mirror glass",item,new Vector3(0,0,.02f),new Vector3(.31f,.31f,.012f),"Chrome");
                    break;
                default:
                    art.Box("Decor wall shelf",item,new Vector3(0,-.12f,.07f),new Vector3(.46f,.035f,.16f),"Wood");
                    art.Cylinder("Decor vase",item,new Vector3(-.1f,-.02f,.07f),new Vector3(.09f,.17f,.09f),prints[(n+2)%prints.Length]);
                    art.Orb("Decor posy",item,new Vector3(-.1f,.1f,.07f),new Vector3(.13f,.1f,.12f),"Leaf");
                    art.Box("Decor book",item,new Vector3(.12f,-.04f,.07f),new Vector3(.14f,.12f,.1f),prints[(n+4)%prints.Length]);
                    break;
            }
        }
        private void CapPiece(Transform item,Vector3 at,int n)
        {
            item.localPosition=at;
            if(n%3==1)
            {
                art.Cylinder("Decor planter",item,new Vector3(0,.09f,0),new Vector3(.2f,.18f,.2f),"Clay");
                art.Orb("Decor foliage",item,new Vector3(0,.27f,0),new Vector3(.32f,.26f,.3f),"Leaf");
            }
            else if(n%3==0)
            {
                art.Cylinder("Decor vase",item,new Vector3(0,.1f,0),new Vector3(.12f,.2f,.12f),"Linen");
                for(int f=0;f<3;f++)art.Orb("Decor flower",item,new Vector3(-.05f+f*.05f,.25f,0),new Vector3(.08f,.07f,.08f),f==1?"Apricot":"Rose");
            }
            else
            {
                art.Cylinder("Decor lamp base",item,new Vector3(0,.08f,0),new Vector3(.1f,.16f,.1f),"Gold");
                art.Cylinder("Decor lamp shade",item,new Vector3(0,.22f,0),new Vector3(.2f,.13f,.2f),"LampLight");
            }
        }

        // ---------- Helpers ----------
        private GameObject Flat(Transform parent,string name,Vector2 center,Vector2 size,float y,float thickness,Material m)
        {
            var box=art.Box(name,parent,new Vector3(center.x,y,center.y),new Vector3(size.x,thickness,size.y),"Ivory");
            box.GetComponent<Renderer>().sharedMaterial=m;return box;
        }
        private static Rect Inset(Rect r,float by)=>new Rect(r.xMin+by,r.yMin+by,Mathf.Max(.1f,r.width-2*by),Mathf.Max(.1f,r.height-2*by));
        private static IEnumerable<Rect> Edges(Rect r,float width)
        {
            yield return new Rect(r.xMin,r.yMin,r.width,width);yield return new Rect(r.xMin,r.yMax-width,r.width,width);
            yield return new Rect(r.xMin,r.yMin+width,width,r.height-2*width);yield return new Rect(r.xMax-width,r.yMin+width,width,r.height-2*width);
        }

        private static Palette[] BuildLibrary()
        {
            Color H(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var c);return c;}
            Palette P(params string[] c)=>new Palette{Floor=H(c[0]),Pattern=H(c[1]),Wall=H(c[2]),Wainscot=H(c[3]),Trim=H(c[4]),Accent=H(c[5]),AccentDark=H(c[6]),
                Warm=H(c[7]),Cool=H(c[8]),Wood=H(c[9]),Fabric=H(c[10]),Rug=H(c[11]),Metal=H(c[12])};
            return new[]
            {
                P("CFE3D2","A9C9B0","EEF5EE","9CC3A6","5E8C6A","6FA380","3E6B4F","E0A07A","7FB3B8","B98A5E","F4F1E6","D9C3A0","B89045"), // mint and oak
                P("D6E4EE","9FBBD0","F1F5F8","7FA3BF","3F6385","5E8FB5","2F4F6E","E8B27D","6CB0C4","C9A77C","F7F3EA","B8CFE0","A7AEB3"), // coastal blue
                P("E3B999","C98E6A","F6ECE2","C77B58","8A4B32","B8694A","7A3E2A","D98B4E","8FB0A8","9C6A45","F5E9DA","D6A07E","B07A3E"), // terracotta
                P("DCD6E8","B7ACD0","F3F0F8","A597C4","6A5A8E","8C7BB5","52447A","E3A8A0","9DB8CF","B08D73","F6F3F7","C9BEDD","B9A26B"), // lavender
                P("F2DE9B","E0BF5A","FBF6E3","D9B24A","8C6A1E","D19A2A","7D5A16","E6883E","6FA7A0","B3884F","FBF4DE","EAC873","C7962E"), // sunflower
                P("B9C9A8","8AA27A","EDF1E6","5F7F5A","34523A","4F7A52","2A4630","C98B5A","7FA6A0","7E5A3C","EFEBDD","A9B98E","9C8750"), // forest
                P("C9CDD1","9AA1A8","F0F1F2","6E7881","3B434B","58646F","2E363D","D9825A","6FA0B8","8C7563","F3F3F1","B4BCC3","2F3438"), // slate
                P("EFD3CF","DDA8A2","FBF2F0","D08F88","9A5A55","C57F7A","7F4541","E3A07A","9CC0BD","B98C77","FBF4F1","E8C0BA","C9A07A"), // blush
                P("BFDCD8","7FB8B1","EEF7F5","4F9A90","2C6760","3F8C83","1F524C","E0955C","5BA7B8","9C7355","F2F4EE","9CCBC4","B08B45"), // deep teal
                P("D9B98C","B8925F","F7EFE2","A67C4E","6E4D2C","9A7A4E","5E4428","D28A4A","86A9A3","A5733F","F5ECDC","C9A77A","B8923E"), // honey oak
                P("E6EEF4","C4D6E4","FFFCF4","A9C2D6","6D8AA3","88A9C4","4E6A83","E7B58D","7CB7CB","CDB08C","FFF9EC","D7E4EE","C0C4C6"), // sky and cream
                P("D2CFAE","AFAA7A","F4F2E4","8E8A55","5B5830","7D7A45","4A4726","C7874C","87A89A","8F6B45","F3EFDF","BDB78B","B8943C"), // olive and brass
                P("F3CDBB","E59C7F","FDF3EC","E08A6B","A2503A","D9785B","8E3F2D","F0A45F","5FB3B2","B7876A","FCF1E8","EDB59D","C79352"), // coral
                P("B7BDD0","7F88A8","E9EBF3","4B557A","283152","45507A","232B48","D9A15E","7C9EC7","6E5845","EEEDF2","9EA7C2","C9A24E"), // midnight
                P("DDE4D3","BCC8AE","F8F7EF","9FB08D","647556","8AA077","4F6142","D9A77F","93B6B0","BA9A74","FAF6EA","CBD4BD","A99A6A"), // sage and linen
                P("ECEAE4","CFCAC0","FAF9F5","D9D3C6","8C7A52","A89A78","5E5236","D8A86A","9EB8BE","7B5E45","FBF9F3","E1DACB","C9A24A"), // marble and gold
                P("D9C6D3","B593AA","F6EEF3","946C88","5C3B53","825A77","4A2F43","DD9A77","88AFC0","8E6852","F8F1F4","C9ABBF","B99056"), // plum
                P("E6EDC3","C9D688","FBFCEF","AFC063","6B7A2A","9CB24A","56661F","F0A640","6FB7A8","B79A62","FBF9E8","D8E0A3","C9A83A"), // citrus
                P("A88B72","86694F","EFE6DA","6E4F38","3E2A1C","7A5A40","45301F","C9804A","7E9E9A","5E3E27","F1E7D8","9E7B5C","C49A4A")  // walnut library
            };
        }
    }
}
