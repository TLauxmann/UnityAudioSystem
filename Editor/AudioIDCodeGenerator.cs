using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;
using System.Linq;

namespace Gaudio.Editor
{
    public static class AudioIDCodeGenerator
    {
        private const string DefaultOutputPath = "Assets/Audio/Generated";
        private const string DefaultNamespace = "Gaudio.IDs";

        public static void GenerateAllCategories()
        {
            var categories = FindAllCategories();

            if (categories.Length == 0)
            {
                Debug.LogWarning("No AudioIDCategory assets found in project!");
                return;
            }

            EnsureDirectoryExists(DefaultOutputPath);

            int generatedCount = 0;
            foreach (var category in categories)
            {
                GenerateCodeForCategory(category);
                generatedCount++;
            }

            AssetDatabase.Refresh();
            Debug.Log($"Generated code for {generatedCount} audio categories in '{DefaultOutputPath}'");
        }

        public static void GenerateCodeForCategory(AudioIDCategory category)
        {
            if (category == null || category.Entries.Count == 0)
                return;

            string className = SanitizeClassName(category.CategoryName);
            string filePath = Path.Combine(DefaultOutputPath, $"{className}.cs");

            StringBuilder sb = new StringBuilder();

            sb.AppendLine("// AUTO-GENERATED CODE - DO NOT MODIFY MANUALLY");
            sb.AppendLine($"// Generated from: {category.name}");
            sb.AppendLine($"// Generation time: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();
            sb.AppendLine($"namespace {DefaultNamespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public static class {className}");
            sb.AppendLine("    {");

            foreach (var entry in category.Entries.OrderBy(e => e.FieldName))
            {
                sb.AppendLine($"        public const string {entry.FieldName} = \"{entry.IDValue}\";");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString());
        }

        public static AudioIDCategory[] FindAllCategories()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(AudioIDCategory)}");
            return guids.Select(guid => AssetDatabase.LoadAssetAtPath<AudioIDCategory>(AssetDatabase.GUIDToAssetPath(guid)))
                        .Where(c => c != null)
                        .ToArray();
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static string SanitizeClassName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "AudioIDs";

            string sanitized = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

            if (char.IsDigit(sanitized[0]))
                sanitized = "_" + sanitized;

            return sanitized;
        }
    }
}
