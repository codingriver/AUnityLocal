using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AUnityLocal.Editor
{
    public enum AssetType
    {
        Invalid = 0,
        Folder,
        Texture,
        Audio,
        Prefab,
        ScriptableObject,
        Script,
        Model, // 3D模型
        Material, // 材质球
        Shader, // 着色器
        Animation, // 动画
        AnimatorController, // 动画控制器
        Scene, // 场景
        Font, // 字体
        Video, // 视频
        Asset,
        PhysicMaterial, // 物理材质 (.physicMaterial)
        PhysicsMaterial2D, // 2D物理材质 (.physicsMaterial2D)
        Cubemap, // 立方体贴图
        RenderTexture, // 渲染纹理
        LightingDataAsset, // 光照数据资源
        TerrainData, // 地形数据
        Mesh, // 网格资源
        Timeline, // Timeline资源
        AudioMixer, // 音频混合器
        AudioMixerGroup, // 音频混合器组
        ComputeShader, // 计算着色器
        Flare, // 镜头光晕
        GUISkin, // GUI皮肤
        LensFlare, // 镜头光晕（旧版）
        ProceduralMaterial, // 程序化材质（Substance）
        TextAsset, // 文本资源
        Other
    }


public static class AssetTypeExtensions
{
    /// <summary>
    /// 获取资源类型的显示名称
    /// </summary>
    public static string GetDisplayName(this AssetType assetType)
    {
        return assetType switch
        {
            AssetType.Invalid => "无效",
            AssetType.Folder => "文件夹",
            AssetType.Texture => "纹理",
            AssetType.Audio => "音频",
            AssetType.Prefab => "预制体",
            AssetType.ScriptableObject => "可脚本化对象",
            AssetType.Script => "脚本",
            AssetType.Model => "模型",
            AssetType.Material => "材质球",
            AssetType.Shader => "着色器",
            AssetType.Animation => "动画",
            AssetType.AnimatorController => "动画控制器",
            AssetType.Scene => "场景",
            AssetType.Font => "字体",
            AssetType.Video => "视频",
            AssetType.Asset => "资源文件",
            AssetType.PhysicMaterial => "物理材质",
            AssetType.PhysicsMaterial2D => "2D物理材质",
            AssetType.Cubemap => "立方体贴图",
            AssetType.RenderTexture => "渲染纹理",
            AssetType.LightingDataAsset => "光照数据",
            AssetType.TerrainData => "地形数据",
            AssetType.Mesh => "网格",
            AssetType.Timeline => "时间轴",
            AssetType.AudioMixer => "音频混合器",
            AssetType.AudioMixerGroup => "音频混合器组",
            AssetType.ComputeShader => "计算着色器",
            AssetType.Flare => "镜头光晕",
            AssetType.GUISkin => "GUI皮肤",
            AssetType.LensFlare => "镜头光晕(旧版)",
            AssetType.ProceduralMaterial => "程序化材质",
            AssetType.TextAsset => "文本资源",
            AssetType.Other => "其他",
            _ => assetType.ToString()
        };
    }

    /// <summary>
    /// 获取资源类型的英文显示名称
    /// </summary>
    public static string GetDisplayNameEn(this AssetType assetType)
    {
        return assetType switch
        {
            AssetType.Invalid => "Invalid",
            AssetType.Folder => "Folder",
            AssetType.Texture => "Texture",
            AssetType.Audio => "Audio",
            AssetType.Prefab => "Prefab",
            AssetType.ScriptableObject => "ScriptableObject",
            AssetType.Script => "Script",
            AssetType.Model => "Model",
            AssetType.Material => "Material",
            AssetType.Shader => "Shader",
            AssetType.Animation => "Animation",
            AssetType.AnimatorController => "Animator Controller",
            AssetType.Scene => "Scene",
            AssetType.Font => "Font",
            AssetType.Video => "Video",
            AssetType.Asset => "Asset File",
            AssetType.PhysicMaterial => "Physic Material",
            AssetType.PhysicsMaterial2D => "Physics Material 2D",
            AssetType.Cubemap => "Cubemap",
            AssetType.RenderTexture => "Render Texture",
            AssetType.LightingDataAsset => "Lighting Data Asset",
            AssetType.TerrainData => "Terrain Data",
            AssetType.Mesh => "Mesh",
            AssetType.Timeline => "Timeline",
            AssetType.AudioMixer => "Audio Mixer",
            AssetType.AudioMixerGroup => "Audio Mixer Group",
            AssetType.ComputeShader => "Compute Shader",
            AssetType.Flare => "Flare",
            AssetType.GUISkin => "GUI Skin",
            AssetType.LensFlare => "Lens Flare (Legacy)",
            AssetType.ProceduralMaterial => "Procedural Material",
            AssetType.TextAsset => "Text Asset",
            AssetType.Other => "Other",
            _ => assetType.ToString()
        };
    }

    /// <summary>
    /// 获取资源类型的图标名称（用于EditorGUIUtility.IconContent）
    /// </summary>
    public static string GetIconName(this AssetType assetType)
    {
        return assetType switch
        {
            AssetType.Folder => "Folder Icon",
            AssetType.Texture => "Texture Icon",
            AssetType.Audio => "AudioClip Icon",
            AssetType.Prefab => "Prefab Icon",
            AssetType.ScriptableObject => "ScriptableObject Icon",
            AssetType.Script => "cs Script Icon",
            AssetType.Model => "Mesh Icon",
            AssetType.Material => "Material Icon",
            AssetType.Shader => "Shader Icon",
            AssetType.Animation => "Animation Icon",
            AssetType.AnimatorController => "AnimatorController Icon",
            AssetType.Scene => "SceneAsset Icon",
            AssetType.Font => "Font Icon",
            AssetType.Video => "VideoClip Icon",
            AssetType.Asset => "ScriptableObject Icon",
            AssetType.PhysicMaterial => "PhysicMaterial Icon",
            AssetType.PhysicsMaterial2D => "PhysicsMaterial2D Icon",
            AssetType.Cubemap => "Cubemap Icon",
            AssetType.RenderTexture => "RenderTexture Icon",
            AssetType.LightingDataAsset => "LightingDataAsset Icon",
            AssetType.TerrainData => "TerrainData Icon",
            AssetType.Mesh => "Mesh Icon",
            AssetType.Timeline => "TimelineAsset Icon",
            AssetType.AudioMixer => "AudioMixerController Icon",
            AssetType.AudioMixerGroup => "AudioMixerGroup Icon",
            AssetType.ComputeShader => "ComputeShader Icon",
            AssetType.Flare => "Flare Icon",
            AssetType.GUISkin => "GUISkin Icon",
            AssetType.LensFlare => "LensFlare Icon",
            AssetType.ProceduralMaterial => "ProceduralMaterial Icon",
            AssetType.TextAsset => "TextAsset Icon",
            _ => "DefaultAsset Icon"
        };
    }

    /// <summary>
    /// 判断是否为可编辑的资源类型
    /// </summary>
    public static bool IsEditable(this AssetType assetType)
    {
        return assetType switch
        {
            AssetType.Script or
            AssetType.Shader or
            AssetType.TextAsset or
            AssetType.Material or
            AssetType.Animation or
            AssetType.AnimatorController or
            AssetType.Timeline or
            AssetType.AudioMixer or
            AssetType.GUISkin or
            AssetType.Asset or
            AssetType.ScriptableObject => true,
            _ => false
        };
    }

    /// <summary>
    /// 判断是否为媒体资源类型
    /// </summary>
    public static bool IsMediaAsset(this AssetType assetType)
    {
        return assetType switch
        {
            AssetType.Texture or
            AssetType.Audio or
            AssetType.Video or
            AssetType.Cubemap or
            AssetType.RenderTexture => true,
            _ => false
        };
    }

    /// <summary>
    /// 判断是否为3D相关资源类型
    /// </summary>
    public static bool Is3DAsset(this AssetType assetType)
    {
        return assetType switch
        {
            AssetType.Model or
            AssetType.Mesh or
            AssetType.Material or
            AssetType.Shader or
            AssetType.Cubemap or
            AssetType.PhysicMaterial or
            AssetType.TerrainData or
            AssetType.LightingDataAsset => true,
            _ => false
        };
    }

    /// <summary>
    /// 判断是否为2D相关资源类型
    /// </summary>
    public static bool Is2DAsset(this AssetType assetType)
    {
        return assetType switch
        {
            AssetType.Texture or
            AssetType.PhysicsMaterial2D or
            AssetType.GUISkin or
            AssetType.Font => true,
            _ => false
        };
    }

    /// <summary>
    /// 判断是否为代码相关资源类型
    /// </summary>
    public static bool IsCodeAsset(this AssetType assetType)
    {
        return assetType switch
        {
            AssetType.Script or
            AssetType.Shader or
            AssetType.ComputeShader => true,
            _ => false
        };
    }

    
    
    /// <summary>
    /// 获取资源类型的文件扩展名列表
    /// </summary>
    public static string[] GetFileExtensions(this AssetType assetType)
    {
        return assetType switch
        {
            AssetType.Texture => new[] { ".png", ".jpg", ".jpeg", ".tga", ".bmp", ".gif", ".psd", ".tiff", ".exr", ".hdr", ".dds" },
            AssetType.Audio => new[] { ".wav", ".mp3", ".ogg", ".aiff", ".aif", ".mod", ".it", ".s3m", ".xm" },
            AssetType.Prefab => new[] { ".prefab" },
            AssetType.Script => new[] { ".cs", ".js", ".boo" },
            AssetType.Model => new[] { ".fbx", ".obj", ".dae", ".3ds", ".dxf", ".blend", ".ma", ".mb", ".max" },
            AssetType.Material => new[] { ".mat" },
            AssetType.Shader => new[] { ".shader", ".cginc", ".hlsl" },
            AssetType.Animation => new[] { ".anim" },
            AssetType.AnimatorController => new[] { ".controller" },
            AssetType.Scene => new[] { ".unity" },
            AssetType.Font => new[] { ".ttf", ".otf", ".dfont", ".fon" },
            AssetType.Video => new[] { ".mp4", ".mov", ".avi", ".asf", ".mpg", ".mpeg", ".mp4v", ".webm" },
            AssetType.Asset => new[] { ".asset" },
            AssetType.PhysicMaterial => new[] { ".physicmaterial" },
            AssetType.PhysicsMaterial2D => new[] { ".physicsmaterial2d" },
            AssetType.Timeline => new[] { ".playable" },
            AssetType.AudioMixer => new[] { ".mixer" },
            AssetType.ComputeShader => new[] { ".compute" },
            AssetType.GUISkin => new[] { ".guiskin" },
            AssetType.Flare => new[] { ".flare" },
            AssetType.TextAsset => new[] { ".txt", ".json", ".xml", ".csv", ".yaml", ".bytes" },
            _ => new string[0]
        };
    }
    

        /// <summary>
        /// 获取资源类型的颜色（用于UI显示）
        /// </summary>
        public static Color GetTypeColor(this AssetType assetType)
        {
            return assetType switch
            {
                AssetType.Folder => new Color(1f, 0.8f, 0.4f), // 橙色
                AssetType.Texture => new Color(0.8f, 0.4f, 1f), // 紫色
                AssetType.Audio => new Color(0.4f, 1f, 0.4f), // 绿色
                AssetType.Prefab => new Color(0.4f, 0.8f, 1f), // 蓝色
                AssetType.Script => new Color(1f, 1f, 0.4f), // 黄色
                AssetType.Model => new Color(0.8f, 0.8f, 0.8f), // 灰色
                AssetType.Material => new Color(1f, 0.4f, 0.4f), // 红色
                AssetType.Shader => new Color(1f, 0.6f, 0.8f), // 粉色
                AssetType.Animation => new Color(0.6f, 1f, 0.8f), // 青绿色
                AssetType.Scene => new Color(0.8f, 1f, 0.6f), // 浅绿色
                AssetType.Font => new Color(0.6f, 0.6f, 1f), // 浅蓝色
                AssetType.Video => new Color(1f, 0.8f, 0.6f), // 浅橙色
                _ => Color.white
            };
        }
    

        /// <summary>
        /// 判断资源类型是否为媒体文件
        /// </summary>
        public static bool IsMediaFile(this AssetType assetType)
        {
            return assetType == AssetType.Texture ||
                   assetType == AssetType.Audio ||
                   assetType == AssetType.Video;
        }

        /// <summary>
        /// 判断资源类型是否为代码文件
        /// </summary>
        public static bool IsCodeFile(this AssetType assetType)
        {
            return assetType == AssetType.Script ||
                   assetType == AssetType.Shader;
        }
        
    

        /// <summary>
        /// 获取资源类型
        /// </summary>
        public static AssetType GetAssetType(string path)
        {
            if (string.IsNullOrEmpty(path))
                return AssetType.Invalid;

            // path=path.Trim().Replace("\\", "/");
            path=path.Trim();
            // 检查是否为文件夹
            if (AssetDatabase.IsValidFolder(path))
                return AssetType.Folder;

            // 首先通过文件扩展名进行快速判断
            string extension = System.IO.Path.GetExtension(path).ToLower();
            var typeByExtension = GetAssetTypeByExtension(extension);
            if (typeByExtension != AssetType.Invalid)
                return typeByExtension;

            // 获取Unity资源类型进行详细判断
            var unityType = AssetDatabase.GetMainAssetTypeAtPath(path);
            if (unityType == null)
                return AssetType.Invalid;

            return ConvertToAssetType(unityType, path);
        }

        /// <summary>
        /// 通过文件扩展名快速获取资源类型
        /// </summary>
        private static AssetType GetAssetTypeByExtension(string extension)
        {
            foreach (AssetType assetType in Enum.GetValues(typeof(AssetType)))
            {
                if (assetType.GetFileExtensions().Contains(extension))
                {
                    return assetType;
                }
            }

            return AssetType.Invalid;
        }

        /// <summary>
        /// 将Unity类型转换为自定义AssetType枚举
        /// </summary>
        private static AssetType ConvertToAssetType(Type unityType, string path)
        {
            if (unityType == null)
                return AssetType.Invalid;

            // 纹理相关类型
            if (typeof(Cubemap).IsAssignableFrom(unityType))
                return AssetType.Cubemap;
            if (typeof(RenderTexture).IsAssignableFrom(unityType))
                return AssetType.RenderTexture;
            if (typeof(Texture).IsAssignableFrom(unityType))
                return AssetType.Texture;

            // 音频类型
            if (typeof(AudioClip).IsAssignableFrom(unityType))
                return AssetType.Audio;

            // 预制体类型
            if (typeof(GameObject).IsAssignableFrom(unityType))
                return AssetType.Prefab;

            // 网格类型
            if (typeof(Mesh).IsAssignableFrom(unityType))
                return AssetType.Mesh;

            // 地形数据
            if (typeof(TerrainData).IsAssignableFrom(unityType))
                return AssetType.TerrainData;

            // 光照数据
#if UNITY_2017_2_OR_NEWER
            if (unityType.Name == "LightingDataAsset")
                return AssetType.LightingDataAsset;
#endif

            // 物理材质
            if (typeof(PhysicMaterial).IsAssignableFrom(unityType))
                return AssetType.PhysicMaterial;
            if (typeof(PhysicsMaterial2D).IsAssignableFrom(unityType))
                return AssetType.PhysicsMaterial2D;

            // 脚本类型
            if (typeof(MonoScript).IsAssignableFrom(unityType))
                return AssetType.Script;

            // 材质球类型
            if (typeof(Material).IsAssignableFrom(unityType))
                return AssetType.Material;

            // 着色器类型
            if (typeof(Shader).IsAssignableFrom(unityType))
                return AssetType.Shader;
            if (typeof(ComputeShader).IsAssignableFrom(unityType))
                return AssetType.ComputeShader;

            // 动画类型
            if (typeof(AnimationClip).IsAssignableFrom(unityType))
                return AssetType.Animation;

            // 动画控制器类型
            if (typeof(RuntimeAnimatorController).IsAssignableFrom(unityType))
                return AssetType.AnimatorController;

            // 场景类型
            if (typeof(SceneAsset).IsAssignableFrom(unityType))
                return AssetType.Scene;

            // 字体类型
            if (typeof(Font).IsAssignableFrom(unityType))
                return AssetType.Font;

            // 视频类型
            if (typeof(UnityEngine.Video.VideoClip).IsAssignableFrom(unityType))
                return AssetType.Video;

            // 音频混合器
#if UNITY_5_0_OR_NEWER
        if (unityType.Name == "AudioMixerController")
            return AssetType.AudioMixer;
        if (unityType.Name == "AudioMixerGroupController")
            return AssetType.AudioMixerGroup;
#endif

            // GUI相关
            if (typeof(GUISkin).IsAssignableFrom(unityType))
                return AssetType.GUISkin;
            if (typeof(Flare).IsAssignableFrom(unityType))
                return AssetType.Flare;

            // 文本资源
            if (typeof(TextAsset).IsAssignableFrom(unityType))
                return AssetType.TextAsset;

            // Timeline
#if UNITY_2017_1_OR_NEWER
            if (unityType.Name.Contains("TimelineAsset") || unityType.Name.Contains("PlayableAsset"))
                return AssetType.Timeline;
#endif

            // ScriptableObject类型（放在最后，因为很多类型都继承自ScriptableObject）
            if (typeof(ScriptableObject).IsAssignableFrom(unityType))
                return AssetType.ScriptableObject;

            // 其他类型
            return AssetType.Other;
        }

        // 辅助方法：判断各种文件扩展名
        private static bool IsTextureExtension(string extension)
        {
            return AssetType.Texture.GetFileExtensions().Contains(extension);
        }

        private static bool IsAudioExtension(string extension)
        {
            return AssetType.Audio.GetFileExtensions().Contains(extension);
        }

        private static bool IsModelExtension(string extension)
        {
            return AssetType.Model.GetFileExtensions().Contains(extension);
        }

        private static bool IsFontExtension(string extension)
        {
            return AssetType.Font.GetFileExtensions().Contains(extension);
        }

        private static bool IsVideoExtension(string extension)
        {
           return AssetType.Video.GetFileExtensions().Contains(extension);
        }
        /// <summary>
        /// 按资源类型分组
        /// </summary>
        public static Dictionary<AssetType, List<string>> GroupAssetsByType(IEnumerable<string> paths)
        {
            var groups = new Dictionary<AssetType, List<string>>();
        
            // 初始化所有类型的列表
            foreach (AssetType assetType in Enum.GetValues(typeof(AssetType)))
            {
                groups[assetType] = new List<string>();
            }
        
            foreach (string path in paths)
            {
                var assetType = GetAssetType(path);
                groups[assetType].Add(path);
            }
        
            return groups;
        }        
    }
}