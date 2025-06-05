#ifndef DATA_HUB
#define DATA_HUB

   /// <summary>
    /// 还是存为点吧。
    /// </summary>
struct PointInfo
{       
    float mass;
        
    float3 position;
        
    float3 velocity;
    
    float padding;
};

    /// <summary>
    /// 距离约束的信息
    /// </summary>
struct DistanceConstraintInfo
{        
    int vIndex0;
        
    int vIndex1;
        
    float restDistance;
    
    float lambda;
};

    
struct BendConstraintInfo
{    
    int vIndex0;
        
    int vIndex1;
        
    int vIndex2;
        
    int vIndex3;
        
    float rest; // 两个面的angle
    
    float lambda;
};

    
struct SizeConstraintInfo
{        
    int vIndex0;
        
    int vIndex1;
        
    int vIndex2;
        
    float rest; //初始面积
    
    float lambda;
};

    
struct FixedConstraintInfo
{
    float3 fixedPosition;
    
    float lambda;
};

    
struct ShearConstraintInfo
{
    int vIndex0;
        
    int vIndex1;
        
    int vIndex2;
        
    float rest; //初始角度
    
    float lambda;
};

#endif