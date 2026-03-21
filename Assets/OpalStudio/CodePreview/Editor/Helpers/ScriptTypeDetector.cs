using System;
using System.Collections.Generic;
using System.IO;
using OpalStudio.CodePreview.Editor.Data;

namespace OpalStudio.CodePreview.Editor.Helpers
{
      internal static class ScriptTypeDetector
      {
            private readonly static Dictionary<string, ScriptType> ExtensionMap = new()
            {
                  { ".cs", ScriptType.CSharp },
                  { ".json", ScriptType.Json },
                  { ".xml", ScriptType.XML },
                  { ".yml", ScriptType.Yaml },
                  { ".yaml", ScriptType.Yaml },
            };

            private readonly static HashSet<string> ReadmeNames = new()
            {
                  "readme", "README", "Readme", "ReadMe", "README.md", "readme.md",
                  "Readme.md", "README.txt", "readme.txt", "Readme.txt"
            };

            internal static ScriptType DetectType(string filePath)
            {
                  if (string.IsNullOrEmpty(filePath))
                  {
                        return ScriptType.Unknown;
                  }

                  string fileName = Path.GetFileName(filePath);
                  string extension = Path.GetExtension(filePath).ToLower();

                  if (IsReadmeFile(fileName))
                  {
                        return ScriptType.Readme;
                  }

                  return ExtensionMap.GetValueOrDefault(extension, ScriptType.Unknown);
            }

            private static bool IsReadmeFile(string fileName)
            {
                  if (string.IsNullOrEmpty(fileName))
                  {
                        return false;
                  }

                  if (ReadmeNames.Contains(fileName))
                  {
                        return true;
                  }

                  string lowerName = fileName.ToLower();

                  return lowerName.StartsWith("readme", StringComparison.OrdinalIgnoreCase) && (lowerName == "readme" || lowerName.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ||
                                                                                                lowerName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));
            }
      }
}