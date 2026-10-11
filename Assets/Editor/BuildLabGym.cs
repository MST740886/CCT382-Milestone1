using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.ProBuilder;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class BuildLabGym
{
    static Transform group;
    static Material navy, wall, white, blue, orange, green;
    static Material Mat(string name, Color color)
    {
        string path = "Assets/LabGym/Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
        m.color = color; m.SetFloat("_Smoothness", .22f); return m;
    }
    static ProBuilderMesh Finish(ProBuilderMesh pb, string name, Vector3 pos, Material mat)
    {
        pb.name = name; pb.transform.SetParent(group); pb.transform.position = pos;
        pb.GetComponent<MeshRenderer>().sharedMaterial = mat;
        pb.ToMesh(); pb.Refresh();
        var col = pb.GetComponent<MeshCollider>(); if (!col) col = pb.gameObject.AddComponent<MeshCollider>();
        col.sharedMesh = pb.GetComponent<MeshFilter>().sharedMesh;
        return pb;
    }
    static ProBuilderMesh Box(string name, Vector3 pos, Vector3 size, Material mat) => Finish(ShapeGenerator.GenerateCube(PivotLocation.Center, size), name, pos, mat);
    static void Ramp(string name, Vector3 pos, float width, float height, float length, Material mat)
    {
        var v = new [] { new Vector3(-width/2,0,0),new Vector3(width/2,0,0),new Vector3(-width/2,0,length),new Vector3(width/2,0,length),new Vector3(-width/2,height,length),new Vector3(width/2,height,length) };
        var f = new [] { new Face(new[]{0,4,1,1,4,5}),new Face(new[]{0,2,4}),new Face(new[]{1,5,3}),new Face(new[]{2,3,4,3,5,4}),new Face(new[]{0,1,2,1,3,2}) };
        Finish(ProBuilderMesh.Create(v,f),name,pos,mat);
    }
    static void Label(string text, Vector3 pos, int size, Color color, float scale=.11f)
    {
        var go = new GameObject(text); go.transform.SetParent(group); go.transform.position = pos;
        var tm = go.AddComponent<TextMesh>(); tm.text = text; tm.fontSize = size; tm.characterSize = scale;
        tm.anchor = TextAnchor.MiddleCenter; tm.alignment = TextAlignment.Center; tm.color = color;
    }
    static void Room(float x, string name, Material accent)
    {
        group = new GameObject(name).transform;
        Box("Floor",new Vector3(x,-.25f,0),new Vector3(16,.5f,16),navy);
        Box("Back wall",new Vector3(x,2,8),new Vector3(16,4,.3f),wall);
        Box("Front low wall",new Vector3(x,.45f,-8),new Vector3(16,.9f,.3f),wall);
        Box("Header stripe",new Vector3(x,3.4f,7.8f),new Vector3(15,.15f,.08f),accent);
        Box("Front stripe",new Vector3(x,.93f,-8),new Vector3(16,.06f,.34f),accent);
        for(int i=-7;i<=7;i++)
        {
            Box("Floor grid X",new Vector3(x+i,.006f,0),new Vector3(.018f,.012f,16),wall);
            Box("Floor grid Z",new Vector3(x,.007f,i),new Vector3(16,.012f,.018f),wall);
        }
        Label(name.ToUpper(),new Vector3(x,2.65f,7.78f),48,Color.white,.08f);
    }
    [MenuItem("Lab 1/Build three-room gym")]
    public static void Build()
    {
        Directory.CreateDirectory("Assets/LabGym/Materials"); Directory.CreateDirectory("Assets/LabGym/Settings");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
        AssetDatabase.CreateAsset(renderer,"Assets/LabGym/Settings/GymRenderer.asset");
        var pipeline = UniversalRenderPipelineAsset.Create(renderer);
        AssetDatabase.CreateAsset(pipeline,"Assets/LabGym/Settings/GymURP.asset");
        GraphicsSettings.defaultRenderPipeline = pipeline; QualitySettings.renderPipeline = pipeline;
        PlayerSettings.companyName = "CCT382"; PlayerSettings.productName = "Lab 1 - Three Room Test Gym";
        PlayerSettings.colorSpace = ColorSpace.Linear;
        navy=Mat("Floor - navy",new Color(.075f,.11f,.17f)); wall=Mat("Concrete",new Color(.32f,.40f,.48f)); white=Mat("Porcelain",new Color(.8f,.86f,.89f));
        blue=Mat("01 - cyan",new Color(.05f,.65f,.9f)); orange=Mat("02 - amber",new Color(1,.45f,.08f)); green=Mat("03 - mint",new Color(.12f,.8f,.49f));
        Room(-16,"01 / RAMP LAB",blue);
        Ramp("ProBuilder wedge ramp - 1 in 3",new Vector3(-19,0,-3),3,2,6,blue);
        Box("Ramp landing",new Vector3(-19,1,4.25f),new Vector3(3,2,2.5f),white);
        Ramp("ProBuilder wedge ramp - low",new Vector3(-13,0,-2),2.5f,1,4,blue);
        Box("Low landing",new Vector3(-13,.5f,3),new Vector3(2.5f,1,2),white);
        Label("SLOPE / 1:3",new Vector3(-19,.55f,-3.12f),32,Color.white,.065f);
        Room(0,"02 / STEP LAB",orange);
        Finish(ShapeGenerator.GenerateStair(PivotLocation.Center,new Vector3(3,2.4f,5),12,true),"ProBuilder stairs - 12 risers",new Vector3(-3,1.2f,0),orange);
        Box("Upper landing",new Vector3(-3,1.2f,3.6f),new Vector3(3,2.4f,2.2f),white);
        Ramp("Return ramp",new Vector3(2,0,-2.5f),3,2.4f,7.2f,orange);
        Box("Bridge",new Vector3(-.5f,2.25f,3.6f),new Vector3(2,.3f,2.2f),white);
        for(int i=0;i<5;i++) Box("Balance step "+(i+1),new Vector3(-4+i*2,.15f,-5),new Vector3(1.2f,.3f,1.2f),orange);
        Room(16,"03 / COVER LAB",green);
        for(int i=0;i<3;i++)
        {
            Box("Low cover "+(i+1),new Vector3(11+i*4,.6f,-3+(i%2)*2),new Vector3(2.4f,1.2f,1),green);
            Box("Tall cover "+(i+1),new Vector3(12+i*4,1.1f,3-(i%2)),new Vector3(1.4f,2.2f,1.4f),white);
        }
        Box("L cover long",new Vector3(21,.8f,-4.5f),new Vector3(3,1.6f,.7f),green);
        Box("L cover return",new Vector3(22.15f,.8f,-3.5f),new Vector3(.7f,1.6f,2.7f),green);
        group=new GameObject("Architecture / connected doorways").transform;
        foreach(float x in new[]{-24f,24f}) Box("Outer wall",new Vector3(x,1.5f,0),new Vector3(.3f,3,16),wall);
        foreach(float x in new[]{-8f,8f})
        {
            Box("Divider back",new Vector3(x,1.6f,2.5f),new Vector3(.3f,3.2f,11),wall);
            Box("Divider front",new Vector3(x,1.6f,-7),new Vector3(.3f,3.2f,2),wall);
            Box("Door lintel",new Vector3(x,3,-4.5f),new Vector3(.4f,.4f,3),white);
            Box("Threshold",new Vector3(x,.025f,-4.5f),new Vector3(1,.05f,3),white);
        }
        var sun=new GameObject("Sun").AddComponent<Light>(); sun.type=LightType.Directional; sun.intensity=2; sun.shadows=LightShadows.Soft; sun.transform.rotation=Quaternion.Euler(48,-35,0);
        RenderSettings.ambientMode=AmbientMode.Flat; RenderSettings.ambientLight=new Color(.55f,.63f,.75f);
        var cam=new GameObject("Overview Camera").AddComponent<Camera>(); cam.tag="MainCamera";
        cam.transform.position=new Vector3(27,33,-42); cam.transform.LookAt(new Vector3(0,0,0)); cam.orthographic=true; cam.orthographicSize=21;
        cam.backgroundColor=new Color(.028f,.045f,.075f); cam.clearFlags=CameraClearFlags.SolidColor; cam.gameObject.AddComponent<AudioListener>();
        var player=new GameObject("Player - TAB to walk"); player.transform.position=new Vector3(-21,.1f,-5); player.transform.rotation=Quaternion.Euler(0,25,0);
        var cc=player.AddComponent<CharacterController>(); cc.height=1.8f; cc.radius=.3f; cc.center=new Vector3(0,.9f,0); cc.stepOffset=.3f; cc.slopeLimit=50;
        var eye=new GameObject("Walk Camera").AddComponent<Camera>(); eye.transform.SetParent(player.transform); eye.transform.localPosition=new Vector3(0,1.6f,0); eye.transform.localRotation=Quaternion.identity; eye.nearClipPlane=.05f; eye.gameObject.AddComponent<AudioListener>();
        var walker=player.AddComponent<GymWalker>(); walker.eye=eye; walker.overview=cam; eye.gameObject.SetActive(false);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Lab1_TestGym.unity",true)};
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/Lab1_TestGym.unity"); AssetDatabase.SaveAssets();
        if(SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.LookAt(new Vector3(0,0,0),Quaternion.Euler(48,-25,0),35);
        Directory.CreateDirectory("Documentation");
        var meshes=Object.FindObjectsByType<ProBuilderMesh>(FindObjectsSortMode.None);
        File.WriteAllText("Documentation/Validation.txt","Unity: "+Application.unityVersion+"\nPipeline: "+GraphicsSettings.defaultRenderPipeline.name+"\nProBuilder meshes: "+meshes.Length+"\nMissing colliders: "+meshes.Count(m=>!m.GetComponent<Collider>())+"\nScene: Assets/Scenes/Lab1_TestGym.unity\n");
        var rt=new RenderTexture(1920,1080,24); cam.targetTexture=rt; cam.Render(); RenderTexture.active=rt;
        var tex=new Texture2D(1920,1080,TextureFormat.RGB24,false); tex.ReadPixels(new Rect(0,0,1920,1080),0,0); tex.Apply();
        File.WriteAllBytes("Documentation/Overview.png",tex.EncodeToPNG()); cam.targetTexture=null; RenderTexture.active=null; Object.DestroyImmediate(rt); Object.DestroyImmediate(tex);
        Debug.Log("LAB_GYM_BUILD_SUCCESS");
    }
}
