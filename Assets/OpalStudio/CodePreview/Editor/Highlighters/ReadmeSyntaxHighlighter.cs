using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OpalStudio.CodePreview.Editor.Core;

namespace OpalStudio.CodePreview.Editor.Highlighters
{
      sealed internal class ReadmeSyntaxHighlighter : BaseSyntaxHighlighter
      {
            private readonly static Dictionary<string, Regex> RegexCache = new();

            internal override void Initialize(bool isDarkTheme)
            {
                  SetColors(new Dictionary<string, string>
                  {
                        ["header"] = isDarkTheme ? "#569CD6" : "#0000FF",
                        ["bold"] = isDarkTheme ? "#DCDCAA" : "#795E26",
                        ["italic"] = isDarkTheme ? "#C586C0" : "#9B59B6",
                        ["code"] = isDarkTheme ? "#CE9178" : "#A31515",
                        ["link"] = isDarkTheme ? "#4EC9B0" : "#2B91AF",
                        ["url"] = isDarkTheme ? "#4EC9B0" : "#0066CC",
                        ["listItem"] = isDarkTheme ? "#B5CEA8" : "#098658",
                        ["quote"] = isDarkTheme ? "#6A9955" : "#008000",
                        ["separator"] = "#808080",
                        ["strikethrough"] = isDarkTheme ? "#808080" : "#999999",
                        ["taskList"] = isDarkTheme ? "#FF6B35" : "#FF4500",
                        ["image"] = isDarkTheme ? "#E6C547" : "#DAA520",
                        ["githubRef"] = isDarkTheme ? "#F78166" : "#D73027",
                        ["table"] = isDarkTheme ? "#808080" : "#666666"
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
                        return ApplyCodeBlockHighlighting(line);
                  }

                  string result = line;

                  if (IsHeader(result))
                  {
                        return ApplyHeaderHighlighting(result);
                  }

                  if (IsTaskList(result))
                  {
                        return ApplyTaskListHighlighting(result);
                  }

                  if (IsNormalList(result))
                  {
                        return ApplyListHighlighting(result);
                  }

                  if (IsQuote(result))
                  {
                        return ApplyQuoteHighlighting(result);
                  }

                  if (IsSeparator(result))
                  {
                        return ApplySeparatorHighlighting(result);
                  }

                  if (IsTable(result))
                  {
                        return ApplyTableHighlighting(result);
                  }

                  result = ApplyStrikethroughHighlighting(result);
                  result = ApplyBoldHighlighting(result);
                  result = ApplyItalicHighlighting(result);
                  result = ApplyInlineCodeHighlighting(result);
                  result = ApplyImageHighlighting(result);
                  result = ApplyLinkHighlighting(result);
                  result = ApplyUrlHighlighting(result);
                  result = ApplyGithubRefsHighlighting(result);

                  return result;
            }

            internal override HashSet<int> GetMultiLineCommentLines(string[] lines)
            {
                  var codeBlocks = new HashSet<int>();
                  bool inCodeBlock = false;

                  for (int i = 0; i < lines.Length; i++)
                  {
                        string line = lines[i].Trim();

                        if (line.StartsWith("```", StringComparison.OrdinalIgnoreCase))
                        {
                              inCodeBlock = !inCodeBlock;
                              codeBlocks.Add(i);
                        }
                        else if (inCodeBlock || lines[i].Length >= 4 && lines[i].StartsWith("    ", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(lines[i][4..]))
                        {
                              codeBlocks.Add(i);
                        }
                  }

                  return codeBlocks;
            }

            private static bool IsHeader(string line) => line.TrimStart().StartsWith("#", StringComparison.OrdinalIgnoreCase);

            private static bool IsTaskList(string line) => Regex.IsMatch(line, @"^\s*[-*+]\s+\[[ xX]\]\s+");

            private static bool IsNormalList(string line) => Regex.IsMatch(line, @"^\s*([-*+]|\d+\.)\s+") && !IsTaskList(line);

            private static bool IsQuote(string line) => line.TrimStart().StartsWith(">", StringComparison.OrdinalIgnoreCase);

            private static bool IsSeparator(string line) => Regex.IsMatch(line.Trim(), @"^[-*_]{3,}$");

            private static bool IsTable(string line) => line.Contains("|", StringComparison.OrdinalIgnoreCase);

            private string ApplyCodeBlockHighlighting(string line)
            {
                  return ApplyColorTag(line, line.Trim().StartsWith("```", StringComparison.OrdinalIgnoreCase) ? this.Colors["separator"] : this.Colors["code"]);
            }

            private string ApplyHeaderHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"^(\s*)(#{1,6}\s+.*)$", RegexOptions.Compiled);

                  return regex.Replace(result, match =>
                  {
                        string indent = match.Groups[1].Value;
                        string headerContent = match.Groups[2].Value;

                        return indent + ApplyColorTag(headerContent, this.Colors["header"]);
                  });
            }

            private string ApplyTaskListHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"^(\s*)([-*+]\s+\[[ xX]\]\s+.*)$", RegexOptions.Compiled);

                  return regex.Replace(result, match =>
                  {
                        string indent = match.Groups[1].Value;
                        string taskContent = match.Groups[2].Value;

                        return indent + ApplyColorTag(taskContent, this.Colors["taskList"]);
                  });
            }

            private string ApplyListHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"^(\s*)([-*+]|\d+\.)\s+(.*)$", RegexOptions.Compiled);

                  return regex.Replace(result, match =>
                  {
                        string indent = match.Groups[1].Value;
                        string marker = match.Groups[2].Value;
                        string content = match.Groups[3].Value;

                        return indent + ApplyColorTag(marker, this.Colors["listItem"]) + " " + content;
                  });
            }

            private string ApplyQuoteHighlighting(string result)
            {
                  return ApplyColorTag(result, this.Colors["quote"]);
            }

            private string ApplySeparatorHighlighting(string result)
            {
                  return ApplyColorTag(result, this.Colors["separator"]);
            }

            private string ApplyTableHighlighting(string result)
            {
                  return result.Replace("|", ApplyColorTag("|", this.Colors["table"]), StringComparison.OrdinalIgnoreCase);
            }

            private string ApplyStrikethroughHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"~~([^~\n]+)~~", RegexOptions.Compiled);

                  return regex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["strikethrough"]));
            }

            private string ApplyBoldHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"\*\*([^*\n]+)\*\*", RegexOptions.Compiled);

                  return regex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["bold"]));
            }

            private string ApplyItalicHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"(?<!\*)\*([^*\n]+)\*(?!\*)", RegexOptions.Compiled);

                  return regex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["italic"]));
            }

            private string ApplyInlineCodeHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"`([^`\n]+)`", RegexOptions.Compiled);

                  return regex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["code"]));
            }

            private string ApplyImageHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"!\[([^\]]*)\]\([^)]+\)", RegexOptions.Compiled);

                  return regex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["image"]));
            }

            private string ApplyLinkHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"(?<!!)\[([^\]]+)\]\([^)]+\)", RegexOptions.Compiled);

                  return regex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["link"]));
            }

            private string ApplyUrlHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"\bhttps?://[^\s<>""]+", RegexOptions.Compiled);

                  return regex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["url"]));
            }

            private string ApplyGithubRefsHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"(?:\B#\d+|\B@\w+)", RegexOptions.Compiled);

                  return regex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["githubRef"]));
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