using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OpalStudio.CodePreview.Editor.Core;

namespace OpalStudio.CodePreview.Editor.Highlighters
{
      sealed internal class YamlSyntaxHighlighter : BaseSyntaxHighlighter
      {
            private readonly static Dictionary<string, Regex> RegexCache = new();

            internal override void Initialize(bool isDarkTheme)
            {
                  SetColors(new Dictionary<string, string>
                  {
                        ["key"] = isDarkTheme ? "#9CDCFE" : "#0451A5",
                        ["value"] = isDarkTheme ? "#CE9178" : "#A31515",
                        ["number"] = isDarkTheme ? "#B5CEA8" : "#098658",
                        ["boolean"] = isDarkTheme ? "#569CD6" : "#0000FF",
                        ["comment"] = isDarkTheme ? "#6A9955" : "#008000",
                        ["listMarker"] = isDarkTheme ? "#DCDCAA" : "#795E26",
                        ["anchor"] = isDarkTheme ? "#C586C0" : "#9B59B6",
                        ["reference"] = isDarkTheme ? "#C586C0" : "#9B59B6",
                        ["directive"] = isDarkTheme ? "#FF6B35" : "#FF4500",
                        ["documentSeparator"] = isDarkTheme ? "#808080" : "#666666",
                        ["unitySpecial"] = isDarkTheme ? "#4EC9B0" : "#2B91AF",
                        ["multilineOperator"] = isDarkTheme ? "#FF6B35" : "#FF4500"
                  });
            }

            internal override string ProcessLine(string line, bool isInMultiLineComment)
            {
                  if (string.IsNullOrEmpty(line))
                  {
                        return line;
                  }

                  string result = line;

                  result = ApplyCommentHighlighting(result);
                  result = ApplyDocumentSeparatorHighlighting(result);
                  result = ApplyDirectiveHighlighting(result);
                  result = ApplyUnitySpecialHighlighting(result);
                  result = ApplyAnchorAndReferenceHighlighting(result);
                  result = ApplyKeyValueHighlighting(result);
                  result = ApplyListHighlighting(result);
                  result = ApplyMultilineOperatorHighlighting(result);

                  return result;
            }

            internal override HashSet<int> GetMultiLineCommentLines(string[] lines)
            {
                  var multiLineStrings = new HashSet<int>();
                  bool inMultiLineString = false;

                  for (int i = 0; i < lines.Length; i++)
                  {
                        string line = lines[i];

                        if (!inMultiLineString)
                        {
                              if ((line.Contains("|", StringComparison.OrdinalIgnoreCase) || line.Contains(">", StringComparison.OrdinalIgnoreCase)) && Regex.IsMatch(line, @":\s*[|>][-+]?\s*$"))
                              {
                                    inMultiLineString = true;
                              }
                        }
                        else
                        {
                              if (string.IsNullOrWhiteSpace(line) || line.StartsWith("  ", StringComparison.OrdinalIgnoreCase) || line.StartsWith("\t", StringComparison.OrdinalIgnoreCase))
                              {
                                    multiLineStrings.Add(i);
                              }
                              else
                              {
                                    inMultiLineString = false;
                              }
                        }
                  }

                  return multiLineStrings;
            }

            private string ApplyCommentHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"(?<![""].*?)#.*$", RegexOptions.Compiled);

                  return regex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["comment"]));
            }

            private string ApplyDocumentSeparatorHighlighting(string result)
            {
                  if (result.Trim() == "---" || result.Trim() == "...")
                  {
                        return ApplyColorTag(result, this.Colors["documentSeparator"]);
                  }

                  return result;
            }

            private string ApplyDirectiveHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"^%\w+.*$", RegexOptions.Compiled);

                  return regex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["directive"]));
            }

            private string ApplyUnitySpecialHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"\b(fileID|guid|type|m_ObjectHideFlags|m_CorrespondingSourceObject|m_PrefabInstance|m_PrefabAsset|m_GameObject|m_Enabled|m_Script):",
                        RegexOptions.Compiled);

                  return regex.Replace(result, match =>
                  {
                        string[] parts = match.Value.Split(':');

                        return ApplyColorTag(parts[0], this.Colors["unitySpecial"]) + ":";
                  });
            }

            private string ApplyAnchorAndReferenceHighlighting(string result)
            {
                  Regex anchorRegex = GetOrCreateRegex(@"&\w+", RegexOptions.Compiled);
                  result = anchorRegex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["anchor"]));

                  Regex referenceRegex = GetOrCreateRegex(@"\*\w+", RegexOptions.Compiled);
                  result = referenceRegex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["reference"]));

                  return result;
            }

            private string ApplyKeyValueHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"^(\s*)([^:\s][^:]*?):\s*(.*)$", RegexOptions.Compiled);

                  return regex.Replace(result, match =>
                  {
                        string indent = match.Groups[1].Value;
                        string key = match.Groups[2].Value;
                        string value = match.Groups[3].Value;

                        if (key.StartsWith("#", StringComparison.OrdinalIgnoreCase) || key.Trim() == "---")
                        {
                              return match.Value;
                        }

                        string processedValue = ProcessValue(value);

                        return indent + ApplyColorTag(key, this.Colors["key"]) + ": " + processedValue;
                  });
            }

            private string ProcessValue(string value)
            {
                  if (string.IsNullOrWhiteSpace(value))
                  {
                        return value;
                  }

                  value = value.Trim();

                  if (value is "true" or "false" or "null" or "~")
                  {
                        return ApplyColorTag(value, this.Colors["boolean"]);
                  }

                  if (Regex.IsMatch(value, @"^-?(\d+\.?\d*|\.\d+)([eE][+-]?\d+)?$"))
                  {
                        return ApplyColorTag(value, this.Colors["number"]);
                  }

                  if ((value.StartsWith("\"", StringComparison.OrdinalIgnoreCase) && value.EndsWith("\"", StringComparison.OrdinalIgnoreCase)) ||
                      (value.StartsWith("'", StringComparison.OrdinalIgnoreCase) && value.EndsWith("'", StringComparison.OrdinalIgnoreCase)) || !string.IsNullOrEmpty(value) && value != "|" &&
                      value != ">" && !value.StartsWith("&", StringComparison.OrdinalIgnoreCase) && !value.StartsWith("*", StringComparison.OrdinalIgnoreCase) &&
                      !value.StartsWith("#", StringComparison.OrdinalIgnoreCase))
                  {
                        return ApplyColorTag(value, this.Colors["value"]);
                  }

                  return value;
            }

            private string ApplyListHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"^(\s*)(-)\s+(.*)$", RegexOptions.Compiled);

                  return regex.Replace(result, match =>
                  {
                        string indent = match.Groups[1].Value;
                        string marker = match.Groups[2].Value;
                        string content = match.Groups[3].Value;

                        string processedContent = ProcessValue(content);

                        return indent + ApplyColorTag(marker, this.Colors["listMarker"]) + " " + processedContent;
                  });
            }

            private string ApplyMultilineOperatorHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@":\s*([|>][-+]?)(\s*#.*)?$", RegexOptions.Compiled);

                  return regex.Replace(result, match =>
                  {
                        string lineOperator = match.Groups[1].Value;
                        string comment = match.Groups[2].Value;

                        string coloredOperator = ApplyColorTag(lineOperator, this.Colors["multilineOperator"]);
                        string coloredComment = string.IsNullOrEmpty(comment) ? "" : ApplyColorTag(comment, this.Colors["comment"]);

                        return ": " + coloredOperator + coloredComment;
                  });
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