using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Mathematics;
using UnityEngine;

public class DataHub
{

    /// <summary>
    /// 
    /// </summary>
    /// <param name="vec"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int2 PackInt2(in int2 vec) => PackInt2(vec.x, vec.y);


    /// <summary>
    /// 
    /// </summary>
    /// <param name="d0"></param>
    /// <param name="d1"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int2 PackInt2(int d0, int d1)
    {
        return d0 < d1 ? new int2(d0, d1) : new int2(d1, d0);
    }



    /// <summary>
    /// 
    /// </summary>
    /// <param name="vec"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int3 PackInt3(in int3 vec) => PackInt3(vec.x, vec.y, vec.z);



    /// <summary>
    /// 
    /// </summary>
    /// <param name="d0"></param>
    /// <param name="d1"></param>
    /// <param name="d2"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int3 PackInt3(int d0, int d1, int d2)
    {
        if (d0 < d1 && d0 < d2)
        {
            // d0,x,x
            if (d1 < d2)
                return new int3(d0, d1, d2);
            else
                return new int3(d0, d2, d1);
        }
        if (d1 < d2)
        {
            // d1,x,x
            if (d0 < d2)
                return new int3(d1, d0, d2);
            else
                return new int3(d1, d2, d0);
        }
        else
        {
            // d2,x,x
            if (d0 < d1)
                return new int3(d2, d0, d1);
            else
                return new int3(d2, d1, d0);
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="vec"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int4 PackInt4(in int4 vec) => PackInt4(vec.x, vec.y, vec.z,vec.w);


    /// <summary>
    /// 
    /// </summary>
    /// <param name="d0"></param>
    /// <param name="d1"></param>
    /// <param name="d2"></param>
    /// <param name="d3"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int4 PackInt4(int d0, int d1, int d2, int d3)
    {
        int w;

        // step1
        if (d0 > d3)
        {
            w = d0;
            d0 = d3;
            d3 = w;
        }
        if (d1 > d2)
        {
            w = d1;
            d1 = d2;
            d2 = w;
        }

        // step2
        if (d0 > d1)
        {
            w = d0;
            d0 = d1;
            d1 = w;
        }
        if (d2 > d3)
        {
            w = d2;
            d2 = d3;
            d3 = w;
        }

        // step3
        if (d1 > d2)
        {
            w = d1;
            d1 = d2;
            d2 = w;
        }

        return new int4(d0, d1, d2, d3);
    }




    /// <summary>
    /// 
    /// </summary>
    /// <param name="hi"></param>
    /// <param name="low"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Pack32(int hi, int low)
    {
        return (uint)hi << 16 | (uint)low & 0xffff;
    }



    /// <summary>
    /// 
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Pack32Sort(int a, int b)
    {
        if (a > b)
        {
            return (uint)b << 16 | (uint)a & 0xffff;
        }
        else
        {
            return (uint)a << 16 | (uint)b & 0xffff;
        }
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="pack"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Unpack32Hi(uint pack)
    {
        return (int)((pack >> 16) & 0xffff);
    }



    /// <summary>
    /// 
    /// </summary>
    /// <param name="pack"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Unpack32Low(uint pack)
    {
        return (int)(pack & 0xffff);
    }



    /// <summary>
    /// 
    /// </summary>
    /// <param name="pack"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Unpack12_20Hi(uint pack)
    {
        return (int)((pack >> 20) & 0xfff);
    }




    /// <summary>
    /// 
    /// </summary>
    /// <param name="pack"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Unpack12_20Low(uint pack)
    {
        return (int)(pack & 0xfffff);
    }



    /// <summary>
    /// 
    /// </summary>
    /// <param name="pack"></param>
    /// <param name="hi"></param>
    /// <param name="low"></param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Unpack12_20(uint pack, out int hi, out int low)
    {
        hi = (int)((pack >> 20) & 0xfff);
        low = (int)(pack & 0xfffff);
    }




    /// <summary>
    /// 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="w"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Pack64(int x, int y, int z, int w)
    {
        return (((ulong)x) & 0xffff) << 48 | (((ulong)y) & 0xffff) << 32 | (((ulong)z) & 0xffff) << 16 | ((ulong)w) & 0xffff;
    }




    /// <summary>
    /// 
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Pack64(in int4 a)
    {
        return Pack64(a.x, a.y, a.z, a.w);
    }





    /// <summary>
    /// 
    /// </summary>
    /// <param name="pack"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int4 Unpack64(in ulong pack)
    {
        return new int4(
            (int)((pack >> 48) & 0xffff),
            (int)((pack >> 32) & 0xffff),
            (int)((pack >> 16) & 0xffff),
            (int)(pack & 0xffff)
            );
    }


    /// <summary>
    /// ulongパックデータからx値を取り出す
    /// </summary>
    /// <param name="pack"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Unpack64X(in ulong pack)
    {
        return (int)((pack >> 48) & 0xffff);
    }



    /// <summary>
    /// ulongパックデータからy値を取り出す
    /// </summary>
    /// <param name="pack"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Unpack64Y(in ulong pack)
    {
        return (int)((pack >> 32) & 0xffff);
    }



    /// <summary>
    /// ulongパックデータからz値を取り出す
    /// </summary>
    /// <param name="pack"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Unpack64Z(in ulong pack)
    {
        return (int)((pack >> 16) & 0xffff);
    }



    /// <summary>
    /// ulongパックデータからw値を取り出す
    /// </summary>
    /// <param name="pack"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Unpack64W(in ulong pack)
    {
        return (int)(pack & 0xffff);
    }



    /// <summary>
    /// ４つのintをbyteに変換し１つのuintにパッキングする
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="z"></param>
    /// <param name="w"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Pack32(int x, int y, int z, int w)
    {
        return (((uint)x) & 0xff) << 24 | (((uint)y) & 0xff) << 16 | (((uint)z) & 0xff) << 8 | ((uint)w) & 0xff;
    }



    /// <summary>
    /// int4をbyteに変換し１つのuintにパッキングする
    /// </summary>
    /// <param name="a"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Pack32(in int4 a)
    {
        return Pack64(a.x, a.y, a.z, a.w);
    }



    /// <summary>
    /// uintパックデータからint4に展開して返す
    /// </summary>
    /// <param name="pack"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int4 Unpack32(in uint pack)
    {
        return new int4(
            (int)((pack >> 24) & 0xff),
            (int)((pack >> 16) & 0xff),
            (int)((pack >> 8) & 0xff),
            (int)(pack & 0xff)
            );
    }
}

