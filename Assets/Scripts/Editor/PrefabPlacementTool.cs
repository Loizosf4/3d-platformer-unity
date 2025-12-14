using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Editor tool for quickly placing prefabs in the scene with mouse clicks.
/// Supports single placement and array/grid placement modes.
/// Access via: Window > Prefab Placement Tool
/// </summary>
public class PrefabPlacementTool : EditorWindow
{
    [MenuItem("Window/Prefab Placement Tool")]
    public static void ShowWindow()
    {
        var window = GetWindow<PrefabPlacementTool>("Prefab Placer");
        window.minSize = new Vector2(300, 450);
    }

    // Settings
    private GameObject selectedPrefab;
    private Vector2 scrollPosition;
    private Vector2 mainScrollPosition;
    private string searchFilter = "";
    private bool isPlacementMode = false;
    private bool snapToGrid = false;
    private float gridSize = 1f;
    private bool alignToSurface = true;
    private bool randomRotation = false;
    private Vector3 rotationOffset = Vector3.zero;
    private bool createParent = true;
    private string parentName = "Placed Objects";
    private GameObject parentObject;
    
    // Array placement
    private bool arrayMode = false;
    private bool is2DArray = false;
    private int arrayCount = 5;
    private int arrayRows = 3;
    private int arrayColumns = 3;
    private Vector3 arrayOffset = new Vector3(2f, 0f, 0f);
    private bool useObjectSizeX = false;
    private bool useObjectSizeY = false;
    private bool useObjectSizeZ = false;
    private float offsetMultiplier = 1f;
    
    // Prefab library
    private List<GameObject> allPrefabs = new List<GameObject>();
    private List<GameObject> filteredPrefabs = new List<GameObject>();
    
    // Cached data
    private GUIStyle buttonStyle;
    private GUIStyle selectedButtonStyle;
    
    private void OnEnable()
    {
        RefreshPrefabList();
        SceneView.duringSceneGui += OnSceneGUI;
    }
    
    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        isPlacementMode = false;
    }
    
    private void OnGUI()
    {
        InitStyles();
        mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);
        
        
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Prefab Placement Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        // Refresh button
        if (GUILayout.Button("Refresh Prefab List", GUILayout.Height(25)))
        {
            RefreshPrefabList();
        }
        
        EditorGUILayout.Space(5);
        
        // Search field
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Search:", GUILayout.Width(50));
        string newSearch = EditorGUILayout.TextField(searchFilter);
        if (newSearch != searchFilter)
        {
            searchFilter = newSearch;
            FilterPrefabs();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(5);
        
        // Selected prefab display
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Selected Prefab:", EditorStyles.boldLabel);
        GameObject newPrefab = (GameObject)EditorGUILayout.ObjectField(selectedPrefab, typeof(GameObject), false, GUILayout.Height(60));
        if (newPrefab != selectedPrefab)
        {
            selectedPrefab = newPrefab;
        }
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        // Placement mode toggle
        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = isPlacementMode ? Color.green : Color.white;
        if (GUILayout.Button(isPlacementMode ? "◉ PLACEMENT MODE ACTIVE (ESC to exit)" : "Activate Placement Mode", GUILayout.Height(30)))
        {
            isPlacementMode = !isPlacementMode;
            if (!isPlacementMode)
            {
                Tools.current = Tool.Move;
            }
        }
        GUI.backgroundColor = originalColor;
        
        EditorGUILayout.Space(5);
        
        // Settings
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Placement Settings", EditorStyles.boldLabel);
        
        snapToGrid = EditorGUILayout.Toggle("Snap to Grid", snapToGrid);
        if (snapToGrid)
        {
            EditorGUI.indentLevel++;
            gridSize = EditorGUILayout.FloatField("Grid Size", gridSize);
            EditorGUI.indentLevel--;
        }
        
        alignToSurface = EditorGUILayout.Toggle("Align to Surface Normal", alignToSurface);
        randomRotation = EditorGUILayout.Toggle("Random Y Rotation", randomRotation);
        
        if (!randomRotation)
        {
            rotationOffset = EditorGUILayout.Vector3Field("Rotation Offset", rotationOffset);
        }
        
        EditorGUILayout.Space(3);
        createParent = EditorGUILayout.Toggle("Create Parent Object", createParent);
        if (createParent)
        {
            EditorGUI.indentLevel++;
            parentName = EditorGUILayout.TextField("Parent Name", parentName);
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        // Array placement settings
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Array Placement", EditorStyles.boldLabel);
        
        arrayMode = EditorGUILayout.Toggle("Enable Array Mode", arrayMode);
        
        if (arrayMode)
        {
            EditorGUI.indentLevel++;
            
            is2DArray = EditorGUILayout.Toggle("2D Array (Grid)", is2DArray);
            
            if (is2DArray)
            {
                arrayRows = EditorGUILayout.IntField("Rows", Mathf.Max(1, arrayRows));
                arrayColumns = EditorGUILayout.IntField("Columns", Mathf.Max(1, arrayColumns));
                EditorGUILayout.LabelField($"Total: {arrayRows * arrayColumns} prefabs", EditorStyles.miniLabel);
            }
            else
            {
                arrayCount = EditorGUILayout.IntField("Count", Mathf.Max(1, arrayCount));
            }
            
            EditorGUILayout.Space(3);
            arrayOffset = EditorGUILayout.Vector3Field("Offset Between Prefabs", arrayOffset);
            
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Auto-Size Offset Options:", EditorStyles.miniBoldLabel);
            EditorGUI.indentLevel++;
            
            EditorGUILayout.BeginHorizontal();
            useObjectSizeX = EditorGUILayout.Toggle("Use Object Size X", useObjectSizeX);
            useObjectSizeY = EditorGUILayout.Toggle("Y", useObjectSizeY);
            useObjectSizeZ = EditorGUILayout.Toggle("Z", useObjectSizeZ);
            EditorGUILayout.EndHorizontal();
            
            if (useObjectSizeX || useObjectSizeY || useObjectSizeZ)
            {
                offsetMultiplier = EditorGUILayout.Slider("Size Multiplier", offsetMultiplier, 0.1f, 5f);
                
                if (selectedPrefab != null && GUILayout.Button("Calculate from Selected Prefab"))
                {
                    CalculateOffsetFromPrefab();
                }
                
                if (selectedPrefab != null)
                {
                    Bounds bounds = GetPrefabBounds(selectedPrefab);
                    EditorGUILayout.LabelField($"Prefab Size: X={bounds.size.x:F2}, Y={bounds.size.y:F2}, Z={bounds.size.z:F2}", EditorStyles.miniLabel);
                }
            }
            
            EditorGUI.indentLevel--;
            
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.Space(5);
        
        // Prefab list
        EditorGUILayout.LabelField($"Available Prefabs ({filteredPrefabs.Count})", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        if (filteredPrefabs.Count == 0)
        {
            EditorGUILayout.HelpBox("No prefabs found. Make sure you have prefabs in your project.", MessageType.Info);
        }
        else
        {
            foreach (var prefab in filteredPrefabs)
            {
                if (prefab == null) continue;
                
                GUIStyle style = (selectedPrefab == prefab) ? selectedButtonStyle : buttonStyle;
                
                EditorGUILayout.BeginHorizontal();
                
                if (GUILayout.Button(prefab.name, style, GUILayout.Height(30)))
                {
                    selectedPrefab = prefab;
                }
                
                if (GUILayout.Button("Select in Project", GUILayout.Width(120), GUILayout.Height(30)))
                {
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }
        
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space(5);
        
        // Instructions
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Instructions:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("• Select a prefab from the list", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("• Click 'Activate Placement Mode'", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("• Click in Scene view to place", EditorStyles.wordWrappedLabel);
        EditorGUILayout.LabelField("• Enable Array Mode for grids/lines", EditorStyles.wordWrappedLabel);
        
        EditorGUILayout.EndScrollView();
        EditorGUILayout.LabelField("• Press ESC to exit placement mode", EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndVertical();
    }
    
    private void OnSceneGUI(SceneView sceneView)
    {
        if (!isPlacementMode || selectedPrefab == null)
            return;
        
        Event e = Event.current;
        
        // Handle ESC key to exit placement mode
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        {
            isPlacementMode = false;
            e.Use();
            Repaint();
            return;
        }
        
        // Prevent normal selection in placement mode
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        
        // Draw preview at mouse position
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            Vector3 placePosition = hit.point;
            Quaternion placeRotation = Quaternion.identity;
            
            if (snapToGrid)
            {
                placePosition = SnapToGrid(placePosition);
            }
            
            if (alignToSurface)
            {
                placeRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }
            
            if (!randomRotation)
            {
                placeRotation *= Quaternion.Euler(rotationOffset);
            }
            
            // Draw preview wireframe
            Handles.color = new Color(0f, 1f, 0f, 0.5f);
            Handles.DrawWireCube(placePosition, Vector3.one * 0.5f);
            
            // Place prefab(s) on click
            if (e.type == EventType.MouseDown && e.button == 0)
            {
                if (arrayMode)
                {
                    PlacePrefabArray(placePosition, placeRotation);
                }
                else
                {
                    PlacePrefab(placePosition, placeRotation);
                }
                e.Use();
            }
        }
        
        // Draw instructions in scene
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, 10, 300, 120));
        GUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("PLACEMENT MODE ACTIVE", EditorStyles.boldLabel);
        GUILayout.Label($"Prefab: {selectedPrefab.name}");
        if (arrayMode)
        {
            string arrayInfo = is2DArray ? $"{arrayRows}x{arrayColumns} Grid" : $"{arrayCount} Linear";
            GUILayout.Label($"Array: {arrayInfo}");
        }
        GUILayout.Label("Left Click: Place | ESC: Exit");
        GUILayout.EndVertical();
        GUILayout.EndArea();
        Handles.EndGUI();
        
        sceneView.Repaint();
    }
    
    private void PlacePrefab(Vector3 position, Quaternion rotation)
    {
        if (selectedPrefab == null) return;
        
        GameObject instance = InstantiatePrefabAtPosition(position, rotation);
        Undo.RegisterCreatedObjectUndo(instance, "Place Prefab");
        Selection.activeGameObject = instance;
    }
    
    private void PlacePrefabArray(Vector3 startPosition, Quaternion rotation)
    {
        if (selectedPrefab == null) return;
        
        // Calculate final offset (use object size if enabled)
        Vector3 finalOffset = CalculateFinalOffset();
        
        List<GameObject> placedInstances = new List<GameObject>();
        
        if (is2DArray)
        {
            // Place 2D grid
            for (int row = 0; row < arrayRows; row++)
            {
                for (int col = 0; col < arrayColumns; col++)
                {
                    Vector3 offset = new Vector3(
                        finalOffset.x * col,
                        finalOffset.y * row,
                        finalOffset.z * row
                    );
                    
                    Vector3 position = startPosition + rotation * offset;
                    GameObject instance = InstantiatePrefabAtPosition(position, rotation);
                    placedInstances.Add(instance);
                }
            }
        }
        else
        {
            // Place 1D array
            for (int i = 0; i < arrayCount; i++)
            {
                Vector3 offset = finalOffset * i;
                Vector3 position = startPosition + rotation * offset;
                GameObject instance = InstantiatePrefabAtPosition(position, rotation);
                placedInstances.Add(instance);
            }
        }
        
        // Group undo
        if (placedInstances.Count > 0)
        {
            foreach (var instance in placedInstances)
            {
                Undo.RegisterCreatedObjectUndo(instance, "Place Prefab Array");
            }
            Selection.objects = placedInstances.ToArray();
        }
    }
    
    private GameObject InstantiatePrefabAtPosition(Vector3 position, Quaternion rotation)
    {
        // Apply random rotation if enabled
        Quaternion finalRotation = rotation;
        if (randomRotation)
        {
            finalRotation *= Quaternion.Euler(0, Random.Range(0f, 360f), 0);
        }
        
        // Instantiate prefab
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(selectedPrefab);
        instance.transform.position = position;
        instance.transform.rotation = finalRotation;
        
        // Parent to container if enabled
        if (createParent)
        {
            if (parentObject == null)
            {
                parentObject = GameObject.Find(parentName);
                if (parentObject == null)
                {
                    parentObject = new GameObject(parentName);
                    Undo.RegisterCreatedObjectUndo(parentObject, "Create Parent Object");
                }
            }
            instance.transform.SetParent(parentObject.transform);
        }
        
        return instance;
    }
    
    private Vector3 CalculateFinalOffset()
    {
        Vector3 finalOffset = arrayOffset;
        
        if (selectedPrefab != null && (useObjectSizeX || useObjectSizeY || useObjectSizeZ))
        {
            Bounds bounds = GetPrefabBounds(selectedPrefab);
            
            if (useObjectSizeX)
                finalOffset.x = bounds.size.x * offsetMultiplier;
            if (useObjectSizeY)
                finalOffset.y = bounds.size.y * offsetMultiplier;
            if (useObjectSizeZ)
                finalOffset.z = bounds.size.z * offsetMultiplier;
        }
        
        return finalOffset;
    }
    
    private void CalculateOffsetFromPrefab()
    {
        if (selectedPrefab == null) return;
        
        Bounds bounds = GetPrefabBounds(selectedPrefab);
        
        if (useObjectSizeX)
            arrayOffset.x = bounds.size.x * offsetMultiplier;
        if (useObjectSizeY)
            arrayOffset.y = bounds.size.y * offsetMultiplier;
        if (useObjectSizeZ)
            arrayOffset.z = bounds.size.z * offsetMultiplier;
    }
    
    private Bounds GetPrefabBounds(GameObject prefab)
    {
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one);
        
        // Try to get bounds from renderers
        Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        // Try colliders if no renderers
        else
        {
            Collider[] colliders = prefab.GetComponentsInChildren<Collider>();
            if (colliders.Length > 0)
            {
                bounds = colliders[0].bounds;
                foreach (Collider collider in colliders)
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }
        }
        
        return bounds;
    }
    
    private Vector3 SnapToGrid(Vector3 position)
    {
        return new Vector3(
            Mathf.Round(position.x / gridSize) * gridSize,
            Mathf.Round(position.y / gridSize) * gridSize,
            Mathf.Round(position.z / gridSize) * gridSize
        );
    }
    
    private void RefreshPrefabList()
    {
        allPrefabs.Clear();
        
        // Find all prefabs in the project
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                allPrefabs.Add(prefab);
            }
        }
        
        // Sort alphabetically
        allPrefabs = allPrefabs.OrderBy(p => p.name).ToList();
        
        FilterPrefabs();
    }
    
    private void FilterPrefabs()
    {
        if (string.IsNullOrEmpty(searchFilter))
        {
            filteredPrefabs = new List<GameObject>(allPrefabs);
        }
        else
        {
            filteredPrefabs = allPrefabs
                .Where(p => p.name.ToLower().Contains(searchFilter.ToLower()))
                .ToList();
        }
    }
    
    private void InitStyles()
    {
        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.alignment = TextAnchor.MiddleLeft;
            buttonStyle.padding = new RectOffset(10, 10, 5, 5);
        }
        
        if (selectedButtonStyle == null)
        {
            selectedButtonStyle = new GUIStyle(buttonStyle);
            selectedButtonStyle.normal.background = selectedButtonStyle.active.background;
            selectedButtonStyle.fontStyle = FontStyle.Bold;
        }
    }
}
