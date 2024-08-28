using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GeneratePlotAR : MonoBehaviour
{
    [Header("Plot Dimensions")]
    [SerializeField] private int xSize;
    [SerializeField] private int zSize;
    [SerializeField] private GameObject pointer;
    [SerializeField] private GameObject gridPoint;
    [SerializeField] private TextMeshPro pointerText;

    [Header("Plot Equation Properties")]
    [SerializeField] private float W0;
    [SerializeField] private float k;
    [SerializeField] private float d;
    [SerializeField] private float thresholdDistance;

    [Header("Weave Plot Dimensions")]
    [SerializeField] private GameObject weavePlot;
    [SerializeField] private GameObject weavePointer;

    private bool build;

    private MeshCollider mc;
    private List<GameObject> gridPoints = new List<GameObject>();

    private Mesh plotMesh;
    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uvs;
    private Mesh plotWeaveMesh;
    private Vector3[] weaveVertices;
    private int[] weaveTriangles;
    private Vector2[] weaveUvs;
    private Vector3 startPointerPosition;
    private Ray ray;

    // used to create initial plot
    public void CreatePlot(out Vector3[] vertices, out int[] triangles, out Vector2[] uvs)
    {
        vertices = new Vector3[(2 * xSize + 1) * (2 * zSize + 1)];
        for (int i = 0, z = -zSize; z <= zSize; z++)
        {
            for (int x = -xSize; x <= xSize; x++)
            {
                vertices[i] = new Vector3(x, 0, z);
                i++;
            }
        }

        triangles = new int[xSize * zSize * 6 * 4];

        int vert = 0;
        int tris = 0;
        for (int z = -zSize; z < zSize; z++)
        {
            for (int x = -xSize; x < xSize; x++)
            {
                triangles[tris + 0] = vert + 0;
                triangles[tris + 1] = vert + 1 + 2 * xSize;
                triangles[tris + 2] = vert + 1;
                triangles[tris + 3] = vert + 1;
                triangles[tris + 4] = vert + 1 + 2 * xSize;
                triangles[tris + 5] = vert + 2 + 2 * xSize;

                vert++;
                tris += 6;
            }
            vert++;
        }

        uvs = new Vector2[vertices.Length];
        for (int i = 0, z = 0; z <= 2 * zSize; z++)
        {
            for (int x = 0; x <= 2 * xSize; x++)
            {
                uvs[i] = new Vector2(((float)x - (-xSize)) / xSize - (-xSize), ((float)z - (-zSize)) / zSize - (-zSize));
                i++;
            }
        }
    }

    // To Update the mesh values at each frame
    private void UpdateMesh(Mesh mesh, Vector3[] vertices, int[] triangles, Vector2[] uvs)
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
    }

    // will be used to get the energy value.
    public float U(float x, float z)
    {
        return -W0 * Mathf.Pow(k, 6) * ((1 / Mathf.Pow(Mathf.Pow(x - d, 2) + Mathf.Pow(z, 2) + Mathf.Pow(k, 2), 3)) + (1 / Mathf.Pow(Mathf.Pow(x + d, 2) + Mathf.Pow(z, 2) + Mathf.Pow(k, 2), 3)));
    }

    //Do Something when plot is touched
    private void StartTouch()
    {
        Ray rayFromTouchPos;
        if (!build && Input.GetMouseButton(0))
        {
            rayFromTouchPos = Camera.main.ScreenPointToRay(Input.mousePosition);
        }
        else
        {
            rayFromTouchPos = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
        }
        ray = rayFromTouchPos;
        if (Physics.Raycast(rayFromTouchPos, out var raycastHit, 100f))
        {
            int i = 0;
            weavePointer.transform.position = raycastHit.point;
            foreach (GameObject point in gridPoints)
            {
                if (Vector3.Distance(point.transform.position, weavePointer.transform.position) <= thresholdDistance)
                {
                    vertices[i].y = U(vertices[i].x, vertices[i].z);
                    pointer.transform.localPosition = startPointerPosition + vertices[i] + new Vector3(-0.65f, -0.65f, 0);
                    GameObject sphere = gridPoint;
                    sphere.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                    Instantiate(sphere, pointer.transform.position + new Vector3(-0.65f, -0.65f, 0), Quaternion.identity, gameObject.transform);
                    pointerText.text = U(vertices[i].x, vertices[i].z).ToString();
                }
                i++;
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        plotMesh = new Mesh();
        plotWeaveMesh = new Mesh();
        GetComponent<MeshFilter>().mesh = plotMesh;
        weavePlot.GetComponent<MeshFilter>().mesh = plotWeaveMesh;

        CreatePlot(out vertices, out triangles, out uvs);
        CreatePlot(out weaveVertices, out weaveTriangles, out weaveUvs);

        mc = GetComponent<MeshCollider>();
        mc.sharedMesh = plotMesh;
        weavePlot.GetComponent<MeshCollider>().sharedMesh = plotWeaveMesh;

        for (int z = -zSize; z <= zSize; z++)
        {
            for (int x = -xSize; x <= xSize; x++)
            {
                GameObject point = Instantiate(gridPoint, weavePlot.transform.position + (new Vector3(x, 0, z) * gameObject.transform.localScale.x), Quaternion.identity, weavePlot.transform);
                gridPoints.Add(point);
            }
        }
        startPointerPosition = pointer.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        pointerText.transform.parent.LookAt(Camera.main.transform.position);
        UpdateMesh(plotWeaveMesh, weaveVertices, weaveTriangles, weaveUvs);
        UpdateMesh(plotMesh, vertices, triangles, uvs);
        if(Input.GetMouseButton(0))
        {
            build = false;
        }
        else if(Input.touchCount > 0 && Input.touches[0].phase == TouchPhase.Began)
        {
            build = true;
        }
        StartTouch();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawRay(ray.origin, ray.direction.normalized * 100f);
    }

    public void QuitButton()
    {
        Application.Quit();
    }
}