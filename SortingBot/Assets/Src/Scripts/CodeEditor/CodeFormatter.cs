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
    // Formats source code based on the following rules:
    //
    // 1. Inserts an indention to the caretPos if needed.
    // 2. If tabSize is not 0, use the string to replace all the tab characters in the code.
    // 3. Color the token that are parsed from the code, with Unity's rich text format, escaping
    //    special characters if necessary.
    //
    // The output argument formatted is the result of applying rule 1 and 2.
    //
    // The output argument changed will be true if the output formatted string is different from the
    // input code.
    //
    // The output argument newCaretPos contains the new caret pos after inserting the indention.
    //
    // The output argument formattedAndColored is the result of applying rule 1, 2, and 3.
    //
    // For performance considerations, we apply all the three rules in a single parsing pass of the
    // code.
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
      int line = 0;
      int col = 0;
      int colInFormatted = 0;
      var formattedBuffer = new StringBuilder();
      var formattedAndColoredBuffer = new StringBuilder();
      changed = false;
      newCaretPos = caretPos;

      while (index < code.Length) {
        char c = code[index];
        if (tabSize > 0 && c == EditorConfig.Tab) {
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
          formattedBuffer.Append(c);
          formattedAndColoredBuffer.Append(c);
          index++;
          line++;
          col = 0;
          colInFormatted = 0;
        } else {
          string escaped = Escape(c);
          formattedBuffer.Append(c);
          formattedAndColoredBuffer.Append(escaped);
          index++;
          col++;
          colInFormatted ++;
        }
        if (index == caretPos) {
          if (!(indention is null)) {
            string converted = TabToSpacesInString(indention, tabSize);
            formattedBuffer.Append(converted);
            formattedAndColoredBuffer.Append(converted);
            newCaretPos += converted.Length;
            colInFormatted += converted.Length;
            changed = true;
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
