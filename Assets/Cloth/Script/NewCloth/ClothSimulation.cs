using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Burst.Intrinsics;
using UnityEngine.UI;
using TMPro.EditorUtilities;

namespace ClothXPBD
{
    //这里我可以认为作为调用ComputeShaderr的总站 ，也是存放数据的总站，而之后应用的话就交给另一个Apply，作为一个单独的存档
    public class ClothSimulation
    {
        private static readonly float3 Gravity = new Vector3(0.0f, -9.8f, 0.0f);

        private static string
            _DistanceConstraintName = "Constraint_Distance",
            _BendConstraintName = "Constraint_Bend",
            _FixedConstraintName = "Constraint_Fixed",
            _ShearConstraintName = "Constraint_Shear",
            _SizeConstraintName = "Constraint_Size",
            _UpadateCallName = "UpdateVelocityAndPos";

        private static string
            _DistanceBufferName = "_DistanceBuffer",
            _BendBufferName = "_BendBuffer",
            _FixedBufferName = "_FixedBuffer",
            _ShearBufferName = "_ShearBuffer",
            _SizeBufferName = "_SizeBuffer",
            _NPBufferName = "_PredictPointBuffer",
            _PPBufferName = "_PostPointBuffer";

        /// <summary>
        /// CS
        /// </summary>
        public static ComputeShader _computeShader;

        /// <summary>
        /// 命令列表
        /// </summary>
        private CommandBuffer _commandBuffer;

        /// <summary>
        /// 每个顶点的质量数组
        /// </summary>
        private NativeList<float> _masses;

        /// <summary>
        /// 距离约束
        /// </summary>
        private ConstraintCall<DistanceConstraintInfo> _distanceConstraints;

        /// <summary>
        /// 弯曲约束
        /// </summary>
        private ConstraintCall<BendConstraintInfo> _bendConstraints;

        /// <summary>
        /// 剪切约束
        /// </summary>
        private ConstraintCall<ShearConstraintInfo> _shearConstraints;

        /// <summary>
        /// 面积约束
        /// </summary>
        private ConstraintCall<SizeConstraintInfo> _sizeConstraints;

        /// <summary>
        /// 固定点
        /// </summary>
        private ConstraintCall<FixedConstraintInfo> _fixedConstraints;

        /// <summary>
        /// 计算更新总点的速度和位置。
        /// </summary>
        private UpdateVelocityAndPositionCall _updateCall;

        private ClothMesh _mesh;
        public int vertexCount
        {
            get
            {
                return _mesh.vertexCount;
            }
        }

        /// <summary>
        /// 每个顶点的位置，这个需要用于更新 当然，在初始化时也是作为初始点的记录。
        /// </summary>
        public NativeArray<float3> positions
        {
            get
            {
                return _mesh.vertices;
            }
        }

        /// <summary>
        /// 每个顶点法线
        /// </summary>
        public NativeArray<float3> normals
        {
            get
            {
                return _mesh.normals;
            }
        }

        public ClothSimulation(ClothMesh ClothMesh,CommandBuffer CommandBuffer,ComputeShader ComputeShader)
        {
            _mesh = ClothMesh; 
            _commandBuffer = CommandBuffer;
            _computeShader = ComputeShader;


            ConstraintCall<DistanceConstraintInfo>.computeShader = _computeShader;
            _distanceConstraints = new ConstraintCall<DistanceConstraintInfo>();
            _distanceConstraints.baseKernelID = _computeShader.FindKernel(_DistanceConstraintName);
            _distanceConstraints.baseBufferName = _DistanceBufferName;


            ConstraintCall<BendConstraintInfo>.computeShader = _computeShader;
            _bendConstraints = new ConstraintCall<BendConstraintInfo>();
            _bendConstraints.baseKernelID = _computeShader.FindKernel(_BendConstraintName);
            _bendConstraints.baseBufferName = _BendBufferName;


            ConstraintCall<ShearConstraintInfo>.computeShader = _computeShader;
            _shearConstraints = new ConstraintCall<ShearConstraintInfo>();
            _shearConstraints.baseKernelID = _computeShader.FindKernel(_ShearConstraintName);
            _shearConstraints.baseBufferName = _ShearBufferName;


            ConstraintCall<FixedConstraintInfo>.computeShader = _computeShader;
            _fixedConstraints = new ConstraintCall<FixedConstraintInfo>();
            _fixedConstraints.baseKernelID = _computeShader.FindKernel(_FixedConstraintName);
            _fixedConstraints.baseBufferName = _FixedBufferName;


            ConstraintCall<SizeConstraintInfo>.computeShader = _computeShader;
            _sizeConstraints = new ConstraintCall<SizeConstraintInfo>();
            _sizeConstraints.baseKernelID = _computeShader.FindKernel(_SizeConstraintName);
            _sizeConstraints.baseBufferName = _SizeBufferName;


            UpdateVelocityAndPositionCall.computeShader = _computeShader;
            _updateCall = new UpdateVelocityAndPositionCall();
            _updateCall.baseKernelID = _computeShader.FindKernel(_UpadateCallName);
            _updateCall.bufferName_Now = _NPBufferName;
            _updateCall.bufferName_Post = _PPBufferName;


            this.BuildMasses();
            this.BuildDistanceConstraint();
            //this.BuildBendConstraint();
            //this.BuildShearConstraint();
            //this.BuildSizeConstraint();

            ///统计所有顶点
            this.BuildPointStruct();

            _distanceConstraints.InitialBuffer();
            _updateCall.InitialBuffer();
            //_sizeConstraints.InitialBuffer();
            //_bendConstraints.InitialBuffer();
            //_shearConstraints.InitialBuffer();
            //_fixedConstraints.InitialBuffer();
        }
        /// <summary>
        /// 内存释放
        /// </summary>
        public void Dispose()
        {
            positions.Dispose();
            _masses.Dispose();
            _bendConstraints.Dispose();
            _shearConstraints.Dispose();
            _fixedConstraints.Dispose();
            _shearConstraints.Dispose();
            _distanceConstraints.Dispose();
        }

        public void Step(CommandBuffer commandBuffer,GraphicsFence fence)
        {
            _commandBuffer.WaitOnAsyncGraphicsFence(fence);
            _updateCall.InitialBuffer();
            _distanceConstraints.InitialBuffer();
            for(int i = 0; i < 5; i++) 
            {
                _computeShader.SetFloat("deltaTime", Time.deltaTime);
                _updateCall.ComputeCall(commandBuffer);
                _commandBuffer.WaitOnAsyncGraphicsFence(fence);
                _updateCall.SetBuffer(commandBuffer, _distanceConstraints);
                _distanceConstraints.ComputeCall(commandBuffer);
                _commandBuffer.WaitOnAsyncGraphicsFence(fence);
            }
        }

        /// <summary>
        /// 为每个顶点的质量进行赋值
        /// </summary>
        public void BuildMasses()
        {
            var indices = _mesh.indices;
            var vertices = _mesh.vertices;
            _masses = new NativeList<float>(this.vertexCount, Allocator.Persistent);
            _masses.Resize(this.vertexCount, NativeArrayOptions.ClearMemory);
            for (var i = 0; i < indices.Length / 3; i++)
            {
                var offset = i * 3;
                var i0 = indices[offset];
                var i1 = indices[offset + 1];
                var i2 = indices[offset + 2];
                var v0 = vertices[i0];
                var v1 = vertices[i1];
                var v2 = vertices[i2];
                var area = CustomMath.GetArea(v0, v1, v2);
                var m = area;
                var m3 = m / 3;
                _masses[i0] = 0.1f;
                _masses[i1] = 0.1f;
                _masses[i2] = 0.1f;
              
            }
            _masses[0] = 0;
        }

        public void BuildDistanceConstraint()
        {
            var edges = _mesh.GetEdgeList();
            _distanceConstraints.constraints = new NativeList<DistanceConstraintInfo>(edges.Count, Allocator.Persistent);
            int i = 0;
            foreach (var e in edges)
            {
                i++;
                this.AddDistanceConstraint(e.vIndex0, e.vIndex1);

                if(e.vIndex0 == 1 || e.vIndex1 == 1)
                {
                    Debug.Log(i - 1);
                }
            }
        }


        public void AddDistanceConstraint(int index0,int index1)
        {
            var restDistance = math.distance(positions[index0], positions[index1]);
            
            _distanceConstraints.AddConstraint(new DistanceConstraintInfo()
            {
                vIndex0 = index0,
                vIndex1 = index1,
                restDistance = restDistance,
                lambda = 0.0f,
            });

        }
        public void BuildBendConstraint()
        {
            var edges = _mesh.GetEdgeList();
            _bendConstraints.constraints = new NativeList<BendConstraintInfo>(edges.Count, Allocator.Persistent);
            foreach (var edge in edges)
            {
                if (edge.triangleIndexes.Count == 2)
                {
                    var v2 = edge.triangleIndexes[0]; //当前边构成三角形的另一个点。
                    var v3 = edge.triangleIndexes[1];

                    var bendConstraint = new BendConstraintInfo();
                    bendConstraint.vIndex0 = edge.vIndex0;
                    bendConstraint.vIndex1 = edge.vIndex1;
                    bendConstraint.vIndex2 = v2;
                    bendConstraint.vIndex3 = v3;
                    bendConstraint.lambda = 0.0f;

                    var p0 = this.positions[bendConstraint.vIndex0];
                    var p1 = this.positions[bendConstraint.vIndex1] - p0;
                    var p2 = this.positions[bendConstraint.vIndex2] - p0;
                    var p3 = this.positions[bendConstraint.vIndex3] - p0;

                    var n1 = math.normalize(math.cross(p1, p2));
                    var n2 = math.normalize(math.cross(p1, p3));

                    bendConstraint.rest = math.acos(math.dot(n1, n2));
                    _bendConstraints.AddConstraint(bendConstraint);
                }
            }
        }


        public void BuildSizeConstraint()
        {
            var edges = _mesh.GetEdgeList();
            _sizeConstraints.constraints = new NativeList<SizeConstraintInfo>(edges.Count, Allocator.Persistent);
            foreach (var edge in edges)
            {
                var v0 = edge.vIndex0;
                var v1 = edge.vIndex1;

                var p0 = positions[v0];
                var p1 = positions[v1] - p0;
                foreach (var v2 in edge.triangleIndexes)
                {
                    var sizeConstraint = new SizeConstraintInfo();
                    var p2 = positions[v2] - p0;

                    sizeConstraint.rest = 0.5f * math.length(math.cross(-p1, -p2));
                    sizeConstraint.vIndex0 = v0;
                    sizeConstraint.vIndex1 = v1;
                    sizeConstraint.vIndex2 = v2;
                    sizeConstraint.lambda = 0.0f;

                    _sizeConstraints.AddConstraint(sizeConstraint);
                }
            }
        }


        public void BuildShearConstraint()
        {
            var edges = _mesh.GetEdgeList();


        }


        public void BuildPointStruct()
        {
            var indices = _mesh.indices;
            var vertex = _mesh.vertices;
            var masses = _masses;

            _updateCall.points = new NativeList<PointInfo>(vertexCount, Allocator.Persistent);
            for (int i = 0; i < vertexCount; i++) 
            {
                var point = new PointInfo();
                point.mass = masses[i];
                point.position = vertex[i];
                point.velocity = 0.0f;
                _updateCall.AddPoint(point);
            }
        }

        public ComputeBuffer GetSimulationResult()
        {
            return _updateCall.pointBuffer_Post;
        }
    }
}
