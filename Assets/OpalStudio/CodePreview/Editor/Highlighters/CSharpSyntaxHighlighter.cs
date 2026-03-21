using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using OpalStudio.CodePreview.Editor.Core;

namespace OpalStudio.CodePreview.Editor.Highlighters
{
      sealed internal class CSharpSyntaxHighlighter : BaseSyntaxHighlighter
      {
            private readonly static Dictionary<string, Regex> RegexCache = new();

            private readonly static string[] Keywords =
            {
                  "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
                  "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
                  "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
                  "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
                  "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
                  "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
                  "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
                  "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual",
                  "void", "volatile", "while", "var", "async", "await", "dynamic", "yield", "where",
                  "select", "from", "group", "into", "orderby", "join", "let", "ascending", "descending",
                  "on", "equals", "by", "value", "global", "partial", "when", "add", "remove", "get", "set", "init"
            };

            private readonly static string[] UnityTypes =
            {
                  "MonoBehaviour", "Transform", "GameObject", "Component", "Rigidbody", "Rigidbody2D",
                  "Collider", "Collider2D", "Renderer", "Camera", "Light", "AudioSource", "Animation",
                  "Animator", "Canvas", "RectTransform", "Image", "Text", "Button", "Vector2", "Vector3",
                  "Vector4", "Quaternion", "Color", "Color32", "Mathf", "Time", "Input", "Physics",
                  "Physics2D", "Random", "Debug", "ScriptableObject", "SerializeField", "Range", "Header",
                  "Space", "Tooltip", "System.Serializable", "TextMesh", "Mesh", "Material", "Texture",
                  "Texture2D", "Sprite", "AudioClip", "AnimationClip", "RuntimeAnimatorController", "ParticleSystem"
            };

            private readonly static HashSet<string> KeywordSet = new(Keywords, StringComparer.Ordinal);
            private readonly static HashSet<string> KeywordSetLower = new(Keywords, StringComparer.OrdinalIgnoreCase);
            private readonly static HashSet<string> UnityTypeSet = new(UnityTypes, StringComparer.Ordinal);

            internal override void Initialize(bool isDarkTheme)
            {
                  SetColors(new Dictionary<string, string>
                  {
                        ["keyword"] = isDarkTheme ? "#569CD6" : "#0000FF",
                        ["comment"] = isDarkTheme ? "#6A9955" : "#008000",
                        ["string"] = isDarkTheme ? "#CE9178" : "#A31515",
                        ["unityType"] = isDarkTheme ? "#4EC9B0" : "#2B91AF",
                        ["number"] = isDarkTheme ? "#B5CEA8" : "#098658",
                        ["preprocessor"] = isDarkTheme ? "#C586C0" : "#9B59B6",
                        ["attribute"] = isDarkTheme ? "#FFD700" : "#FF8C00",
                        ["method"] = isDarkTheme ? "#DCDCAA" : "#795E26",
                        ["customType"] = isDarkTheme ? "#4EC9B0" : "#2B91AF"
                  });
            }

            internal override string ProcessLine(string line, bool isInMultiLineComment)
            {
                  if (string.IsNullOrEmpty(line))
                  {
                        return line;
                  }

                  if (isInMultiLineComment)
                  {
                        return ApplyColorTag(WrapAngleBrackets(line), Colors["comment"]);
                  }

                  string trimmed = line.TrimStart();

                  if (trimmed.StartsWith("//", StringComparison.Ordinal))
                  {
                        return ApplyColorTag(WrapAngleBrackets(line), Colors["comment"]);
                  }

                  if (trimmed.StartsWith("#", StringComparison.Ordinal))
                  {
                        return ApplyColorTag(WrapAngleBrackets(line), Colors["preprocessor"]);
                  }

                  Match usingMatch = Regex.Match(line, @"^(\s*)using\s+([\w\.]+);");

                  if (usingMatch.Success)
                  {
                        return usingMatch.Groups[1].Value + ApplyColorTag("using", Colors["keyword"]) + " " + ApplyColorTag(usingMatch.Groups[2].Value, Colors["customType"]) + ";";
                  }

                  return TokenizeAndColor(line);
            }

            internal override HashSet<int> GetMultiLineCommentLines(string[] lines)
            {
                  var result = new HashSet<int>();
                  string full = string.Join("\n", lines);
                  Regex regex = GetOrCreateRegex(@"/\*[\s\S]*?\*/", RegexOptions.Compiled);

                  foreach (Match match in regex.Matches(full))
                  {
                        int start = CountNewlines(full, match.Index);
                        int end = CountNewlines(full, match.Index + match.Length);

                        for (int i = start; i <= end; i++)
                        {
                              result.Add(i);
                        }
                  }

                  return result;
            }

            private string TokenizeAndColor(string line)
            {
                  var spans = new List<(int start, int end, string output)>();

                  Match cm = Regex.Match(line, "//.*$");

                  if (cm.Success)
                  {
                        spans.Add((cm.Index, cm.Index + cm.Length, ApplyColorTag(WrapAngleBrackets(cm.Value), Colors["comment"])));
                  }

                  foreach (Match m in Regex.Matches(line, "\"([^\"\\\\]*(\\\\.[^\"\\\\]*)*)\""))
                  {
                        if (!OverlapsAny(spans, m.Index, m.Index + m.Length))
                        {
                              spans.Add((m.Index, m.Index + m.Length, ApplyColorTag(WrapAngleBrackets(m.Value), Colors["string"])));
                        }
                  }

                  foreach (Match m in Regex.Matches(line, @"\[[\w\.\(\),\s=""]+\]"))
                  {
                        if (!OverlapsAny(spans, m.Index, m.Index + m.Length))
                        {
                              spans.Add((m.Index, m.Index + m.Length, ApplyColorTag(WrapAngleBrackets(m.Value), Colors["attribute"])));
                        }
                  }

                  spans.Sort(static (a, b) => a.start.CompareTo(b.start));

                  var sb = new StringBuilder();
                  int pos = 0;

                  foreach ((int start, int end, string output) in spans)
                  {
                        if (pos < start)
                        {
                              sb.Append(ColorizeRawSegment(line[pos..start]));
                        }

                        sb.Append(output);
                        pos = end;
                  }

                  if (pos < line.Length)
                  {
                        sb.Append(ColorizeRawSegment(line[pos..]));
                  }

                  return sb.ToString();
            }

            private string ColorizeRawSegment(string raw)
            {
                  if (string.IsNullOrEmpty(raw))
                  {
                        return raw;
                  }

                  var sb = new StringBuilder(raw.Length * 2);
                  int i = 0;

                  while (i < raw.Length)
                  {
                        char c = raw[i];

                        if (c is '<' or '>')
                        {
                              sb.Append("<noparse>");
                              sb.Append(c);
                              sb.Append("</noparse>");
                              i++;

                              continue;
                        }

                        Match numMatch = Regex.Match(raw[i..], @"^\d+\.?\d*[fFdDmMlLuU]?");

                        if (numMatch.Success && numMatch.Length > 0)
                        {
                              sb.Append(ApplyColorTag(numMatch.Value, Colors["number"]));
                              i += numMatch.Length;

                              continue;
                        }

                        Match identMatch = Regex.Match(raw[i..], "^[A-Za-z_][A-Za-z0-9_]*");

                        if (identMatch.Success)
                        {
                              string word = identMatch.Value;
                              int afterIdx = i + word.Length;

                              int j = afterIdx;

                              while (j < raw.Length && raw[j] == ' ')
                              {
                                    j++;
                              }

                              bool followedByParen = j < raw.Length && raw[j] == '(';

                              sb.Append(ClassifyIdentifier(word, followedByParen));
                              i = afterIdx;

                              continue;
                        }

                        sb.Append(c);
                        i++;
                  }

                  return sb.ToString();
            }

            private string ClassifyIdentifier(string word, bool followedByParen)
            {
                  if (KeywordSet.Contains(word))
                  {
                        return ApplyColorTag(word, Colors["keyword"]);
                  }

                  if (UnityTypeSet.Contains(word))
                  {
                        return ApplyColorTag(word, Colors["unityType"]);
                  }

                  if (followedByParen && char.IsUpper(word[0]))
                  {
                        return ApplyColorTag(word, Colors["method"]);
                  }

                  if (char.IsUpper(word[0]) && !KeywordSetLower.Contains(word))
                  {
                        return ApplyColorTag(word, Colors["customType"]);
                  }

                  return word;
            }

            private static string WrapAngleBrackets(string text)
            {
                  if (text.IndexOf('<') < 0 && text.IndexOf('>') < 0)
                  {
                        return text;
                  }

                  var sb = new StringBuilder(text.Length + 32);

                  foreach (char c in text)
                  {
                        if (c is '<' or '>')
                        {
                              sb.Append("<noparse>");
                              sb.Append(c);
                              sb.Append("</noparse>");
                        }
                        else
                        {
                              sb.Append(c);
                        }
                  }

                  return sb.ToString();
            }

            private static bool OverlapsAny(List<(int start, int end, string output)> spans, int start, int end)
            {
                  foreach ((int start, int end, string output) s in spans)
                  {
                        if (start < s.end && end > s.start)
                        {
                              return true;
                        }
                  }

                  return false;
            }

            private static int CountNewlines(string text, int upTo)
            {
                  int count = 0;

                  for (int i = 0; i < upTo && i < text.Length; i++)
                  {
                        if (text[i] == '\n')
                        {
                              count++;
                        }
                  }

                  return count;
            }

            private static Regex GetOrCreateRegex(string pattern, RegexOptions options)
            {
                  string key = $"{pattern}_{options}";

                  if (!RegexCache.TryGetValue(key, out Regex regex))
                  {
                        regex = new Regex(pattern, options);
                        RegexCache[key] = regex;
                  }

                  return regex;
            }
      }
}