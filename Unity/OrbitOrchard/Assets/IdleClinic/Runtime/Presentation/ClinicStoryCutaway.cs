using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace IdleClinic.Presentation
{
    /// <summary>The conversation camera's cutaway. While the story camera holds the boss and Maya, a wall, door frame, post or
    /// sign that stands between the camera and either of them is drawn as shadow only, so the room's light does not change,
    /// together with its trim (cap, skirting, lining, the pictures on it). It comes back once it has hidden nobody for a
    /// moment, as soon as the player looks around, and when the conversation view ends. Furniture, people and glass are never
    /// cut away, and nothing is removed or moved: the layout, routes and taps are untouched.</summary>
    internal sealed class ClinicStoryCutaway
    {
        // Where each speaker is checked, in metres above the feet: knees, hips, chest and face, and the body's sides at the
        // hips and chest.
        private static readonly float[] Heights={.45f,.9f,1.35f,1.7f};
        private static readonly float[] SideHeights={.9f,1.35f};
        private const float Side=.16f;
        // A piece stays cut for this long after it last hid someone (no flicker as they walk past a post).
        private const float Hold=.45f;
        // How often the scenery is listed again (rooms are furnished and restyled during the opening), and how often the
        // view is checked (about twelve times a second).
        private const float Refresh=1.5f,Interval=.08f;
        // Trim is anything lying within this of a cut piece's outline.
        private const float TrimMargin=.08f;

        private readonly Transform scene;
        private readonly Func<Transform,bool> isPerson;
        private readonly List<MeshRenderer> found=new List<MeshRenderer>(2048);
        // Opaque, shadow-casting scenery (never people) and where each piece was when listed.
        private readonly List<Renderer> scenery=new List<Renderer>(2048);
        private readonly List<Bounds> listed=new List<Bounds>(2048);
        // The pieces that can hide a standing person: walls, partitions, door leaves and frames, posts and overhead signs.
        private readonly List<Renderer> blockers=new List<Renderer>(512);
        private readonly Dictionary<Renderer,Trim> trims=new Dictionary<Renderer,Trim>();
        // Cut pieces: when each last hid someone, and how it cast shadows before.
        private readonly Dictionary<Renderer,float> cut=new Dictionary<Renderer,float>();
        private readonly Dictionary<Renderer,ShadowCastingMode> original=new Dictionary<Renderer,ShadowCastingMode>();
        private readonly List<Renderer> expired=new List<Renderer>();
        private readonly Ray[] rays=new Ray[2*(Heights.Length+SideHeights.Length*2)];
        private float clock,sinceRefresh,sinceCheck;
        private bool active;

        private sealed class Trim
        {
            internal Vector3 At;
            internal readonly List<Renderer> Pieces=new List<Renderer>();
        }

        internal ClinicStoryCutaway(Transform scene,Func<Transform,bool> isPerson){this.scene=scene;this.isPerson=isPerson;}

        /// <summary>How many pieces are cut away right now.</summary>
        internal int CutCount=>cut.Count;

        /// <summary>Keep the two speakers, standing at <paramref name="a"/> and <paramref name="b"/>, in plain view of
        /// <paramref name="camera"/>.</summary>
        internal void Update(Camera camera,Vector3 a,Vector3 b,float deltaTime)
        {
            if(camera==null||scene==null)return;
            deltaTime=Mathf.Max(0,deltaTime);clock+=deltaTime;
            if(!active){active=true;sinceRefresh=Refresh;sinceCheck=Interval;}
            sinceRefresh+=deltaTime;sinceCheck+=deltaTime;
            if(sinceRefresh>=Refresh){List();sinceRefresh=0;}
            if(sinceCheck<Interval)return;
            sinceCheck=0;
            int count=Aim(camera,a,0);count=Aim(camera,b,count);
            for(int i=0;i<blockers.Count;i++)
            {
                var piece=blockers[i];if(piece==null)continue;
                var bounds=piece.bounds;
                for(int r=0;r<count;r++)if(bounds.IntersectRay(rays[r])){Cut(piece);break;}
            }
            expired.Clear();
            foreach(var entry in cut)if(entry.Key==null||clock-entry.Value>Hold)expired.Add(entry.Key);
            for(int i=0;i<expired.Count;i++)Restore(expired[i]);
        }

        /// <summary>Everything back as it was: the conversation view has ended or the player is looking around.</summary>
        internal void Clear()
        {
            if(!active&&cut.Count==0)return;
            active=false;expired.Clear();expired.AddRange(cut.Keys);
            for(int i=0;i<expired.Count;i++)Restore(expired[i]);
            cut.Clear();original.Clear();trims.Clear();
        }

        // Rays from the speaker's knees, hips, chest, face and sides towards the (orthographic) camera.
        private int Aim(Camera camera,Vector3 feet,int count)
        {
            var toCamera=-camera.transform.forward;var right=camera.transform.right;
            for(int h=0;h<Heights.Length;h++)rays[count++]=new Ray(feet+Vector3.up*Heights[h],toCamera);
            for(int h=0;h<SideHeights.Length;h++)
                for(int side=-1;side<=1;side+=2)rays[count++]=new Ray(feet+Vector3.up*SideHeights[h]+right*(side*Side),toCamera);
            return count;
        }

        private void List()
        {
            found.Clear();scenery.Clear();listed.Clear();blockers.Clear();trims.Clear();
            scene.GetComponentsInChildren(false,found);
            for(int i=0;i<found.Count;i++)
            {
                var renderer=found[i];if(!renderer.enabled)continue;
                var mode=original.TryGetValue(renderer,out var before)?before:renderer.shadowCastingMode;
                // Glass and markers cast no shadow; transparent pieces never hide anyone.
                if(mode==ShadowCastingMode.Off||mode==ShadowCastingMode.ShadowsOnly)continue;
                var material=renderer.sharedMaterial;if(material!=null&&material.renderQueue>(int)RenderQueue.GeometryLast)continue;
                if(isPerson!=null&&isPerson(renderer.transform))continue;
                var bounds=renderer.bounds;scenery.Add(renderer);listed.Add(bounds);
                if(CanHideSomeone(bounds))blockers.Add(renderer);
            }
        }

        /// <summary>Walls, partitions, door leaves and jambs, posts and facade mullions (thin and at least hip high), and
        /// overhead pieces (lintels, wall caps, the name sign, mouldings). Desks, counters, beds and chairs are never cut.</summary>
        internal static bool CanHideSomeone(Bounds bounds)
        {
            float thin=Mathf.Min(bounds.size.x,bounds.size.z);
            if(thin<=.35f&&bounds.size.y>=.8f&&bounds.max.y>=.9f)return true;
            return bounds.min.y>=1.7f&&thin<=.6f;
        }

        private void Cut(Renderer piece)
        {
            Hide(piece);
            var trim=TrimOf(piece);
            for(int i=0;i<trim.Count;i++)Hide(trim[i]);
        }

        private void Hide(Renderer piece)
        {
            if(piece==null)return;
            if(!cut.ContainsKey(piece)){original[piece]=piece.shadowCastingMode;piece.shadowCastingMode=ShadowCastingMode.ShadowsOnly;}
            cut[piece]=clock;
        }

        private void Restore(Renderer piece)
        {
            if(piece!=null&&original.TryGetValue(piece,out var mode))piece.shadowCastingMode=mode;
            cut.Remove(piece);original.Remove(piece);
        }

        // The trim lying on a piece: what fits inside its outline grown by a few centimetres. Worked out again only when the
        // piece has moved (a sliding door leaf); walls keep theirs until the scenery is listed again.
        private List<Renderer> TrimOf(Renderer piece)
        {
            var bounds=piece.bounds;
            if(trims.TryGetValue(piece,out var trim)&&(trim.At-bounds.center).sqrMagnitude<.0004f)return trim.Pieces;
            if(trim==null){trim=new Trim();trims[piece]=trim;}
            trim.At=bounds.center;trim.Pieces.Clear();
            var outline=bounds;outline.Expand(TrimMargin*2);
            // Pieces that may have slid with it since they were listed are looked at where they are now.
            var near=outline;near.Expand(3f);
            for(int i=0;i<scenery.Count;i++)
            {
                var other=scenery[i];if(other==null||other==piece||!near.Intersects(listed[i]))continue;
                var now=other.bounds;
                if(outline.Contains(now.min)&&outline.Contains(now.max))trim.Pieces.Add(other);
            }
            return trim.Pieces;
        }
    }
}
