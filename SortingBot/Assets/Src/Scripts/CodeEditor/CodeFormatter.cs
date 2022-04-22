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

using SeedLang.Common;

namespace CodeEditor {
  public static class CodeFomatter {
    // Formats source code based on the following rules:
    //
    // 1. Inserts an indention to the caretPos if needed.
    // 2. If tabSize is not 0, use the string to replace all the tab characters in the code.
    // 3. Color the token that are parsed from the code, with Unity's rich text format, escaping
    //    special characters if necessary.
    //
    // To achieve a good performance, we apply all above rules in a single parsing pass.
    //
    // Output arguments:
    //
    // - formatted: The result of applying rule 1 and 2 to the input.
    // - changed: True if formatted is different from the original code.
    // - newCaretPos: The new caret pos if formatted is different from the original code.
    // - formattedAndColored: The result of applying rule 1, 2, and 3 to the input.
    public static void FormatAndColor(string code,
                                      int caretPos,
                                      string indention,
                                      int tabSize,
                                      IReadOnlyList<TokenInfo> tokens,
                                      out string formatted,
                                      out bool changed,
                                      out int newCaretPos,
                                      out string formattedAndColored) {
      Debug.Assert(!(code is null));

      int index = 0;
      int line = 1;
      int col = 0;
      int colInFormatted = 0;
      int tokenIndex = 0;

      changed = false;
      newCaretPos = caretPos;

      var formattedBuffer = new StringBuilder();
      var formattedAndColoredBuffer = new StringBuilder();

      // Applies all the conversion/formatting rules in a single parsing pass.
      while (index < code.Length) {
        char c = code[index];
        if (tabSize > 0 && c == EditorConfig.Tab) {
          // Converts tab to spaces if needed.
          changed = true;
          string spaces = TabToSpaces(colInFormatted, tabSize);
          formattedBuffer.Append(spaces);
          formattedAndColoredBuffer.Append(spaces);
          if (index < caretPos) {
            newCaretPos += spaces.Length - 1;
          }
          index++;
          col++;
          colInFormatted += spaces.Length;
        } else if (c == EditorConfig.Ret) {
          // Starts a new line.
          formattedBuffer.Append(c);
          formattedAndColoredBuffer.Append(c);
          index++;
          line++;
          col = 0;
          colInFormatted = 0;
        } else if (tokenIndex < tokens.Count &&
                   line == tokens[tokenIndex].Range.Start.Line &&
                   col == tokens[tokenIndex].Range.Start.Column) {
          // The next token is met. Outputs the original token to formatted, and outputs colored
          // token to formattedAndColored.

          // Doesn't support multi-line tokens for now.
          Debug.Assert(tokens[tokenIndex].Range.Start.Line == tokens[tokenIndex].Range.End.Line);

          string tokenColor = EditorConfig.DefaultTokenColor;
          if (EditorConfig.TokenColors.TryGetValue(tokens[tokenIndex].Type, out string color)) {
            tokenColor = color;
          }
          formattedAndColoredBuffer.Append($"<{tokenColor}>");
          for (int i = tokens[tokenIndex].Range.Start.Column;
               i <= tokens[tokenIndex].Range.End.Column;
               i++) {
            c = code[index];
            formattedBuffer.Append(c);
            formattedAndColoredBuffer.Append(c);
            index++;
            col++;
            colInFormatted++;
          }
          formattedAndColoredBuffer.Append($"</color>");
          tokenIndex++;
        } else {
          // For a non-token character, copies it to both formatted and formattedAndColored.
          // formattedAndColored requires that special characters such as "<" and ">" must be
          // escaped.
          formattedBuffer.Append(c);
          string escaped = Escape(c);
          formattedAndColoredBuffer.Append(escaped);
          index++;
          col++;
          colInFormatted++;
        }
        if (index == caretPos) {
          // If the current caret pos is met and an auto-indention string is required, copies the
          // indention string to both formatted and formattedAndColored.
          if (!(indention is null)) {
            changed = true;
            string converted = TabToSpacesInString(indention, tabSize);
            formattedBuffer.Append(converted);
            formattedAndColoredBuffer.Append(converted);
            newCaretPos += converted.Length;
            colInFormatted += converted.Length;
          }
        }
      }
      formatted = formattedBuffer.ToString();
      formattedAndColored = formattedAndColoredBuffer.ToString();
    }

    private static string Escape(char c) {
      if (EditorConfig.CharEscapeTable.TryGetValue(c, out string escaped)) {
        return escaped;
      } else {
        return c.ToString();
      }
    }

    // Converts a tab located at col to spaces.
    private static string TabToSpaces(int col, int tabSize) {
      Debug.Assert(tabSize > 0);
      // Aligns the current tab to the next n * tabSize col of the current line.
      int n = tabSize - col % tabSize;
      return new string(EditorConfig.Space, n);
    }

    // Converts all the tabs in a text line to spaces. The string line must not contain Ret
    // characters.
    private static string TabToSpacesInString(string line, int tabSize) {
      if (tabSize <= 0) {
        return line;
      }
      StringBuilder buf = new StringBuilder();
      for (int i = 0; i < line.Length; i++) {
        char c = line[i];
        Debug.Assert(c != EditorConfig.Ret);
        int col = buf.Length;
        if (c == EditorConfig.Tab) {
          buf.Append(TabToSpaces(col, tabSize));
        } else {
          buf.Append(c);
        }
      }
      return buf.ToString();
    }
  }
}
