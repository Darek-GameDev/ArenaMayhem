using System.Collections.Generic;
using System.Text.RegularExpressions;
using OpalStudio.CodePreview.Editor.Core;
using OpalStudio.CodePreview.Editor.Data;
using OpalStudio.CodePreview.Editor.Settings;
using UnityEditor;

namespace OpalStudio.CodePreview.Editor.Highlighters
{
      sealed internal class SyntaxHighlighter
      {
            private readonly Dictionary<ScriptType, BaseSyntaxHighlighter> _highlighters = new();

            private string[] _processedLines = System.Array.Empty<string>();

            private string[] _currentLines;
            private ScriptType _currentScriptType;
            private HashSet<int> _searchResults = new();
            private string _searchQuery = "";

            internal SyntaxHighlighter()
            {
                  _highlighters[ScriptType.CSharp] = new CSharpSyntaxHighlighter();
                  _highlighters[ScriptType.Json] = new JsonSyntaxHighlighter();
                  _highlighters[ScriptType.XML] = new XmlSyntaxHighlighter();
                  _highlighters[ScriptType.Readme] = new ReadmeSyntaxHighlighter();
                  _highlighters[ScriptType.Yaml] = new YamlSyntaxHighlighter();
            }

            internal void ProcessContent(string[] lines, ScriptType scriptType, PreviewSettings settings)
            {
                  if (lines == null || lines.Length == 0)
                  {
                        _processedLines = System.Array.Empty<string>();

                        return;
                  }

                  _currentLines = lines;
                  _currentScriptType = scriptType;

                  if (!settings.ShouldUseSyntaxHighlighting(lines.Length))
                  {
                        _processedLines = BuildPlainLines(lines, settings);

                        return;
                  }

                  if (!_highlighters.TryGetValue(scriptType, out BaseSyntaxHighlighter highlighter))
                  {
                        _processedLines = BuildPlainLines(lines, settings);

                        return;
                  }

                  highlighter.Initialize(settings.IsDarkTheme);
                  HashSet<int> multiLineComments = highlighter.GetMultiLineCommentLines(lines);

                  _processedLines = new string[lines.Length];

                  for (int i = 0; i < lines.Length; i++)
                  {
                        string line = lines[i].TrimEnd('\r');
                        bool isInMultiLine = multiLineComments.Contains(i);
                        string processedLine = highlighter.ProcessLine(line, isInMultiLine);

                        processedLine = ApplySearchHighlighting(processedLine, i);
                        processedLine = ProcessLineNumbers(processedLine, i, settings.ShowLineNumbers, lines.Length);

                        _processedLines[i] = processedLine;
                  }
            }

            private static string[] BuildPlainLines(string[] lines, PreviewSettings settings)
            {
                  string[] result = new string[lines.Length];

                  for (int i = 0; i < lines.Length; i++)
                  {
                        result[i] = ProcessLineNumbers(lines[i], i, settings.ShowLineNumbers, lines.Length);
                  }

                  return result;
            }

            private static string ProcessLineNumbers(string line, int lineIndex, bool showLineNumbers, int totalLines)
            {
                  if (!showLineNumbers)
                  {
                        return line;
                  }

                  string lineNumber = (lineIndex + 1).ToString().PadLeft(totalLines.ToString().Length);

                  return $"<color=#808080>{lineNumber}</color>  {line}";
            }

            private string ApplySearchHighlighting(string line, int lineIndex)
            {
                  if (string.IsNullOrEmpty(_searchQuery) || !_searchResults.Contains(lineIndex))
                  {
                        return line;
                  }

                  bool isDark = EditorGUIUtility.isProSkin;
                  string highlightColor = isDark ? "#FFEB3B" : "#FFD700";
                  const string textColor = "#000000";
                  string escapedTerm = Regex.Escape(_searchQuery);

                  return Regex.Replace(line, escapedTerm, $"<mark={highlightColor}><color={textColor}><b>$0</b></color></mark>", RegexOptions.IgnoreCase);
            }

            internal void UpdateSearchHighlighting(string searchQuery, HashSet<int> searchResults)
            {
                  _searchQuery = searchQuery;
                  _searchResults = searchResults;

                  if (_currentLines != null)
                  {
                        var settings = new PreviewSettings();
                        ProcessContent(_currentLines, _currentScriptType, settings);
                  }
            }

            internal string[] GetProcessedLines() => _processedLines;

            internal void SetErrorContent(string errorMessage)
            {
                  _processedLines = new[] { $"<color=red>{errorMessage}</color>" };
            }
      }
}