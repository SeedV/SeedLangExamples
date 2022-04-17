// Copyright 2021-2022 The SeedV Lab.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CodeEditor {
  public static class CodeFomatter {
    private const string _defaultColor = "#666";

    // TODO: Complete the color list once the SeedLang plugin is ready.
    private static readonly Dictionary<TokenType, string> _tokenColors =
        new Dictionary<TokenType, string>() {
          [TokenType.Label] = "#09F",
          [TokenType.Number] = "#0C6",
        };

    // Formats a source code and returns the formatted colorful code in Unity's rich text format.
    public static string Format(string code, IReadOnlyList<TokenInfo> tokens) {
      if (string.IsNullOrEmpty(code.TrimEnd())) {
        return "";
      }
      var lines = code.Split('\n');
      int lastLineIndex = 0;
      int currentCol = 0;
      var coloredTextBuffer = new StringBuilder();
      foreach (var token in tokens)
      {
        if (!_tokenColors.TryGetValue(token.Type, out string color)) {
          color = _defaultColor;
        }
        int currentLineIndex = token.Range.Start.Line - 1;
        int startCol = token.Range.Start.Column;
        int endCol = token.Range.End.Column;
        // Reads source code until the start line of the token is met.
        while (lastLineIndex < currentLineIndex)
        {
          while (currentCol < lines[lastLineIndex].Length)
          {
            coloredTextBuffer.Append(lines[lastLineIndex][currentCol]);
            currentCol++;
          }
          coloredTextBuffer.Append('\n');
          lastLineIndex++;
          currentCol = 0;
        }
        // Reads source code until the start col of the token is met.
        while (currentCol < startCol)
        {
          coloredTextBuffer.Append(lines[currentLineIndex][currentCol]);
          currentCol++;
        }
        string tokeText = lines[currentLineIndex].Substring(startCol, endCol - startCol + 1);
        // "<" and ">" must be escaped, otherwise they will be treated as Unity's rich text
        // formatters.
        string escapedTokenText = ReplaceMultipleChars(tokeText, new Dictionary<char, string>()
        {
          ['<'] = "<noparse><</noparse>",
          ['>'] = "<noparse>></noparse>",
        });
        coloredTextBuffer.Append($"<color={color}>{escapedTokenText}</color>");
        lastLineIndex = currentLineIndex;
        currentCol = endCol + 1;
      }
      coloredTextBuffer.Append('\n');
      return coloredTextBuffer.ToString();
    }

    // TODO: Need to check character of the whole code, instead of checking only the parsed tokens.
    private static string ReplaceMultipleChars(
        string src, IReadOnlyDictionary<char, string> subStringToReplacementMap) {
      var dest = new StringBuilder();
      foreach (char c in src) {
        if (subStringToReplacementMap.ContainsKey(c)) {
          dest.Append(subStringToReplacementMap[c]);
        } else {
          dest.Append(c);
        }
      }
      return dest.ToString();
    }
  }
}
