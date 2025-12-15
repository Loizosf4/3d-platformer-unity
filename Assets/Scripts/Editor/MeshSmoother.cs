using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class MeshSmoother : EditorWindow
{
    private enum SmoothingMethod
    {
        Subdivision,
        LaplacianSmooth,
        SubdivisionThenSmooth
    }
    
    private SmoothingMethod smoothingMethod = SmoothingMethod.SubdivisionThenSmooth;
    private int subdivisionIterations = 1;
    private int smoothingIterations = 2;
    private float smoothingStrength = 0.5f;
    private bool createCopy = true;
    private bool recalculateNormals = true;
    
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/Mesh Smoother")]
    public static void ShowWindow()
    {
        GetWindow<MeshSmoother>("Mesh Smoother");
    }
    
    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        GUILayout.Label("Mesh Smoothing Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);
        
        EditorGUILayout.HelpBox(
            "Select GameObjects with MeshFilter or SkinnedMeshRenderer components to smooth their meshes.\n\n" +
            "• Subdivision: Splits each triangle into 4, adding more geometry\n" +
            "• Laplacian Smooth: Moves vertices toward neighbor average\n" +
            "• Combined: Best results - subdivide then smooth",
            MessageType.Info);
        
        EditorGUILayout.Space(10);
        
        // Method selection
        GUILayout.Label("Smoothing Settings", EditorStyles.boldLabel);
        smoothingMethod = (SmoothingMethod)EditorGUILayout.EnumPopup("Method", smoothingMethod);
        
        EditorGUILayout.Space(5);
        
        // Show relevant options based on method
        if (smoothingMethod == SmoothingMethod.Subdivision || smoothingMethod == SmoothingMethod.SubdivisionThenSmooth)
        {
            subdivisionIterations = EditorGUILayout.IntSlider("Subdivision Iterations", subdivisionIterations, 1, 3);
            
            int estimatedMultiplier = (int)Mathf.Pow(4, subdivisionIterations);
            EditorGUILayout.HelpBox($"Each iteration multiplies triangles by 4.\n{subdivisionIterations} iteration(s) = {estimatedMultiplier}x triangles", MessageType.None);
        }
        
        if (smoothingMethod == SmoothingMethod.LaplacianSmooth || smoothingMethod == SmoothingMethod.SubdivisionThenSmooth)
        {
            EditorGUILayout.Space(5);
            smoothingIterations = EditorGUILayout.IntSlider("Smoothing Iterations", smoothingIterations, 1, 10);
            smoothingStrength = EditorGUILayout.Slider("Smoothing Strength", smoothingStrength, 0.1f, 1f);
        }
        
        EditorGUILayout.Space(10);
        
        // Options
        GUILayout.Label("Options", EditorStyles.boldLabel);
        createCopy = EditorGUILayout.Toggle("Create Copy (Recommended)", createCopy);
        recalculateNormals = EditorGUILayout.Toggle("Recalculate Normals", recalculateNormals);
        
        EditorGUILayout.Space(10);
        
        // Selection info
        GameObject[] selected = Selection.gameObjects;
        int meshCount = CountMeshes(selected);
        
        EditorGUILayout.HelpBox($"Selected: {selected.Length} object(s) with {meshCount} mesh(es)", MessageType.None);
        
        EditorGUILayout.Space(10);
        
        // Apply button
        GUI.enabled = meshCount > 0;
        if (GUILayout.Button("Apply Smoothing", GUILayout.Height(30)))
        {
            ApplySmoothing();
        }
        GUI.enabled = true;
        
        EditorGUILayout.Space(10);
        
        // Additional utilities
        GUILayout.Label("Utilities", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Show Mesh Info"))
        {
            ShowMeshInfo();
        }
        
        EditorGUILayout.EndScrollView();
    }
    
    private int CountMeshes(GameObject[] objects)
    {
        int count = 0;
        foreach (var obj in objects)
        {
            if (obj.GetComponent<MeshFilter>() != null || obj.GetComponent<SkinnedMeshRenderer>() != null)
                count++;
        }
        return count;
    }
    
    private void ApplySmoothing()
    {
        GameObject[] selected = Selection.gameObjects;
        int processedCount = 0;
        
        foreach (GameObject obj in selected)
        {
            Mesh originalMesh = null;
            MeshFilter mf = obj.GetComponent<MeshFilter>();
            SkinnedMeshRenderer smr = obj.GetComponent<SkinnedMeshRenderer>();
            
            if (mf != null)
                originalMesh = mf.sharedMesh;
            else if (smr != null)
                originalMesh = smr.sharedMesh;
            
            if (originalMesh == null)
                continue;
            
            // Create working copy
            Mesh workingMesh = Instantiate(originalMesh);
            workingMesh.name = originalMesh.name + "_Smoothed";
            
            // Apply smoothing based on method
            switch (smoothingMethod)
            {
                case SmoothingMethod.Subdivision:
                    for (int i = 0; i < subdivisionIterations; i++)
                        workingMesh = Subdivide(workingMesh);
                    break;
                    
                case SmoothingMethod.LaplacianSmooth:
                    workingMesh = LaplacianSmooth(workingMesh, smoothingIterations, smoothingStrength);
                    break;
                    
                case SmoothingMethod.SubdivisionThenSmooth:
                    for (int i = 0; i < subdivisionIterations; i++)
                        workingMesh = Subdivide(workingMesh);
                    workingMesh = LaplacianSmooth(workingMesh, smoothingIterations, smoothingStrength);
                    break;
            }
            
            if (recalculateNormals)
            {
                workingMesh.RecalculateNormals();
                workingMesh.RecalculateTangents();
            }
            
            workingMesh.RecalculateBounds();
            
            // Apply the mesh
            if (createCopy)
            {
                // Save as asset
                string path = EditorUtility.SaveFilePanelInProject(
                    "Save Smoothed Mesh",
                    workingMesh.name,
                    "asset",
                    "Save the smoothed mesh as an asset");
                
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.CreateAsset(workingMesh, path);
                    AssetDatabase.SaveAssets();
                    
                    Mesh savedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    
                    Undo.RecordObject(obj, "Apply Smoothed Mesh");
                    
                    if (mf != null)
                        mf.sharedMesh = savedMesh;
                    else if (smr != null)
                        smr.sharedMesh = savedMesh;
                    
                    processedCount++;
                }
            }
            else
            {
                // Apply directly (warning: modifies original if it's an asset)
                Undo.RecordObject(obj, "Apply Smoothed Mesh");
                
                if (mf != null)
                    mf.sharedMesh = workingMesh;
                else if (smr != null)
                    smr.sharedMesh = workingMesh;
                
                processedCount++;
            }
        }
        
        Debug.Log($"Smoothed {processedCount} mesh(es).");
    }
    
    private Mesh Subdivide(Mesh mesh)
    {
        // Loop subdivision - split each triangle into 4
        Vector3[] oldVertices = mesh.vertices;
        Vector2[] oldUVs = mesh.uv;
        Vector3[] oldNormals = mesh.normals;
        int[] oldTriangles = mesh.triangles;
        
        // Dictionary to store midpoint vertices (edge -> new vertex index)
        Dictionary<long, int> edgeMidpoints = new Dictionary<long, int>();
        List<Vector3> newVertices = new List<Vector3>(oldVertices);
        List<Vector2> newUVs = new List<Vector2>(oldUVs.Length > 0 ? oldUVs : new Vector2[oldVertices.Length]);
        List<Vector3> newNormals = new List<Vector3>(oldNormals.Length > 0 ? oldNormals : new Vector3[oldVertices.Length]);
        List<int> newTriangles = new List<int>();
        
        // Ensure we have UVs and normals
        if (oldUVs.Length == 0)
            oldUVs = new Vector2[oldVertices.Length];
        if (oldNormals.Length == 0)
        {
            mesh.RecalculateNormals();
            oldNormals = mesh.normals;
        }
        
        // Process each triangle
        for (int i = 0; i < oldTriangles.Length; i += 3)
        {
            int v0 = oldTriangles[i];
            int v1 = oldTriangles[i + 1];
            int v2 = oldTriangles[i + 2];
            
            // Get or create midpoint for each edge
            int m01 = GetOrCreateMidpoint(v0, v1, oldVertices, oldUVs, oldNormals, 
                                          newVertices, newUVs, newNormals, edgeMidpoints);
            int m12 = GetOrCreateMidpoint(v1, v2, oldVertices, oldUVs, oldNormals,
                                          newVertices, newUVs, newNormals, edgeMidpoints);
            int m20 = GetOrCreateMidpoint(v2, v0, oldVertices, oldUVs, oldNormals,
                                          newVertices, newUVs, newNormals, edgeMidpoints);
            
            // Create 4 new triangles
            // Triangle 1: v0, m01, m20
            newTriangles.Add(v0);
            newTriangles.Add(m01);
            newTriangles.Add(m20);
            
            // Triangle 2: m01, v1, m12
            newTriangles.Add(m01);
            newTriangles.Add(v1);
            newTriangles.Add(m12);
            
            // Triangle 3: m20, m12, v2
            newTriangles.Add(m20);
            newTriangles.Add(m12);
            newTriangles.Add(v2);
            
            // Triangle 4: m01, m12, m20 (center)
            newTriangles.Add(m01);
            newTriangles.Add(m12);
            newTriangles.Add(m20);
        }
        
        Mesh newMesh = new Mesh();
        newMesh.name = mesh.name;
        
        // Handle large meshes
        if (newVertices.Count > 65535)
            newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        
        newMesh.vertices = newVertices.ToArray();
        newMesh.uv = newUVs.ToArray();
        newMesh.normals = newNormals.ToArray();
        newMesh.triangles = newTriangles.ToArray();
        
        return newMesh;
    }
    
    private int GetOrCreateMidpoint(int v0, int v1, 
        Vector3[] oldVerts, Vector2[] oldUVs, Vector3[] oldNormals,
        List<Vector3> newVerts, List<Vector2> newUVs, List<Vector3> newNormals,
        Dictionary<long, int> edgeMidpoints)
    {
        // Create unique edge key (order independent)
        int min = Mathf.Min(v0, v1);
        int max = Mathf.Max(v0, v1);
        long edgeKey = ((long)min << 32) | (long)max;
        
        if (edgeMidpoints.TryGetValue(edgeKey, out int existingIndex))
            return existingIndex;
        
        // Create new midpoint vertex
        int newIndex = newVerts.Count;
        
        Vector3 midPos = (oldVerts[v0] + oldVerts[v1]) * 0.5f;
        newVerts.Add(midPos);
        
        if (oldUVs.Length > v0 && oldUVs.Length > v1)
            newUVs.Add((oldUVs[v0] + oldUVs[v1]) * 0.5f);
        else
            newUVs.Add(Vector2.zero);
        
        if (oldNormals.Length > v0 && oldNormals.Length > v1)
            newNormals.Add(((oldNormals[v0] + oldNormals[v1]) * 0.5f).normalized);
        else
            newNormals.Add(Vector3.up);
        
        edgeMidpoints[edgeKey] = newIndex;
        return newIndex;
    }
    
    private Mesh LaplacianSmooth(Mesh mesh, int iterations, float strength)
    {
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        
        // Build adjacency list (which vertices are connected to which)
        Dictionary<int, HashSet<int>> adjacency = new Dictionary<int, HashSet<int>>();
        
        for (int i = 0; i < vertices.Length; i++)
            adjacency[i] = new HashSet<int>();
        
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v0 = triangles[i];
            int v1 = triangles[i + 1];
            int v2 = triangles[i + 2];
            
            adjacency[v0].Add(v1);
            adjacency[v0].Add(v2);
            adjacency[v1].Add(v0);
            adjacency[v1].Add(v2);
            adjacency[v2].Add(v0);
            adjacency[v2].Add(v1);
        }
        
        // Apply smoothing iterations
        for (int iter = 0; iter < iterations; iter++)
        {
            Vector3[] newVertices = new Vector3[vertices.Length];
            
            for (int i = 0; i < vertices.Length; i++)
            {
                HashSet<int> neighbors = adjacency[i];
                
                if (neighbors.Count == 0)
                {
                    newVertices[i] = vertices[i];
                    continue;
                }
                
                // Calculate average position of neighbors
                Vector3 avg = Vector3.zero;
                foreach (int neighbor in neighbors)
                    avg += vertices[neighbor];
                avg /= neighbors.Count;
                
                // Move vertex toward average based on strength
                newVertices[i] = Vector3.Lerp(vertices[i], avg, strength);
            }
            
            vertices = newVertices;
        }
        
        Mesh smoothedMesh = Instantiate(mesh);
        smoothedMesh.vertices = vertices;
        
        return smoothedMesh;
    }
    
    private void ShowMeshInfo()
    {
        GameObject[] selected = Selection.gameObjects;
        
        foreach (GameObject obj in selected)
        {
            Mesh mesh = null;
            MeshFilter mf = obj.GetComponent<MeshFilter>();
            SkinnedMeshRenderer smr = obj.GetComponent<SkinnedMeshRenderer>();
            
            if (mf != null)
                mesh = mf.sharedMesh;
            else if (smr != null)
                mesh = smr.sharedMesh;
            
            if (mesh != null)
            {
                Debug.Log($"Mesh Info for '{obj.name}':\n" +
                         $"  Name: {mesh.name}\n" +
                         $"  Vertices: {mesh.vertexCount}\n" +
                         $"  Triangles: {mesh.triangles.Length / 3}\n" +
                         $"  Submeshes: {mesh.subMeshCount}\n" +
                         $"  Bounds: {mesh.bounds}");
            }
        }
    }
}
