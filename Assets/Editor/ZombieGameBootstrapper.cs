using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ZombieGameBootstrapper
{
    private const string ScenePath = "Assets/Scenes/SampleScene.unity";
    private const string PrefabFolder = "Assets/Prefabs/ZombieGame";
    private const string ZombiePrefabPath = PrefabFolder + "/Zombie.prefab";
    private const string HealthPickupPrefabPath = PrefabFolder + "/HealthPickup.prefab";

    [MenuItem("Zombie Game/Setup 3D Zombie Game")]
    public static void Setup()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        RemoveOldZombieGameObjects();
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Prefabs/ZombieGame"));
        AssetDatabase.Refresh();
        DeleteGeneratedMaterials();
        FixAdventureCharacterMaterials();
        FixWeaponAndHelicopterMaterials();

        var terrain = Object.FindFirstObjectByType<Terrain>();
        RestoreUrpProjectAndTerrain(terrain);
        var root = new GameObject("ZG_GameRoot");
        var player = CreatePlayer(GetTerrainPoint(terrain, 0.48f, 0.42f));
        var manager = new GameObject("ZG_GameManager").AddComponent<ZGGameManager>();
        manager.transform.SetParent(root.transform);
        var zombiePrefab = CreateZombiePrefab();
        var spawner = CreateSpawner(root.transform, zombiePrefab, player.transform, manager, terrain);
        var helicopter = CreateHelicopter(root.transform, terrain);
        var winZone = CreateWinZone(root.transform, terrain, manager);
        var hud = CreateHud(player.GetComponent<ZGHealth>(), player.GetComponent<ZGWeaponController>(), manager);

        var serializedManager = new SerializedObject(manager);
        serializedManager.FindProperty("playerHealth").objectReferenceValue = player.GetComponent<ZGHealth>();
        serializedManager.FindProperty("spawner").objectReferenceValue = spawner;
        serializedManager.FindProperty("helicopter").objectReferenceValue = helicopter;
        serializedManager.FindProperty("hud").objectReferenceValue = hud;
        serializedManager.FindProperty("winZone").objectReferenceValue = winZone;
        serializedManager.ApplyModifiedPropertiesWithoutUndo();

        CreateCampAndLighting(root.transform, terrain);
        CreateEventSystem();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("3D Zombie Game setup completed for rubric.");
    }


    private static void DeleteGeneratedMaterials()
    {
        var materialFolder = "Assets/Materials";
        if (!AssetDatabase.IsValidFolder(materialFolder))
        {
            return;
        }

        foreach (var guid in AssetDatabase.FindAssets("ZG_ t:Material", new[] { materialFolder }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path).StartsWith("ZG_"))
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
    }

    private static void RemoveOldZombieGameObjects()
    {
        var toDestroy = new System.Collections.Generic.HashSet<GameObject>();
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (go != null && (go.name.StartsWith("ZG_") || go.name == "EventSystem"))
            {
                toDestroy.Add(go.transform.root.gameObject.name.StartsWith("ZG_") ? go.transform.root.gameObject : go);
            }
        }

        foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (camera != null)
            {
                toDestroy.Add(camera.gameObject);
            }
        }

        foreach (var go in toDestroy)
        {
            if (go != null)
            {
                Object.DestroyImmediate(go);
            }
        }
    }


    private static void RestoreUrpProjectAndTerrain(Terrain terrain)
    {
        var renderPipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/New Universal Render Pipeline Asset.asset");
        if (renderPipeline != null)
        {
            GraphicsSettings.defaultRenderPipeline = renderPipeline;
            QualitySettings.renderPipeline = renderPipeline;
            EditorUtility.SetDirty(GraphicsSettings.GetGraphicsSettings());
        }

        if (terrain == null)
        {
            return;
        }

        var terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/ProceduralTerrainPainter/_Demo/PTP_TerrainMat.mat");
        if (terrainMaterial != null)
        {
            terrain.materialTemplate = terrainMaterial;
        }
        terrain.drawInstanced = false;
        EditorUtility.SetDirty(terrain);
        if (terrain.terrainData != null)
        {
            EditorUtility.SetDirty(terrain.terrainData);
        }
    }

    private static Vector3 GetTerrainPoint(Terrain terrain, float normalizedX, float normalizedZ)
    {
        if (terrain == null)
        {
            return new Vector3(0f, 2f, 0f);
        }

        var data = terrain.terrainData;
        var pos = terrain.transform.position;
        var world = new Vector3(pos.x + data.size.x * normalizedX, pos.y, pos.z + data.size.z * normalizedZ);
        world.y = terrain.SampleHeight(world) + pos.y + 0.05f;
        return world;
    }

    private static GameObject CreatePlayer(Vector3 position)
    {
        var player = new GameObject("ZG_Player");
        player.transform.position = position;

        var controller = player.AddComponent<CharacterController>();
        controller.height = 1.85f;
        controller.radius = 0.38f;
        controller.center = new Vector3(0f, 0.92f, 0f);

        var health = player.AddComponent<ZGHealth>();
        var healthSerialized = new SerializedObject(health);
        healthSerialized.FindProperty("maxHealth").intValue = 100;
        healthSerialized.ApplyModifiedPropertiesWithoutUndo();

        var body = InstantiatePrefab(
            "Assets/Adventure_Character/Prefabs/Man_03.prefab",
            player.transform,
            "ZG_PlayerModel",
            Vector3.zero,
            Quaternion.identity,
            Vector3.one);
        if (body == null)
        {
            body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "ZG_PlayerModel";
            body.transform.SetParent(player.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.92f, 0f);
            body.transform.localScale = new Vector3(0.75f, 0.95f, 0.75f);
            body.GetComponent<Renderer>().material = CreateMaterial("ZG_Player_Mat", new Color(0.15f, 0.32f, 0.95f));
        }
        StripColliders(body);
        var bodyAnimator = body.GetComponentInChildren<Animator>();
        if (bodyAnimator != null)
        {
            bodyAnimator.runtimeAnimatorController = CreatePlayerAnimatorController();
            bodyAnimator.applyRootMotion = false;
        }

        var pivot = new GameObject("ZG_CameraPivot");
        pivot.transform.SetParent(player.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 1.55f, 0f);

        var camObj = new GameObject("ZG_MainCamera");
        camObj.tag = "MainCamera";
        camObj.transform.SetParent(pivot.transform, false);
        camObj.transform.localPosition = new Vector3(1.15f, 0.55f, -4.25f);
        camObj.transform.localEulerAngles = Vector3.zero;
        var camera = camObj.AddComponent<Camera>();
        camera.fieldOfView = 62f;
        camObj.AddComponent<AudioListener>();

        var weaponModel = InstantiatePrefab(
            "Assets/HornetRifle/Prefab/riffle_hornet.prefab",
            player.transform,
            "ZG_HornetRifle",
            new Vector3(0.42f, 1.18f, 0.72f),
            Quaternion.Euler(0f, 0f, 0f),
            Vector3.one * 0.16f);
        if (weaponModel != null)
        {
            StripColliders(weaponModel);
        }

        var firstPersonWeapon = InstantiatePrefab(
            "Assets/HornetRifle/Prefab/riffle_hornet.prefab",
            camObj.transform,
            "ZG_FirstPersonRifle",
            new Vector3(0.42f, -0.28f, 0.78f),
            Quaternion.Euler(0f, 0f, 0f),
            Vector3.one * 0.13f);
        if (firstPersonWeapon != null)
        {
            StripColliders(firstPersonWeapon);
            firstPersonWeapon.SetActive(false);
        }

        var muzzle = new GameObject("ZG_Muzzle");
        muzzle.transform.SetParent(player.transform, false);
        muzzle.transform.localPosition = new Vector3(0.42f, 1.2f, 1.28f);

        var playerController = player.AddComponent<ZGPlayerController>();
        var pcSerialized = new SerializedObject(playerController);
        pcSerialized.FindProperty("cameraPivot").objectReferenceValue = pivot.transform;
        pcSerialized.FindProperty("playerModel").objectReferenceValue = body.transform;
        pcSerialized.FindProperty("thirdPersonWeapon").objectReferenceValue = weaponModel != null ? weaponModel.transform : null;
        pcSerialized.FindProperty("firstPersonWeapon").objectReferenceValue = firstPersonWeapon != null ? firstPersonWeapon.transform : null;
        pcSerialized.ApplyModifiedPropertiesWithoutUndo();

        var weapon = player.AddComponent<ZGWeaponController>();
        var weaponSerialized = new SerializedObject(weapon);
        weaponSerialized.FindProperty("playerCamera").objectReferenceValue = camera;
        weaponSerialized.FindProperty("muzzlePoint").objectReferenceValue = muzzle.transform;
        weaponSerialized.FindProperty("recoilRoot").objectReferenceValue = firstPersonWeapon != null ? firstPersonWeapon.transform : (weaponModel != null ? weaponModel.transform : null);
        weaponSerialized.FindProperty("thirdPersonWeaponRoot").objectReferenceValue = weaponModel != null ? weaponModel.transform : null;
        weaponSerialized.FindProperty("firstPersonWeaponRoot").objectReferenceValue = firstPersonWeapon != null ? firstPersonWeapon.transform : null;
        AssignAudioArray(weaponSerialized, "fireClips", new[]
        {
            "Assets/FPS Horror Sound Pack/Weapon Sounds/Rifle/Rifle_Fire1.wav",
            "Assets/FPS Horror Sound Pack/Weapon Sounds/Rifle/Rifle_Fire2.wav",
            "Assets/FPS Horror Sound Pack/Weapon Sounds/Rifle/Rifle_Fire3.wav",
            "Assets/FPS Horror Sound Pack/Weapon Sounds/Rifle/Rifle_Fire4.wav"
        });
        weaponSerialized.FindProperty("reloadClipAsset").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/FPS Horror Sound Pack/Weapon Sounds/Rifle/Rifle_Reload.wav");
        weaponSerialized.FindProperty("muzzleFlashPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FPS Horror Sound Pack/Demo/Prefabs/MuzzleFlash.prefab");
        weaponSerialized.ApplyModifiedPropertiesWithoutUndo();

        var playerAnimator = player.AddComponent<ZGPlayerAnimator>();
        var playerAnimatorSerialized = new SerializedObject(playerAnimator);
        playerAnimatorSerialized.FindProperty("animator").objectReferenceValue = bodyAnimator;
        playerAnimatorSerialized.FindProperty("weapon").objectReferenceValue = weapon;
        playerAnimatorSerialized.ApplyModifiedPropertiesWithoutUndo();

        return player;
    }

    private static GameObject CreateZombiePrefab()
    {
        var zombie = new GameObject("ZG_ZombiePrefab");
        var controller = zombie.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.42f;
        controller.center = new Vector3(0f, 0.9f, 0f);
        var ai = zombie.AddComponent<ZGZombieAI>();

        var modelRoot = new GameObject("ModelRoot");
        modelRoot.transform.SetParent(zombie.transform, false);
        modelRoot.transform.localPosition = Vector3.zero;
        var basicModel = InstantiatePrefab(
            "Assets/NewPunch/ShirtlessZombieFree/Prefabs/ShirtlessZombie_FREE_URP.prefab",
            modelRoot.transform,
            "Zombie_Basic_Model",
            Vector3.zero,
            Quaternion.identity,
            Vector3.one);
        var heavyModel = InstantiatePrefab(
            "Assets/NewPunch/ShirtlessZombieFree/Prefabs/ShirtlessZombie_FREE_URP.prefab",
            modelRoot.transform,
            "Zombie_Heavy_Model",
            Vector3.zero,
            Quaternion.identity,
            Vector3.one * 1.17f);

        if (basicModel == null)
        {
            var zombieMat = CreateMaterial("ZG_Zombie_Mat", new Color(0.36f, 0.82f, 0.38f));
            basicModel = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            basicModel.name = "Zombie_Basic_Model";
            basicModel.transform.SetParent(modelRoot.transform, false);
            basicModel.transform.localPosition = new Vector3(0f, 0.88f, 0f);
            basicModel.transform.localScale = new Vector3(0.75f, 0.9f, 0.75f);
            basicModel.GetComponent<Renderer>().material = zombieMat;
        }
        StripColliders(basicModel);
        if (heavyModel != null)
        {
            StripColliders(heavyModel);
            heavyModel.SetActive(false);
        }
        var zombieController = CreateZombieAnimatorController();
        AssignAnimatorController(basicModel, zombieController);
        AssignAnimatorController(heavyModel, zombieController);

        var headHitbox = new GameObject("ZG_HeadHitbox");
        headHitbox.transform.SetParent(zombie.transform, false);
        headHitbox.transform.localPosition = new Vector3(0f, 1.76f, 0f);
        var headCollider = headHitbox.AddComponent<SphereCollider>();
        headCollider.radius = 0.21f;
        headCollider.isTrigger = false;
        var hitZone = headHitbox.AddComponent<ZGHitZone>();
        var hitZoneSerialized = new SerializedObject(hitZone);
        hitZoneSerialized.FindProperty("headshot").boolValue = true;
        hitZoneSerialized.ApplyModifiedPropertiesWithoutUndo();

        var serialized = new SerializedObject(ai);
        serialized.FindProperty("modelRoot").objectReferenceValue = basicModel.transform;
        serialized.FindProperty("modelVariants").arraySize = heavyModel != null ? 2 : 1;
        serialized.FindProperty("modelVariants").GetArrayElementAtIndex(0).objectReferenceValue = basicModel.transform;
        if (heavyModel != null)
        {
            serialized.FindProperty("modelVariants").GetArrayElementAtIndex(1).objectReferenceValue = heavyModel.transform;
        }
        AssignAudioArray(serialized, "hitClips", new[]
        {
            "Assets/FPS Horror Sound Pack/Impact Sounds/Bullet/Bullet_Impact_Flesh1.wav",
            "Assets/FPS Horror Sound Pack/Impact Sounds/Bullet/Bullet_Impact_Flesh2.wav",
            "Assets/FPS Horror Sound Pack/Impact Sounds/Bullet/Bullet_Impact_Flesh3.wav",
            "Assets/Leohpaz/RPG_Essentials_Free/10_Battle_SFX/15_Impact_flesh_02.wav"
        });
        AssignAudioArray(serialized, "deathClips", new[]
        {
            "Assets/Leohpaz/RPG_Essentials_Free/10_Battle_SFX/69_Enemy_death_01.wav",
            "Assets/FPS Horror Sound Pack/Monster Sounds/Creature1.wav",
            "Assets/FPS Horror Sound Pack/Monster Sounds/Creature2.wav"
        });
        AssignAudioArray(serialized, "attackClips", new[]
        {
            "Assets/Leohpaz/RPG_Essentials_Free/10_Battle_SFX/08_Bite_04.wav",
            "Assets/Leohpaz/RPG_Essentials_Free/10_Battle_SFX/03_Claw_03.wav"
        });
        serialized.FindProperty("bloodPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BloodDecalsAndEffects/BloodGushFX/BloodSprayFX.prefab");
        serialized.FindProperty("headshotBloodPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/BloodDecalsAndEffects/BloodGushFX/BloodSprayFX_Extra.prefab");
        serialized.FindProperty("healthPickupPrefab").objectReferenceValue = CreateHealthPickupPrefab();
        serialized.FindProperty("healthDropChance").floatValue = 0.28f;
        serialized.FindProperty("headshotDamageMultiplier").floatValue = 3f;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(zombie, ZombiePrefabPath);
        Object.DestroyImmediate(zombie);
        return prefab;
    }

    private static ZGSpawner CreateSpawner(Transform root, GameObject zombiePrefab, Transform player, ZGGameManager manager, Terrain terrain)
    {
        var spawnerObj = new GameObject("ZG_ZombieSpawner");
        spawnerObj.transform.SetParent(root, false);
        var spawner = spawnerObj.AddComponent<ZGSpawner>();
        var points = new Transform[6];
        var coords = new[] { new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.25f), new Vector2(0.2f, 0.7f), new Vector2(0.8f, 0.72f), new Vector2(0.52f, 0.82f), new Vector2(0.35f, 0.58f) };
        for (var i = 0; i < points.Length; i++)
        {
            var p = new GameObject("ZG_SpawnPoint_" + i);
            p.transform.SetParent(spawnerObj.transform, false);
            p.transform.position = GetTerrainPoint(terrain, coords[i].x, coords[i].y);
            points[i] = p.transform;
        }

        var serialized = new SerializedObject(spawner);
        serialized.FindProperty("zombiePrefab").objectReferenceValue = zombiePrefab;
        serialized.FindProperty("player").objectReferenceValue = player;
        serialized.FindProperty("gameManager").objectReferenceValue = manager;
        serialized.FindProperty("spawnPoints").arraySize = points.Length;
        for (var i = 0; i < points.Length; i++)
        {
            serialized.FindProperty("spawnPoints").GetArrayElementAtIndex(i).objectReferenceValue = points[i];
        }
        serialized.FindProperty("totalToSpawn").intValue = 15;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return spawner;
    }

    private static ZGHelicopterLanding CreateHelicopter(Transform root, Terrain terrain)
    {
        var heli = new GameObject("ZG_Helicopter");
        heli.transform.SetParent(root, false);
        var landed = GetTerrainPoint(terrain, 0.82f, 0.42f) + Vector3.up * 2.2f;
        heli.transform.position = landed + Vector3.up * 24f;
        heli.transform.rotation = Quaternion.Euler(0f, -35f, 0f);

        var heliModel = InstantiatePrefab(
            "Assets/Low Poly Helicopters Pack Free/Prefabs/Heli_2.prefab",
            heli.transform,
            "ZG_HelicopterModel",
            Vector3.zero,
            Quaternion.identity,
            Vector3.one * 1.15f);
        StripColliders(heliModel);

        var rotor = FindChildContaining(heliModel != null ? heliModel.transform : heli.transform, "Rotor_Twin_Upper");
        if (heliModel == null || rotor == null)
        {
            var bodyMat = CreateMaterial("ZG_Helicopter_Mat", new Color(0.08f, 0.09f, 0.1f));
            var bladeMat = CreateMaterial("ZG_Helicopter_Blade_Mat", new Color(0.02f, 0.02f, 0.025f));
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(heli.transform, false);
            body.transform.localScale = new Vector3(2.8f, 0.85f, 1.2f);
            body.GetComponent<Renderer>().material = bodyMat;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            rotor = new GameObject("Rotor").transform;
            rotor.SetParent(heli.transform, false);
            rotor.localPosition = new Vector3(0f, 0.7f, 0f);
            for (var i = 0; i < 2; i++)
            {
                var blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = "Blade" + i;
                blade.transform.SetParent(rotor, false);
                blade.transform.localEulerAngles = new Vector3(0f, i * 90f, 0f);
                blade.transform.localScale = new Vector3(5.2f, 0.06f, 0.18f);
                blade.GetComponent<Renderer>().material = bladeMat;
                Object.DestroyImmediate(blade.GetComponent<Collider>());
            }
        }

        var landing = heli.AddComponent<ZGHelicopterLanding>();
        var serialized = new SerializedObject(landing);
        serialized.FindProperty("rotor").objectReferenceValue = rotor;
        serialized.FindProperty("landedLocalPosition").vector3Value = root.InverseTransformPoint(landed);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return landing;
    }

    private static GameObject CreateHealthPickupPrefab()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(HealthPickupPrefabPath);
        if (existing != null)
        {
            RefreshHealthPickupMaterials(existing);
            return existing;
        }

        var pickup = new GameObject("ZG_HealthPickupPrefab");
        var stemMat = CreateMaterial("ZG_HealthPickup_Dark_Mat", new Color(0.05f, 0.2f, 0.08f));
        var crossMat = CreateMaterial("ZG_HealthPickup_Cross_Mat", new Color(0.15f, 1f, 0.32f));

        var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        baseObj.name = "Case";
        baseObj.transform.SetParent(pickup.transform, false);
        baseObj.transform.localScale = new Vector3(0.55f, 0.18f, 0.55f);
        baseObj.GetComponent<Renderer>().material = stemMat;
        Object.DestroyImmediate(baseObj.GetComponent<Collider>());

        var vertical = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vertical.name = "CrossVertical";
        vertical.transform.SetParent(pickup.transform, false);
        vertical.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        vertical.transform.localScale = new Vector3(0.12f, 0.08f, 0.42f);
        vertical.GetComponent<Renderer>().material = crossMat;
        Object.DestroyImmediate(vertical.GetComponent<Collider>());

        var horizontal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        horizontal.name = "CrossHorizontal";
        horizontal.transform.SetParent(pickup.transform, false);
        horizontal.transform.localPosition = new Vector3(0f, 0.13f, 0f);
        horizontal.transform.localScale = new Vector3(0.42f, 0.08f, 0.12f);
        horizontal.GetComponent<Renderer>().material = crossMat;
        Object.DestroyImmediate(horizontal.GetComponent<Collider>());

        var trigger = pickup.AddComponent<SphereCollider>();
        trigger.radius = 0.75f;
        trigger.isTrigger = true;
        var pickupScript = pickup.AddComponent<ZGHealthPickup>();
        var serialized = new SerializedObject(pickupScript);
        serialized.FindProperty("pickupClip").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Leohpaz/RPG_Essentials_Free/8_Buffs_Heals_SFX/02_Heal_02.wav");
        serialized.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(pickup, HealthPickupPrefabPath);
        Object.DestroyImmediate(pickup);
        RefreshHealthPickupMaterials(prefab);
        return prefab;
    }

    private static void RefreshHealthPickupMaterials(GameObject pickup)
    {
        if (pickup == null)
        {
            return;
        }

        var stemMat = CreateMaterial("ZG_HealthPickup_Dark_Mat", new Color(0.05f, 0.2f, 0.08f));
        var crossMat = CreateMaterial("ZG_HealthPickup_Cross_Mat", new Color(0.15f, 1f, 0.32f));
        foreach (var renderer in pickup.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sharedMaterial = renderer.name.Contains("Cross") ? crossMat : stemMat;
            EditorUtility.SetDirty(renderer);
        }
        EditorUtility.SetDirty(pickup);
    }

    private static GameObject CreateWinZone(Transform root, Terrain terrain, ZGGameManager manager)
    {
        var zone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        zone.name = "ZG_HelicopterWinZone";
        zone.transform.SetParent(root, false);
        zone.transform.position = GetTerrainPoint(terrain, 0.82f, 0.42f) + Vector3.up * 0.04f;
        zone.transform.localScale = new Vector3(3.8f, 0.08f, 3.8f);
        zone.GetComponent<Renderer>().material = CreateMaterial("ZG_WinZone_Mat", new Color(0.15f, 0.8f, 1f, 0.45f));
        var collider = zone.GetComponent<Collider>();
        collider.isTrigger = true;
        var win = zone.AddComponent<ZGWinZone>();
        var serialized = new SerializedObject(win);
        serialized.FindProperty("gameManager").objectReferenceValue = manager;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return zone;
    }

    private static ZGHud CreateHud(ZGHealth health, ZGWeaponController weapon, ZGGameManager manager)
    {
        var canvasObj = new GameObject("ZG_HUD");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        canvasObj.AddComponent<GraphicRaycaster>();

        CreateHudPlate(canvasObj.transform, "LeftHudPlate", new Vector2(24f, -24f), new Vector2(250f, 82f), new Vector2(0f, 1f));
        CreateHudPlate(canvasObj.transform, "RightHudPlate", new Vector2(-24f, -24f), new Vector2(230f, 52f), new Vector2(1f, 1f));
        var healthText = CreateText(canvasObj.transform, "HealthText", new Vector2(38f, -28f), TextAnchor.UpperLeft, 23);
        var ammoText = CreateText(canvasObj.transform, "AmmoText", new Vector2(38f, -60f), TextAnchor.UpperLeft, 20);
        var objectiveText = CreateText(canvasObj.transform, "ObjectiveText", new Vector2(-38f, -30f), TextAnchor.UpperRight, 23);
        var centerText = CreateText(canvasObj.transform, "CenterText", Vector2.zero, TextAnchor.MiddleCenter, 42);
        var hitMarkerText = CreateText(canvasObj.transform, "HitMarkerText", Vector2.zero, TextAnchor.MiddleCenter, 34);
        hitMarkerText.text = string.Empty;
        var damageOverlay = CreateDamageOverlay(canvasObj.transform);
        var endOverlay = CreateEndOverlay(canvasObj.transform);
        var restartButton = CreateButton(canvasObj.transform, "RestartButton", "TEKRAR OYNA", new Vector2(0f, -96f));
        CreateCrosshair(canvasObj.transform);
        var mainMenuPanel = CreateMenuPanel(canvasObj.transform, "MainMenuPanel", "ZOMBİ AVLAMA");
        var playButton = CreateButton(mainMenuPanel.transform, "PlayButton", "OYNA", new Vector2(0f, -18f));
        var openSettingsButton = CreateButton(mainMenuPanel.transform, "SettingsButton", "AYARLAR", new Vector2(0f, -88f));
        var settingsPanel = CreateMenuPanel(canvasObj.transform, "SettingsPanel", "AYARLAR");
        var volumeSlider = CreateSlider(settingsPanel.transform, "VolumeSlider", "SES", new Vector2(0f, -18f), 0f, 1f);
        var sensitivitySlider = CreateSlider(settingsPanel.transform, "SensitivitySlider", "NİŞAN HASSASİYETİ", new Vector2(0f, -100f), 0.35f, 2.25f);
        var closeSettingsButton = CreateButton(settingsPanel.transform, "CloseSettingsButton", "GERİ", new Vector2(0f, -184f));

        var hud = canvasObj.AddComponent<ZGHud>();
        var serialized = new SerializedObject(hud);
        serialized.FindProperty("playerHealth").objectReferenceValue = health;
        serialized.FindProperty("weapon").objectReferenceValue = weapon;
        serialized.FindProperty("healthText").objectReferenceValue = healthText;
        serialized.FindProperty("ammoText").objectReferenceValue = ammoText;
        serialized.FindProperty("objectiveText").objectReferenceValue = objectiveText;
        serialized.FindProperty("centerText").objectReferenceValue = centerText;
        serialized.FindProperty("hitMarkerText").objectReferenceValue = hitMarkerText;
        serialized.FindProperty("damageOverlay").objectReferenceValue = damageOverlay;
        serialized.FindProperty("endOverlay").objectReferenceValue = endOverlay;
        serialized.FindProperty("mainMenuPanel").objectReferenceValue = mainMenuPanel;
        serialized.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        serialized.FindProperty("volumeSlider").objectReferenceValue = volumeSlider;
        serialized.FindProperty("sensitivitySlider").objectReferenceValue = sensitivitySlider;
        serialized.FindProperty("restartButton").objectReferenceValue = restartButton;
        serialized.FindProperty("gameManager").objectReferenceValue = manager;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(restartButton.onClick, hud.Restart);
        UnityEventTools.AddPersistentListener(playButton.onClick, hud.StartGame);
        UnityEventTools.AddPersistentListener(openSettingsButton.onClick, hud.OpenSettings);
        UnityEventTools.AddPersistentListener(closeSettingsButton.onClick, hud.CloseSettings);
        UnityEventTools.AddPersistentListener(volumeSlider.onValueChanged, hud.OnVolumeChanged);
        UnityEventTools.AddPersistentListener(sensitivitySlider.onValueChanged, hud.OnSensitivityChanged);
        return hud;
    }

    private static Image CreateHudPlate(Transform parent, string name, Vector2 anchored, Vector2 size, Vector2 anchor)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = anchored;
        rect.sizeDelta = size;
        var image = obj.AddComponent<Image>();
        image.color = new Color(0.025f, 0.035f, 0.045f, 0.72f);
        image.raycastTarget = false;
        return image;
    }

    private static GameObject CreateMenuPanel(Transform parent, string name, string title)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = obj.AddComponent<Image>();
        image.color = new Color(0.015f, 0.018f, 0.022f, 0.86f);

        var titleText = CreateText(obj.transform, name + "Title", new Vector2(0f, 118f), TextAnchor.MiddleCenter, 46);
        titleText.text = title;
        titleText.color = new Color(0.9f, 0.96f, 1f, 1f);
        return obj;
    }

    private static Slider CreateSlider(Transform parent, string name, string label, Vector2 anchored, float minValue, float maxValue)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);
        var rect = root.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchored;
        rect.sizeDelta = new Vector2(360f, 54f);

        var labelText = CreateText(root.transform, name + "Label", new Vector2(0f, 22f), TextAnchor.MiddleCenter, 20);
        labelText.text = label;
        labelText.rectTransform.sizeDelta = new Vector2(360f, 28f);

        var slider = root.AddComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;

        var background = CreateSliderImage(root.transform, "Background", new Vector2(0f, -12f), new Vector2(360f, 12f), new Color(0.12f, 0.15f, 0.18f, 1f));
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(root.transform, false);
        var fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = fillAreaRect.anchorMax = fillAreaRect.pivot = new Vector2(0.5f, 0.5f);
        fillAreaRect.anchoredPosition = new Vector2(0f, -12f);
        fillAreaRect.sizeDelta = new Vector2(340f, 12f);
        var fill = CreateSliderImage(fillArea.transform, "Fill", Vector2.zero, new Vector2(340f, 12f), new Color(0.28f, 0.72f, 1f, 1f));
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = Vector2.zero;
        fill.rectTransform.offsetMax = Vector2.zero;

        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(root.transform, false);
        var handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = handleAreaRect.anchorMax = handleAreaRect.pivot = new Vector2(0.5f, 0.5f);
        handleAreaRect.anchoredPosition = new Vector2(0f, -12f);
        handleAreaRect.sizeDelta = new Vector2(360f, 28f);
        var handle = CreateSliderImage(handleArea.transform, "Handle", Vector2.zero, new Vector2(22f, 22f), new Color(0.95f, 0.98f, 1f, 1f));

        slider.targetGraphic = handle;
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        background.raycastTarget = false;
        fill.raycastTarget = false;
        return slider;
    }

    private static Image CreateSliderImage(Transform parent, string name, Vector2 anchored, Vector2 size, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchored;
        rect.sizeDelta = size;
        var image = obj.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static Text CreateText(Transform parent, string name, Vector2 anchored, TextAnchor anchor, int size)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.sizeDelta = anchor == TextAnchor.MiddleCenter ? new Vector2(760f, 160f) : new Vector2(460f, 64f);
        if (anchor == TextAnchor.UpperRight)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
        }
        else if (anchor == TextAnchor.MiddleCenter)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        }
        else
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        }
        rect.anchoredPosition = anchored;
        var text = obj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = size;
        text.alignment = anchor;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(Transform parent, string label, string text, Vector2 anchored)
    {
        var obj = new GameObject(label);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchored;
        rect.sizeDelta = new Vector2(260f, 56f);
        var image = obj.AddComponent<Image>();
        image.color = new Color(0.08f, 0.12f, 0.16f, 0.92f);
        var button = obj.AddComponent<Button>();
        button.targetGraphic = image;
        var labelText = CreateText(obj.transform, "Label", Vector2.zero, TextAnchor.MiddleCenter, 24);
        labelText.text = text;
        labelText.rectTransform.sizeDelta = rect.sizeDelta;
        return button;
    }

    private static void CreateCrosshair(Transform parent)
    {
        var root = new GameObject("Crosshair");
        root.transform.SetParent(parent, false);
        var rect = root.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(24f, 24f);

        CreateCrosshairLine(root.transform, "Vertical", new Vector2(2f, 15f));
        CreateCrosshairLine(root.transform, "Horizontal", new Vector2(15f, 2f));
    }

    private static Image CreateDamageOverlay(Transform parent)
    {
        var obj = new GameObject("DamageOverlay");
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = obj.AddComponent<Image>();
        image.color = new Color(1f, 0f, 0f, 0f);
        image.raycastTarget = false;
        return image;
    }

    private static Image CreateEndOverlay(Transform parent)
    {
        var obj = new GameObject("EndOverlay");
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = obj.AddComponent<Image>();
        image.color = new Color(0.12f, 0f, 0f, 0.65f);
        image.raycastTarget = false;
        obj.SetActive(false);
        obj.transform.SetAsFirstSibling();
        return image;
    }

    private static void CreateCrosshairLine(Transform parent, string name, Vector2 size)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;
        var image = obj.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.9f);
        image.raycastTarget = false;
    }

    private static void CreateCampAndLighting(Transform root, Terrain terrain)
    {
        RenderSettings.fog = true;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.15f, 0.165f, 0.18f);
        RenderSettings.fogColor = new Color(0.06f, 0.075f, 0.09f);
        RenderSettings.fogDensity = 0.014f;

        var light = new GameObject("ZG_MoonLight").AddComponent<Light>();
        light.transform.SetParent(root, false);
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(42f, -35f, 0f);
        light.color = new Color(0.64f, 0.72f, 0.9f);
        light.intensity = 0.68f;
        light.shadowStrength = 0.45f;

        var firePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Camping Low Poly Pack Lite/Prefabs/Campfire.prefab");
        if (firePrefab != null)
        {
            var fire = PrefabUtility.InstantiatePrefab(firePrefab) as GameObject;
            fire.name = "ZG_Campfire_ObjectiveHint";
            fire.transform.SetParent(root, false);
            fire.transform.position = GetTerrainPoint(terrain, 0.5f, 0.5f);
            var point = fire.AddComponent<Light>();
            point.type = LightType.Point;
            point.color = new Color(1f, 0.44f, 0.18f);
            point.intensity = 2.15f;
            point.range = 7.25f;
        }
    }

    private static void CreateEventSystem()
    {
        var obj = new GameObject("EventSystem");
        obj.AddComponent<EventSystem>();
        obj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
    }

    private static GameObject InstantiatePrefab(string path, Transform parent, string name, Vector3 localPosition, Quaternion localRotation, Vector3 localScale)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
        {
            return null;
        }

        var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            return null;
        }

        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = localPosition;
        instance.transform.localRotation = localRotation;
        instance.transform.localScale = localScale;
        return instance;
    }

    private static void StripColliders(GameObject root)
    {
        if (root == null)
        {
            return;
        }

        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
        {
            Object.DestroyImmediate(collider);
        }
    }

    private static Transform FindChildContaining(Transform root, string namePart)
    {
        if (root == null)
        {
            return null;
        }

        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name.Contains(namePart))
            {
                return child;
            }
        }

        return null;
    }

    private static void AssignAudioArray(SerializedObject serialized, string propertyName, string[] paths)
    {
        var property = serialized.FindProperty(propertyName);
        property.arraySize = paths.Length;
        for (var i = 0; i < paths.Length; i++)
        {
            property.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(paths[i]);
        }
    }

    private static RuntimeAnimatorController CreatePlayerAnimatorController()
    {
        var path = PrefabFolder + "/ZG_Player.controller";
        AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Shoot", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Reload", AnimatorControllerParameterType.Trigger);

        var layer = controller.layers[0];
        var machine = layer.stateMachine;
        var idle = machine.AddState("Idle");
        idle.motion = LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Idles/HumanM@Idle01.fbx");
        var walk = machine.AddState("Walk");
        walk.motion = LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Forward.fbx");
        var run = machine.AddState("Run");
        run.motion = LoadClip("Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Forward.fbx");
        machine.defaultState = idle;

        AddFloatTransition(idle, walk, "Speed", AnimatorConditionMode.Greater, 0.1f);
        AddFloatTransition(walk, idle, "Speed", AnimatorConditionMode.Less, 0.1f);
        AddFloatTransition(walk, run, "Speed", AnimatorConditionMode.Greater, 6.1f);
        AddFloatTransition(run, walk, "Speed", AnimatorConditionMode.Less, 6.1f);
        AddFloatTransition(idle, run, "Speed", AnimatorConditionMode.Greater, 6.1f);
        AddFloatTransition(run, idle, "Speed", AnimatorConditionMode.Less, 0.1f);
        return controller;
    }

    private static RuntimeAnimatorController CreateZombieAnimatorController()
    {
        var path = PrefabFolder + "/ZG_Zombie.controller";
        AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Die", AnimatorControllerParameterType.Trigger);

        var layer = controller.layers[0];
        var machine = layer.stateMachine;
        var idle = machine.AddState("Idle");
        idle.motion = LoadClip("Assets/Zombie/Animations/Z_Idle.anim");
        var walk = machine.AddState("Walk");
        walk.motion = LoadClip("Assets/Zombie/Animations/Z_Walk_InPlace.anim");
        var run = machine.AddState("Run");
        run.motion = LoadClip("Assets/Zombie/Animations/Z_Run_InPlace.anim");
        var attack = machine.AddState("Attack");
        attack.motion = LoadClip("Assets/Zombie/Animations/Z_Attack.anim");
        var die = machine.AddState("Die");
        die.motion = LoadClip("Assets/Zombie/Animations/Z_FallingBack.anim");
        machine.defaultState = idle;

        AddFloatTransition(idle, walk, "Speed", AnimatorConditionMode.Greater, 0.5f);
        AddFloatTransition(walk, idle, "Speed", AnimatorConditionMode.Less, 0.5f);
        AddFloatTransition(walk, run, "Speed", AnimatorConditionMode.Greater, 1.5f);
        AddFloatTransition(run, walk, "Speed", AnimatorConditionMode.Less, 1.5f);
        AddTriggerTransition(machine, attack, "Attack", false);
        AddTriggerTransition(machine, die, "Die", true);
        AddTimedTransition(attack, walk, 0.75f);
        return controller;
    }

    private static void AssignAnimatorController(GameObject root, RuntimeAnimatorController controller)
    {
        if (root == null || controller == null)
        {
            return;
        }

        var animator = root.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            animator = root.AddComponent<Animator>();
        }
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
    }

    private static AnimationClip LoadClip(string path)
    {
        var direct = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (direct != null)
        {
            return direct;
        }

        foreach (var asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
        {
            if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
            {
                return clip;
            }
        }

        return null;
    }

    private static void AddFloatTransition(AnimatorState from, AnimatorState to, string parameter, AnimatorConditionMode mode, float threshold)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.16f;
        transition.AddCondition(mode, threshold, parameter);
    }

    private static void AddTriggerTransition(AnimatorStateMachine machine, AnimatorState to, string parameter, bool canTransitionToSelf)
    {
        var transition = machine.AddAnyStateTransition(to);
        transition.hasExitTime = false;
        transition.duration = 0.08f;
        transition.canTransitionToSelf = canTransitionToSelf;
        transition.AddCondition(AnimatorConditionMode.If, 0f, parameter);
    }

    private static void AddTimedTransition(AnimatorState from, AnimatorState to, float exitTime)
    {
        var transition = from.AddTransition(to);
        transition.hasExitTime = true;
        transition.exitTime = exitTime;
        transition.duration = 0.18f;
    }

    private static void FixAdventureCharacterMaterials()
    {
        UpdateMaterialToUrp("Assets/Adventure_Character/Materials/Man_Body.mat", "Assets/Adventure_Character/Textures/Man_Body_Albedo.tga", "Assets/Adventure_Character/Textures/Man_Body_Normals.tga", new Color(0.8f, 0.64f, 0.52f));
        UpdateMaterialToUrp("Assets/Adventure_Character/Materials/Man_Cloth_Up_01.mat", "Assets/Adventure_Character/Textures/Man_Cloth_Up_Albedo_01.tga", "Assets/Adventure_Character/Textures/Man_Cloth_Up_Normals.tga", new Color(0.18f, 0.22f, 0.26f));
        UpdateMaterialToUrp("Assets/Adventure_Character/Materials/Man_Cloth_Up_02.mat", "Assets/Adventure_Character/Textures/Man_Cloth_Up_Albedo_02.tga", "Assets/Adventure_Character/Textures/Man_Cloth_Up_Normals.tga", new Color(0.18f, 0.22f, 0.26f));
        UpdateMaterialToUrp("Assets/Adventure_Character/Materials/Man_Cloth_Up_03.mat", "Assets/Adventure_Character/Textures/Man_Cloth_Up_Albedo_03.tga", "Assets/Adventure_Character/Textures/Man_Cloth_Up_Normals.tga", new Color(0.18f, 0.22f, 0.26f));
        UpdateMaterialToUrp("Assets/Adventure_Character/Materials/Man_Cloth_Down_01.mat", "Assets/Adventure_Character/Textures/Man_Cloth_Down_Albedo_01.tga", "Assets/Adventure_Character/Textures/Man_Cloth_Down_Normals.tga", new Color(0.12f, 0.12f, 0.13f));
        UpdateMaterialToUrp("Assets/Adventure_Character/Materials/Man_Cloth_Down_02.mat", "Assets/Adventure_Character/Textures/Man_Cloth_Down_Albedo_02.tga", "Assets/Adventure_Character/Textures/Man_Cloth_Down_Normals.tga", new Color(0.12f, 0.12f, 0.13f));
        UpdateMaterialToUrp("Assets/Adventure_Character/Materials/Man_Cloth_Down_03.mat", "Assets/Adventure_Character/Textures/Man_Cloth_Down_Albedo_03.tga", "Assets/Adventure_Character/Textures/Man_Cloth_Down_Normals.tga", new Color(0.12f, 0.12f, 0.13f));
        UpdateMaterialToUrp("Assets/Adventure_Character/Materials/Stand.mat", null, null, new Color(0.22f, 0.22f, 0.22f));
        AssetDatabase.SaveAssets();
    }

    private static void FixWeaponAndHelicopterMaterials()
    {
        UpdateMaterialToUrp("Assets/HornetRifle/Model/Materials/HornetRifle_black.mat", "Assets/HornetRifle/Model/Materials/riffle_hornet_Material_AlbedoTransparency_black.png", "Assets/HornetRifle/Model/Materials/riffle_hornet_Material_Normal.png", new Color(0.04f, 0.04f, 0.045f));
        UpdateMaterialToUrp("Assets/HornetRifle/Model/Materials/HornetRifle_grey.mat", "Assets/HornetRifle/Model/Materials/riffle_hornet_Material_AlbedoTransparency_grey.png", "Assets/HornetRifle/Model/Materials/riffle_hornet_Material_Normal.png", new Color(0.25f, 0.25f, 0.25f));
        UpdateMaterialToUrp("Assets/HornetRifle/Model/Materials/HornetRifle_white.mat", "Assets/HornetRifle/Model/Materials/riffle_hornet_Material_AlbedoTransparency_white.png", "Assets/HornetRifle/Model/Materials/riffle_hornet_Material_Normal.png", new Color(0.7f, 0.7f, 0.68f));
        UpdateMaterialToUrp("Assets/HornetRifle/Model/Materials/HornetRifle_red.mat", "Assets/HornetRifle/Model/Materials/riffle_hornet_Material_AlbedoTransparency_red.png", "Assets/HornetRifle/Model/Materials/riffle_hornet_Material_Normal.png", new Color(0.45f, 0.04f, 0.03f));
        UpdateMaterialToUrp("Assets/HornetRifle/Model/Materials/HornetRifle_orange.mat", "Assets/HornetRifle/Model/Materials/riffle_hornet_Material_AlbedoTransparency_orange.png", "Assets/HornetRifle/Model/Materials/riffle_hornet_Material_Normal.png", new Color(0.7f, 0.28f, 0.06f));
        UpdateMaterialToUrp("Assets/HornetRifle/Model/Materials/HornetRifle_green.mat", "Assets/HornetRifle/Model/Materials/riffle_hornet_Material_AlbedoTransparency_green.png", "Assets/HornetRifle/Model/Materials/riffle_hornet_Material_Normal.png", new Color(0.12f, 0.35f, 0.16f));
        UpdateMaterialToUrp("Assets/HornetRifle/Model/Materials/Material.mat", "Assets/HornetRifle/Model/Materials/riffle_hornet_Material_AlbedoTransparency_black.png", "Assets/HornetRifle/Model/Materials/riffle_hornet_Material_Normal.png", new Color(0.05f, 0.05f, 0.05f));
        UpdateMaterialToUrp("Assets/Low Poly Helicopters Pack Free/Models/Materials/Plane.mat", "Assets/Low Poly Helicopters Pack Free/Models/Textures/PaletteTextureDiffuse.png", null, Color.white);
        UpdateMaterialToUrp("Assets/Low Poly Helicopters Pack Free/Models/Materials/Plane URP.mat", "Assets/Low Poly Helicopters Pack Free/Models/Textures/PaletteTextureDiffuse.png", null, Color.white);
        UpdateMaterialToUrp("Assets/Low Poly Helicopters Pack Free/Models/Materials/Plane HDRP.mat", "Assets/Low Poly Helicopters Pack Free/Models/Textures/PaletteTextureDiffuse.png", null, Color.white);
        UpdateMaterialToUrp("Assets/Low Poly Helicopters Pack Free/Models/Materials/PaletteTextureDiffuse.mat", "Assets/Low Poly Helicopters Pack Free/Models/Textures/PaletteTextureDiffuse.png", null, Color.white);
        UpdateMaterialToUrp("Assets/NewPunch/ShirtlessZombieFree/Materials/URP/ZombieBB_Body_URP.mat", "Assets/NewPunch/ShirtlessZombieFree/Textures/ZombieBB_Body_AlbedoTransparency.png", "Assets/NewPunch/ShirtlessZombieFree/Textures/ZombieBB_Body_Normal.png", Color.white);
        UpdateMaterialToUrp("Assets/NewPunch/ShirtlessZombieFree/Materials/URP/ZombieBB_Clothes_URP.mat", "Assets/NewPunch/ShirtlessZombieFree/Textures/ZombieBB_Clothes_AlbedoTransparency.png", "Assets/NewPunch/ShirtlessZombieFree/Textures/ZombieBB_Clothes_Normal.png", Color.white);
        UpdateMaterialToUrp("Assets/NewPunch/ShirtlessZombieFree/Materials/URP/Terrain_MAT_URP.mat", "Assets/NewPunch/ShirtlessZombieFree/Textures/ZombieBB_Body_AlbedoTransparency.png", null, Color.white);
        AssetDatabase.SaveAssets();
    }

    private static void UpdateMaterialToUrp(string materialPath, string albedoPath, string normalPath, Color fallbackColor)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            return;
        }

        material.shader = Shader.Find("Universal Render Pipeline/Lit");
        var albedo = string.IsNullOrEmpty(albedoPath) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
        if (albedo != null)
        {
            material.SetTexture("_BaseMap", albedo);
        }
        material.SetColor("_BaseColor", fallbackColor);

        var normal = string.IsNullOrEmpty(normalPath) ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
        if (normal != null)
        {
            material.SetTexture("_BumpMap", normal);
            material.EnableKeyword("_NORMALMAP");
        }
        EditorUtility.SetDirty(material);
    }

    private static Material CreateMaterial(string name, Color color)
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
        {
            AssetDatabase.CreateFolder("Assets", "Materials");
        }

        var path = "Assets/Materials/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = Shader.Find("Universal Render Pipeline/Lit");
        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);
        return material;
    }

}
