using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;

namespace AUnityLocal
{

///扩展类型,扩展方法
public static class ObjectExtention
{
    public static IList<T>Clone<T>(this IList<T>originList)
    {
        // UnityEngine.Debug.LogError("FullName::" + typeof(T).FullName);
        // UnityEngine.Debug.LogError("originList FullName::" + originList.GetType().FullName);
        IList<T> list =(IList<T>)Activator.CreateInstance(originList.GetType());
        int count = originList.Count;
        for (int i = 0; i < count; i++)
        {
            list.Add(originList[i]);
        }

        return list;
    }
    
    static StringBuilder stringBuilder = new StringBuilder();
    public static string ToStr<T>(this IList<T> originList, Func<T, string> itemToStrFunc)
    {
        return ToStr(originList, originList.Count, 0, " ", itemToStrFunc);
    }
    public static string ToStr<T>(this IList<T> originList,string spacing)
    {
        return ToStr(originList, originList.Count, 0, spacing, null);
    }    
    public static string ToStr<T>(this IList<T> originList, Func<T, string> itemToStrFunc,string spacing)
    {
        return ToStr(originList, originList.Count, 0, spacing, itemToStrFunc);
    }
    public static string ToStr<T>(this IList<T> originList, int count = 0, int index = 0, string spacing = " ", Func<T, string> itemToStrFunc = null)
    {
        if (count == -1 || count == 0)
        {
            count = originList.Count;
        }

        stringBuilder.Clear();
        StringBuilder sb = stringBuilder;
        sb.Append("[ ");
        if (count > 0)
        {
            sb.Append( spacing);
        }
        
        Type type = typeof(T);
        for (int i = index; i < count; i++)
        {
            if (itemToStrFunc != null)
            {
                sb.Append(itemToStrFunc(originList[i]) + spacing);
            }
            else
            {
                if (originList is List<Vector2> ls1)
                {
                    Vector2 it = ls1[i];
                    sb.Append($"({(int)it.x},{(int)it.y})" + spacing);
                }
                else if (originList is List<Vector3> ls2)
                {
                    Vector3 it = ls2[i];
                    sb.Append($"({(int)it.x},{(int)it.y},{(int)it.z})" + spacing);
                }
                else if (typeof(UnityEngine.Object).IsAssignableFrom(typeof(T)))
                {
                    T it = originList[i];
                    if (it is UnityEngine.UI.Text uitext)
                    {
                        sb.Append($"{uitext.text}" + spacing);
                    }
                    else if (type.IsValueType)
                    {
                        sb.Append((it as UnityEngine.Object) + spacing);
                    }
                    else if(it!=null)
                    {
                        sb.Append((it as UnityEngine.Object).name + spacing);
                    }
                    else
                    {
                        sb.Append(originList[i].ToString() + spacing);
                    }
                }
                else
                {
                    sb.Append(originList[i].ToString() + spacing);
                }
                
            }

        }
        sb.Append("| "+"Len:" + count +" ]");
        return sb.ToString();
    }


    public static string ToStr<T>(this IEnumerable<T> enumerable)
    {
        StringBuilder sb = new StringBuilder("[ ");
        IEnumerator<T> itor= enumerable.GetEnumerator();
        int i = 0;
        while (itor.MoveNext())
        {
            sb.Append(itor.Current.ToString()+" ");
            i++;
        }
        sb.Insert(1, $"Length:{i}| ");
        sb.Append(" ]");
        return sb.ToString();
    }
    
    public static string RemoveFirstAndLastLine(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;
        
        var lines = str.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length <= 2)
            return string.Empty;
        
        return string.Join(Environment.NewLine, lines.Skip(1).Take(lines.Length - 2));
    }    

}


}