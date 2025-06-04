using ClothXPBD;
using Unity.Collections;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class ClothApply : MonoBehaviour
{
    CommandBuffer _commandBuffer;
    public ComputeShader _shader;

    Mesh _mesh;
    MeshFilter _meshFilter;

    ClothSimulation _clothSimulation;
    ClothMesh _clothMesh;
    public void Start()
    {
        _commandBuffer = new CommandBuffer();
        _commandBuffer.name = "NewCloth";
        _meshFilter = GetComponent<MeshFilter>();
        _mesh = _meshFilter.mesh;

        _clothMesh = ClothMesh.CreateFromMeshFilter(_meshFilter);
        _clothSimulation = new ClothSimulation(_clothMesh, _commandBuffer, _shader);
    }

    public void Update()
    {

        GraphicsFence fence = _commandBuffer.CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation, SynchronisationStageFlags.ComputeProcessing);

        _clothSimulation.Step(_commandBuffer, fence);

        Graphics.ExecuteCommandBuffer(_commandBuffer);
        _commandBuffer.Clear();

        AsyncGPUReadback.Request(_clothSimulation.GetSimulationResult(), request => {
            if (request.hasError) return;
            NativeArray<PointInfo> particles = request.GetData<PointInfo>();
            // 处理数据...
            Vector3[] temp = new Vector3[_clothSimulation.vertexCount];

            for (int i = 0; i < _clothSimulation.vertexCount; i++)
            {
                temp[i] = particles[i].position;
            }
            _mesh.vertices = temp;
        });

        /*
        PointInfo[] particles = new PointInfo[_clothSimulation.vertexCount];
        _clothSimulation.GetSimulationResult().GetData(particles);
        
        Vector3[] temp = new Vector3[_clothSimulation.vertexCount];

        for (int i = 0; i < _clothSimulation.vertexCount; i++)
        {
            temp[i] = particles[i].position;
        }
        _mesh.vertices = temp;
        */
    }
}
