using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using UnityEngine;
using UnityEditor;

namespace AUnityLocal.Editor.Roslyn
{
    [InitializeOnLoad]
    public static class RoslynLoader
    {
        static RoslynLoader()
        {
        }

        // ========== 核心：Unity 2022兼容的快捷键绑定 ==========
        // % = Ctrl, # = Shift, & = Alt, _F3 = F3键
        [MenuItem("Tools/Roslyn/执行动态编译 _F3", false, 1)]
        private static void TriggerRoslynCompile()
        {
            Debug.LogWarning("F3 ok! Start Compile And Run");
            Run();
        }

        // 验证快捷键是否可用（必须实现，否则MenuItem会报错）
        [MenuItem("Tools/Roslyn/执行动态编译 _F3", true)]
        private static bool ValidateTriggerRoslynCompile()
        {
            // 始终返回true，表示快捷键可用
            return true;
        }
        [MenuItem("Tools/Roslyn/执行测试  _F4", false, 1)]
        private static void Test()
        {
            Debug.LogWarning("F4 ok!");
            var marchCache = IGG.Game.Data.Cache.AppCache.WorldMap.GetMapCache<IGG.Game.Data.Cache.Player.PlayerCache.MarchCache>();
            UnityEngine.Debug.Log(AUnityLocal.Dumper.Do(marchCache,6));
        }

        // 验证快捷键是否可用（必须实现，否则MenuItem会报错）
        [MenuItem("Tools/Roslyn/执行测试  _F4", true)]
        private static bool ValidateTest()
        {
            // 始终返回true，表示快捷键可用
            return true;
        }
        static void Run()
        {
            string code = null;
            try
            {
                code = File.ReadAllText(Path.Combine(Application.dataPath, "AUnityLocal/Editor/Roslyn/RoslynRunner.cs.txt"));
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError(e.ToString());
            }
            
            string staticMethodClassName = "RoslynRunner";
            string staticMethodName = "Run";
            if (code == null)
            {
                code = testCode;
            }
            RunRoslynCompilation(code, staticMethodClassName, staticMethodName);
        }

        // 待编译的测试代码
        const string testCode = @"
                using System;
                public class RoslynRunner
                {
                    public static void Run()
                    {
                        int a=100;
                        int b=200;
                        int c=a+b;
                        UnityEngine.Debug.Log(""c=""+c);
                    }
                }
        ";

        // ========== Roslyn核心编译逻辑 ==========
       public static void RunRoslynCompilation(string code, string staticMethodClassName, string staticMethodName)
        {
            try
            {
                float time = UnityEngine.Time.realtimeSinceStartup;
                // 1. 配置编译选项
                var compileOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithOptimizationLevel(OptimizationLevel.Debug)
                    .WithPlatform(Platform.AnyCpu);

                // 2. 构建程序集引用（适配Unity 2022）
                // string pluginPath = Path.Combine(Application.dataPath, "Editor/Roslyn/Plugins");
                // var references = new MetadataReference[]
                // {
                //     // 核心系统库
                //     MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                //     MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                //     // Unity编辑器/运行时库
                //     MetadataReference.CreateFromFile(typeof(UnityEditor.Editor).Assembly.Location),
                //     MetadataReference.CreateFromFile(typeof(GameObject).Assembly.Location),
                //     // Roslyn依赖库（必须）
                //     MetadataReference.CreateFromFile(Path.Combine(pluginPath, "System.Collections.Immutable.dll")),
                //     MetadataReference.CreateFromFile(Path.Combine(pluginPath, "Microsoft.CodeAnalysis.CSharp.dll")),
                //     MetadataReference.CreateFromFile(Path.Combine(pluginPath, "Microsoft.CodeAnalysis.Common.dll")),
                //     MetadataReference.CreateFromFile(Path.Combine(pluginPath, "System.Reflection.Metadata.dll")),
                //     MetadataReference.CreateFromFile(Path.Combine(pluginPath, "System.Runtime.CompilerServices.Unsafe.dll"))
                // };
                var references = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
                    .Select(a => MetadataReference.CreateFromFile(a.Location))
                    .Cast<MetadataReference>();
                float time1 = UnityEngine.Time.realtimeSinceStartup;
                Debug.Log("计时：：time1:"+(time1-time));
                // 3. 解析语法树
                var syntaxTree = CSharpSyntaxTree.ParseText(code);
                Debug.Log("计时：：time2:"+(UnityEngine.Time.realtimeSinceStartup-time));
                // 4. 创建编译对象
                var compilation = CSharpCompilation.Create(
                    "RoslynCompilationEditorAssembly",
                    new[] { syntaxTree },
                    references,
                    compileOptions
                );
                Debug.Log("计时：：time3:"+(UnityEngine.Time.realtimeSinceStartup-time));
                // 5. 执行编译
                using (var ms = new MemoryStream())
                {
                    var emitResult = compilation.Emit(ms);
                    Debug.Log("计时：：time4:"+(UnityEngine.Time.realtimeSinceStartup-time));
                    // 6. 处理编译结果
                    if (!emitResult.Success)
                    {
                        foreach (var diagnostic in emitResult.Diagnostics)
                        {
                            if (diagnostic.Severity == DiagnosticSeverity.Error)
                            {
                                Debug.LogError($"[Roslyn编译错误] {diagnostic.Id}: {diagnostic.GetMessage()}");
                            }
                            else if (diagnostic.Severity == DiagnosticSeverity.Warning)
                            {
                                Debug.LogWarning($"[Roslyn编译警告] {diagnostic.Id}: {diagnostic.GetMessage()}");
                            }
                        }

                        return;
                    }
                    Debug.Log("计时：：time45:"+(UnityEngine.Time.realtimeSinceStartup-time));
                    // 7. 加载并执行编译后的程序集
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = Assembly.Load(ms.ToArray());

                    // Type calculatorType = assembly.GetType("DynamicCode.Calculator");
                    // if (calculatorType != null)
                    // {
                    //     object instance = Activator.CreateInstance(calculatorType);
                    //     
                    //     // 调用Add方法
                    //     MethodInfo addMethod = calculatorType.GetMethod("Add");
                    //     int addResult = (int)addMethod.Invoke(instance, new object[] { 100, 200 });
                    //     Debug.Log($"Add方法执行结果: {addResult}");
                    //
                    //     // 调用GetMessage方法
                    //     MethodInfo msgMethod = calculatorType.GetMethod("GetMessage");
                    //     string msgResult = (string)msgMethod.Invoke(instance, null);
                    //     Debug.Log($"GetMessage方法执行结果: {msgResult}");
                    // }
                    // else
                    // {
                    //     Debug.LogError("未找到DynamicCode.Calculator类");
                    // }
                    Debug.Log("计时：：time6:"+(UnityEngine.Time.realtimeSinceStartup-time));
                    Type type = assembly.GetType(staticMethodClassName);
                    if (type != null)
                    {
                        // 调用A方法
                        MethodInfo method = type.GetMethod(staticMethodName);
                        if (method != null)
                        {
                            method.Invoke(null, null);
                            Debug.Log($"[Roslyn]执行完成");
                        }
                        else
                        {
                            Debug.LogError($"[Roslyn]未找到类[{staticMethodClassName}]的方法[{staticMethodName}]");
                        }
                    }
                    else
                    {
                        Debug.LogError($"[Roslyn]未找到类[{staticMethodClassName}]");
                    }
                    Debug.Log("计时：：time7:"+(UnityEngine.Time.realtimeSinceStartup-time));
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Roslyn]执行异常: {e.Message}\n{e.StackTrace}");
            }
        }
    }
}