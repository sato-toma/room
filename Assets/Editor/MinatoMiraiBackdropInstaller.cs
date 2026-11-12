using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class MinatoMiraiBackdropInstaller
{
    private const string PrefabPath = "Assets/Generated/Prefabs/NeonCityViewBackdrop.prefab";
    private const string GeneratedRootName = "Minato Mirai Landmarks";
    private const string MeshFolderPath = "Assets/Generated/Meshes/MinatoMiraiLandmarks";
    private const string FacadeAPath = "Assets/Generated/Materials/NeonCityFacadeA.mat";
    private const string FacadeBPath = "Assets/Generated/Materials/NeonCityFacadeB.mat";
    private const string WarmLightPath = "Assets/Generated/Materials/NeonCityStreetWarmLight.mat";
    private const string CyanAccentPath = "Assets/Generated/Materials/NeonCityArcAccent.mat";
    private const string ChampagnePath = "Assets/Generated/Materials/ChampagneTitanium.mat";
    private const string TowerSignalRedPath = "Assets/Generated/Materials/JapanTowerSignalRed.mat";
    private const string HarborWaterPath = "Assets/Generated/Materials/JapanHarborWater.mat";
    private const string HarborHullPath = "Assets/Generated/Materials/JapanHarborHull.mat";

    public static string Inspect()
    {
        var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var renderers = prefab.GetComponentsInChildren<Renderer>(true);
            var bounds = CalculateBounds(renderers);
            var names = new List<string>();
            foreach (Transform child in prefab.transform)
                names.Add($"{child.name} @ {child.localPosition}");
            return $"Prefab: {PrefabPath}\nBounds: {bounds}\nRoot children:\n{string.Join("\n", names)}";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    public static string InspectHarbor()
    {
        var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var result = new List<string>();
            foreach (var name in new[] { "City Podium", $"{GeneratedRootName}/Yokohama Night Harbor" })
            {
                var target = prefab.transform.Find(name);
                if (target == null)
                {
                    result.Add($"{name}: missing");
                    continue;
                }

                result.Add($"{name} position={target.position}, scale={target.lossyScale}");
                foreach (var renderer in target.GetComponentsInChildren<Renderer>(true))
                    result.Add($"  {renderer.name}: bounds={renderer.bounds}, material={renderer.sharedMaterial?.name}");
            }
            return string.Join("\n", result);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    public static string Install()
    {
        var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var facadeA = LoadMaterial(FacadeAPath);
            var facadeB = LoadMaterial(FacadeBPath);
            var warmLight = LoadMaterial(WarmLightPath);
            var cyanAccent = LoadMaterial(CyanAccentPath);
            var champagne = LoadMaterial(ChampagnePath);
            var towerSignalRed = CreateOrUpdateSignalMaterial();
            var harborWater = CreateOrUpdateHarborWaterMaterial();
            var harborHull = CreateOrUpdateHarborHullMaterial();

            var oldRoot = prefab.transform.Find(GeneratedRootName);
            if (oldRoot != null)
                UnityEngine.Object.DestroyImmediate(oldRoot.gameObject);

            var landmarks = new GameObject(GeneratedRootName);
            landmarks.transform.SetParent(prefab.transform, false);

            EnsureFolder("Assets/Generated/Meshes");
            EnsureFolder(MeshFolderPath);
            BuildLandmarkTower(landmarks.transform, facadeA, warmLight, cyanAccent);
            BuildSailHotel(landmarks.transform, facadeB, warmLight, champagne);
            BuildFerrisWheel(landmarks.transform, champagne, warmLight, cyanAccent);
            BuildJapaneseNightscapeLandmarks(landmarks.transform, facadeA, facadeB, warmLight, champagne, cyanAccent, towerSignalRed);
            BuildNightHarbor(landmarks.transform, harborWater, harborHull, warmLight, champagne, cyanAccent);

            var colliders = landmarks.GetComponentsInChildren<Collider>(true);
            if (colliders.Length != 0)
                throw new InvalidOperationException("Generated city landmarks must not have colliders.");

            AssetDatabase.SaveAssets();
            var saved = PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
            if (saved == null)
                throw new IOException($"Could not save the city backdrop prefab: {PrefabPath}");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return $"Updated {PrefabPath}\nLandmarks: {landmarks.transform.childCount}\nRenderers: {landmarks.GetComponentsInChildren<Renderer>(true).Length}\nColliders: {colliders.Length}";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    public static string Validate()
    {
        var prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var landmarks = prefab.transform.Find(GeneratedRootName);
            if (landmarks == null)
                throw new InvalidOperationException($"Missing generated root: {GeneratedRootName}");

            var expected = new[]
            {
                "Landmark Tower",
                "Sail Hotel",
                "Cosmo Clock Ferris Wheel",
                "Tokyo Tower",
                "Tokyo Skytree",
                "Tokyo Metropolitan Government Building",
                "Umeda Sky Building",
                "Abeno Harukas",
                "Kobe Port Tower",
                "Nagoya TV Tower",
                "Sapporo TV Tower",
                "Fukuoka Tower",
                "Kyoto Tower",
                "Yokohama Night Harbor"
            };
            foreach (var name in expected)
            {
                if (landmarks.Find(name) == null)
                    throw new InvalidOperationException($"Missing landmark: {name}");
            }

            var colliders = landmarks.GetComponentsInChildren<Collider>(true);
            if (colliders.Length != 0)
                throw new InvalidOperationException($"Unexpected colliders in city landmarks: {colliders.Length}");

            var renderers = landmarks.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException("No landmark renderers were generated.");

            var filters = landmarks.GetComponentsInChildren<MeshFilter>(true);
            foreach (var filter in filters)
            {
                if (filter.sharedMesh == null || filter.sharedMesh.vertexCount == 0)
                    throw new InvalidOperationException($"Landmark mesh is missing or empty: {filter.name}");
            }

            var summary = new List<string>();
            foreach (var name in expected)
            {
                var landmark = landmarks.Find(name);
                var landmarkRenderers = landmark.GetComponentsInChildren<Renderer>(true);
                summary.Add($"{name}: {CalculateBounds(landmarkRenderers)}");
            }

            var harbor = landmarks.Find("Yokohama Night Harbor");
            foreach (var shipName in new[] { "Cruise Ship", "Harbor Ferry", "Harbor Tug" })
            {
                if (harbor.Find(shipName) == null)
                    throw new InvalidOperationException($"Missing harbor vessel: {shipName}");
            }

            return $"Validated {GeneratedRootName}: {expected.Length - 1} landmarks, {harbor.childCount} harbor elements, {renderers.Length} renderers, {filters.Length} non-empty meshes, no colliders.\n{string.Join("\n", summary)}";
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefab);
        }
    }

    public static string FrameCityView()
    {
        var view = SceneView.lastActiveSceneView;
        if (view == null)
            throw new InvalidOperationException("There is no active Unity Scene View to frame.");

        view.LookAt(new Vector3(0f, -3f, 48f), Quaternion.Euler(45f, 0f, 0f), 65f);
        view.Repaint();
        return "Framed the Scene View toward the city and harbor.";
    }

    private static void BuildLandmarkTower(Transform parent, Material facade, Material windows, Material cyan)
    {
        var landmark = CreateRoot("Landmark Tower", parent, new Vector3(-30f, 0f, 48f));
        var structure = new MeshBuilder();
        structure.AddBox(new Vector3(0f, 6.5f, 0f), new Vector3(11f, 13f, 10f));
        structure.AddBox(new Vector3(0f, 28f, 0f), new Vector3(8f, 30f, 7.6f));
        structure.AddBox(new Vector3(0f, 47f, 0f), new Vector3(8.7f, 8f, 8.3f));
        structure.AddBox(new Vector3(0f, 53f, 0f), new Vector3(0.55f, 5f, 0.55f));
        structure.AddBox(new Vector3(-3.2f, 42f, 0f), new Vector3(0.24f, 10f, 7.9f));
        structure.AddBox(new Vector3(3.2f, 42f, 0f), new Vector3(0.24f, 10f, 7.9f));
        CreateMeshObject("Tower Structure", landmark.transform, structure, facade);

        var windowMesh = new MeshBuilder();
        for (var floor = 0; floor < 12; floor++)
        {
            var y = 16f + floor * 2.25f;
            for (var column = 0; column < 5; column++)
            {
                var x = -2.5f + column * 1.25f;
                windowMesh.AddBox(new Vector3(x, y, -3.84f), new Vector3(0.48f, 0.72f, 0.07f));
            }
        }
        windowMesh.AddBox(new Vector3(0f, 44.1f, -4.2f), new Vector3(7.4f, 0.22f, 0.1f));
        CreateMeshObject("Tower Windows", landmark.transform, windowMesh, windows);

        var accentMesh = new MeshBuilder();
        accentMesh.AddBox(new Vector3(0f, 13.15f, -5.08f), new Vector3(8.8f, 0.2f, 0.12f));
        accentMesh.AddBox(new Vector3(0f, 50.2f, -4.23f), new Vector3(7.1f, 0.14f, 0.1f));
        CreateMeshObject("Tower Cyan Bands", landmark.transform, accentMesh, cyan);
    }

    private static void BuildSailHotel(Transform parent, Material facade, Material windows, Material trim)
    {
        var hotel = CreateRoot("Sail Hotel", parent, new Vector3(18f, 0f, 58f));
        var structure = new MeshBuilder();
        structure.AddPrism(
            new[]
            {
                new Vector2(-7.2f, 0f),
                new Vector2(7.2f, 0f),
                new Vector2(7.2f, 24f),
                new Vector2(1.2f, 36f),
                new Vector2(-4.6f, 31f),
                new Vector2(-7.2f, 17f)
            },
            8f);
        CreateMeshObject("Sail-Shaped Hotel Structure", hotel.transform, structure, facade);

        var windowMesh = new MeshBuilder();
        var rows = new[]
        {
            new Vector3(0f, 4f, 12f),
            new Vector3(0f, 8f, 12f),
            new Vector3(0f, 12f, 11f),
            new Vector3(0f, 16f, 10f),
            new Vector3(0f, 20f, 9f),
            new Vector3(0f, 24f, 7f),
            new Vector3(0.4f, 28f, 5f)
        };
        foreach (var row in rows)
        {
            var halfWidth = row.z * 0.5f;
            var count = Mathf.Max(2, Mathf.FloorToInt(row.z / 1.7f));
            var segmentWidth = row.z / count;
            for (var i = 0; i < count; i++)
            {
                var x = -halfWidth + (i + 0.5f) * segmentWidth;
                windowMesh.AddBox(new Vector3(x, row.y, -4.08f), new Vector3(segmentWidth * 0.58f, 0.72f, 0.08f));
            }
        }
        CreateMeshObject("Hotel Window Bands", hotel.transform, windowMesh, windows);

        var trimMesh = new MeshBuilder();
        AddBeam2D(trimMesh, new Vector2(-4.6f, 31f), new Vector2(1.2f, 36f), 0.22f, 8.12f);
        AddBeam2D(trimMesh, new Vector2(1.2f, 36f), new Vector2(7.2f, 24f), 0.22f, 8.12f);
        CreateMeshObject("Sail Roofline Trim", hotel.transform, trimMesh, trim);
    }

    private static void BuildFerrisWheel(Transform parent, Material frame, Material cabins, Material cyan)
    {
        var wheel = CreateRoot("Cosmo Clock Ferris Wheel", parent, new Vector3(49f, 0f, 44f));
        const float centerY = 20f;
        const float outerRadius = 14f;
        const float innerRadius = 10.5f;

        var frameMesh = new MeshBuilder();
        AddRing(frameMesh, centerY, outerRadius, 0.34f, 0.32f, 32);
        AddRing(frameMesh, centerY, innerRadius, 0.24f, 0.24f, 32);
        for (var i = 0; i < 16; i++)
        {
            var angle = i * Mathf.PI * 2f / 16f;
            var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
            var length = outerRadius - 0.6f;
            var center = new Vector3(0f, centerY, 0f) + direction * (length * 0.5f);
            frameMesh.AddBox(center, new Vector3(0.18f, length, 0.2f),
                Quaternion.FromToRotation(Vector3.up, direction));
        }
        AddBeam(frameMesh, new Vector3(-7f, 0.4f, 0.2f), new Vector3(0f, centerY, 0.2f), 0.48f, 0.48f);
        AddBeam(frameMesh, new Vector3(7f, 0.4f, 0.2f), new Vector3(0f, centerY, 0.2f), 0.48f, 0.48f);
        CreateMeshObject("Ferris Wheel Frame", wheel.transform, frameMesh, frame);

        var cabinMesh = new MeshBuilder();
        for (var i = 0; i < 16; i++)
        {
            var angle = i * Mathf.PI * 2f / 16f;
            var x = Mathf.Cos(angle) * (outerRadius - 0.5f);
            var y = centerY + Mathf.Sin(angle) * (outerRadius - 0.5f);
            cabinMesh.AddBox(new Vector3(x, y, -0.02f), new Vector3(0.92f, 0.78f, 0.68f));
        }
        CreateMeshObject("Ferris Wheel Gondolas", wheel.transform, cabinMesh, cabins);

        var hubMesh = new MeshBuilder();
        hubMesh.AddBox(new Vector3(0f, centerY, -0.16f), new Vector3(1.55f, 1.55f, 0.65f));
        CreateMeshObject("Clock Hub", wheel.transform, hubMesh, cyan);
    }

    private static void BuildJapaneseNightscapeLandmarks(
        Transform parent, Material facadeA, Material facadeB, Material windows, Material warmFrame,
        Material cyan, Material towerSignalRed)
    {
        BuildTokyoTower(parent, towerSignalRed, windows, cyan);
        BuildSkytree(parent, facadeB, windows, cyan);
        BuildTokyoGovernmentBuilding(parent, facadeA, windows, warmFrame);
        BuildUmedaSkyBuilding(parent, facadeB, windows, cyan);
        BuildAbenoHarukas(parent, facadeA, windows, cyan);
        BuildKobePortTower(parent, towerSignalRed, windows, cyan);
        BuildNagoyaTvTower(parent, facadeB, windows, warmFrame);
        BuildSapporoTvTower(parent, facadeA, windows, warmFrame);
        BuildFukuokaTower(parent, facadeB, windows, cyan);
        BuildKyotoTower(parent, facadeA, windows, warmFrame);
    }

    private static void BuildNightHarbor(
        Transform parent, Material water, Material hull, Material windows, Material frame, Material cyan)
    {
        var harbor = CreateRoot("Yokohama Night Harbor", parent, Vector3.zero);

        var sea = new MeshBuilder();
        sea.AddBox(new Vector3(-46.5f, -3.9f, 45f), new Vector3(34f, 0.12f, 60f));
        sea.AddBox(new Vector3(46.5f, -3.9f, 45f), new Vector3(34f, 0.12f, 60f));
        CreateMeshObject("Harbor Water", harbor.transform, sea, water);

        var reflections = new MeshBuilder();
        var westReflections = new[]
        {
            new Vector3(-62f, -3.79f, 32f), new Vector3(-43f, -3.79f, 46f),
            new Vector3(-55f, -3.79f, 57f), new Vector3(-35f, -3.79f, 72f),
            new Vector3(-64f, -3.79f, 88f)
        };
        var eastReflections = new[]
        {
            new Vector3(35f, -3.79f, 38f), new Vector3(58f, -3.79f, 49f),
            new Vector3(39f, -3.79f, 64f), new Vector3(61f, -3.79f, 77f),
            new Vector3(43f, -3.79f, 94f)
        };
        AddWaterReflections(reflections, westReflections);
        AddWaterReflections(reflections, eastReflections);
        CreateMeshObject("Harbor Light Reflections", harbor.transform, reflections, cyan);

        BuildHarborPier(harbor.transform, frame, windows, cyan, -28.8f, "West Pier");
        BuildHarborPier(harbor.transform, frame, windows, cyan, 28.8f, "East Pier");
        BuildCruiseShip(harbor.transform, hull, windows, frame, cyan);
        BuildHarborFerry(harbor.transform, hull, windows, frame, cyan);
        BuildHarborTug(harbor.transform, hull, windows, frame);
        BuildHarborCranes(harbor.transform, frame, cyan, -63f, "West Harbor Crane");
        BuildHarborCranes(harbor.transform, frame, cyan, 64f, "East Harbor Crane");
    }

    private static void AddWaterReflections(MeshBuilder mesh, Vector3[] positions)
    {
        for (var i = 0; i < positions.Length; i++)
        {
            var width = i % 2 == 0 ? 5.5f : 3.5f;
            mesh.AddBox(positions[i], new Vector3(width, 0.025f, 0.09f));
            mesh.AddBox(positions[i] + new Vector3(1.7f, 0.006f, 0.28f),
                new Vector3(width * 0.45f, 0.02f, 0.06f));
        }
    }

    private static void BuildHarborPier(
        Transform parent, Material structureMaterial, Material lightsMaterial, Material cyan, float x, string name)
    {
        var pier = CreateRoot(name, parent, new Vector3(x, 0f, 47f));
        var structure = new MeshBuilder();
        structure.AddBox(new Vector3(0f, -3.55f, 0f), new Vector3(2.4f, 0.45f, 48f));
        for (var z = -21f; z <= 21f; z += 7f)
            structure.AddBox(new Vector3(0f, -3.86f, z), new Vector3(0.35f, 0.8f, 0.35f));
        CreateMeshObject("Pier Deck and Piles", pier.transform, structure, structureMaterial);

        var lights = new MeshBuilder();
        for (var z = -20f; z <= 20f; z += 5f)
        {
            lights.AddBox(new Vector3(-0.88f, -3.27f, z), new Vector3(0.12f, 0.12f, 0.12f));
            lights.AddBox(new Vector3(0.88f, -3.27f, z), new Vector3(0.12f, 0.12f, 0.12f));
        }
        CreateMeshObject("Pier Bollard Lights", pier.transform, lights, lightsMaterial);

        var edge = new MeshBuilder();
        edge.AddBox(new Vector3(-1.13f, -3.31f, 0f), new Vector3(0.08f, 0.07f, 46f));
        edge.AddBox(new Vector3(1.13f, -3.31f, 0f), new Vector3(0.08f, 0.07f, 46f));
        CreateMeshObject("Pier Cyan Edge", pier.transform, edge, cyan);
    }

    private static void BuildCruiseShip(Transform parent, Material hullMaterial, Material windows, Material trim, Material cyan)
    {
        var ship = CreateRoot("Cruise Ship", parent, new Vector3(-47f, -4.02f, 20f));
        var hull = new MeshBuilder();
        hull.AddPrism(new[]
        {
            new Vector2(-14f, 0.4f), new Vector2(11f, 0.4f), new Vector2(14f, 1.5f),
            new Vector2(10f, 3.1f), new Vector2(-10f, 3.1f), new Vector2(-13f, 1.6f)
        }, 4.2f);
        hull.AddBox(new Vector3(-1f, 3.4f, 0f), new Vector3(17f, 1.2f, 3.6f));
        hull.AddBox(new Vector3(-2f, 5f, 0.2f), new Vector3(12f, 2.2f, 3.1f));
        hull.AddBox(new Vector3(-3.5f, 6.8f, 0.3f), new Vector3(6f, 1.6f, 2.4f));
        hull.AddBox(new Vector3(2f, 8.2f, 0.3f), new Vector3(1.3f, 1.2f, 1.3f));
        hull.AddBox(new Vector3(2f, 9.2f, 0.3f), new Vector3(0.16f, 0.9f, 0.16f));
        CreateMeshObject("Passenger Liner Hull and Decks", ship.transform, hull, hullMaterial);

        var cabinLights = new MeshBuilder();
        for (var x = -9f; x <= 8f; x += 2.1f)
        {
            cabinLights.AddBox(new Vector3(x, 4.9f, -1.62f), new Vector3(0.75f, 0.42f, 0.08f));
            cabinLights.AddBox(new Vector3(x, 6.1f, -1.27f), new Vector3(0.58f, 0.34f, 0.08f));
        }
        CreateMeshObject("Passenger Deck Windows", ship.transform, cabinLights, windows);

        var accent = new MeshBuilder();
        accent.AddBox(new Vector3(0f, 3.24f, -2.13f), new Vector3(18f, 0.16f, 0.09f));
        accent.AddBox(new Vector3(-11f, 2.8f, -2.13f), new Vector3(3.2f, 0.18f, 0.09f));
        CreateMeshObject("Liner Waterline Trim", ship.transform, accent, cyan);

        var mast = new MeshBuilder();
        mast.AddBox(new Vector3(2f, 9.8f, 0.3f), new Vector3(0.18f, 0.5f, 0.18f));
        CreateMeshObject("Liner Signal Mast", ship.transform, mast, trim);
    }

    private static void BuildHarborFerry(Transform parent, Material hullMaterial, Material windows, Material trim, Material cyan)
    {
        var ferry = CreateRoot("Harbor Ferry", parent, new Vector3(43f, -4.02f, 27f));
        var body = new MeshBuilder();
        body.AddPrism(new[]
        {
            new Vector2(-8f, 0.35f), new Vector2(6f, 0.35f), new Vector2(8f, 1.5f),
            new Vector2(5.5f, 2.5f), new Vector2(-6.5f, 2.5f)
        }, 3.2f);
        body.AddBox(new Vector3(-0.5f, 3.05f, 0f), new Vector3(10f, 1.2f, 2.8f));
        body.AddBox(new Vector3(-1.5f, 4.2f, 0f), new Vector3(5.4f, 1.4f, 2.3f));
        CreateMeshObject("Ferry Hull and Bridge", ferry.transform, body, hullMaterial);

        var lights = new MeshBuilder();
        for (var x = -4f; x <= 3f; x += 1.4f)
            lights.AddBox(new Vector3(x, 4.2f, -1.18f), new Vector3(0.55f, 0.52f, 0.08f));
        CreateMeshObject("Ferry Cabin Windows", ferry.transform, lights, windows);

        var trimMesh = new MeshBuilder();
        trimMesh.AddBox(new Vector3(0f, 2.55f, -1.62f), new Vector3(11f, 0.13f, 0.08f));
        CreateMeshObject("Ferry Cyan Waterline", ferry.transform, trimMesh, cyan);

        var mast = new MeshBuilder();
        mast.AddBox(new Vector3(0f, 5.35f, 0f), new Vector3(0.14f, 1.4f, 0.14f));
        CreateMeshObject("Ferry Mast", ferry.transform, mast, trim);
    }

    private static void BuildHarborTug(Transform parent, Material hullMaterial, Material windows, Material trim)
    {
        var tug = CreateRoot("Harbor Tug", parent, new Vector3(59f, -4.02f, 38f));
        var hull = new MeshBuilder();
        hull.AddPrism(new[]
        {
            new Vector2(-5.3f, 0.2f), new Vector2(4.5f, 0.2f), new Vector2(5.3f, 1.4f),
            new Vector2(3.7f, 2.2f), new Vector2(-4.4f, 2.2f)
        }, 2.8f);
        hull.AddBox(new Vector3(-0.4f, 2.65f, 0f), new Vector3(4.2f, 1.2f, 2.3f));
        hull.AddBox(new Vector3(-0.7f, 3.9f, 0f), new Vector3(2.5f, 1.5f, 1.9f));
        hull.AddBox(new Vector3(1.8f, 3.45f, 0.1f), new Vector3(1.2f, 0.55f, 1.5f));
        CreateMeshObject("Tugboat Hull and Cabin", tug.transform, hull, hullMaterial);

        var lights = new MeshBuilder();
        lights.AddBox(new Vector3(-0.7f, 3.9f, -0.98f), new Vector3(1.75f, 0.62f, 0.08f));
        CreateMeshObject("Tugboat Cabin Window", tug.transform, lights, windows);
        AddAntennaBeacon(tug.transform, trim, new Vector3(-0.8f, 5f, 0f));
    }

    private static void BuildHarborCranes(Transform parent, Material structureMaterial, Material cyan, float x, string name)
    {
        var crane = CreateRoot(name, parent, new Vector3(x, -3.9f, 55f));
        var frameMesh = new MeshBuilder();
        AddBeam(frameMesh, new Vector3(-3.5f, 0.4f, 0f), new Vector3(-3.5f, 14f, 0f), 0.55f, 0.55f);
        AddBeam(frameMesh, new Vector3(3.5f, 0.4f, 0f), new Vector3(3.5f, 14f, 0f), 0.55f, 0.55f);
        AddBeam(frameMesh, new Vector3(-3.5f, 14f, 0f), new Vector3(6f, 14f, 0f), 0.7f, 0.65f);
        AddBeam(frameMesh, new Vector3(5.5f, 13.8f, 0f), new Vector3(5.5f, 3.5f, 0f), 0.22f, 0.24f);
        AddBeam(frameMesh, new Vector3(-3.5f, 0.5f, 0f), new Vector3(3.5f, 0.5f, 0f), 0.55f, 0.55f);
        AddBeam(frameMesh, new Vector3(-3.5f, 4f, 0f), new Vector3(3.5f, 11f, 0f), 0.2f, 0.24f);
        AddBeam(frameMesh, new Vector3(3.5f, 4f, 0f), new Vector3(-3.5f, 11f, 0f), 0.2f, 0.24f);
        CreateMeshObject("Port Crane Frame", crane.transform, frameMesh, structureMaterial);

        var accents = new MeshBuilder();
        accents.AddBox(new Vector3(0f, 14.1f, -0.36f), new Vector3(6.7f, 0.14f, 0.08f));
        accents.AddBox(new Vector3(3.5f, 1.5f, -0.3f), new Vector3(4.8f, 0.13f, 0.08f));
        CreateMeshObject("Crane Cyan Safety Lights", crane.transform, accents, cyan);
    }

    private static void BuildTokyoTower(Transform parent, Material frame, Material windows, Material cyan)
    {
        var root = CreateRoot("Tokyo Tower", parent, new Vector3(-42f, 0f, 49f));
        var structure = new MeshBuilder();
        AddBeam(structure, new Vector3(-5f, 0f, 0f), new Vector3(-1.1f, 31f, 0f), 0.6f, 0.65f);
        AddBeam(structure, new Vector3(5f, 0f, 0f), new Vector3(1.1f, 31f, 0f), 0.6f, 0.65f);
        AddBeam(structure, new Vector3(-5f, 0f, 0f), new Vector3(0f, 34f, 0f), 0.52f, 0.55f);
        AddBeam(structure, new Vector3(5f, 0f, 0f), new Vector3(0f, 34f, 0f), 0.52f, 0.55f);
        for (var level = 0; level < 5; level++)
        {
            var y = 4f + level * 5.2f;
            var halfWidth = Mathf.Lerp(4.5f, 0.9f, y / 34f);
            AddBeam(structure, new Vector3(-halfWidth, y, 0f), new Vector3(halfWidth, y, 0f), 0.28f, 0.35f);
            if (level < 4)
            {
                var nextY = y + 5.2f;
                var nextHalfWidth = Mathf.Lerp(4.5f, 0.9f, nextY / 34f);
                AddBeam(structure, new Vector3(-halfWidth, y, 0f), new Vector3(nextHalfWidth, nextY, 0f), 0.17f, 0.22f);
                AddBeam(structure, new Vector3(halfWidth, y, 0f), new Vector3(-nextHalfWidth, nextY, 0f), 0.17f, 0.22f);
            }
        }
        structure.AddBox(new Vector3(0f, 19f, 0f), new Vector3(4.6f, 1.4f, 2.8f));
        structure.AddBox(new Vector3(0f, 28f, 0f), new Vector3(2.4f, 1.1f, 2.2f));
        structure.AddBox(new Vector3(0f, 35.2f, 0f), new Vector3(0.28f, 2.8f, 0.3f));
        CreateMeshObject("Orange Lattice Tower", root.transform, structure, frame);
        AddAntennaBeacon(root.transform, windows, new Vector3(0f, 36.2f, -0.2f));
        AddBaseAccent(root.transform, cyan, 7f, 0.03f);
    }

    private static void BuildSkytree(Transform parent, Material facade, Material windows, Material cyan)
    {
        var root = CreateRoot("Tokyo Skytree", parent, new Vector3(-59f, 0f, 69f));
        var shaft = new MeshBuilder();
        shaft.AddPrism(new[]
        {
            new Vector2(-4.2f, 0f), new Vector2(4.2f, 0f), new Vector2(2.6f, 27f),
            new Vector2(1.5f, 43f), new Vector2(0.9f, 56f), new Vector2(-0.9f, 56f),
            new Vector2(-1.5f, 43f), new Vector2(-2.6f, 27f)
        }, 4f);
        shaft.AddBox(new Vector3(0f, 22f, -2.05f), new Vector3(7.7f, 1.4f, 0.28f));
        shaft.AddBox(new Vector3(0f, 38f, -1.65f), new Vector3(4.5f, 1.1f, 0.25f));
        shaft.AddBox(new Vector3(0f, 50f, -1.15f), new Vector3(2.9f, 0.75f, 0.22f));
        CreateMeshObject("Tapered Skytree Shaft", root.transform, shaft, facade);

        var lights = new MeshBuilder();
        for (var y = 8f; y <= 48f; y += 4f)
        {
            var width = Mathf.Lerp(6.7f, 1.3f, y / 56f);
            lights.AddBox(new Vector3(0f, y, -2.1f), new Vector3(width, 0.13f, 0.08f));
        }
        CreateMeshObject("Skytree Observation Lights", root.transform, lights, windows);
        AddAntennaBeacon(root.transform, cyan, new Vector3(0f, 56f, -0.1f));
    }

    private static void BuildTokyoGovernmentBuilding(Transform parent, Material facade, Material windows, Material trim)
    {
        var root = CreateRoot("Tokyo Metropolitan Government Building", parent, new Vector3(-23f, 0f, 88f));
        var structure = new MeshBuilder();
        structure.AddBox(new Vector3(-3.1f, 14f, 0f), new Vector3(5.4f, 28f, 5.6f));
        structure.AddBox(new Vector3(3.1f, 14f, 0f), new Vector3(5.4f, 28f, 5.6f));
        structure.AddBox(new Vector3(0f, 1.5f, 0.2f), new Vector3(14f, 3f, 7f));
        for (var side = -1; side <= 1; side += 2)
        {
            var x = side * 3.1f;
            structure.AddPrism(new[]
            {
                new Vector2(x - 2.7f, 27f), new Vector2(x + 2.7f, 27f),
                new Vector2(x + 2.7f, 32f), new Vector2(x + 0.45f, 35f),
                new Vector2(x - 0.45f, 35f), new Vector2(x - 2.7f, 32f)
            }, 5.6f);
        }
        CreateMeshObject("Twin Government Towers", root.transform, structure, facade);

        var lights = new MeshBuilder();
        for (var i = 0; i < 10; i++)
        {
            var y = 5f + i * 2.2f;
            lights.AddBox(new Vector3(-3.1f, y, -2.86f), new Vector3(4.3f, 0.12f, 0.06f));
            lights.AddBox(new Vector3(3.1f, y, -2.86f), new Vector3(4.3f, 0.12f, 0.06f));
        }
        CreateMeshObject("Twin Tower Window Bands", root.transform, lights, windows);
        var crownLink = new MeshBuilder();
        AddBeam2D(crownLink, new Vector2(-3.1f, 34f), new Vector2(3.1f, 34f), 0.3f, 5.75f);
        CreateMeshObject("Twin Tower Crown Link", root.transform, crownLink, trim);
    }

    private static void BuildUmedaSkyBuilding(Transform parent, Material facade, Material windows, Material cyan)
    {
        var root = CreateRoot("Umeda Sky Building", parent, new Vector3(-2f, 0f, 94f));
        var structure = new MeshBuilder();
        structure.AddBox(new Vector3(-3.2f, 12f, 0f), new Vector3(5.6f, 24f, 5.4f));
        structure.AddBox(new Vector3(3.2f, 12f, 0f), new Vector3(5.6f, 24f, 5.4f));
        structure.AddBox(new Vector3(0f, 25.2f, 0f), new Vector3(10.5f, 1.3f, 6.2f));
        structure.AddBox(new Vector3(0f, 27.1f, 0f), new Vector3(9.2f, 0.35f, 5.2f));
        CreateMeshObject("Twin Towers and Floating Observatory", root.transform, structure, facade);

        var lights = new MeshBuilder();
        for (var i = 0; i < 8; i++)
        {
            var y = 5f + i * 2.3f;
            lights.AddBox(new Vector3(-3.2f, y, -2.76f), new Vector3(4.2f, 0.1f, 0.05f));
            lights.AddBox(new Vector3(3.2f, y, -2.76f), new Vector3(4.2f, 0.1f, 0.05f));
        }
        CreateMeshObject("Umeda Window Bands", root.transform, lights, windows);
        var ring = new MeshBuilder();
        AddRing(ring, 27.7f, 4.4f, 0.2f, 0.2f, 24);
        CreateMeshObject("Floating Observatory Ring", root.transform, ring, cyan);
    }

    private static void BuildAbenoHarukas(Transform parent, Material facade, Material windows, Material cyan)
    {
        var root = CreateRoot("Abeno Harukas", parent, new Vector3(26f, 0f, 88f));
        var structure = new MeshBuilder();
        structure.AddBox(new Vector3(0f, 17f, 0f), new Vector3(9.6f, 34f, 7.6f));
        structure.AddBox(new Vector3(0f, 36.5f, 0f), new Vector3(10.2f, 5f, 8.2f));
        structure.AddBox(new Vector3(0f, 40f, 0f), new Vector3(7.6f, 2f, 6.2f));
        CreateMeshObject("Abeno Tower Profile", root.transform, structure, facade);

        var windowsMesh = new MeshBuilder();
        for (var y = 5f; y < 33f; y += 2.4f)
            windowsMesh.AddBox(new Vector3(0f, y, -3.84f), new Vector3(8.4f, 0.1f, 0.06f));
        windowsMesh.AddBox(new Vector3(0f, 37f, -4.16f), new Vector3(8.8f, 0.14f, 0.06f));
        CreateMeshObject("Abeno Window Bands", root.transform, windowsMesh, windows);

        var fins = new MeshBuilder();
        for (var x = -3; x <= 3; x += 2)
            fins.AddBox(new Vector3(x, 20f, -3.92f), new Vector3(0.12f, 26f, 0.1f));
        CreateMeshObject("Abeno Vertical Fins", root.transform, fins, cyan);
    }

    private static void BuildKobePortTower(Transform parent, Material frame, Material windows, Material cyan)
    {
        var root = CreateRoot("Kobe Port Tower", parent, new Vector3(67f, 0f, 57f));
        var structure = new MeshBuilder();
        AddRing(structure, 6f, 2.1f, 0.32f, 0.3f, 12);
        AddRing(structure, 13f, 1.45f, 0.32f, 0.3f, 12);
        AddRing(structure, 20f, 1.45f, 0.32f, 0.3f, 12);
        AddRing(structure, 26f, 2.1f, 0.32f, 0.3f, 12);
        for (var i = 0; i < 12; i++)
        {
            var angle = i * Mathf.PI * 2f / 12f;
            var lower = new Vector3(Mathf.Cos(angle) * 2.1f, 6f, 0f);
            var middle = new Vector3(Mathf.Cos(angle) * 1.45f, 13f, 0f);
            var upper = new Vector3(Mathf.Cos(angle) * 1.45f, 20f, 0f);
            var top = new Vector3(Mathf.Cos(angle) * 2.1f, 26f, 0f);
            AddBeam(structure, lower, middle, 0.2f, 0.22f);
            AddBeam(structure, middle, upper, 0.2f, 0.22f);
            AddBeam(structure, upper, top, 0.2f, 0.22f);
        }
        structure.AddBox(new Vector3(0f, 16.5f, 0f), new Vector3(2.1f, 4f, 2f));
        structure.AddBox(new Vector3(0f, 29f, 0f), new Vector3(0.24f, 4f, 0.24f));
        CreateMeshObject("Hourglass Port Tower Frame", root.transform, structure, frame);

        var beacon = new MeshBuilder();
        beacon.AddBox(new Vector3(0f, 16.5f, -1.1f), new Vector3(1.6f, 3f, 0.08f));
        CreateMeshObject("Observation Deck Windows", root.transform, beacon, windows);
        AddAntennaBeacon(root.transform, cyan, new Vector3(0f, 31f, -0.15f));
    }

    private static void BuildNagoyaTvTower(Transform parent, Material facade, Material windows, Material frame)
    {
        var root = CreateRoot("Nagoya TV Tower", parent, new Vector3(-73f, 0f, 52f));
        var structure = new MeshBuilder();
        AddBeam(structure, new Vector3(-4.2f, 0.4f, 0f), new Vector3(0f, 24f, 0f), 0.45f, 0.45f);
        AddBeam(structure, new Vector3(4.2f, 0.4f, 0f), new Vector3(0f, 24f, 0f), 0.45f, 0.45f);
        AddBeam(structure, new Vector3(-4.2f, 0.4f, 0f), new Vector3(4.2f, 0.4f, 0f), 0.45f, 0.45f);
        AddBeam(structure, new Vector3(-0.42f, 0f, 0f), new Vector3(0f, 28f, 0f), 0.35f, 0.35f);
        for (var i = 0; i < 5; i++)
        {
            var y = 3f + i * 4f;
            var halfWidth = Mathf.Lerp(3.7f, 0.55f, y / 24f);
            AddBeam(structure, new Vector3(-halfWidth, y, 0f), new Vector3(halfWidth, y, 0f), 0.25f, 0.3f);
        }
        structure.AddBox(new Vector3(0f, 15f, 0f), new Vector3(5.4f, 2f, 3.6f));
        structure.AddBox(new Vector3(0f, 28f, 0f), new Vector3(0.2f, 4f, 0.2f));
        CreateMeshObject("Central TV Tower Mast", root.transform, structure, frame);

        var windowsMesh = new MeshBuilder();
        windowsMesh.AddBox(new Vector3(0f, 15f, -1.86f), new Vector3(4.8f, 1f, 0.08f));
        CreateMeshObject("TV Tower Observation Deck", root.transform, windowsMesh, windows);
        AddAntennaBeacon(root.transform, facade, new Vector3(0f, 30f, -0.1f));
    }

    private static void BuildSapporoTvTower(Transform parent, Material facade, Material windows, Material frame)
    {
        var root = CreateRoot("Sapporo TV Tower", parent, new Vector3(-74f, 0f, 36f));
        var structure = new MeshBuilder();
        AddBeam(structure, new Vector3(-3.5f, 0f, 0f), new Vector3(-0.65f, 22f, 0f), 0.5f, 0.55f);
        AddBeam(structure, new Vector3(3.5f, 0f, 0f), new Vector3(0.65f, 22f, 0f), 0.5f, 0.55f);
        AddBeam(structure, new Vector3(0f, 1f, 0f), new Vector3(0f, 26f, 0f), 0.35f, 0.35f);
        for (var level = 0; level < 4; level++)
        {
            var y = 4f + level * 4f;
            var width = Mathf.Lerp(2.8f, 0.8f, y / 20f);
            AddBeam(structure, new Vector3(-width, y, 0f), new Vector3(width, y, 0f), 0.22f, 0.28f);
        }
        structure.AddBox(new Vector3(0f, 16f, 0f), new Vector3(4.5f, 3.3f, 3.5f));
        structure.AddBox(new Vector3(0f, 26f, 0f), new Vector3(0.18f, 3f, 0.18f));
        CreateMeshObject("Sapporo TV Tower Frame", root.transform, structure, facade);

        var lights = new MeshBuilder();
        lights.AddBox(new Vector3(0f, 16f, -1.8f), new Vector3(4f, 2.2f, 0.08f));
        CreateMeshObject("Sapporo Observation Windows", root.transform, lights, windows);
        AddAntennaBeacon(root.transform, frame, new Vector3(0f, 27.5f, -0.15f));
    }

    private static void BuildFukuokaTower(Transform parent, Material facade, Material windows, Material cyan)
    {
        var root = CreateRoot("Fukuoka Tower", parent, new Vector3(44f, 0f, 91f));
        var structure = new MeshBuilder();
        structure.AddPrism(new[]
        {
            new Vector2(-3.7f, 0f), new Vector2(3.7f, 0f), new Vector2(2.1f, 28f),
            new Vector2(0f, 34f), new Vector2(-2.1f, 28f)
        }, 4.8f);
        for (var y = 6f; y < 29f; y += 3.2f)
        {
            var width = Mathf.Lerp(6.7f, 4.2f, y / 34f);
            structure.AddBox(new Vector3(0f, y, -2.43f), new Vector3(width, 0.18f, 0.14f));
        }
        CreateMeshObject("Triangular Mirror Tower", root.transform, structure, facade);

        var lights = new MeshBuilder();
        for (var y = 7f; y < 28f; y += 4.2f)
        {
            var width = Mathf.Lerp(5.8f, 3.8f, y / 34f);
            lights.AddBox(new Vector3(0f, y, -2.52f), new Vector3(width * 0.65f, 0.7f, 0.06f));
        }
        CreateMeshObject("Fukuoka Tower Light Bands", root.transform, lights, windows);
        AddAntennaBeacon(root.transform, cyan, new Vector3(0f, 34f, -0.15f));
    }

    private static void BuildKyotoTower(Transform parent, Material facade, Material windows, Material frame)
    {
        var root = CreateRoot("Kyoto Tower", parent, new Vector3(74f, 0f, 39f));
        var structure = new MeshBuilder();
        structure.AddBox(new Vector3(0f, 2f, 0f), new Vector3(8f, 4f, 6f));
        structure.AddPrism(new[]
        {
            new Vector2(-2.1f, 4f), new Vector2(2.1f, 4f), new Vector2(1.25f, 17f),
            new Vector2(0.55f, 20f), new Vector2(-0.55f, 20f), new Vector2(-1.25f, 17f)
        }, 2.8f);
        structure.AddBox(new Vector3(0f, 16f, 0f), new Vector3(3.1f, 2.6f, 3.4f));
        structure.AddPrism(new[]
        {
            new Vector2(-0.6f, 18f), new Vector2(0.6f, 18f), new Vector2(0.2f, 25f),
            new Vector2(-0.2f, 25f)
        }, 1.4f);
        CreateMeshObject("Kyoto Tower Profile", root.transform, structure, facade);

        var windowsMesh = new MeshBuilder();
        windowsMesh.AddBox(new Vector3(0f, 16f, -1.76f), new Vector3(2.7f, 1.3f, 0.08f));
        CreateMeshObject("Kyoto Observation Deck Windows", root.transform, windowsMesh, windows);
        AddAntennaBeacon(root.transform, frame, new Vector3(0f, 25f, -0.1f));
    }

    private static void AddAntennaBeacon(Transform parent, Material material, Vector3 position)
    {
        var beacon = new MeshBuilder();
        beacon.AddBox(position, new Vector3(0.65f, 0.65f, 0.35f));
        CreateMeshObject("Aviation Beacon", parent, beacon, material);
    }

    private static Material CreateOrUpdateSignalMaterial()
    {
        var shader = Shader.Find("Standard");
        if (shader == null)
            throw new InvalidOperationException("The built-in Standard shader is unavailable.");

        var material = AssetDatabase.LoadAssetAtPath<Material>(TowerSignalRedPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, TowerSignalRedPath);
        }

        material.name = "JapanTowerSignalRed";
        material.shader = shader;
        material.color = new Color(0.7f, 0.12f, 0.055f);
        material.SetFloat("_Metallic", 0.32f);
        material.SetFloat("_Glossiness", 0.48f);
        material.SetColor("_EmissionColor", new Color(0.24f, 0.025f, 0.006f));
        material.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateOrUpdateHarborWaterMaterial()
    {
        var shader = Shader.Find("Standard");
        if (shader == null)
            throw new InvalidOperationException("The built-in Standard shader is unavailable.");

        var material = AssetDatabase.LoadAssetAtPath<Material>(HarborWaterPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, HarborWaterPath);
        }

        material.name = "JapanHarborWater";
        material.shader = shader;
        material.color = new Color(0.012f, 0.035f, 0.065f);
        material.SetFloat("_Metallic", 0.42f);
        material.SetFloat("_Glossiness", 0.82f);
        material.SetColor("_EmissionColor", new Color(0.003f, 0.012f, 0.024f));
        material.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material CreateOrUpdateHarborHullMaterial()
    {
        var shader = Shader.Find("Standard");
        if (shader == null)
            throw new InvalidOperationException("The built-in Standard shader is unavailable.");

        var material = AssetDatabase.LoadAssetAtPath<Material>(HarborHullPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, HarborHullPath);
        }

        material.name = "JapanHarborHull";
        material.shader = shader;
        material.color = new Color(0.38f, 0.48f, 0.57f);
        material.SetFloat("_Metallic", 0.48f);
        material.SetFloat("_Glossiness", 0.66f);
        material.SetColor("_EmissionColor", new Color(0.045f, 0.065f, 0.085f));
        material.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void AddBaseAccent(Transform parent, Material material, float width, float z)
    {
        var accent = new MeshBuilder();
        accent.AddBox(new Vector3(0f, 1f, z), new Vector3(width, 0.16f, 0.12f));
        CreateMeshObject("Base Light Accent", parent, accent, material);
    }

    private static void AddRing(MeshBuilder mesh, float centerY, float radius, float width, float depth, int segments)
    {
        var segmentLength = 2f * Mathf.PI * radius / segments * 1.08f;
        for (var i = 0; i < segments; i++)
        {
            var angle = (i + 0.5f) * Mathf.PI * 2f / segments;
            var center = new Vector3(Mathf.Cos(angle) * radius, centerY + Mathf.Sin(angle) * radius, 0f);
            mesh.AddBox(center, new Vector3(segmentLength, width, depth),
                Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg + 90f));
        }
    }

    private static void AddBeam(MeshBuilder mesh, Vector3 start, Vector3 end, float width, float depth)
    {
        var direction = end - start;
        mesh.AddBox((start + end) * 0.5f, new Vector3(width, direction.magnitude, depth),
            Quaternion.FromToRotation(Vector3.up, direction.normalized));
    }

    private static void AddBeam2D(MeshBuilder mesh, Vector2 start, Vector2 end, float width, float depth)
    {
        var direction = end - start;
        var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        mesh.AddBox(new Vector3((start.x + end.x) * 0.5f, (start.y + end.y) * 0.5f, -depth * 0.5f),
            new Vector3(direction.magnitude, width, depth), Quaternion.Euler(0f, 0f, angle));
    }

    private static GameObject CreateRoot(string name, Transform parent, Vector3 position)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.localPosition = position;
        return root;
    }

    private static void CreateMeshObject(string name, Transform parent, MeshBuilder builder, Material material)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        var filter = gameObject.AddComponent<MeshFilter>();
        var mesh = builder.Build(name);
        var assetPath = $"{MeshFolderPath}/{parent.name}-{name}.asset";
        var existingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
        if (existingMesh == null)
        {
            AssetDatabase.CreateAsset(mesh, assetPath);
        }
        else
        {
            EditorUtility.CopySerialized(mesh, existingMesh);
            existingMesh.name = name;
            EditorUtility.SetDirty(existingMesh);
            UnityEngine.Object.DestroyImmediate(mesh);
            mesh = existingMesh;
        }
        filter.sharedMesh = mesh;
        var renderer = gameObject.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        var folderName = Path.GetFileName(path);
        if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(folderName))
            throw new InvalidOperationException($"Invalid Unity asset folder path: {path}");
        if (!AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        var guid = AssetDatabase.CreateFolder(parent, folderName);
        if (string.IsNullOrEmpty(guid))
            throw new IOException($"Could not create Unity asset folder: {path}");
    }

    private static Material LoadMaterial(string path)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
            throw new FileNotFoundException($"Required city landmark material was not found: {path}", path);
        return material;
    }

    private static Bounds CalculateBounds(Renderer[] renderers)
    {
        if (renderers.Length == 0)
            return new Bounds();

        var bounds = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return bounds;
    }

    private sealed class MeshBuilder
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<Vector2> uv = new List<Vector2>();
        private readonly List<int> triangles = new List<int>();

        public void AddBox(Vector3 center, Vector3 size)
        {
            AddBox(center, size, Quaternion.identity);
        }

        public void AddBox(Vector3 center, Vector3 size, Quaternion rotation)
        {
            var half = size * 0.5f;
            var corners = new[]
            {
                new Vector3(-half.x, -half.y, -half.z),
                new Vector3(half.x, -half.y, -half.z),
                new Vector3(half.x, half.y, -half.z),
                new Vector3(-half.x, half.y, -half.z),
                new Vector3(-half.x, -half.y, half.z),
                new Vector3(half.x, -half.y, half.z),
                new Vector3(half.x, half.y, half.z),
                new Vector3(-half.x, half.y, half.z)
            };
            for (var i = 0; i < corners.Length; i++)
                corners[i] = center + rotation * corners[i];

            AddFace(corners[0], corners[3], corners[2], corners[1]);
            AddFace(corners[4], corners[5], corners[6], corners[7]);
            AddFace(corners[0], corners[4], corners[7], corners[3]);
            AddFace(corners[1], corners[2], corners[6], corners[5]);
            AddFace(corners[3], corners[7], corners[6], corners[2]);
            AddFace(corners[0], corners[1], corners[5], corners[4]);
        }

        public void AddPrism(Vector2[] outline, float depth)
        {
            if (outline == null || outline.Length < 3)
                throw new ArgumentException("A prism outline must have at least three points.", nameof(outline));

            var center = Vector2.zero;
            foreach (var point in outline)
                center += point;
            center /= outline.Length;
            var frontZ = -depth * 0.5f;
            var backZ = depth * 0.5f;

            for (var i = 0; i < outline.Length; i++)
            {
                var next = (i + 1) % outline.Length;
                AddTriangle(new Vector3(center.x, center.y, frontZ),
                    new Vector3(outline[next].x, outline[next].y, frontZ),
                    new Vector3(outline[i].x, outline[i].y, frontZ));
                AddTriangle(new Vector3(center.x, center.y, backZ),
                    new Vector3(outline[i].x, outline[i].y, backZ),
                    new Vector3(outline[next].x, outline[next].y, backZ));

                AddFace(
                    new Vector3(outline[i].x, outline[i].y, frontZ),
                    new Vector3(outline[i].x, outline[i].y, backZ),
                    new Vector3(outline[next].x, outline[next].y, backZ),
                    new Vector3(outline[next].x, outline[next].y, frontZ));
            }
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name };
            if (vertices.Count > 65535)
                mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void AddFace(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            var first = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            vertices.Add(d);
            uv.Add(new Vector2(0f, 0f));
            uv.Add(new Vector2(1f, 0f));
            uv.Add(new Vector2(1f, 1f));
            uv.Add(new Vector2(0f, 1f));
            triangles.Add(first);
            triangles.Add(first + 1);
            triangles.Add(first + 2);
            triangles.Add(first);
            triangles.Add(first + 2);
            triangles.Add(first + 3);
        }

        private void AddTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            var first = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            uv.Add(Vector2.zero);
            uv.Add(Vector2.right);
            uv.Add(Vector2.up);
            triangles.Add(first);
            triangles.Add(first + 1);
            triangles.Add(first + 2);
        }
    }
}
