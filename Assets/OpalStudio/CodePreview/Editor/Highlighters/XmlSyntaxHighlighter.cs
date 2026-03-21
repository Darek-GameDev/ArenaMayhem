using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using OpalStudio.CodePreview.Editor.Core;

namespace OpalStudio.CodePreview.Editor.Highlighters
{
      sealed internal class XmlSyntaxHighlighter : BaseSyntaxHighlighter
      {
            private readonly static Dictionary<string, Regex> RegexCache = new();

            internal override void Initialize(bool isDarkTheme)
            {
                  SetColors(new Dictionary<string, string>
                  {
                        ["tagName"] = isDarkTheme ? "#569CD6" : "#0000FF",
                        ["attributeName"] = isDarkTheme ? "#9CDCFE" : "#FF0000",
                        ["attributeValue"] = isDarkTheme ? "#CE9178" : "#0000FF",
                        ["xmlDeclaration"] = isDarkTheme ? "#C586C0" : "#9B59B6",
                        ["comment"] = isDarkTheme ? "#6A9955" : "#008000",
                        ["cdata"] = isDarkTheme ? "#DCDCAA" : "#795E26",
                        ["bracket"] = "#808080",
                        ["text"] = isDarkTheme ? "#D4D4D4" : "#000000"
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
                        return ApplyColorTag(line, this.Colors["comment"]);
                  }

                  string result = line;

                  result = ApplyCommentHighlighting(result);
                  result = ApplyCDataHighlighting(result);
                  result = ApplyXmlDeclarationHighlighting(result);
                  result = ApplyTagHighlighting(result);

                  return result;
            }

            internal override HashSet<int> GetMultiLineCommentLines(string[] lines)
            {
                  var multiLineComments = new HashSet<int>();
                  string fullText = string.Join("\n", lines);

                  Regex commentRegex = GetOrCreateRegex(@"<!--[\s\S]*?-->", RegexOptions.Compiled);

                  foreach (Match match in commentRegex.Matches(fullText))
                  {
                        int start = fullText[..match.Index].Count(static c => c == '\n');
                        int end = fullText[..(match.Index + match.Length)].Count(static c => c == '\n');

                        for (int i = start; i <= end; i++)
                        {
                              multiLineComments.Add(i);
                        }
                  }

                  Regex cdataRegex = GetOrCreateRegex(@"<!\[CDATA\[[\s\S]*?\]\]>", RegexOptions.Compiled);

                  foreach (Match match in cdataRegex.Matches(fullText))
                  {
                        int start = fullText[..match.Index].Count(static c => c == '\n');
                        int end = fullText[..(match.Index + match.Length)].Count(static c => c == '\n');

                        for (int i = start; i <= end; i++)
                        {
                              multiLineComments.Add(i);
                        }
                  }

                  return multiLineComments;
            }

            private string ApplyCommentHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"<!--.*?-->", RegexOptions.Compiled);

                  return regex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["comment"]));
            }

            private string ApplyCDataHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"<!\[CDATA\[.*?\]\]>", RegexOptions.Compiled);

                  return regex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["cdata"]));
            }

            private string ApplyXmlDeclarationHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"<\?xml.*?\?>", RegexOptions.Compiled);

                  return regex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["xmlDeclaration"]));
            }

            private string ApplyTagHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"<(/?)(\w+(?::\w+)?)((?:\s+\w+(?::\w+)?(?:\s*=\s*(?:""[^""]*""|'[^']*'))?)*)\s*(/?)>", RegexOptions.Compiled);

                  return regex.Replace(result, match =>
                  {
                        string openSlash = match.Groups[1].Value;
                        string tagName = match.Groups[2].Value;
                        string attributes = match.Groups[3].Value;
                        string closeSlash = match.Groups[4].Value;

                        string resultTemp = ApplyColorTag("<", this.Colors["bracket"]);

                        if (!string.IsNullOrEmpty(openSlash))
                        {
                              resultTemp += ApplyColorTag(openSlash, this.Colors["bracket"]);
                        }

                        resultTemp += ApplyColorTag(tagName, this.Colors["tagName"]);

                        if (!string.IsNullOrEmpty(attributes))
                        {
                              resultTemp += ApplyAttributeHighlighting(attributes);
                        }

                        if (!string.IsNullOrEmpty(closeSlash))
                        {
                              resultTemp += ApplyColorTag(closeSlash, this.Colors["bracket"]);
                        }

                        resultTemp += ApplyColorTag(">", this.Colors["bracket"]);

                        return resultTemp;
                  });
            }

            private string ApplyAttributeHighlighting(string attributes)
            {
                  Regex regex = GetOrCreateRegex(@"(\s+)(\w+(?::\w+)?)(\s*=\s*)([""'])(.*?)\4", RegexOptions.Compiled);

                  return regex.Replace(attributes, match =>
                  {
                        string whitespace = match.Groups[1].Value;
                        string attrName = match.Groups[2].Value;
                        string equals = match.Groups[3].Value;
                        string quote = match.Groups[4].Value;
                        string attrValue = match.Groups[5].Value;

                        return whitespace + ApplyColorTag(attrName, this.Colors["attributeName"]) + equals + ApplyColorTag(quote + attrValue + quote, this.Colors["attributeValue"]);
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