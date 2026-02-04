using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using System.Text;
using UnityEditor;

namespace AUnityLocal
{
    ///扩展类型,扩展方法
    public static class ObjectExtention
    {
        public static bool IsNull<T>(this T obj) where T : class
        {
            return obj == null || obj.Equals(null);
        }

        public static bool IsValid<T>(this T obj) where T : class
        {
            return obj != null && !obj.Equals(null);
        }

        public static bool IsMissing<T>(this T obj) where T : UnityEngine.Object
        {
            return obj != null && obj.Equals(null);
        }

        public static IList<T> Clone<T>(this IList<T> originList)
        {
            // UnityEngine.Debug.LogError("FullName::" + typeof(T).FullName);
            // UnityEngine.Debug.LogError("originList FullName::" + originList.GetType().FullName);
            IList<T> list = (IList<T>)Activator.CreateInstance(originList.GetType());
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

        public static string ToStr<T>(this IList<T> originList, string spacing)
        {
            return ToStr(originList, originList.Count, 0, spacing, null);
        }

        public static string ToStr<T>(this IList<T> originList, Func<T, string> itemToStrFunc, string spacing)
        {
            return ToStr(originList, originList.Count, 0, spacing, itemToStrFunc);
        }

        public static string ToStr<T>(this IList<T> originList, int count = 0, int index = 0, string space = " ",
            Func<T, string> itemToStrFunc = null)
        {
            if (originList == null || originList.Count == 0)
                return string.Empty;

            // 参数验证和调整
            if (index < 0) index = 0;
            if (index >= originList.Count) index = originList.Count - 1;

            if (count <= 0)
                count = originList.Count;

            count = Math.Min(index + count, originList.Count);

            stringBuilder.Clear();
            StringBuilder sb = stringBuilder;

            for (int i = index; i < count; i++)
            {
                T item = originList[i];
                string spacing = (i == count - 1) ? string.Empty : space;

                string itemStr = GetItemString(item, itemToStrFunc);
                if (!string.IsNullOrEmpty(itemStr))
                {
                    sb.Append(itemStr + spacing);
                }
            }

            return sb.ToString();
        }

        private static string GetItemString<T>(T item, Func<T, string> itemToStrFunc)
        {
            // 优先使用自定义转换函数
            if (itemToStrFunc != null)
            {
                return itemToStrFunc(item);
            }

            // 处理null值
            if (item == null || (item is UnityEngine.Object unityObj && unityObj.IsNull()))
            {
                return "null";
            }

            // Unity基础类型
            switch (item)
            {
                case Vector2 v2:
                    return $"({v2.x:F2},{v2.y:F2})";
                case Vector3 v3:
                    return $"({v3.x:F2},{v3.y:F2},{v3.z:F2})";
                case Vector4 v4:
                    return $"({v4.x:F2},{v4.y:F2},{v4.z:F2},{v4.w:F2})";
                case Vector2Int v2i:
                    return $"({v2i.x},{v2i.y})";
                case Vector3Int v3i:
                    return $"({v3i.x},{v3i.y},{v3i.z})";
                case Quaternion q:
                    return $"({q.x:F2},{q.y:F2},{q.z:F2},{q.w:F2})";
                case Color c:
                    return $"RGBA({c.r:F2},{c.g:F2},{c.b:F2},{c.a:F2})";
                case Color32 c32:
                    return $"RGBA({c32.r},{c32.g},{c32.b},{c32.a})";
                case Rect rect:
                    return $"Rect({rect.x:F2},{rect.y:F2},{rect.width:F2},{rect.height:F2})";
                case RectInt rectInt:
                    return $"RectInt({rectInt.x},{rectInt.y},{rectInt.width},{rectInt.height})";
                case Bounds bounds:
                    return $"Bounds(center:{bounds.center}, size:{bounds.size})";
                case Matrix4x4 matrix:
                    return $"Matrix4x4({matrix.m00:F2},{matrix.m01:F2},{matrix.m02:F2},{matrix.m03:F2}...)";
            }

            // Unity UI组件
            if (item is UnityEngine.UI.Text uitext)
            {
                return uitext.text;
            }
            else if (item is UnityEngine.UI.Image uiimage)
            {
                string str = (uiimage.sprite != null) ? uiimage.sprite.name : "null";
                return $"Image({str})";
            }
            else if (item is UnityEngine.UI.Button uibutton)
            {
                return $"Button({uibutton.name})";
            }
            else if (item is UnityEngine.UI.Toggle uitoggle)
            {
                return $"Toggle({uitoggle.name}, {uitoggle.isOn})";
            }
            else if (item is UnityEngine.UI.Slider uislider)
            {
                return $"Slider({uislider.name}, {uislider.value:F2})";
            }
            else if (item is UnityEngine.UI.InputField uiinput)
            {
                return $"InputField({uiinput.text})";
            }

            // Unity核心组件
            switch (item)
            {
                case Transform transform:
                    return $"Transform({transform.name}, P:{transform.position:F2})";
                case GameObject gameObject:
                    return $"GameObject({gameObject.name}, Active:{gameObject.activeInHierarchy})";
                case Rigidbody rigidbody:
                    return $"Rigidbody({rigidbody.name}, Mass:{rigidbody.mass:F2})";
                case Collider collider:
                    return $"Collider({collider.name}, Enabled:{collider.enabled})";
                case Renderer renderer:
                    return $"Renderer({renderer.name}, Enabled:{renderer.enabled})";
                case Camera camera:
                    return $"Camera({camera.name}, FOV:{camera.fieldOfView:F1})";
                case Light light:
                    return $"Light({light.name}, Intensity:{light.intensity:F2})";
                case AudioSource audioSource:
                    return $"AudioSource({audioSource.name}, Volume:{audioSource.volume:F2})";
                case Component component:
                    return $"{component.name}";
                case UnityEngine.Object unityObject:
                    return $"{unityObject.name}";
            }

            // 基础数据类型
            switch (item)
            {
                case float f:
                    return f.ToString("F2");
                case double d:
                    return d.ToString("F2");
                case bool b:
                    return b ? "True" : "False";
                case DateTime dt:
                    return dt.ToString("yyyy-MM-dd HH:mm:ss");
                case TimeSpan ts:
                    return ts.ToString(@"hh\:mm\:ss");
            }

            // 默认ToString
            return item.ToString();
        }


        public static string ToStr<T>(this IEnumerable<T> enumerable, int count = 0, int skip = 0, string separator = " ", Func<T, string> itemToStrFunc = null)
        {
            if (enumerable == null)
                return string.Empty;

            // 参数验证
            if (skip < 0) skip = 0;
            if (count < 0) count = 0;

            stringBuilder.Clear();
            StringBuilder sb = stringBuilder;

            using (var enumerator = enumerable.GetEnumerator())
            {
                // 跳过指定数量的元素
                for (int i = 0; i < skip && enumerator.MoveNext(); i++) { }

                int processedCount = 0;
                bool hasNext = enumerator.MoveNext();

                while (hasNext && (count == 0 || processedCount < count))
                {
                    T item = enumerator.Current;
                    hasNext = enumerator.MoveNext();
            
                    string itemStr = GetItemString(item, itemToStrFunc);
                    if (!string.IsNullOrEmpty(itemStr))
                    {
                        sb.Append(itemStr);
                
                        // 如果不是最后一个元素且还有下一个元素，添加分隔符
                        if (hasNext && (count == 0 || processedCount < count - 1))
                        {
                            sb.Append(separator);
                        }
                    }
            
                    processedCount++;
                }
            }

            return sb.ToString();
        }

// 重载方法：简化版本，保持向后兼容
        public static string ToStr<T>(this IEnumerable<T> enumerable, string separator)
        {
            return enumerable.ToStr(0, 0, separator);
        }

// 重载方法：使用自定义转换函数
        public static string ToStr<T>(this IEnumerable<T> enumerable, Func<T, string> itemToStrFunc)
        {
            return enumerable.ToStr(0, 0, " ", itemToStrFunc);
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

        public static bool IsMatch(this string str, string searchName, bool exactMatch = false)
        {
            if (string.IsNullOrEmpty(str))
            {
                return false;
            }

            if (exactMatch)
            {
                return str.Equals(searchName, System.StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return str.IndexOf(searchName, System.StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        public static bool IsNameMatch<T>(this T o, string searchName, bool exactMatch = false)
            where T : UnityEngine.Object
        {
            string objectName = o.IsValid() ? o.name : string.Empty;
            if (string.IsNullOrEmpty(objectName))
            {
                return true;
            }

            if (exactMatch)
            {
                return objectName.Equals(searchName, System.StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return objectName.IndexOf(searchName, System.StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }

        public static bool IsTextMatch<T>(this T uitext, string searchName, bool exactMatch = false)
            where T : UnityEngine.UI.Text
        {
            string objectName = uitext.IsValid() ? uitext.text : string.Empty;
            if (string.IsNullOrEmpty(objectName))
            {
                return true;
            }

            if (exactMatch)
            {
                return objectName.Equals(searchName, System.StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                return objectName.IndexOf(searchName, System.StringComparison.OrdinalIgnoreCase) >= 0;
            }
        }
        public static List<string> GetAllChildrenFullName(this Transform parent,  bool includeSelf = false,
            int maxDepth = 0,Transform root = null, bool includeRootName = false)
        {
            List<Transform> result = new List<Transform>();
            GetAllChildrenRecursive(parent, result, includeSelf, maxDepth, 0);
            List<string> fullNames = new List<string>();
            foreach (var child in result)
            {
                fullNames.Add(child.FullName(root, includeRootName));
            }
            return fullNames;
        }
        public static List<Transform> GetAllChildren(this Transform transform, bool includeSelf = false,
            int maxDepth = 0)
        {
            List<Transform> result = new List<Transform>();
            GetAllChildrenRecursive(transform, result, includeSelf, maxDepth, 0);
            return result;
        }

        public static void GetAllChildren(this Transform transform, List<Transform> result, bool includeSelf = false,
            int maxDepth = 0)
        {
            GetAllChildrenRecursive(transform, result, includeSelf, maxDepth, 0);
        }

        private static void GetAllChildrenRecursive(Transform transform, List<Transform> result, bool includeSelf,
            int maxDepth, int currentDepth)
        {
            if (!transform.IsValid())
            {
                return;
            }

            // 只有在第一层且includeParent为true时才添加父物体
            if (currentDepth == 0 && includeSelf)
            {
                result.Add(transform);
            }

            // 如果设置了最大深度限制且已达到限制，则停止递归
            if (maxDepth > 0 && currentDepth >= maxDepth)
            {
                return;
            }

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                result.Add(child);

                // 递归获取子物体的子物体
                GetAllChildrenRecursive(child, result, false, maxDepth, currentDepth + 1);
            }
        }

        public static string FullName(this Transform transform, Transform root = null, bool includeRootName = false)
        {
            if (transform == null) return "";

            List<string> path = new List<string>();
            Transform current = transform;

            while (current != null && current != root)
            {
                path.Add(current.name);
                current = current.parent;
            }

            if (root != null && current != root)
            {
                return "不是子节点";
            }

            // 如果需要包含相对变换的名称
            if (includeRootName && root != null && current == root)
            {
                path.Add(root.name);
            }

            // 反转路径并用StringBuilder构建字符串
            if (path.Count == 0) return "";

            var sb = new System.Text.StringBuilder();
            for (int i = path.Count - 1; i >= 0; i--)
            {
                if (sb.Length > 0) sb.Append("/");
                sb.Append(path[i]);
            }

            return sb.ToString();
        }
        public static void Reset(this Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }    
        public static string FileName(this string path,bool ext=false)
        {
            if(ext)
            {
                return Path.GetFileName(path);
            }
            else
            {
                return Path.GetFileNameWithoutExtension(path);
            }
        }           
        
        
        
        public static T FindAndGetComponent<T>(string name,bool enable = true) where T : Behaviour
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                var com = go.GetComponent<T>();
                if (com != null)
                {
                    com.enabled = enable;
                    return com;
                }
            }
            return null;
        }
        public static void FindAndSetGameObject(string name,bool active = true)
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                go.SetActive(active);
            }
        }    
        public static string Dumper<T>(this T obj)
        {
            return AUnityLocal.Dumper.Do(obj);
        }        
        
    
    }
    

}