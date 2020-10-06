using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Utils
{
    public static List<int> RandomPermutation(int start, int end)
    {
        var list = Enumerable.Range(start, end).ToList();
        RandomShuffle(list);
        return list;
    }
    
    private static void RandomShuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++) {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }
}
