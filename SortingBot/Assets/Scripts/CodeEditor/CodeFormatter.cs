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
    // Formats a source code and returns the formatted colorful code in Unity's rich text format.
    public static string Format(string code, IReadOnlyList<TokenInfo> tokens) {
      Debug.Assert(!(code is null) && !(tokens is null));
      var lines = code.Split(EditorConfig.Ret);
      int lineIndex = 0;
      int charIndex = 0;
      var coloredTextBuffer = new StringBuilder();
      foreach (var token in tokens) {
        // Doesn't support multi-line tokens for now.
        Debug.Assert(token.Range.Start.Line == token.Range.End.Line);

        if (!EditorConfig.TokenColors.TryGetValue(token.Type, out string color)) {
          color = EditorConfig.DefaultTokenColor;
        }

        int startLine = token.Range.Start.Line - 1;
        int startCol = token.Range.Start.Column;
        int endCol = token.Range.End.Column;

        // Reads source code until the start line of the token is met.
        while (lineIndex < startLine) {
          while (charIndex < lines[lineIndex].Length) {
            coloredTextBuffer.Append(Escape(lines[lineIndex][charIndex]));
            charIndex++;
          }
          coloredTextBuffer.Append(EditorConfig.Ret);
          lineIndex++;
          charIndex = 0;
        }
        // Reads source code until the start col of the token is met.
        while (charIndex < startCol) {
          coloredTextBuffer.Append(Escape(lines[startLine][charIndex]));
          charIndex++;
        }
        string tokeText = lines[startLine].Substring(startCol, endCol - startCol + 1);
        string escapedTokenText = Escape(tokeText);
        coloredTextBuffer.Append($"<color={color}>{escapedTokenText}</color>");
        lineIndex = startLine;
        charIndex = endCol + 1;
      }
      // Reads the rest of source code that haven't been tokenized.
      while (lineIndex < lines.Length) {
        while (charIndex < lines[lineIndex].Length) {
          coloredTextBuffer.Append(Escape(lines[lineIndex][charIndex]));
          charIndex++;
        }
        coloredTextBuffer.Append(EditorConfig.Ret);
        lineIndex++;
        charIndex = 0;
      }
      coloredTextBuffer.Append(EditorConfig.Ret);
      return coloredTextBuffer.ToString();
    }

    // Escapes a character. Returns the character itself if it doesn't need to be escaped.
    private static string Escape(char c) {
      if (EditorConfig.CharEscapeTable.TryGetValue(c, out string escaped)) {
        return escaped;
      } else {
        return c.ToString();
      }
    }

    // Escapes a string.
    private static string Escape(string s) {
      StringBuilder buf = new StringBuilder();
      foreach (char c in s) {
        buf.Append(Escape(c));
      }
      return buf.ToString();
    }
  }
}
