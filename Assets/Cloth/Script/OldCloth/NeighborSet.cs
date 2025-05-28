using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;

public class NeighborSet<TKey, TValue>
{
    //保证一下Key值的顺序，不过竟然能使用泛型
    private SortedDictionary<TKey, HashSet<TValue>> keyToValues;
    public NeighborSet()
    {
        keyToValues = new SortedDictionary<TKey, HashSet<TValue>>();
    }


    public void UniqueAdd(TKey key, TValue value)
    {
        if (!keyToValues.ContainsKey(key))
        {
            keyToValues.Add(key, new HashSet<TValue>());
        }

        if (!Contains(key, value))
        {
            keyToValues[key].Add(value);
        }

    }

    public bool Contains(TKey key,TValue value)
    {
        if(keyToValues.ContainsKey(key))
            if (keyToValues[key].Contains(value))
            {
                return true;
            }
            return false;
    }
    public HashSet<TValue> GetValue(TKey key)
    {
        return keyToValues[key];
    }

    public void Remove(TKey key,TValue value)
    {
        if (keyToValues.ContainsKey(key))
        {
            keyToValues[key].Remove(value); // 从 HashSet 中移除邻接点
            if (keyToValues[key].Count == 0)
            {
                keyToValues.Remove(key); // 如果没有任何邻接点了，移除整个顶点
            }
        }
    }
    
    public void SplitSet(out List<int> valuesIndex,out List<TValue> values)
    {
        valuesIndex = new List<int>();
        values = new List<TValue>();

        int lastnum = 0;
        foreach(var kvp in keyToValues)
        {
            values.AddRange(kvp.Value.ToArray()); //把数据存储进去，这是必要的
            valuesIndex.Add(lastnum); //把索引加入 比如第一个是从0 开始到第二个是边 第二个是 从 3  开始到第三个是边 第三个顶点 是从 6 开始到第四个是边。 
            lastnum += kvp.Value.Count;
        }

        
    }

}
