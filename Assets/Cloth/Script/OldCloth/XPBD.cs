
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro.EditorUtilities;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.UIElements;






//可以认为是每个顶点的质点 （基本每个顶点都有 剩下的都是加）
public struct Particle
{
    public float Mass { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }

    public float lambda_prev;
    public Particle(Vector3 position,float mass)
    {
        Position = position;
        Mass = mass;
        Velocity = new Vector3(0, 0, 0);
        lambda_prev = 0;
    }
}





/// <summary>
/// 一种约束 每种约束 需要走完所有的顶点 相当于有n个顶点 这一种约束需要进行 Dispatch 至少n个线程去处理顶点。还要算梯度上面的 约束的条件写在ComputeShader里 这个类是用来执行每种约束(每种里面还有至少n个约束)的
/// </summary>
public class Constraint
{

    protected static string m_NowPosBufferName = "_NowPoint";
    protected static string m_PrePosBufferName = "_PrePoint";
    protected static string m_PostPosBufferName = "_PostPoint";
    protected static string m_NeighborDateBufferName = "V2VDataBuffer";
    protected static string m_NeighborIndexBufferName = "V2VIndexBuffer";

    protected int VertexNum = 0;
    public string ConstraintKernelName = "Constraint";
    public int ConstraintKernelIndex = 0;
    public CommandBuffer ConstraintCmd { get; set; }

    public ComputeShader  ConstraintCompute { get; set; }

    //基础3Buffer 
    public ComputeBuffer m__PrePointBuffer;
    public ComputeBuffer m__PostPointBuffer;
    public ComputeBuffer m__PredictPointBuffer;

    public ComputeBuffer m_NeighborDataBuffer;
    public ComputeBuffer m_NeighborIndexBuffer;

    public Constraint()
    {
     
    }
    /// <summary>
    /// 初始化Buffer的 传入3Buffer就行了？有的是不同的
    /// </summary>
    public virtual void InitialBuffer(ComputeBuffer nowPosBuffer,ComputeBuffer prePosBuffer,ComputeBuffer postPosBuffer,ComputeBuffer nDataBuffer,ComputeBuffer nIndexBuffer)
    {
        m__PrePointBuffer = prePosBuffer;
        m__PredictPointBuffer = nowPosBuffer;
        m__PostPointBuffer = postPosBuffer;

        m_NeighborDataBuffer = nDataBuffer;
        m_NeighborIndexBuffer = nIndexBuffer;   
    }
    /// <summary>
    /// 约束的执行，具体实现写在update里了. 不同的约束不同执行方式吧，这个是最基本的
    /// </summary>
    /// <param name="dt"></param>
    public virtual void Execute(int DispatchNumX, int DispatchNumY, int DispatchNumZ, float dt = 0.02f , float Stiffness = 0.5f, float Gamma =  0,int StepNums = 1)
    {
        //示例
        /*
        ConstraintCmd.DispatchCompute(ConstraintCompute, ConstraintKernelIndex, DispatchNumX, DispatchNumY, DispatchNumZ);
        */
    }
     public void InitialVertexCount(int Count)
    {
        VertexNum = Count;
    }
}








/// <summary>
/// 距离约束的约束类
/// </summary>
public class Constraint_Distance : Constraint
{
    /// <summary>
    /// 每一个约束自己的粒子类
    /// </summary>
    public struct CustomParticle 
    {
        public float Mass { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }

        public float padding;
        public CustomParticle(Vector3 position, float mass)
        {
            Position = position;
            Mass = mass;
            Velocity = new Vector3(0, 0, 0);
            padding = 0;
        }

    }

    int testIndex;
    int testIndex_2;
    int testIndex_3;
    int testIndex_4;
    public Constraint_Distance(CommandBuffer cmd, string kernelName, ComputeShader computeShader)
    {
        ConstraintCompute = computeShader;
        ConstraintCmd = cmd;
        ConstraintKernelName = kernelName;
        ConstraintKernelIndex = ConstraintCompute.FindKernel(ConstraintKernelName);
        testIndex = ConstraintCompute.FindKernel("Constraint_Size");
        testIndex_2 = ConstraintCompute.FindKernel("Constraint_Fixed");
        testIndex_3 = ConstraintCompute.FindKernel("Constraint_Bend");
        testIndex_4 = ConstraintCompute.FindKernel("Constraint_Shear");
    }
    public override void Execute( int DispatchNumX, int DispatchNumY, int DispatchNumZ, float dt, float Stiffness, float Gamma, int StepNums)
    {
        ConstraintCompute.SetFloat("gamma", Gamma);
        ConstraintCompute.SetFloat("alpha",  1 / Stiffness);
        ConstraintCompute.SetFloat("deltaTime", dt / StepNums);
        ConstraintCompute.SetInt("simulationTimes", StepNums);
        ConstraintCompute.SetInt("meshVertexNums", VertexNum);
        ConstraintCompute.SetInt("_RawCount", (int)Mathf.Sqrt(VertexNum));

        //没找到哪里出问题了现在
        ConstraintCmd.BeginSample("Constraint");
   

        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, ConstraintKernelIndex, m_PrePosBufferName, m__PrePointBuffer);
        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, ConstraintKernelIndex, m_NowPosBufferName, m__PredictPointBuffer);
        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, ConstraintKernelIndex, m_PostPosBufferName, m__PostPointBuffer);
        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, ConstraintKernelIndex, m_NeighborDateBufferName, m_NeighborDataBuffer);
        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, ConstraintKernelIndex, m_NeighborIndexBufferName, m_NeighborIndexBuffer);
        ConstraintCmd.DispatchCompute(ConstraintCompute, ConstraintKernelIndex, DispatchNumX, DispatchNumY, DispatchNumZ);

        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, testIndex, m_PrePosBufferName, m__PrePointBuffer);
        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, testIndex, m_NowPosBufferName, m__PredictPointBuffer);
        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, testIndex, m_PostPosBufferName, m__PostPointBuffer);
        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, testIndex, m_NeighborDateBufferName, m_NeighborDataBuffer);
        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, testIndex, m_NeighborIndexBufferName, m_NeighborIndexBuffer);
        ConstraintCmd.DispatchCompute(ConstraintCompute, testIndex, DispatchNumX, DispatchNumY, DispatchNumZ);

        //感觉这个更像不可塑的约束 mesh不能压缩只能抖动
        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, testIndex_2, m_PrePosBufferName, m__PrePointBuffer);
        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, testIndex_2, m_NowPosBufferName, m__PredictPointBuffer);
        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, testIndex_2, m_PostPosBufferName, m__PostPointBuffer);
        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, testIndex_2, m_NeighborDateBufferName, m_NeighborDataBuffer);
        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, testIndex_2, m_NeighborIndexBufferName, m_NeighborIndexBuffer);
        ConstraintCmd.DispatchCompute(ConstraintCompute, testIndex_2, DispatchNumX, DispatchNumY, DispatchNumZ);

        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, testIndex_3, m_PrePosBufferName, m__PrePointBuffer);
        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, testIndex_3, m_NowPosBufferName, m__PredictPointBuffer);
        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, testIndex_3, m_PostPosBufferName, m__PostPointBuffer);
        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, testIndex_3, m_NeighborDateBufferName, m_NeighborDataBuffer);
        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, testIndex_3, m_NeighborIndexBufferName, m_NeighborIndexBuffer);
        ConstraintCmd.DispatchCompute(ConstraintCompute, testIndex_3, DispatchNumX, DispatchNumY, DispatchNumZ);

        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, testIndex_4, m_PrePosBufferName, m__PrePointBuffer);
        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, testIndex_4, m_NowPosBufferName, m__PredictPointBuffer);
        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, testIndex_4, m_PostPosBufferName, m__PostPointBuffer);
        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, testIndex_4, m_NeighborDateBufferName, m_NeighborDataBuffer);
        ConstraintCmd.SetComputeBufferParam(ConstraintCompute, testIndex_4, m_NeighborIndexBufferName, m_NeighborIndexBuffer);
        ConstraintCmd.DispatchCompute(ConstraintCompute, testIndex_4, DispatchNumX, DispatchNumY, DispatchNumZ);

  

      

        ConstraintCmd.EndSample("Constraint");
        //可能还需要设置一些？       
    }
}









/// <summary>
///  d_lambda = (-C - alpha*lambda) / (gradC * w_T * gradC_T + alpha)
///  x = x + deltaX where deltaX = gradC * w_T(i) * lambda
/// </summary>
public class XPBD : MonoBehaviour
{
    private static int MAXVERTEX = 65536;
    private string m_InteractionName = "Interaction";
    private string m_CalculateName = "Calculate";
    static string m_NowPosBufferName = "_NowPoint";
    static string m_PrePosBufferName = "_PrePoint";
    static string m_PostPosBufferName = "_PostPoint";

    [Range(0.0001f,20f)]
    [Header("柔度")]
    public float stiffness;
    [Range(0,1)]
    [Header("阻尼")]
    public float gamma;
    public Mesh testMesh;
    //距离的迭代约束项.
    public Constraint_Distance m_Constraint_Distance;
    public CommandBuffer cmd;
    public Material m_Material;
    public ComputeShader m_ComputeShader;
    //布料与外力交互的项(为速度什么赋值什么的，毕竟是XPBD)
    public ComputeShader m_InteractionCS;
    //布料最后的总结项(统计速度什么的，毕竟是XPBD)
    public ComputeShader m_CalculateCS;

    //基础的3Buffer (分别代表初始位置 上一次的位置)
    public ComputeBuffer m__PrePointBuffer;
    public ComputeBuffer m__PostPointBuffer;
    public ComputeBuffer m__PredictPointBuffer;

    public ComputeBuffer m_NeighborDataBuffer; //邻接表的CB
    public ComputeBuffer m_NeighborIndexBuffer;

    private List<Particle> m_PointList; //所有点的数据
    private NeighborSet<int, int> m_NeighborSet; //邻接表的数组 用于存放所有顶点
    private List<int2> m_EdgeList;
    Particle[] particles;


    int VertexNum;
    int DispatchNumX;
    int DispatchNumY;
    int DispatchNumZ;

    int num = 0;

    /// <summary>
    /// 初始化3Buffer
    /// </summary>
    private void Initialize3Buffer()
    {
        m__PredictPointBuffer = new ComputeBuffer(MAXVERTEX,Marshal.SizeOf<Particle>());
        m__PrePointBuffer = new ComputeBuffer(MAXVERTEX, Marshal.SizeOf<Particle>());
        m__PostPointBuffer = new ComputeBuffer(MAXVERTEX, Marshal.SizeOf<Particle>());

        m_NeighborDataBuffer = new ComputeBuffer(MAXVERTEX, Marshal.SizeOf<int>());
        m_NeighborIndexBuffer = new ComputeBuffer(MAXVERTEX, Marshal.SizeOf<int>());
    }


    private void Awake()
    {
        Initialize3Buffer();

        testMesh = GetComponent<MeshFilter>().mesh;
        cmd = new CommandBuffer();
        m_Constraint_Distance = new Constraint_Distance(cmd, "Constraint", m_ComputeShader);
        m_PointList = new List<Particle>();
        m_NeighborSet = new NeighborSet<int, int>();
        m_EdgeList = new List<int2>();
        cmd.name = "Constraint";


        {
            //需不需要转世界坐标？
            for (int i = 0; i < testMesh.vertices.Length; i++)
            {
                //
                if (i == 0 || i == (int)Mathf.Sqrt(testMesh.vertexCount) - 1)//|| i == testMesh.vertexCount - 1 || i == testMesh.vertexCount - (int)Mathf.Sqrt(testMesh.vertexCount))
                {
                    m_PointList.Add(new Particle(testMesh.vertices[i], 0f));
                }
                else
                    m_PointList.Add(new Particle(testMesh.vertices[i], 1f));
            }
        }

        particles = new Particle[testMesh.vertices.Length];
        
        
        //初始化DisPatch
        VertexNum = m_PointList.Count;
        DispatchNumX = Mathf.Min(VertexNum, 1024) / 64 + 1;
        DispatchNumY = VertexNum / 1024 + 1;
        DispatchNumZ = VertexNum / (1024 * 1024) + 1;

        //初始化ComputeBuffer
        m__PrePointBuffer.SetData(m_PointList.ToArray());
        m__PredictPointBuffer.SetData(m_PointList.ToArray());
        m__PostPointBuffer.SetData(m_PointList.ToArray());
     
        InitialInteraciton();
        InitialCalculate();
       
        SetPointFormTriangle(testMesh);

        m_NeighborSet.SplitSet(out List<int> valuesIndex, out List<int> values);

        m_NeighborDataBuffer.SetData(values.ToArray());
        m_NeighborIndexBuffer.SetData(valuesIndex.ToArray());

       

        m_Constraint_Distance.InitialVertexCount(testMesh.vertices.Length);
        //初始化初始位置.
        m_Constraint_Distance.InitialBuffer(m__PredictPointBuffer, m__PrePointBuffer, m__PostPointBuffer, m_NeighborDataBuffer, m_NeighborIndexBuffer);

        m_Material.SetBuffer("_NowPoint",m__PredictPointBuffer);
    }

    private void Update()
    {
        num++;
        {
            
            m__PredictPointBuffer.GetData(particles);


            Vector3[] temp = new Vector3[m_PointList.Count];

            for (int i = 0; i < testMesh.vertexCount; i++)
            {
                temp[i] = particles[i].Position;
            }
                testMesh.vertices = temp;
            testMesh.colors[5] = Color.white;

            testMesh.RecalculateBounds();

            testMesh.RecalculateNormals();


            cmd.SetBufferData(m__PredictPointBuffer, particles);
         
        }

        cmd.BeginSample("ClothCompute"); 
        {
            //交互项执行
                InteractionExecute();
            //这中间是约束项执行
            {
                m_Constraint_Distance.Execute(DispatchNumX, DispatchNumY, DispatchNumZ, Time.deltaTime, stiffness, gamma, 20);
            }
            //最后速度结算执行
            CalculateExecute();
        }
        
        cmd.EndSample("ClothCompute");
        Graphics.ExecuteCommandBuffer(cmd);
        cmd.Clear();

    }

    /// <summary>
    /// 初始化交互计算的CS
    /// </summary>
    private void InitialInteraciton()
    {
        m_InteractionCS.SetFloat("deltaTime", Time.deltaTime / 5f);
        m_InteractionCS.SetInt("meshVertexNums", VertexNum);
        m_InteractionCS.SetInt("_RawCount", (int)Mathf.Sqrt(VertexNum));
    }

    /// <summary>
    /// 执行交互计算的CS
    /// </summary>
    private void InteractionExecute()
    {
        Matrix4x4 temp = this.transform.localToWorldMatrix.inverse;
        m_InteractionCS.SetMatrix("WorldToLocal", temp);

        int InteractionKernelIndex = m_InteractionCS.FindKernel(m_InteractionName);
        cmd.SetComputeBufferParam(m_InteractionCS, InteractionKernelIndex, m_PrePosBufferName, m__PrePointBuffer);
        cmd.SetComputeBufferParam(m_InteractionCS, InteractionKernelIndex, m_NowPosBufferName, m__PredictPointBuffer);
        cmd.SetComputeBufferParam(m_InteractionCS, InteractionKernelIndex, m_PostPosBufferName, m__PostPointBuffer);
        cmd.DispatchCompute(m_InteractionCS, InteractionKernelIndex, DispatchNumX, DispatchNumY, DispatchNumZ);
    }

    /// <summary>
    /// 执行结算项的初始化,
    /// </summary>
    private void InitialCalculate()
    {
        m_CalculateCS.SetFloat("deltaTime", Time.deltaTime / 5f);
        m_CalculateCS.SetInt("meshVertexNums", VertexNum);
        m_CalculateCS.SetInt("_RawCount", (int)Mathf.Sqrt(VertexNum));
    }

    /// <summary>
    /// 执行最后速度计算的CS
    /// </summary>
    private void CalculateExecute()
    {
        m_CalculateCS.SetFloat("gamma", gamma);
        int CalculateKernelIndex = m_CalculateCS.FindKernel(m_CalculateName);
        cmd.SetComputeBufferParam(m_CalculateCS, CalculateKernelIndex, m_PrePosBufferName, m__PrePointBuffer);
        cmd.SetComputeBufferParam(m_CalculateCS, CalculateKernelIndex, m_NowPosBufferName, m__PredictPointBuffer);
        cmd.SetComputeBufferParam(m_CalculateCS, CalculateKernelIndex, m_PostPosBufferName, m__PostPointBuffer);
        cmd.DispatchCompute(m_CalculateCS, CalculateKernelIndex, DispatchNumX, DispatchNumY, DispatchNumZ);
    }

    /// <summary>
    /// 优化方案可以参考使用VirtuaMesh来计算模拟 这样似乎会更加的？方便？ 具体就是以现在的mesh生成一个虚拟的Mesh吧
    /// </summary>
    /// <param name="mesh"></param>
    private void SetPointFormTriangle(Mesh mesh)
    {
        for(int i = 0; i < mesh.triangles.Length;i += 3)
        {
            //这里的索引方式有问题似乎
            int3 tri = new int3(mesh.triangles[i], mesh.triangles[i + 1], mesh.triangles[i + 2]);


            int x = (int)tri.x;
            int y = (int)tri.y;
            int z = (int)tri.z;

            m_NeighborSet.UniqueAdd(tri.x, y);
            m_NeighborSet.UniqueAdd(tri.x, z);

            m_NeighborSet.UniqueAdd(tri.y, x);
            m_NeighborSet.UniqueAdd(tri.y, z);

            m_NeighborSet.UniqueAdd(tri.z, x);
            m_NeighborSet.UniqueAdd(tri.z, y);

            m_EdgeList.Add(DataHub.PackInt2(tri.xy));
            m_EdgeList.Add(DataHub.PackInt2(tri.yz));
            m_EdgeList.Add(DataHub.PackInt2(tri.zx));
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

            m_NeighborSet.UniqueAdd(tri.x, y);
            m_NeighborSet.UniqueAdd(tri.x, z);
            m_NeighborSet.UniqueAdd(tri.y, x);
            m_NeighborSet.UniqueAdd(tri.y, z);
            m_NeighborSet.UniqueAdd(tri.z, x);
            m_NeighborSet.UniqueAdd(tri.z, y);

            m_EdgeList.Add(DataHub.PackInt2(tri.xy));
            m_EdgeList.Add(DataHub.PackInt2(tri.yz));
            m_EdgeList.Add(DataHub.PackInt2(tri.zx));
        }
    }

}