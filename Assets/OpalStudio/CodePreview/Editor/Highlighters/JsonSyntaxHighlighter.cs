using System.Collections.Generic;
using System.Text.RegularExpressions;
using OpalStudio.CodePreview.Editor.Core;

namespace OpalStudio.CodePreview.Editor.Highlighters
{
      sealed internal class JsonSyntaxHighlighter : BaseSyntaxHighlighter
      {
            private readonly static Dictionary<string, Regex> RegexCache = new();

            internal override void Initialize(bool isDarkTheme)
            {
                  SetColors(new Dictionary<string, string>
                  {
                        ["property"] = isDarkTheme ? "#9CDCFE" : "#0451A5",
                        ["string"] = isDarkTheme ? "#CE9178" : "#A31515",
                        ["number"] = isDarkTheme ? "#B5CEA8" : "#098658",
                        ["boolean"] = isDarkTheme ? "#569CD6" : "#0000FF",
                        ["null"] = isDarkTheme ? "#569CD6" : "#0000FF",
                        ["bracket"] = isDarkTheme ? "#FFD700" : "#DAA520",
                        ["punctuation"] = isDarkTheme ? "#D4D4D4" : "#000000"
                  });
            }

            internal override string ProcessLine(string line, bool isInMultiLineComment)
            {
                  if (string.IsNullOrEmpty(line))
                  {
                        return line;
                  }

                  string result = line;

                  result = ApplyStringAndPropertyHighlighting(result);
                  result = ApplyNumberHighlighting(result);
                  result = ApplyBooleanAndNullHighlighting(result);
                  result = ApplyStructuralHighlighting(result);

                  return result;
            }

            internal override HashSet<int> GetMultiLineCommentLines(string[] lines)
            {
                  return new HashSet<int>();
            }

            private string ApplyStringAndPropertyHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex("\"([^\"\\\\]*(\\\\.[^\"\\\\]*)*)\"(\\s*:)?", RegexOptions.Compiled);

                  return regex.Replace(result, match =>
                  {
                        string quotedContent = match.Groups[1].Value;
                        string fullQuote = $"\"{quotedContent}\"";
                        bool isProperty = !string.IsNullOrEmpty(match.Groups[3].Value);

                        return isProperty ? ApplyColorTag(fullQuote, this.Colors["property"]) + match.Groups[3].Value : ApplyColorTag(fullQuote, this.Colors["string"]);
                  });
            }

            private string ApplyNumberHighlighting(string result)
            {
                  Regex regex = GetOrCreateRegex(@"\b-?\d+\.?\d*([eE][+-]?\d+)?\b", RegexOptions.Compiled);

                  return regex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["number"]));
            }

            private string ApplyBooleanAndNullHighlighting(string result)
            {
                  Regex boolRegex = GetOrCreateRegex(@"\b(true|false)\b", RegexOptions.Compiled);
                  result = boolRegex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["boolean"]));

                  Regex nullRegex = GetOrCreateRegex(@"\bnull\b", RegexOptions.Compiled);
                  result = nullRegex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["null"]));

                  return result;
            }

            private string ApplyStructuralHighlighting(string result)
            {
                  Regex bracketRegex = GetOrCreateRegex(@"[\{\}\[\]]", RegexOptions.Compiled);
                  result = bracketRegex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["bracket"]));

                  Regex punctRegex = GetOrCreateRegex("[,:;]", RegexOptions.Compiled);
                  result = punctRegex.Replace(result, match => ApplyColorTag(match.Value, this.Colors["punctuation"]));

                  return result;
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