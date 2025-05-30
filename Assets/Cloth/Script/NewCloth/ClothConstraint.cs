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
        public abstract void ComputeCall(CommandBuffer commandBuffer);
    }

    /// <summary>
    /// 如果说存的Buffer数据是需要公用的，那么需要实现这个接口
    /// </summary>
    public interface IComputeBufferStore
    {
        public abstract void SetBuffer(CommandBuffer commandBuffer, ComputeShader computeShader, string kernelName);
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
        public float lambda;
    }

    public struct BendConstraintInfo
    {
        public int vIndex0;
        public int vIndex1;
        public int vIndex2;
        public int vIndex3;
        public float rest; // 两个面的angle
        public float lambda;
    }

    public struct SizeConstraintInfo
    {
        public int vIndex0;
        public int vIndex1;
        public int vIndex2;
        public float rest;//初始面积
        public float lambda;
    }

    public struct FixedConstraintInfo
    {
        public float3 fixedPosition;
        public float lambda;
    }

    public struct ShearConstraintInfo
    {
        public int vIndex0;
        public int vIndex1;
        public int vIndex2;
        public float rest;//初始角度
        public float lambda;
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
        public int baseKernelID;

        /// <summary>
        /// 存储所有距离约束数据的BufferName
        /// </summary>
        public string baseBufferName;

        /// <summary>
        /// 存储用的Buffer
        /// </summary>
        public ComputeBuffer baseConstraintBuffer;

        /// <summary>
        /// 存储了所有的DistanceConstraint
        /// </summary>
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
            baseConstraintBuffer = new ComputeBuffer(constraints.Length,UnsafeUtility.SizeOf<T>());
            baseConstraintBuffer.SetData(constraints.ToArray(Allocator.Temp));
        }

        /// <summary>
        /// 把调用指令加入Cmd中
        /// </summary>
        /// <param name="commandBuffer"></param>
        public void ComputeCall(CommandBuffer commandBuffer)
        {
            int3 DispatchArgs = new int3(constraints.Length , 1, 1);
            commandBuffer.SetComputeBufferParam(computeShader, baseKernelID, baseBufferName, baseConstraintBuffer);
            commandBuffer.DispatchCompute(computeShader, baseKernelID, DispatchArgs.x, DispatchArgs.y, DispatchArgs.z);
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
    public struct UpdateVelocityAndPositionCall : IComputeShaderCall, IDisposable, IComputeBufferStore
    {
        public  static ComputeShader computeShader;

        public int baseKernelID;

        public string bufferName_Now; //当前顶点位置对应的名字
        public string bufferName_Post;//上一布顶点位置对应的名字

        public ComputeBuffer pointBuffer_Now;
        public ComputeBuffer pointBuffer_Post;

        /// <summary>
        /// 存储所有点
        /// </summary>
        public NativeList<PointInfo> points;

        public void InitialBuffer()
        {
            pointBuffer_Now = new ComputeBuffer(points.Length,UnsafeUtility.SizeOf<PointInfo>());
            pointBuffer_Now.SetData(points.ToArray(Allocator.Temp));

            pointBuffer_Post = new ComputeBuffer(points.Length, UnsafeUtility.SizeOf<PointInfo>());
            pointBuffer_Post.SetData(points.ToArray(Allocator.Temp));
        }

        public void ComputeCall(CommandBuffer commandBuffer)
        {
            int3 DispatchArgs = new int3(points.Length, 1, 1);
            commandBuffer.SetComputeBufferParam(computeShader, baseKernelID, bufferName_Now, pointBuffer_Now);
            commandBuffer.SetComputeBufferParam(computeShader, baseKernelID, bufferName_Post, pointBuffer_Post);
            commandBuffer.DispatchCompute(computeShader, baseKernelID, DispatchArgs.x, DispatchArgs.y, DispatchArgs.z);
        }

        /// <summary>
        /// 这个就算更精准的引用方法了。
        /// </summary>
        /// <param name="commandBuffer"></param>
        /// <param name="computeShader"></param>
        /// <param name="kernelName"></param>
        public void SetBuffer(CommandBuffer commandBuffer , ComputeShader computeShader ,string kernelName)
        {
            int kernelID = computeShader.FindKernel(kernelName);
            commandBuffer.SetComputeBufferParam(computeShader, kernelID, bufferName_Now, pointBuffer_Now);
            commandBuffer.SetComputeBufferParam(computeShader, kernelID, bufferName_Post, pointBuffer_Post);
        }

        /// <summary>
        /// 这里还是需要用这个Buffer在Shader中的标准名字。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="commandBuffer"></param>
        /// <param name="constraintCall"></param>
        public void SetBuffer<T>(CommandBuffer commandBuffer, ConstraintCall<T> constraintCall) where T : unmanaged
        {
            commandBuffer.SetComputeBufferParam(ConstraintCall<T>.computeShader, constraintCall.baseKernelID, bufferName_Now, pointBuffer_Now);
            commandBuffer.SetComputeBufferParam(ConstraintCall<T>.computeShader, constraintCall.baseKernelID, bufferName_Post, pointBuffer_Post);
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
