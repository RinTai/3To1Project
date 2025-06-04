using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using Unity.VisualScripting;
using System.Linq;


/// <summary>
/// 一条边，连接两个顶点，两侧各有一个三角面
/// </summary>
public class Edge
{
    public int vIndex0;
    public int vIndex1;
    public List<int> triangleIndexes = new List<int>(2);

    public Edge(int vIndex0, int vIndex1)
    {
        this.vIndex0 = math.min(vIndex0, vIndex1);
        this.vIndex1 = math.max(vIndex0, vIndex1);
    }

    public override int GetHashCode()
    {
        return (vIndex0 << 16 | vIndex1);
    }

    public override bool Equals(object obj)
    {
        if (!(obj is Edge))
        {
            return false;
        }
        var edge2 = (Edge)obj;

        return vIndex0 == edge2.vIndex0 && vIndex1 == edge2.vIndex1;
    }

    public static bool operator ==(Edge e1, Edge e2)
    {
        var n1 = object.ReferenceEquals(e1, null);
        var n2 = object.ReferenceEquals(e2, null);
        if (n1 && n2)
        {
            return true;
        }
        if (n1 != n2)
        {
            return false;
        }
        return e1.vIndex0 == e2.vIndex0 && e1.vIndex1 == e2.vIndex1;
    }

    public static bool operator !=(Edge e1, Edge e2)
    {
        var n1 = object.ReferenceEquals(e1, null);
        var n2 = object.ReferenceEquals(e2, null);
        if (n1 && n2)
        {
            return false;
        }
        if (n1 != n2)
        {
            return true;
        }
        return e1.vIndex0 != e2.vIndex0 || e1.vIndex1 != e2.vIndex1;
    }

    public static int GetEdgeHash(int vIndex0, int vIndex1)
    {
        if (vIndex0 < vIndex1)
        {
            return (vIndex0 << 16 | vIndex1);
        }
        else
        {
            return (vIndex1 << 16 | vIndex0);
        }
    }

}

public class ClothMesh
{
    public Mesh _Testmesh;
    /// <summary>
    /// 从上个XPBD中搬过来的邻接表
    /// </summary>
    private NeighborSet<int, int> _neighborSet;

    /// <summary>
    /// 边缘的表(这里还需要存一个这条边对应的三角形。怎么存放呢：换成List存放了)
    /// </summary>
    private Dictionary<int,Edge> _edgeDict;

    public int _vertexCount;
    private NativeList<float3> _vertices;
    private NativeList<float3> _normals;
    private NativeList<float2> _uv;
    private NativeList<int> _indices;

    /// <summary>
    /// 三角形索引
    /// </summary>
    public NativeArray<int> indices
    {
        get
        {
            return _indices;
        }
    }

    /// <summary>
    /// uv存放
    /// </summary>
    public NativeArray<float2> uvs
    {
        get
        {
            return _uv;
        }
    }

    /// <summary>
    /// 顶点的位置
    /// </summary>
    public NativeArray<float3> vertices
    {
        get
        {
            return _vertices;
        }
    }

    /// <summary>
    /// 顶点的法线
    /// </summary>
    public NativeArray<float3> normals
    {
        get
        {
            return _normals;
        }
    }

    public int vertexCount
    {
        get
        {
            return _vertexCount;
        }
    }

    private ClothMesh(NativeList<float3> vertices, NativeList<float2> uvs, NativeList<int> indices, NativeList<float3> normals,MeshFilter meshFilter)
    {
        this._vertices = vertices;
        this._uv = uvs;
        this._indices = indices;
        this._normals = normals;
        this._Testmesh = meshFilter.mesh;

        _neighborSet = new NeighborSet<int, int>();
        _edgeDict = new Dictionary<int, Edge>();
        _vertexCount = vertices.Length;

        SetPointFormTriangle(meshFilter.mesh);
    }

    /// <summary>
    /// 从三角形状中获得邻接点和边
    /// </summary>
    /// <param name="mesh"></param>
    private void SetPointFormTriangle(Mesh mesh)
    {
        for (int i = 0; i < mesh.triangles.Length; i += 3)
        {
            int3 tri = new int3(mesh.triangles[i], mesh.triangles[i + 1], mesh.triangles[i + 2]);

            int x = (int)tri.x;
            int y = (int)tri.y;
            int z = (int)tri.z;

            _neighborSet.UniqueAdd(tri.x, y);
            _neighborSet.UniqueAdd(tri.x, z);

            _neighborSet.UniqueAdd(tri.y, x);
            _neighborSet.UniqueAdd(tri.y, z);

            _neighborSet.UniqueAdd(tri.z, x);
            _neighborSet.UniqueAdd(tri.z, y);


            int xyHash = Edge.GetEdgeHash(x, y);
            if (!_edgeDict.TryGetValue(xyHash,out Edge xyEdge))
            {
                xyEdge = new Edge(x, y);
                _edgeDict.Add(xyHash, xyEdge); // 新边才添加到全局列表
            }
            xyEdge.triangleIndexes.Add(z); // 无论新旧边，都添加当前三角形的第三个顶点

            // 处理边 yz
            int yzHash = Edge.GetEdgeHash(y, z);
            if (!_edgeDict.TryGetValue(yzHash, out Edge yzEdge))
            {
                yzEdge = new Edge(y, z);
                _edgeDict.Add(yzHash,yzEdge);
            }
            yzEdge.triangleIndexes.Add(x);

            // 处理边 zx
            int zxHash = Edge.GetEdgeHash(z, x);
            if (!_edgeDict.TryGetValue(zxHash, out Edge zxEdge))
            {
                zxEdge = new Edge(z, x);
                _edgeDict.Add(zxHash, zxEdge);   
            }
            zxEdge.triangleIndexes.Add(y);
        }
    }

    /// <summary>
    /// 从边列表上得到邻接表,由于得到本身的边列表就需要遍历一遍所有三角形和顶点 ，所以需要写一个大型的初始化才行，这个我感觉以后再说吧，可能需要重新调整架构
    /// </summary>
    /// <param name="mesh"></param>
    private void SetPointFormEdge(Mesh mesh)
    {
        for (int i = 0; i < mesh.triangles.Length; i++)
        {
            //
            int3 tri = mesh.triangles[i];

            ushort x = (ushort)tri.x;
            ushort y = (ushort)tri.y;
            ushort z = (ushort)tri.z;

            _neighborSet.UniqueAdd(tri.x, y);
            _neighborSet.UniqueAdd(tri.x, z);
            _neighborSet.UniqueAdd(tri.y, x);
            _neighborSet.UniqueAdd(tri.y, z);
            _neighborSet.UniqueAdd(tri.z, x);
            _neighborSet.UniqueAdd(tri.z, y);


            int xyHash = Edge.GetEdgeHash(x, y);
            if (!_edgeDict.TryGetValue(xyHash, out Edge xyEdge))
            {
                xyEdge = new Edge(x, y);
                _edgeDict.Add(xyHash, xyEdge); // 新边才添加到全局列表
            }
            xyEdge.triangleIndexes.Add(z); // 无论新旧边，都添加当前三角形的第三个顶点

            // 处理边 yz
            int yzHash = Edge.GetEdgeHash(y, z);
            if (!_edgeDict.TryGetValue(yzHash, out Edge yzEdge))
            {
                yzEdge = new Edge(y, z);
                _edgeDict.Add(yzHash, yzEdge);
            }
            yzEdge.triangleIndexes.Add(x);

            // 处理边 zx
            int zxHash = Edge.GetEdgeHash(z, x);
            if (!_edgeDict.TryGetValue(zxHash, out Edge zxEdge))
            {
                zxEdge = new Edge(z, x);
                _edgeDict.Add(zxHash, zxEdge);
            }
            zxEdge.triangleIndexes.Add(y);
        }
    }

    /// <summary>
    /// 从MeshFilter中创建类
    /// </summary>
    /// <param name="meshFilter"></param>
    /// <returns></returns>
    public static ClothMesh CreateFromMeshFilter(MeshFilter meshFilter)
    {
        var mesh = meshFilter.sharedMesh;
        var vertices = mesh.vertices;
        var uvs = mesh.uv;
        var indices = mesh.triangles;

        var verticesList = new NativeList<float3>(vertices.Length, Allocator.Persistent);
        verticesList.Resize(vertices.Length, NativeArrayOptions.UninitializedMemory);
        var localToWorld = meshFilter.transform.localToWorldMatrix;
        for (var i = 0; i < vertices.Length; i++)
        {
            verticesList[i] = localToWorld.MultiplyPoint3x4(vertices[i]);
        }
        var normals = new NativeList<float3>(vertices.Length, Allocator.Persistent);
        normals.Resize(vertices.Length, NativeArrayOptions.UninitializedMemory);


        var uvList = new NativeList<float2>(uvs.Length, Allocator.Persistent);
        uvList.Resize(uvs.Length, NativeArrayOptions.UninitializedMemory);
        uvList.AsArray().Reinterpret<Vector2>().CopyFrom(uvs);

        var indicesList = new NativeList<int>(indices.Length, Allocator.Persistent);
        indicesList.Resize(indices.Length, NativeArrayOptions.UninitializedMemory);
        indicesList.AsArray().Reinterpret<int>().CopyFrom(indices);

        return new ClothMesh(verticesList, uvList, indicesList, normals, meshFilter);
    }

    public Dictionary<int, Edge> GetEdgeList()
    {
        return _edgeDict;
    }

    public NeighborSet<int, int> GetNeighborSet()
    { 
        return _neighborSet;
    }
}
