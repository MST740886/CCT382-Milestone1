using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.Shapes;
using UnityEngine.SceneManagement;

public static class BuildSampleSceneLayout
{
    const string RootName = "Lab 1 - Three Level Rooms";
    static Transform root;
    static Material existingMaterial;

    [MenuItem("Lab 1/Build simple layout in SampleScene")]
    public static void Build()
    {
        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "SampleScene")
        {
            Debug.LogError("Open SampleScene before building the Lab 1 layout.");
            return;
        }
        var plane = GameObject.Find("Plane");
        var stairs = GameObject.Find("Stairs");
        var door = GameObject.Find("Door");
        if (!plane || !stairs || !door)
        {
            Debug.LogError("SampleScene needs its original Plane, Stairs and Door.");
            return;
        }
        if (GameObject.Find(RootName))
        {
            Debug.LogWarning("The Lab 1 layout is already in SampleScene.");
            return;
        }

        existingMaterial = plane.GetComponent<MeshRenderer>().sharedMaterial;
        root = new GameObject(RootName).transform;

        // Two ground-floor rooms. Keep the original Plane and its material as room one.
        plane.name = "Room 1 Floor (original Plane)";
        plane.transform.position = new Vector3(-10f, .001f, 0f);
        plane.transform.localScale = new Vector3(.3f, 1f, .3f);
        plane.transform.SetParent(root, true);
        var secondFloor = UnityEngine.Object.Instantiate(plane, root);
        secondFloor.name = "Room 2 Floor";
        secondFloor.transform.position = new Vector3(10f, .001f, 0f);

        // Upper level, built as four ProBuilder slabs around a clear 5 m stairwell.
        Box("Level 2 front deck", new Vector3(10f, 3f, -4.25f), new Vector3(12f, .2f, 3.5f));
        Box("Level 2 back deck", new Vector3(10f, 3f, 4.25f), new Vector3(12f, .2f, 3.5f));
        Box("Level 2 left deck", new Vector3(6.5f, 3f, 0f), new Vector3(5f, .2f, 5f));
        Box("Level 2 right deck", new Vector3(13.5f, 3f, 0f), new Vector3(5f, .2f, 5f));

        // Low bridge rises, crosses level, then returns to the ground room.
        Ramp("Bridge ramp - up", new Vector3(-2.5f, .75f, 0f), 3f, 1.5f, 3f, 0f);
        Box("Bridge flat", new Vector3(0f, 1.5f, 0f), new Vector3(2f, .2f, 3f));
        Ramp("Bridge ramp - down", new Vector3(2.5f, .75f, 0f), 3f, 1.5f, 3f, 180f);

        // Reuse the user's existing ProBuilder Stairs, turning them into the spiral stair.
        var pbShape = stairs.GetComponent("ProBuilderShape") as Component;
        if (pbShape)
        {
            var shapeField = pbShape.GetType().GetField("m_Shape", BindingFlags.Instance | BindingFlags.NonPublic);
            var stairShape = shapeField?.GetValue(pbShape) as Stairs;
            if (stairShape != null)
            {
                SetField(stairShape, "m_Circumference", 330f);
                SetField(stairShape, "m_InnerRadius", .65f);
                SetField(stairShape, "m_StepsCount", 18);
                stairShape.sides = false;
                var size = pbShape.GetType().GetProperty("size");
                size?.SetValue(pbShape, new Vector3(4.6f, 3f, 4.6f));
                var rebuild = pbShape.GetType().GetMethod("UpdateShape", BindingFlags.Instance | BindingFlags.NonPublic);
                rebuild?.Invoke(pbShape, null);
            }
        }
        stairs.name = "Spiral Stairs (original ProBuilder Stairs)";
        stairs.transform.position = new Vector3(10f, 1.5f, 0f);
        stairs.transform.localScale = Vector3.one;
        stairs.transform.SetParent(root, true);

        // Reuse the original ProBuilder Door and duplicate it for the upper-room doorway.
        door.name = "Door - Room 1 entrance (original Door)";
        SetDoorTransform(door.transform, new Vector3(-16f, 1.75f, 0f));
        door.transform.SetParent(root, true);
        var upperDoor = UnityEngine.Object.Instantiate(door, root);
        upperDoor.name = "Door - Level 2 entrance";
        SetDoorTransform(upperDoor.transform, new Vector3(16f, 4.75f, 0f));

        // Simple cover blocks in the first room.
        Box("Cover block 1", new Vector3(-13f, .65f, -3f), new Vector3(1.4f, 1.3f, 1.2f));
        Box("Cover block 2", new Vector3(-8f, .65f, -3f), new Vector3(1.4f, 1.3f, 1.2f));
        Box("Cover block 3", new Vector3(-12f, .65f, 3f), new Vector3(1.4f, 1.3f, 1.2f));
        Box("Cover block 4", new Vector3(-7f, .65f, 3f), new Vector3(1.4f, 1.3f, 1.2f));

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = GameObject.Find(RootName);
        if (SceneView.lastActiveSceneView)
            SceneView.lastActiveSceneView.Frame(new Bounds(new Vector3(0f, 1.5f, 0f), new Vector3(37f, 12f, 17f)), false);
        Debug.Log("Lab 1 simple three-level layout added to SampleScene. Original scene objects and materials were reused.");
    }

    static void SetField(object instance, string name, object value)
    {
        instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(instance, value);
    }

    static void SetDoorTransform(Transform t, Vector3 position)
    {
        t.position = position;
        t.rotation = Quaternion.Euler(0f, 90f, 0f);
        t.localScale = Vector3.one * .5f;
    }

    static ProBuilderMesh Box(string name, Vector3 position, Vector3 size)
    {
        var mesh = ShapeGenerator.GenerateCube(PivotLocation.Center, size);
        mesh.name = name;
        mesh.transform.SetParent(root, true);
        mesh.transform.position = position;
        mesh.GetComponent<MeshRenderer>().sharedMaterial = existingMaterial;
        mesh.ToMesh(); mesh.Refresh();
        mesh.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh.GetComponent<MeshFilter>().sharedMesh;
        return mesh;
    }

    static void Ramp(string name, Vector3 position, float length, float height, float width, float rotationY)
    {
        Vector3[] v = {
            new Vector3(-length/2,-height/2,-width/2), new Vector3(length/2,-height/2,-width/2),
            new Vector3(-length/2,-height/2,width/2), new Vector3(length/2,-height/2,width/2),
            new Vector3(-length/2,height/2,width/2), new Vector3(length/2,height/2,width/2)
        };
        Face[] f = {
            new Face(new[]{0,4,1,1,4,5}), new Face(new[]{0,2,4}), new Face(new[]{1,5,3}),
            new Face(new[]{2,3,4,3,5,4}), new Face(new[]{0,1,2,1,3,2})
        };
        var mesh = ProBuilderMesh.Create(v, f);
        mesh.name = name;
        mesh.transform.SetParent(root, true);
        mesh.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, rotationY, 0f));
        mesh.GetComponent<MeshRenderer>().sharedMaterial = existingMaterial;
        mesh.ToMesh(); mesh.Refresh();
        mesh.gameObject.AddComponent<MeshCollider>().sharedMesh = mesh.GetComponent<MeshFilter>().sharedMesh;
    }
}
