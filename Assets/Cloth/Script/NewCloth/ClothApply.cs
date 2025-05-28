using ClothXPBD;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class ClothApply : MonoBehaviour
{
    CommandBuffer _commandBuffer;

    Mesh _mesh;
    MeshFilter _meshFilter;

    ClothSimulation _clothSimulation;
    ClothMesh _clothMesh;
    public void Start()
    {
        _commandBuffer = new CommandBuffer();
        _meshFilter = GetComponent<MeshFilter>();
        _mesh = _meshFilter.sharedMesh;

        _clothMesh = ClothMesh.CreateFromMeshFilter(_meshFilter);
        _clothSimulation  = new ClothSimulation(_clothMesh, _commandBuffer);
    }

    public void Update()
    {
        
    }
}
