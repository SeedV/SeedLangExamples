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

using System.Text;
using UnityEngine;

namespace CodeEditor {
  public static class AutoIndenter {
    // Given the input code and the current caret pos, calculates if the current input pos needs to
    // be auto indented.
    //
    // Returns true if the current input pos needs auto-indention. Also outputs the indented text
    // and the new caret pos.
    //
    // Return false if the current input pos doesn't need auto-indention. In this case, newCode will
    // be set to null and newCaretPos will be set to 0.
    //
    // TODO: Currently we simply use TABs to indent the code. Support TAB-as-spaces later.
    public static bool AutoIndent(string code, int caretPos,
                                  out string newCode, out int newCaretPos) {
      newCode = null;
      newCaretPos = 0;
      if (caretPos >= 1 && code[caretPos - 1] == EditorConfig.Ret) {
        int lastLineEndPos = caretPos - 1;
        int lastLineStartPos = lastLineEndPos;
        while (lastLineStartPos >= 1 && code[lastLineStartPos - 1] != EditorConfig.Ret) {
          lastLineStartPos--;
        }
        if (code[lastLineStartPos] == EditorConfig.Ret) {
          lastLineStartPos++;
        }
        if (lastLineStartPos < lastLineEndPos) {
          string lastLine = code.Substring(lastLineStartPos, lastLineEndPos - lastLineStartPos);
          int lastLineLeadingSpacesEndPos = GetLeadingSpacesEndPos(lastLine);
          int lastLineLastCharPos = GetLastNonSpaceCharPos(lastLine);

          var autoIndentText = new StringBuilder();
          autoIndentText.Append(code.Substring(0, caretPos));
          if (lastLineLeadingSpacesEndPos >= 0) {
            autoIndentText.Append(
                code.Substring(lastLineStartPos, lastLineLeadingSpacesEndPos + 1));
          }
          if (lastLineLastCharPos >= 0 &&
              AutoIncreaseIndent(lastLine[lastLineLastCharPos], out string additionalIndent)) {
            autoIndentText.Append(additionalIndent);
          }
          newCaretPos = autoIndentText.Length;
          if (caretPos < code.Length) {
            autoIndentText.Append(code.Substring(caretPos, code.Length - caretPos));
          }
          newCode = autoIndentText.ToString();
          return true;
        }
      }
      return false;
    }

    private static int GetLastNonSpaceCharPos(string line) {
      int i = line.Length - 1;
      while (i >= 0 && char.IsWhiteSpace(line[i])) {
        i--;
      }
      return i;
    }

    private static int GetLeadingSpacesEndPos(string line) {
      int i = 0;
      while (i < line.Length && char.IsWhiteSpace(line[i])) {
        i++;
      }
      return i - 1;
    }

    // Determines if the current line needs to auto-increase the indention level. If so, outputs the
    // additional spaces as a string via additionalIndent.
    //
    // TODO: support auto-decreasing the indention level too.
    private static bool AutoIncreaseIndent(char lastLineLastChar, out string additionalIndent) {
      if (EditorConfig.EndCharsToIncreaseIndent.Contains(lastLineLastChar)) {
        additionalIndent = EditorConfig.Tab.ToString();
        return true;
      } else {
        additionalIndent = null;
        return false;
      }
    }
  }
}
