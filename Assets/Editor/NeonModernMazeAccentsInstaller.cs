using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class NeonModernMazeAccentsInstaller
{
    private const string ModelPath = "Assets/Generated/Blender/neon-modern-maze-accents-20261110.fbx";
    private const string LabyrinthModelPath = "Assets/Generated/Blender/nocturne-aether-3d-labyrinth-20261110.fbx";
    private const string CyanMaterialPath = "Assets/Generated/Materials/NeonCyanGlow.mat";
    private const string MagentaMaterialPath = "Assets/Generated/Materials/NeonMagentaGlow.mat";
    private const string ChampagneGlowPath = "Assets/Generated/Materials/NeonChampagneGlow.mat";
    private const string MagentaOutlinePath = "Assets/Project/Png/SpinOutlineGlowMagenta.mat";
    private const string ChampagneOutlinePath = "Assets/Project/Png/SpinOutlineGlowChampagne.mat";
    private const string MagentaCorePath = "Assets/Project/Png/SpinOutlineCoreMagenta.mat";
    private const string ChampagneCorePath = "Assets/Project/Png/SpinOutlineCoreChampagne.mat";
    private const string TitaniumMaterialPath = "Assets/Generated/Materials/NocturneTitanium.mat";
    private const string ChampagneMaterialPath = "Assets/Generated/Materials/ChampagneTitanium.mat";
    private const string MainScenePath = "Assets/room.unity";
    private const string PreviewScenePath = "Assets/Generated/Scenes/NeonGeometryRoomPreview.unity";
    private const string InstanceName = "Neon Modern Maze Accents";
    private const string LabyrinthInstanceName = "Nocturne 3D Labyrinth";
    private const string LightingRigName = "Nocturne Lighting Rig";

    [MenuItem("Tools/Room/Apply Nocturne 3D Labyrinth Design")]
    public static void Install()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var labyrinthModel = AssetDatabase.LoadAssetAtPath<GameObject>(LabyrinthModelPath);
        if (model == null)
            throw new FileNotFoundException("Maze accents FBX could not be imported.", ModelPath);
        if (labyrinthModel == null)
            throw new FileNotFoundException("3D labyrinth FBX could not be imported.", LabyrinthModelPath);

        var magentaMaterial = AssetDatabase.LoadAssetAtPath<Material>(MagentaMaterialPath);
        if (magentaMaterial != null && !File.Exists(ChampagneGlowPath))
            MoveMaterialAsset(MagentaMaterialPath, ChampagneGlowPath);
        MoveMaterialAsset(MagentaOutlinePath, ChampagneOutlinePath);
        MoveMaterialAsset(MagentaCorePath, ChampagneCorePath);

        var cyanMaterial = AssetDatabase.LoadAssetAtPath<Material>(CyanMaterialPath);
        var champagneGlow = AssetDatabase.LoadAssetAtPath<Material>(ChampagneGlowPath);
        if (cyanMaterial == null)
            throw new FileNotFoundException("Cyan emission material could not be loaded.", CyanMaterialPath);
        if (champagneGlow == null)
            throw new FileNotFoundException("Champagne emission material could not be loaded.", ChampagneGlowPath);

        var titanium = CreateOrUpdateMetal(TitaniumMaterialPath, "NocturneTitanium",
            new Color(0.1f, 0.12f, 0.17f), 0.48f, 0.62f);
        var champagne = CreateOrUpdateMetal(ChampagneMaterialPath, "ChampagneTitanium",
            new Color(0.48f, 0.34f, 0.21f), 0.62f, 0.58f);

        ConfigureGlow(cyanMaterial, new Color(0.01f, 0.34f, 0.48f), new Color(0.04f, 0.48f, 0.68f));
        ConfigureGlow(champagneGlow, new Color(0.46f, 0.22f, 0.08f), new Color(0.72f, 0.34f, 0.11f));
        ConfigureOutlineMaterials();

        InstallInScene(MainScenePath, model, labyrinthModel, cyanMaterial, champagneGlow, titanium, champagne);
        InstallInScene(PreviewScenePath, model, labyrinthModel, cyanMaterial, champagneGlow, titanium, champagne);
        AssetDatabase.SaveAssets();
    }

    private static void MoveMaterialAsset(string source, string destination)
    {
        if (!File.Exists(source) || File.Exists(destination))
            return;

        var moveError = AssetDatabase.MoveAsset(source, destination);
        if (!string.IsNullOrEmpty(moveError))
            throw new IOException($"Could not rename material {source}: {moveError}");
    }

    private static void ConfigureOutlineMaterials()
    {
        var outline = AssetDatabase.LoadAssetAtPath<Material>(ChampagneOutlinePath);
        var core = AssetDatabase.LoadAssetAtPath<Material>(ChampagneCorePath);
        if (outline == null)
            throw new FileNotFoundException("Maze outline material could not be loaded.", ChampagneOutlinePath);
        if (core == null)
            throw new FileNotFoundException("Maze core material could not be loaded.", ChampagneCorePath);

        outline.name = "SpinOutlineGlowChampagne";
        outline.SetColor("_Color", new Color(0.86f, 0.43f, 0.16f, 0.18f));
        outline.SetColor("_Emission", new Color(0.34f, 0.13f, 0.035f));
        core.name = "SpinOutlineCoreChampagne";
        core.SetColor("_Color", new Color(0.86f, 0.48f, 0.21f));
        EditorUtility.SetDirty(outline);
        EditorUtility.SetDirty(core);
    }

    private static Material CreateOrUpdateMetal(string path, string materialName, Color color,
        float metallic, float smoothness)
    {
        var shader = Shader.Find("Standard");
        if (shader == null)
            throw new InvalidOperationException("The built-in Standard shader is unavailable.");

        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        material.name = materialName;
        material.shader = shader;
        material.color = color;
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Glossiness", smoothness);
        material.SetColor("_EmissionColor", new Color(0.006f, 0.009f, 0.016f));
        material.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static void ConfigureGlow(Material material, Color color, Color emission)
    {
        material.name = material.name.Replace("Magenta", "Champagne");
        material.color = color;
        material.SetColor("_EmissionColor", emission);
        material.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);
    }

    private static void InstallInScene(string scenePath, GameObject model, GameObject labyrinthModel,
        Material cyanMaterial, Material champagneGlow, Material titanium, Material champagne)
    {
        if (!File.Exists(scenePath))
            throw new FileNotFoundException("Target Unity scene does not exist.", scenePath);

        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        var existingAccents = FindInstance(scene, InstanceName);
        var accentRenderers = InstallModel(scene, model, existingAccents, InstanceName,
            renderer => AssignAccentMaterial(renderer, cyanMaterial, titanium, champagne));
        if (accentRenderers.Length != 55)
            throw new InvalidOperationException($"Expected 55 existing accents; imported {accentRenderers.Length}.");

        var existingLabyrinth = FindInstance(scene, LabyrinthInstanceName);
        var labyrinthRenderers = InstallModel(scene, labyrinthModel, existingLabyrinth, LabyrinthInstanceName,
            renderer => AssignLabyrinthMaterial(renderer, cyanMaterial, champagneGlow, titanium, champagne));
        if (labyrinthRenderers.Length != 67)
            throw new InvalidOperationException($"Expected 67 labyrinth meshes; imported {labyrinthRenderers.Length}.");
        RefineLayeredGeometry(labyrinthModel, labyrinthRenderers);

        InstallLighting(scene);
        SetNightLighting(scene);

        var bounds = CalculateBounds(labyrinthRenderers);
        if (bounds.min.y < 0f || bounds.max.y > 5.85f || bounds.min.z < -1f || bounds.max.z > 17f)
            throw new InvalidOperationException($"3D labyrinth geometry is outside the room bounds: {bounds}.");

        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException($"Could not save scene: {scenePath}");

        Debug.Log(
            $"Installed 55 rethemed accents, 67 non-colliding 3D labyrinth meshes, and 4 shadowless lights " +
            $"in {scenePath}; labyrinth bounds center={bounds.center}, size={bounds.size}.");
    }

    private static Renderer[] InstallModel(Scene scene, GameObject model, GameObject instance, string instanceName,
        Action<Renderer> assignMaterial)
    {
        if (instance == null)
        {
            instance = PrefabUtility.InstantiatePrefab(model, scene) as GameObject;
            if (instance == null)
                throw new InvalidOperationException($"Could not instantiate model: {model.name}");
            instance.name = instanceName;
        }

        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(90f, 0f, 0f));
        instance.transform.localScale = Vector3.one;

        var renderers = instance.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            assignMaterial(renderer);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        if (instance.GetComponentsInChildren<Collider>(true).Length != 0)
            throw new InvalidOperationException($"{instanceName} must not add colliders.");

        return renderers;
    }

    private static void AssignAccentMaterial(Renderer renderer, Material cyan, Material titanium,
        Material champagne)
    {
        var name = renderer.gameObject.name;
        if (name.IndexOf("Cyan", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Cove", StringComparison.OrdinalIgnoreCase) >= 0 ||
            name.IndexOf("Glow", StringComparison.OrdinalIgnoreCase) >= 0)
            renderer.sharedMaterial = cyan;
        else if (name.IndexOf("Gold", StringComparison.OrdinalIgnoreCase) >= 0)
            renderer.sharedMaterial = champagne;
        else
            renderer.sharedMaterial = titanium;
    }

    private static void AssignLabyrinthMaterial(Renderer renderer, Material cyan, Material champagneGlow,
        Material titanium, Material champagne)
    {
        var name = renderer.gameObject.name;
        if (name.StartsWith("Ice ", StringComparison.OrdinalIgnoreCase))
            renderer.sharedMaterial = cyan;
        else if (name.StartsWith("Amber ", StringComparison.OrdinalIgnoreCase))
            renderer.sharedMaterial = champagneGlow;
        else if (name.StartsWith("Champagne ", StringComparison.OrdinalIgnoreCase))
            renderer.sharedMaterial = champagne;
        else
            renderer.sharedMaterial = titanium;
    }

    private static Bounds CalculateBounds(Renderer[] renderers)
    {
        if (renderers.Length == 0)
            throw new InvalidOperationException("The imported model contains no renderers.");

        var bounds = renderers[0].bounds;
        for (var index = 1; index < renderers.Length; index++)
            bounds.Encapsulate(renderers[index].bounds);
        return bounds;
    }

    private static void RefineLayeredGeometry(GameObject model, Renderer[] renderers)
    {
        var sourceRenderers = model.GetComponentsInChildren<Renderer>(true);
        if (sourceRenderers.Length != renderers.Length)
            throw new InvalidOperationException("The labyrinth instance no longer matches its source model.");

        for (var index = 0; index < renderers.Length; index++)
        {
            var renderer = renderers[index];
            var source = sourceRenderers[index];
            if (renderer.gameObject.name != source.gameObject.name)
                throw new InvalidOperationException(
                    $"The labyrinth instance mesh order changed at {renderer.gameObject.name}.");

            var item = renderer.transform;
            item.localPosition = source.transform.localPosition;
            item.localRotation = source.transform.localRotation;
            item.localScale = source.transform.localScale;
            var scale = item.localScale;

            if (item.name == "Obsidian Upper Deck A" || item.name == "Obsidian Upper Deck C" ||
                item.name == "Obsidian Upper Deck E" || item.name == "Obsidian Upper Deck F")
                item.localScale = new Vector3(scale.x, scale.y * 0.72f, scale.z);
            else if (item.name == "Obsidian Upper Deck B" || item.name == "Obsidian Upper Deck D")
                item.localScale = new Vector3(scale.x * 0.72f, scale.y, scale.z);
            else if (item.name.IndexOf("Deck Support", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var position = item.position;
                item.position = new Vector3(position.x, 1.82f, position.z);
                item.localScale = new Vector3(scale.x, scale.y, scale.z * 1.05f);
            }

            var worldPosition = item.position;
            switch (item.name)
            {
                case "Champagne A Edge North":
                    item.position = new Vector3(worldPosition.x, worldPosition.y, 4.02f);
                    break;
                case "Champagne A Edge South":
                    item.position = new Vector3(worldPosition.x, worldPosition.y, 4.98f);
                    break;
                case "Champagne B Edge West":
                    item.position = new Vector3(5.92f, worldPosition.y, worldPosition.z);
                    break;
                case "Champagne B Edge East":
                    item.position = new Vector3(6.88f, worldPosition.y, worldPosition.z);
                    break;
                case "Champagne C Edge North":
                    item.position = new Vector3(worldPosition.x, worldPosition.y, 0.97f);
                    break;
                case "Champagne C Edge South":
                    item.position = new Vector3(worldPosition.x, worldPosition.y, 1.83f);
                    break;
                case "Champagne D Edge West":
                    item.position = new Vector3(-3.68f, worldPosition.y, worldPosition.z);
                    break;
                case "Champagne D Edge East":
                    item.position = new Vector3(-2.72f, worldPosition.y, worldPosition.z);
                    break;
                case "Champagne E Edge North":
                    item.position = new Vector3(worldPosition.x, worldPosition.y, 6.76f);
                    break;
                case "Champagne E Edge South":
                    item.position = new Vector3(worldPosition.x, worldPosition.y, 7.64f);
                    break;
                case "Champagne F Edge North":
                    item.position = new Vector3(worldPosition.x, worldPosition.y, 4.94f);
                    break;
                case "Champagne F Edge South":
                    item.position = new Vector3(worldPosition.x, worldPosition.y, 5.66f);
                    break;
            }
        }
    }

    private static void InstallLighting(Scene scene)
    {
        var rig = FindInstance(scene, LightingRigName);
        if (rig == null)
        {
            rig = new GameObject(LightingRigName);
            SceneManager.MoveGameObjectToScene(rig, scene);
        }

        CreateSpot(rig.transform, "Arrival Champagne Pool", new Vector3(0f, 5.1f, 12.2f),
            new Color(1f, 0.72f, 0.43f), 3.2f, 6.5f, 58f);
        CreatePoint(rig.transform, "Labyrinth Warm Key", new Vector3(-1.6f, 4.4f, 4.6f),
            new Color(1f, 0.77f, 0.52f), 2.3f, 7.5f);
        CreatePoint(rig.transform, "Upper Maze Ice Fill", new Vector3(6.1f, 4.2f, 5.1f),
            new Color(0.5f, 0.72f, 1f), 1.5f, 5.8f);
        CreatePoint(rig.transform, "West Passage Warm Pool", new Vector3(-6f, 3.8f, 4.8f),
            new Color(1f, 0.66f, 0.38f), 1.35f, 5.2f);
    }

    private static void CreateSpot(Transform parent, string name, Vector3 position, Color color,
        float intensity, float range, float angle)
    {
        var light = GetOrCreateLight(parent, name, position, color, intensity, range);
        light.type = LightType.Spot;
        light.spotAngle = angle;
        light.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
    }

    private static void CreatePoint(Transform parent, string name, Vector3 position, Color color,
        float intensity, float range)
    {
        var light = GetOrCreateLight(parent, name, position, color, intensity, range);
        light.type = LightType.Point;
    }

    private static Light GetOrCreateLight(Transform parent, string name, Vector3 position, Color color,
        float intensity, float range)
    {
        var lightTransform = parent.Find(name);
        GameObject lightObject;
        if (lightTransform == null)
        {
            lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
        }
        else
        {
            lightObject = lightTransform.gameObject;
        }

        lightObject.transform.localPosition = position;
        lightObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var light = lightObject.GetComponent<Light>();
        if (light == null)
            light = lightObject.AddComponent<Light>();
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        light.enabled = true;
        return light;
    }

    private static void SetNightLighting(Scene scene)
    {
        SceneManager.SetActiveScene(scene);
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.045f, 0.06f, 0.105f);
        RenderSettings.ambientEquatorColor = new Color(0.025f, 0.035f, 0.06f);
        RenderSettings.ambientGroundColor = new Color(0.008f, 0.012f, 0.024f);
        RenderSettings.ambientIntensity = 0.55f;
        RenderSettings.fogColor = new Color(0.008f, 0.012f, 0.03f);

        var directionals = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var light in directionals)
        {
            if (light.type != LightType.Directional ||
                (light.gameObject.name != "Directional Light" && light.gameObject.name != "Neon Ambient Key"))
                continue;

            light.color = new Color(0.68f, 0.75f, 0.9f);
            light.intensity = 0.45f;
            light.shadows = LightShadows.None;
            light.shadowStrength = 0.55f;
            light.shadowBias = 0.08f;
            light.shadowNormalBias = 0.35f;
        }
    }

    private static GameObject FindInstance(Scene scene, string name)
    {
        foreach (var root in scene.GetRootGameObjects())
        {
            if (root.name == name)
                return root;
        }

        return null;
    }
}
