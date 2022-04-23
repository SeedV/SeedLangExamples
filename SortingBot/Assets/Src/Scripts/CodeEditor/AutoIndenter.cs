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

namespace CodeEditor {
  public static class AutoIndenter {
    // Given the input code and the current caret pos, calculates if the current input pos needs to
    // be auto indented, then returns the indention string of the new line.
    //
    // Returns null if the current input pos doesn't need auto-indention.
    //
    // If the new line has the same indention as the previous line, the returned indention will be a
    // copy of the previous line's indention.
    //
    // If the new line has additional indention levels, the returned string will be appended with
    // extra tab characters.
    public static string GetIndention(string code, int caretPos) {
      if (caretPos >= 1 && code[caretPos - 1] == EditorConfig.Ret) {
        int lastLineEndPos = caretPos - 1;
        int lastLineStartPos = lastLineEndPos;
        while (lastLineStartPos >= 1 && code[lastLineStartPos - 1] != EditorConfig.Ret) {
          lastLineStartPos--;
        }
        if (lastLineStartPos < lastLineEndPos) {
          int lastLineLeadingSpacesEndPos = GetLeadingSpacesEndPos(code,
                                                                   lastLineStartPos,
                                                                   lastLineEndPos);
          var indention = new StringBuilder();
          if (lastLineLeadingSpacesEndPos >= lastLineStartPos) {
            indention.Append(code.Substring(lastLineStartPos,
                                            lastLineLeadingSpacesEndPos - lastLineStartPos + 1));
          }
          int lastLineLastCharPos = GetLastNonSpaceCharPos(code, lastLineStartPos, lastLineEndPos);
          if (lastLineLastCharPos >= 0) {
            string additionalIndent = GetExtraIndention(code[lastLineLastCharPos]);
            if (!(additionalIndent is null)) {
              indention.Append(additionalIndent);
            }
          }
          if (indention.Length > 0) {
            return indention.ToString();
          }
        }
      }
      return null;
    }

    private static int GetLastNonSpaceCharPos(string code,
                                              int lastLineStartPos,
                                              int lastLineEndPos) {
      int i = lastLineEndPos - 1;
      while (i >= lastLineStartPos && char.IsWhiteSpace(code[i])) {
        i--;
      }
      return i;
    }

    private static int GetLeadingSpacesEndPos(string code,
                                              int lastLineStartPos,
                                              int lastLineEndPos) {
      int i = lastLineStartPos;
      while (i < lastLineEndPos && char.IsWhiteSpace(code[i])) {
        i++;
      }
      return i - 1;
    }

    // Determines if the line needs to auto-increase the indention level. If so, returns a number of
    // extra tabs as a string. Otherwise, returns null.
    //
    // TODO: support auto-decreasing the indention level too.
    //
    // TODO: consider the case that needs to increase more than one indention levels.
    private static string GetExtraIndention(char lastLineLastChar) {
      if (EditorConfig.EndCharsToIncreaseIndent.Contains(lastLineLastChar)) {
        return EditorConfig.Tab.ToString();
      } else {
        return null;
      }
    }
  }
}
