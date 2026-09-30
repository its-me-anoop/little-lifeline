using System;
using System.IO;
using IdleClinic.Core;
using IdleClinic.Presentation;
using UnityEngine;

namespace IdleClinic.Tests
{
    /// <summary>Editor-only rendered scene inspection. Scripted fixtures are not native gameplay evidence.</summary>
    public static class ClinicSceneReview
    {
        public static void Capture()
        {
            string output=Environment.GetEnvironmentVariable("CLINIC_REVIEW_OUTPUT");
            if(string.IsNullOrEmpty(output))throw new InvalidOperationException("Set a new CLINIC_REVIEW_OUTPUT directory.");
            if(Directory.Exists(output))throw new InvalidOperationException("Review output already exists.");
            Directory.CreateDirectory(output);
            var host=new GameObject("Scripted clinic scene inspection");
            try
            {
                var world=host.AddComponent<ClinicWorld>();world.Initialize();
                var state=ClinicSimulation.CreateNew().State;state.Patients.Clear();
                state.Room(ClinicRoom.Waiting).Built=true;state.Room(ClinicRoom.Waiting).FacilitiesLevel=6;
                state.Room(ClinicRoom.FirstAid).StationCount=2;state.ReceptionDesks.Add(new ReceptionDeskState{Id=1});
                state.Amenity(ClinicAmenity.Toilet).Level=3;state.Amenity(ClinicAmenity.Vending).Level=3;
                for(int i=0;i<3;i++)state.Room((ClinicRoom)i).Tier=3;
                foreach(int level in new[]{0,1,3})
                {
                    state.Amenity(ClinicAmenity.Parking).Level=level;state.Patients.Clear();
                    for(int i=0;i<level*2;i++)state.Patients.Add(new ClinicPatientState{Id=100+i,AppearanceId=i,ParkingBayId=i,Phase=ClinicPatientPhase.Seated,
                        FromAnchor="waiting.seat."+i,ToAnchor="waiting.seat."+i});
                    world.Render(state,.1f,true);
                    Save(world,new Vector3(-10.4f,.14f,-.15f),8.2f,750,1334,Path.Combine(output,"parking-level-"+level+".png"));
                }
                Save(world,new Vector3(0,.45f,.45f),7.8f,1200,1000,Path.Combine(output,"joined-clinic.png"));
                File.WriteAllText(Path.Combine(output,"SCOPE.txt"),"Scripted Unity Editor scene renders. Includes staged parking levels and joined walls. Not simulator input, a saved player session, or a performance measurement.\n");
            }
            finally{UnityEngine.Object.DestroyImmediate(host);}
        }
        /// <summary>Renders every room's equipment at one version, in both clinics: -executeMethod IdleClinic.Tests.ClinicSceneReview.CaptureGear.</summary>
        public static void CaptureGear()
        {
            string output=Environment.GetEnvironmentVariable("CLINIC_REVIEW_OUTPUT");
            if(string.IsNullOrEmpty(output))throw new InvalidOperationException("Set a new CLINIC_REVIEW_OUTPUT directory.");
            Directory.CreateDirectory(output);
            int version=int.TryParse(Environment.GetEnvironmentVariable("CLINIC_REVIEW_VERSION"),out var v)?v:8;
            ClinicWorld.PreviewGear=version;
            var host=new GameObject("Scripted equipment inspection");
            try
            {
                var world=host.AddComponent<ClinicWorld>();world.Initialize();
                var starter=ClinicSimulation.CreateNew().State;starter.Patients.Clear();starter.Room(ClinicRoom.Waiting).Built=true;
                foreach(var r in starter.Rooms){r.Built=true;r.Tier=20;}
                world.Render(starter,.1f,true);
                Save(world,new Vector3(-.4f,.14f,-.4f),9.5f,1200,1200,Path.Combine(output,"starter.png"));
                Save(world,new Vector3(-2.9f,.14f,-2.4f),4.2f,1200,1000,Path.Combine(output,"starter-reception.png"));
                Save(world,new Vector3(-2.9f,.14f,2.3f),4.2f,1200,1000,Path.Combine(output,"starter-firstaid.png"));
                Save(world,new Vector3(3.4f,.14f,1.6f),4.6f,1200,1200,Path.Combine(output,"starter-waiting.png"));
                world.ConfigureLocation(ClinicLocation.DoctorsClinic);
                var doctors=ClinicSimulation.CreateForLocation(ClinicLocation.DoctorsClinic).State;
                foreach(var r in doctors.Rooms){r.Built=true;r.Tier=40;}
                world.Render(doctors,.1f,true);
                Save(world,new Vector3(-1f,.14f,1f),16f,1400,1200,Path.Combine(output,"doctors.png"));
                var names=new[]{"reception","firstaid","waiting","consultation","pharmacy"};
                for(int i=0;i<5;i++)
                {
                    var b=DoctorsRoomBounds(i);
                    Save(world,b.center,Mathf.Max(b.size.x,b.size.z*1.2f)*.42f,1400,1000,Path.Combine(output,"doctors-"+names[i]+".png"));
                }
            }
            finally{ClinicWorld.PreviewGear=0;UnityEngine.Object.DestroyImmediate(host);}
        }
        private static Bounds DoctorsRoomBounds(int room)
        {
            var b=new Bounds[]{new Bounds(new Vector3(-6.85f,.14f,-5.50f),new Vector3(10.7f,.02f,4.7f)),new Bounds(new Vector3(-6.85f,.14f,.6f),new Vector3(10.7f,.02f,4.7f)),
                new Bounds(new Vector3(5.85f,.14f,-4.575f),new Vector3(8.7f,.02f,6.55f)),new Bounds(new Vector3(-1f,.14f,6.8f),new Vector3(22.4f,.02f,4.4f)),new Bounds(new Vector3(5.85f,.14f,1.425f),new Vector3(8.7f,.02f,3.05f))};
            return b[room];
        }
        private static void Save(ClinicWorld world,Vector3 focus,float size,int width,int height,string path)
        {
            world.SetRenderSize(width,height);var camera=world.SceneCamera;camera.orthographicSize=size;
            camera.transform.position=focus-camera.transform.forward*22;camera.Render();
            var previous=RenderTexture.active;RenderTexture.active=world.Texture;
            var pixels=new Texture2D(world.Texture.width,world.Texture.height,TextureFormat.RGB24,false);
            try{pixels.ReadPixels(new Rect(0,0,pixels.width,pixels.height),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());}
            finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(pixels);}
        }
    }
}
