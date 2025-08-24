using UnityEngine;
using UnityEditor;
using Unity.VectorGraphics;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class SvgLevelImporter : EditorWindow
{
    [MenuItem("Tools/Import SVG Level")]
    public static void ShowWindow()
    {
        GetWindow<SvgLevelImporter>("SVG Level Importer");
    }

    private TextAsset svgFile;
    private float floorHeight = 0.1f;
    private float wallHeight = 2f;
    private float sphereStartHeight = 2.5f;

    // Sphere Physics Properties
    private float sphereMass = 5f;
    private float sphereDrag = 0.01f;
    private float sphereAngularDrag = 0.05f;
    private float sphereBounciness = 0.8f;
    private float sphereDynamicFriction = 0.1f;
    private float sphereStaticFriction = 0.1f;

    private Material floorMaterial;
    private Material wallMaterial;
    private Material startMarkerMaterial;
    private Material endMarkerMaterial;
    private Material sphereMarkerMaterial;

    void OnGUI()
    {
        svgFile = (TextAsset)EditorGUILayout.ObjectField("SVG File", svgFile, typeof(TextAsset), false);
        floorHeight = EditorGUILayout.FloatField("Floor Height", floorHeight);
        wallHeight = EditorGUILayout.FloatField("Wall Height", wallHeight);
        sphereStartHeight = EditorGUILayout.FloatField("Sphere Start Height", sphereStartHeight);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Sphere Physics", EditorStyles.boldLabel);
        sphereMass = EditorGUILayout.FloatField("Mass", sphereMass);
        sphereDrag = EditorGUILayout.FloatField("Drag", sphereDrag);
        sphereAngularDrag = EditorGUILayout.FloatField("Angular Drag", sphereAngularDrag);
        sphereBounciness = EditorGUILayout.Slider("Bounciness", sphereBounciness, 0, 1);
        sphereDynamicFriction = EditorGUILayout.Slider("Dynamic Friction", sphereDynamicFriction, 0, 1);
        sphereStaticFriction = EditorGUILayout.Slider("Static Friction", sphereStaticFriction, 0, 1);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel);
        floorMaterial = (Material)EditorGUILayout.ObjectField("Floor Material", floorMaterial, typeof(Material), false);
        wallMaterial = (Material)EditorGUILayout.ObjectField("Wall Material", wallMaterial, typeof(Material), false);
        startMarkerMaterial = (Material)EditorGUILayout.ObjectField("Start Marker", startMarkerMaterial, typeof(Material), false);
        endMarkerMaterial = (Material)EditorGUILayout.ObjectField("End Marker", endMarkerMaterial, typeof(Material), false);
        sphereMarkerMaterial = (Material)EditorGUILayout.ObjectField("Sphere Marker", sphereMarkerMaterial, typeof(Material), false);
        EditorGUILayout.Space();

        if (GUILayout.Button("Import SVG"))
        {
            if (svgFile == null)
            {
                Debug.LogError("No SVG file selected.");
                return;
            }

            ImportSvg(svgFile.text, floorHeight, wallHeight);
        }
    }

    private void ImportSvg(string svgText, float floorHeight, float wallHeight)
    {
        var sceneInfo = SVGParser.ImportSVG(new StringReader(svgText));

        var tessOptions = new VectorUtils.TessellationOptions()
        {
            StepDistance = 0.03f,
            MaxCordDeviation = 0.05f,
            MaxTanAngleDeviation = 0.1f,
            SamplingStepSize = 0.02f
        };

        var geoms = VectorUtils.TessellateScene(sceneInfo.Scene, tessOptions);
        var rootGO = new GameObject("Level_" + svgFile.name);
        GameObject sphereGO = null;
        GameObject finalRoot = rootGO;

        for (int i = 0; i < geoms.Count; i++)
        {
            var geom = geoms[i];
            var node = sceneInfo.Scene.Root.Children[i];
            if (node.Shapes == null || node.Shapes.Count == 0) continue;

            var shape = node.Shapes[0];
            Color color = Color.clear;
            if (shape.Fill is SolidFill solid)
                color = solid.Color;

            if (IsMarkerColor(color))
                continue;

            var allContours = new List<List<Vector2>>();
            foreach (var contour in shape.Contours)
            {
                var points = new List<Vector2>();
                foreach (var segment in contour.Segments)
                    points.Add(segment.P0);
                if (points.Count >= 3)
                    allContours.Add(points);
            }

            float height = IsColor(color, "#cccccc") ? floorHeight : wallHeight;
            bool isFloor = IsColor(color, "#cccccc");
            float baseY = isFloor ? 0 : floorHeight;
            
            var contoursForExtrusion = allContours;

            if (isFloor && allContours.Count > 1)
            {
                var processedContours = new List<List<Vector2>> { allContours[0] };
                for (int j = 1; j < allContours.Count; j++)
                {
                    processedContours.Add(CreateRegularPolygonContour(allContours[j], 24));
                }
                contoursForExtrusion = processedContours;

                var tempGO = new GameObject("TempCollider");
                try
                {
                    var collider = tempGO.AddComponent<PolygonCollider2D>();
                    collider.pathCount = processedContours.Count;
                    for (int j = 0; j < processedContours.Count; j++)
                    {
                        var contour = processedContours[j];
                        float winding = GetContourWinding(contour);
                        if (j == 0) { if (winding < 0) contour.Reverse(); } 
                        else { if (winding > 0) contour.Reverse(); }
                        collider.SetPath(j, contour.ToArray());
                    }
                    
                    var tempMesh = collider.CreateMesh(true, true);
                    if (tempMesh != null)
                    {
                        var tempVertices = tempMesh.vertices;
                        // vertices2D = new Vector2[tempVertices.Length]; // This line is removed
                        // for (int j = 0; j < tempVertices.Length; j++) // This line is removed
                        // { // This line is removed
                        //     vertices2D[j] = tempVertices[j]; // This line is removed
                        // } // This line is removed

                        // var tempTriangles = tempMesh.triangles; // This line is removed
                        // indices2D = new ushort[tempTriangles.Length]; // This line is removed
                        // for (int j = 0; j < tempTriangles.Length; j++) // This line is removed
                        // { // This line is removed
                        //     indices2D[j] = (ushort)tempTriangles[j]; // This line is removed
                        // } // This line is removed
                        DestroyImmediate(tempMesh, true);
                    }
                    else
                    {
                         Debug.LogError("PolygonCollider2D failed to create a mesh for the floor.");
                    }
                }
                finally
                {
                    DestroyImmediate(tempGO);
                }
            }

            var mesh = Extrude(height, baseY, contoursForExtrusion, isFloor);

            string label = IsColor(color, "#cccccc") ? "Floor" : $"Wall_{i}";
            var go = new GameObject(label);
            go.transform.parent = rootGO.transform;

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;

            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = label.StartsWith("Wall") ? wallMaterial : floorMaterial;

            go.AddComponent<MeshCollider>();
        }

        // Marker placement
        foreach (var node in sceneInfo.Scene.Root.Children)
        {
            if (node.Shapes == null || node.Shapes.Count == 0) continue;
            var shape = node.Shapes[0];

            Color color = Color.clear;
            if (shape.Fill is SolidFill solid)
                color = solid.Color;

            string markerType = null;
            if (IsColor(color, "#00ff00")) markerType = "start";
            else if (IsColor(color, "#ff0000")) markerType = "end";
            else if (IsColor(color, "#0000ff")) markerType = "sphere";
            if (markerType == null) continue;

            var bounds = VectorUtils.Bounds(shape.Contours[0].Segments);
            Vector2 center = bounds.center;
            float width = bounds.size.x;
            float height = bounds.size.y;

            Vector3 worldPos;
            GameObject marker = null;
            if (markerType == "sphere")
            {
                worldPos = new Vector3(center.x, sphereStartHeight, center.y);
                marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.transform.localScale = new Vector3(width, width, height);
                if (sphereMarkerMaterial != null) marker.GetComponent<Renderer>().sharedMaterial = sphereMarkerMaterial;
                
                // Add and configure Rigidbody
                var rb = marker.AddComponent<Rigidbody>();
                rb.mass = sphereMass;
                rb.drag = sphereDrag;
                rb.angularDrag = sphereAngularDrag;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                
                // Create and assign Physic Material
                var col = marker.GetComponent<Collider>();
                var spherePhysicMaterial = new PhysicMaterial("SpherePhysicMaterial");
                spherePhysicMaterial.bounciness = sphereBounciness;
                spherePhysicMaterial.dynamicFriction = sphereDynamicFriction;
                spherePhysicMaterial.staticFriction = sphereStaticFriction;
                spherePhysicMaterial.frictionCombine = PhysicMaterialCombine.Minimum;
                spherePhysicMaterial.bounceCombine = PhysicMaterialCombine.Average;
                col.material = spherePhysicMaterial;

                sphereGO = marker;
            }
            else
            {
                worldPos = new Vector3(center.x, floorHeight + 0.01f, center.y);
                marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marker.transform.localScale = new Vector3(width, 0.01f, height);
                if (markerType == "start" && startMarkerMaterial != null) 
                    marker.GetComponent<Renderer>().sharedMaterial = startMarkerMaterial;
                else if (markerType == "end" && endMarkerMaterial != null) 
                    marker.GetComponent<Renderer>().sharedMaterial = endMarkerMaterial;
            }

            marker.name = char.ToUpper(markerType[0]) + markerType.Substring(1) + "Marker";
            marker.transform.position = worldPos;
            marker.transform.parent = rootGO.transform;
        }

        // Recenter the pivot of the entire level assembly based on the walls' center
        var renderersToConsider = rootGO.GetComponentsInChildren<Renderer>()
            .Where(r => r.gameObject.name.StartsWith("Wall") || r.gameObject.name.StartsWith("Floor"))
            .ToList();
            
        if (renderersToConsider.Count > 0)
        {
            var totalBounds = renderersToConsider[0].bounds;
            for(int i = 1; i < renderersToConsider.Count; i++)
            {
                totalBounds.Encapsulate(renderersToConsider[i].bounds);
            }

            var center = totalBounds.center;
            
            var pivot = new GameObject(rootGO.name + "_Pivot");
            pivot.transform.position = center;
            
            rootGO.transform.parent = pivot.transform;
            rootGO.transform.localPosition = Vector3.zero;

            // To make the workflow clean, we effectively replace the original root
            // with our new pivot object.
            pivot.name = rootGO.name;
            rootGO.name += "_Mesh";
            finalRoot = pivot;
        }

        if (finalRoot.GetComponent<MazeController>() == null)
        {
            finalRoot.AddComponent<MazeController>();
            // Since MazeController requires a Rigidbody, it's guaranteed to be there.
            // We configure it here to ensure the maze is non-physics-interactive by default.
            var rb = finalRoot.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }

        // Decouple the sphere after centering
        if (sphereGO != null)
        {
            sphereGO.transform.parent = null;
        }
    }

    private Mesh Extrude(float height, float baseY, List<List<Vector2>> contours, bool isFloor)
    {
        // MESH 1: TOP AND BOTTOM SURFACES
        var surfaceMesh = new Mesh();
        if (contours.Count > 0)
        {
            var surfaceVertices = new List<Vector3>();
            var surfaceTriangles = new List<int>();
            var triangulatedSurface = Triangulate(contours); 

            foreach (var tri in triangulatedSurface)
            {
                int baseIdx = surfaceVertices.Count;
                surfaceVertices.Add(new Vector3(tri.a.x, baseY + height, tri.a.y));
                surfaceVertices.Add(new Vector3(tri.b.x, baseY + height, tri.b.y));
                surfaceVertices.Add(new Vector3(tri.c.x, baseY + height, tri.c.y));
                surfaceTriangles.Add(baseIdx); surfaceTriangles.Add(baseIdx + 1); surfaceTriangles.Add(baseIdx + 2);

                baseIdx = surfaceVertices.Count;
                surfaceVertices.Add(new Vector3(tri.a.x, baseY, tri.a.y));
                surfaceVertices.Add(new Vector3(tri.b.x, baseY, tri.b.y));
                surfaceVertices.Add(new Vector3(tri.c.x, baseY, tri.c.y));
                surfaceTriangles.Add(baseIdx + 2); surfaceTriangles.Add(baseIdx + 1); surfaceTriangles.Add(baseIdx);
            }
            surfaceMesh.SetVertices(surfaceVertices);
            surfaceMesh.SetTriangles(surfaceTriangles, 0);
            surfaceMesh.RecalculateNormals();
            surfaceMesh.RecalculateBounds();
        }

        // MESH 2: SIDE WALLS
        var sideWallsMesh = new Mesh();
        if (contours.Count > 0)
        {
            var sideVertices = new List<Vector3>();
            var sideTriangles = new List<int>();

            for (int c = 0; c < contours.Count; c++)
            {
                var contour = contours[c];
                // For walls, every contour is an outer wall.
                // For the floor, only the first contour is the outer wall. Others are holes.
                bool isHole = isFloor && c > 0;

                for (int i = 0; i < contour.Count; i++)
                {
                    Vector2 p0 = contour[i];
                    Vector2 p1 = contour[(i + 1) % contour.Count];
                    int baseIndex = sideVertices.Count;
                    sideVertices.Add(new Vector3(p0.x, baseY, p0.y));
                    sideVertices.Add(new Vector3(p0.x, baseY + height, p0.y));
                    sideVertices.Add(new Vector3(p1.x, baseY, p1.y));
                    sideVertices.Add(new Vector3(p1.x, baseY + height, p1.y));
                    if (!isHole)
                    {
                        // This case applies to all wall objects, and the outer contour of the floor.
                        // It seems the floor's outer contour has a reversed winding compared to the wall objects.
                        if (isFloor) 
                        {
                            // Reversed winding for the floor's outer wall
                            sideTriangles.Add(baseIndex); sideTriangles.Add(baseIndex + 2); sideTriangles.Add(baseIndex + 3);
                            sideTriangles.Add(baseIndex); sideTriangles.Add(baseIndex + 3); sideTriangles.Add(baseIndex + 1);
                        }
                        else
                        {
                            // Standard winding for labyrinth walls
                            sideTriangles.Add(baseIndex); sideTriangles.Add(baseIndex + 1); sideTriangles.Add(baseIndex + 3);
                            sideTriangles.Add(baseIndex); sideTriangles.Add(baseIndex + 3); sideTriangles.Add(baseIndex + 2);
                        }
                    }
                    else
                    {
                        // This case only applies to holes in the floor, and it works correctly.
                        sideTriangles.Add(baseIndex); sideTriangles.Add(baseIndex + 2); sideTriangles.Add(baseIndex + 3);
                        sideTriangles.Add(baseIndex); sideTriangles.Add(baseIndex + 3); sideTriangles.Add(baseIndex + 1);
                    }
                }
            }
            sideWallsMesh.SetVertices(sideVertices);
            sideWallsMesh.SetTriangles(sideTriangles, 0);
            sideWallsMesh.RecalculateNormals();
            sideWallsMesh.RecalculateBounds();
        }

        // COMBINE
        var combine = new CombineInstance[2];
        combine[0].mesh = surfaceMesh;
        combine[0].transform = Matrix4x4.identity;
        combine[1].mesh = sideWallsMesh;
        combine[1].transform = Matrix4x4.identity;

        var finalMesh = new Mesh();
        finalMesh.CombineMeshes(combine, true, false);
        finalMesh.RecalculateNormals();
        
        // In recent Unity versions, destroying meshes used in CombineMeshes is not always necessary 
        // and can sometimes cause issues, especially in the editor.
        // It's safer to let Unity's garbage collector handle them if they are not referenced elsewhere.
        // DestroyImmediate(surfaceMesh, true);
        // DestroyImmediate(sideWallsMesh, true);

        return finalMesh;
    }

    private struct Triangle { public Vector2 a, b, c; }
    private List<Triangle> Triangulate(List<List<Vector2>> allContours)
    {
        var triangles = new List<Triangle>();
        var tempGO = new GameObject("TempTriangulator");
        try
        {
            var collider = tempGO.AddComponent<PolygonCollider2D>();
            collider.pathCount = allContours.Count;
            for (int j = 0; j < allContours.Count; j++)
            {
                var contour = new List<Vector2>(allContours[j]);
                float winding = GetContourWinding(contour);
                if (j == 0) { if (winding < 0) contour.Reverse(); } 
                else { if (winding > 0) contour.Reverse(); }
                collider.SetPath(j, contour.ToArray());
            }

            var tempMesh = collider.CreateMesh(false, false);
            if (tempMesh != null)
            {
                for (int i = 0; i < tempMesh.triangles.Length; i += 3)
                {
                    triangles.Add(new Triangle {
                        a = tempMesh.vertices[tempMesh.triangles[i]],
                        b = tempMesh.vertices[tempMesh.triangles[i+1]],
                        c = tempMesh.vertices[tempMesh.triangles[i+2]],
                    });
                }
                DestroyImmediate(tempMesh);
            }
        }
        finally { DestroyImmediate(tempGO); }
        return triangles;
    }

    private List<Vector2> CreateRegularPolygonContour(List<Vector2> circleContour, int sides)
    {
        if (circleContour == null || circleContour.Count == 0) return new List<Vector2>();

        float minX = circleContour[0].x, maxX = circleContour[0].x;
        float minY = circleContour[0].y, maxY = circleContour[0].y;
        foreach (var p in circleContour)
        {
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }
        Vector2 center = new Vector2((minX + maxX) / 2, (minY + maxY) / 2);
        float radius = ((maxX - minX) + (maxY - minY)) / 4;

        var polygonPoints = new List<Vector2>();
        if (sides < 3) sides = 3;
        for (int i = 0; i < sides; i++)
        {
            float angle = (360f / sides * i) * Mathf.Deg2Rad;
            float x = center.x + radius * Mathf.Cos(angle);
            float y = center.y + radius * Mathf.Sin(angle);
            polygonPoints.Add(new Vector2(x, y));
        }
        return polygonPoints;
    }

    private float GetContourWinding(List<Vector2> contour)
    {
        float signedArea = 0;
        for (int i = 0; i < contour.Count; i++)
        {
            Vector2 p0 = contour[i];
            Vector2 p1 = contour[(i + 1) % contour.Count];
            signedArea += (p1.x - p0.x) * (p1.y + p0.y);
        }
        return signedArea;
    }

    private bool IsColor(Color actual, string hex)
    {
        if (!ColorUtility.TryParseHtmlString(hex, out var expected))
            return false;
        return Vector4.Distance(actual, expected) < 0.01f;
    }

    private bool IsMarkerColor(Color color)
    {
        return IsColor(color, "#00ff00") || IsColor(color, "#ff0000") || IsColor(color, "#0000ff");
    }
}