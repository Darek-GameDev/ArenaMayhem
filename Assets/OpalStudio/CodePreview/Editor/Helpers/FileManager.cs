using System;
using System.IO;
using UnityEditor;
using FileInfo = OpalStudio.CodePreview.Editor.Data.FileInfo;
using Object = UnityEngine.Object;

namespace OpalStudio.CodePreview.Editor.Helpers
{
      sealed internal class FileManager
      {
            private string _filePath;
            private FileInfo _fileInfo;
            private string[] _originalLines;
            private string[] _displayLines;

            internal bool CheckForChanges(Object asset)
            {
                  string path = AssetDatabase.GetAssetPath(asset);

                  if (string.IsNullOrEmpty(path) || !File.Exists(path))
                  {
                        return false;
                  }

                  DateTime lastWrite = File.GetLastWriteTime(path);

                  if (_filePath == path && lastWrite == _fileInfo?.LastModifiedTime)
                  {
                        return false;
                  }

                  return true;
            }

            internal void LoadScript(MonoScript script)
            {
                  _filePath = AssetDatabase.GetAssetPath(script);

                  if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
                  {
                        throw new FileNotFoundException($"Script file not found: {_filePath}");
                  }

                  try
                  {
                        string rawContent = File.ReadAllText(_filePath);
                        _originalLines = rawContent.Split('\n');
                        _displayLines = _originalLines;

                        _fileInfo = CalculateFileInfo(_originalLines);
                        _fileInfo.LastModifiedTime = File.GetLastWriteTime(_filePath);
                        _fileInfo.FileSize = new System.IO.FileInfo(_filePath).Length;
                  }
                  catch (Exception e)
                  {
                        throw new Exception($"Error reading script: {e.Message}", e);
                  }
            }

            internal void LoadFromContent(string[] lines, string filePath)
            {
                  _filePath = filePath;
                  _originalLines = lines;
                  _displayLines = lines;

                  _fileInfo = CalculateFileInfo(_originalLines);

                  if (!string.IsNullOrEmpty(_filePath) && File.Exists(_filePath))
                  {
                        _fileInfo.LastModifiedTime = File.GetLastWriteTime(_filePath);
                        _fileInfo.FileSize = new System.IO.FileInfo(_filePath).Length;
                  }
                  else
                  {
                        _fileInfo.LastModifiedTime = DateTimeOffset.UtcNow;
                        _fileInfo.FileSize = string.Join("\n", lines).Length;
                  }
            }

            internal void SetLimitedLines(string[] limitedLines)
            {
                  _displayLines = limitedLines;
            }

            internal string[] GetLines() => _originalLines;

            internal string[] GetDisplayLines() => _displayLines;

            internal string GetFilePath() => _filePath;

            internal FileInfo GetFileInfo() => _fileInfo;

            internal bool HasContent() => _originalLines is { Length: > 0 };

            private static FileInfo CalculateFileInfo(string[] lines)
            {
                  var info = new FileInfo
                  {
                        TotalLines = lines.Length
                  };

                  foreach (string line in lines)
                  {
                        info.TotalChars += line.Length;
                        info.TotalWords += line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length;

                        string trimmed = line.TrimStart();

                        if (trimmed.StartsWith("//", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("/*", StringComparison.OrdinalIgnoreCase) ||
                            trimmed.StartsWith("*", StringComparison.OrdinalIgnoreCase) || trimmed.Contains("*/", StringComparison.OrdinalIgnoreCase))
                        {
                              info.CommentLines++;
                        }
                  }

                  return info;
            }
      }
}