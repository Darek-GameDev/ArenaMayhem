using System.Collections.Generic;

namespace OpalStudio.CodePreview.Editor.Core
{
      internal abstract class BaseSyntaxHighlighter
      {
            protected Dictionary<string, string> Colors { get; private set; }

            internal abstract void Initialize(bool isDarkTheme);

            internal abstract string ProcessLine(string line, bool isInMultiLineComment);

            internal abstract HashSet<int> GetMultiLineCommentLines(string[] lines);

            protected void SetColors(Dictionary<string, string> colors)
            {
                  Colors = colors;
            }

            protected static string ApplyColorTag(string text, string color)
            {
                  return $"<color={color}>{text}</color>";
            }
      }
}