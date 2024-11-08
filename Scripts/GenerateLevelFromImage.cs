using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder;

public class GenerateLevelFromImage : MonoBehaviour
{
    [System.Serializable]
    public struct GenerationSettings
    {
        public int tileSize;
        public int tileHeight;
        public Vector2 uvScale;

        [Space]
        public string objectTag;
        public bool addCollider;

        [Space]
        public bool createFloors;
        public bool createWalls;
        public bool createCeilings;

        [Space]
        public Material floorMaterial;
        public Material wallMaterial;
        public Material ceilingMaterial;
    }

    class MeshBuilder
    {
        public struct MaterialGroup
        {
            public Material material;
            public List<Face> faces;
        }

        public List<Vector3> positions;
        public List<Face> faces;
        public List<MaterialGroup> materialGroups;

        int numQuads;

        public MeshBuilder()
        {
            positions = new List<Vector3>();
            faces = new List<Face>();
            materialGroups = new List<MaterialGroup>();
            numQuads = 0;
        }

        public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4, int materialGroupIndex)
        {
            positions.Add(v1);
            positions.Add(v2);
            positions.Add(v3);
            positions.Add(v4);

            int index = numQuads * 4;

            Face face1 = new Face(new int[] { index, index + 2, index + 1 });
            Face face2 = new Face(new int[] { index + 2, index + 3, index + 1 });

            faces.Add(face1);
            faces.Add(face2);

            materialGroups[materialGroupIndex].faces.Add(face1);
            materialGroups[materialGroupIndex].faces.Add(face2);

            numQuads++;
        }

        public void CreateNewMaterialGroup(Material material)
        {
            MaterialGroup newMaterialGroup = new MaterialGroup();
            newMaterialGroup.material = material;
            newMaterialGroup.faces = new List<Face>();

            materialGroups.Add(newMaterialGroup);
        }
    }

    public Texture2D image;
    public GenerationSettings settings;

    public void Generate(Texture2D image, GenerationSettings settings)
    {
        DestroyChildren();
        CreateMeshes(image, settings);
        Debug.Log("Level generated sucessfully");
    }

    void CreateMeshes(Texture2D image, GenerationSettings settings)
    {
        for (int x = 0; x < image.width; x++)
        {
            for (int y = 0; y < image.height; y++)
            {
                if (IndexFilled(image, x, y))
                {
                    CreateSegment(image, settings, x, y);
                }
            }
        }
    }

    GameObject CreateSegment(Texture2D image, GenerationSettings settings, int x, int y)
    {
        ProBuilderMesh mesh = CreateSegmentMesh(image, settings, x, y);
        GameObject segment = mesh.gameObject;

        segment.name = x + ", " + y;
        if (settings.objectTag != string.Empty)
        {
            segment.tag = settings.objectTag;
        }
        
        segment.transform.SetParent(transform);

        int xPos = (x - (image.width / 2)) * settings.tileSize * 2;
        int yPos = (y - (image.height / 2)) * settings.tileSize * 2;
        segment.transform.localPosition = new Vector3(xPos, 0, yPos);

        if (settings.addCollider)
        {
            segment.AddComponent<MeshCollider>();
        }
        
        return segment;
    }

    ProBuilderMesh CreateSegmentMesh(Texture2D image, GenerationSettings settings, int x, int y)
    {
        MeshBuilder meshBuilder = new MeshBuilder();

        meshBuilder.CreateNewMaterialGroup(settings.floorMaterial);
        meshBuilder.CreateNewMaterialGroup(settings.wallMaterial);
        meshBuilder.CreateNewMaterialGroup(settings.ceilingMaterial);

        int s = settings.tileSize;
        int h = settings.tileHeight;

        if (settings.createFloors)
        {
            meshBuilder.AddQuad(
                new Vector3(-s, 0, -s),
                new Vector3(s, 0, -s),
                new Vector3(-s, 0, s),
                new Vector3(s, 0, s),
                0
            );
        }

        if (settings.createWalls)
        {
            // North
            if (!IndexInBounds(image, x, y + 1) || !IndexFilled(image, x, y + 1))
            {
                meshBuilder.AddQuad(
                    new Vector3(s, 0, s),
                    new Vector3(s, h, s),
                    new Vector3(-s, 0, s),
                    new Vector3(-s, h, s),
                    1
                );
            }

            // East
            if (!IndexInBounds(image, x + 1, y) || !IndexFilled(image, x + 1, y))
            {
                meshBuilder.AddQuad(
                    new Vector3(s, 0, -s),
                    new Vector3(s, h, -s),
                    new Vector3(s, 0, s),
                    new Vector3(s, h, s),
                    1
                );
            }

            // South
            if (!IndexInBounds(image, x, y - 1) || !IndexFilled(image, x, y - 1))
            {
                meshBuilder.AddQuad(
                    new Vector3(s, h, -s),
                    new Vector3(s, 0, -s),
                    new Vector3(-s, h, -s),
                    new Vector3(-s, 0, -s),
                    1
                );
            }

            // West
            if (!IndexInBounds(image, x - 1, y) || !IndexFilled(image, x - 1, y))
            {
                meshBuilder.AddQuad(
                    new Vector3(-s, h, -s),
                    new Vector3(-s, 0, -s),
                    new Vector3(-s, h, s),
                    new Vector3(-s, 0, s),
                    1
                );
            }
        }

        if (settings.createCeilings)
        {
            meshBuilder.AddQuad(
                new Vector3(s, h, -s),
                new Vector3(-s, h, -s),
                new Vector3(s, h, s),
                new Vector3(-s, h, s),
                2
            );
        }

        ProBuilderMesh mesh = ProBuilderMesh.Create(meshBuilder.positions, meshBuilder.faces);

        foreach (MeshBuilder.MaterialGroup materialGroup in meshBuilder.materialGroups)
        {
            mesh.SetMaterial(materialGroup.faces, materialGroup.material);
        }

        foreach (Face face in mesh.faces)
        {
            AutoUnwrapSettings uv = face.uv;
            uv.scale = settings.uvScale;
            face.uv = uv;
        }

        mesh.RefreshUV(mesh.faces);

        mesh.Refresh();
        mesh.ToMesh();

        return mesh;
    }

    bool IndexFilled(Texture2D image, int x, int y)
    {
        return image.GetPixel(x, y) == Color.black;
    }

    bool IndexInBounds(Texture2D image, int x, int y)
    {
        if (x < 0 || x >= image.width)
        {
            return false;
        }

        if (y < 0 || y >= image.height)
        {
            return false;
        }

        return true;
    }

    public void DestroyChildren()
    {
        GameObject[] destroyObjects = new GameObject[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            destroyObjects[i] = transform.GetChild(i).gameObject;
        }

        foreach (GameObject destroyObject in destroyObjects)
        {
            DestroyImmediate(destroyObject);
        }
    }
}
