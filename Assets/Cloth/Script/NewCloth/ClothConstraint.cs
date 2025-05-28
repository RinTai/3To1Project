using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using System;

namespace ClothXPBD
{

    /// <summary>
    /// 需要调用ComputeShader的接口
    /// </summary>
    public interface IComputeShaderCall
    {
        public void ComputeCall(CommandBuffer commandBuffer);
    }

    /// <summary>
    /// 还是存为点吧。
    /// </summary>
    public struct PointInfo
    {
        public float mass;
        public float3 position;
        public float3 velocity;
    }

    /// <summary>
    /// 距离约束的信息
    /// </summary>
    public struct DistanceConstraintInfo
    {
        public int vIndex0;
        public int vIndex1;
        public float restDistance;
    }

    public struct BendConstraintInfo
    {
        public int vIndex0;
        public int vIndex1;
        public int vIndex2;
        public int vIndex3;
        public float rest; // 两个面的angle
    }

    public struct SizeConstraintInfo
    {
        public int vIndex0;
        public int vIndex1;
        public int vIndex2;
        public float rest;//初始面积
    }

    public struct FixedConstraintInfo
    {
        public float3 fixedPosition;
    }

    public struct ShearConstraintInfo
    {
        public int vIndex0;
        public int vIndex1;
        public int vIndex2;
        public float rest;//初始角度
    }

    public enum ConstraintType
    {
        Distance,
        Bend,
        Shear,
        Fiexd,
        Size,
    }
    /// <summary>
    /// 作为一个对ComputeShader的Call和数据存放处
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct ConstraintCall<T> : IComputeShaderCall,IDisposable  where T : unmanaged
    {
        /// <summary>
        /// 计算用CS
        /// </summary>
        public static ComputeShader computeShader;

        /// <summary>
        /// 计算用的CS中的Kernel名称
        /// </summary>
        public int kernelID;

        /// <summary>
        /// 存储所有距离约束数据的BufferName
        /// </summary>
        public string bufferName;

        /// <summary>
        /// 存储用的Buffer
        /// </summary>
        public ComputeBuffer constraintBuffer;

        /// <summary>
        /// 存储了所有的DistanceConstraint
        /// </summary>
        [ReadOnly]
        public NativeList<T> constraints;

        /*
         * 必须做初始化这一步。
        public ConstraintCall(ComputeShader ComputeShader,string Kernel_Name,string Buffer_Name,in List<DistanceConstraintInfo> DistanceConstraints)
        {
            computeShader = ComputeShader;
            kernelID = ComputeShader.FindKernel(Kernel_Name);
            bufferName = Buffer_Name;
            Constraints = Constraints;
        }
        */

        /// <summary>
        /// 初始化Buffer 至少要在Call之前调用
        /// </summary>
        public void InitialBuffer()
        {
            constraintBuffer = new ComputeBuffer(constraints.Length,UnsafeUtility.SizeOf<T>());
            constraintBuffer.SetData(constraints.ToArray(Allocator.Temp));
        }

        /// <summary>
        /// 把调用指令加入Cmd中
        /// </summary>
        /// <param name="commandBuffer"></param>
        public void ComputeCall(CommandBuffer commandBuffer)
        {
            int3 DispatchArgs = new int3(constraints.Length, 1, 1);
            commandBuffer.SetComputeBufferParam(computeShader, kernelID, bufferName, constraintBuffer);
            commandBuffer.DispatchCompute(computeShader, kernelID, DispatchArgs.x, DispatchArgs.y, DispatchArgs.z);
        }

        /// <summary>
        /// 释放
        /// </summary>
        public void Dispose()
        {
            constraints.Dispose();
        }

        /// <summary>
        /// 为约束数组添加Constraint
        /// </summary>
        /// <param name="constraintStruct"></param>
        public void AddConstraint(T constraintStruct)
        {
            constraints.Add(constraintStruct);
        }
    }

    /// <summary>
    /// 相当于在旧XPBD中的总结计算位移和速度的部分
    /// </summary>
    public struct UpdateVelocityAndPositionCall : IComputeShaderCall, IDisposable 
    {
        public ComputeShader computeShader;

        public int kernelID;

        public string bufferName;

        public ComputeBuffer pointBuffer;
        /// <summary>
        /// 存储所有点
        /// </summary>
        public NativeList<PointInfo> points;

        public void InitialBuffer()
        {
            pointBuffer = new ComputeBuffer(points.Length,UnsafeUtility.SizeOf<PointInfo>());
            pointBuffer.SetData(points.ToArray(Allocator.Temp));
        }

        public void ComputeCall(CommandBuffer commandBuffer)
        {
            int3 DispatchArgs = new int3(points.Length, 1, 1);
            commandBuffer.SetComputeBufferParam(computeShader, kernelID, bufferName, pointBuffer);
            commandBuffer.DispatchCompute(computeShader, kernelID, DispatchArgs.x, DispatchArgs.y, DispatchArgs.z);
        }

        public void AddPoint(PointInfo point)
        {
            points.Add(point);
        }

        public void Dispose()
        {
            points.Dispose();
        }
    }
}
