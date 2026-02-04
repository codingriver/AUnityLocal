using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AUnityLocal
{
    public static class Dumper
    {
        private static StringBuilder _sb = new StringBuilder(1024);

        private static HashSet<object> _visited =
            new HashSet<object>(new ReferenceEqualityComparer());

        private static bool _includeStatic;
        private static int _depth ;
        
        public static HashSet<Type> IgnoreTypeList=>_ignoreTypeList;
        private static HashSet<Type> _ignoreTypeList=new HashSet<Type> {
        
        };
        public static HashSet<Type> UseDefaultToStringTypeList=>_useDefaultToStringTypeList;
        private static HashSet<Type>_useDefaultToStringTypeList=new HashSet<Type> {
        
        };

        public static string Do(object obj, int depth=2, bool includeStatic=false)
        {
            _includeStatic = includeStatic;
            _sb.Clear();
            _visited.Clear();
            _depth = depth;
            DoDump(obj, 0);
            return _sb.ToString();
        }

        private static void DoDump(object obj, int indent)
        {
            if (obj == null)
            {
                _sb.Append("[null]");
                return;
            }

            if (indent > _depth)
            {
                _sb.Append("[ 超出递归解析范围，中断]");
                return;
            }

            var type = obj.GetType();

            // 循环引用检测（仅引用类型）
            if (!type.IsValueType)
            {
                if (_visited.Contains(obj))
                {
                    _sb.Append("<circular>");
                    return;
                }

                _visited.Add(obj);
            }

            if (type.IsPrimitive || obj is string || type.IsEnum)
            {
                _sb.Append(obj);
                return;
            }

            if (obj is IEnumerable enumerable)
            {
                _sb.Append("[\n");
                foreach (var item in enumerable)
                {
                    AppendIndent(indent + 1);
                    DoDump(item, indent + 1);
                    _sb.Append(",\n");
                }

                AppendIndent(indent);
                _sb.Append("]");
                return;
            }

            // 类 / 结构体
            _sb.Append(type.Name);
            _sb.Append("\n");
            AppendIndent(indent);
            _sb.Append("{\n");

            DumpMembers(type, obj, indent + 1);

            AppendIndent(indent);
            _sb.Append("}");
        }

        private static void DumpMembers(Type type, object target, int indent)
        {
           if(_useDefaultToStringTypeList.Contains(type))
           {
               _sb.Append(target.ToString());
               return;
           }
           if(_ignoreTypeList.Contains(type))
           {
               _sb.Append("[ignore]");
               return;
           }
            BindingFlags flags =
                BindingFlags.Public |
                BindingFlags.Instance |
                (_includeStatic ? BindingFlags.Static : 0);

            // Properties
            foreach (var p in type.GetProperties(flags))
            {
                if (!p.CanRead) continue;
                if (p.GetIndexParameters().Length > 0) continue;

                AppendIndent(indent);
                _sb.Append(p.Name);

                if (p.GetMethod.IsStatic)
                    _sb.Append(" (static)");

                _sb.Append(" = ");

                try
                {
                    var value = p.GetValue(p.GetMethod.IsStatic ? null : target);
                    DoDump(value, indent);
                }
                catch
                {
                    _sb.Append("?");
                }

                _sb.Append(",\n");
            }

            // Fields
            foreach (var f in type.GetFields(flags))
            {
                AppendIndent(indent);
                _sb.Append(f.Name);

                if (f.IsStatic)
                    _sb.Append(" (static)");

                _sb.Append(" = ");

                try
                {
                    var value = f.GetValue(f.IsStatic ? null : target);
                    DoDump(value, indent);
                }
                catch
                {
                    _sb.Append("?");
                }

                _sb.Append(",\n");
            }
        }

        private static void AppendIndent(int indent)
        {
            for (int i = 0; i < indent; i++)
                _sb.Append("    ");
        }

        class ReferenceEqualityComparer : IEqualityComparer<object>
        {
            public new bool Equals(object x, object y)
                => ReferenceEquals(x, y);

            public int GetHashCode(object obj)
                => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
        }
        
    }
}