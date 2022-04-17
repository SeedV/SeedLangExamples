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
  public static class AutoIndenter {
    private const char _ret = '\n';
    private const char _tab = '\t';

    // TODO: this definition is specific for Python language. Make a general definition to cover
    // different languages.
    private static readonly HashSet<char> _endCharsToAddIndent = new HashSet<char> {
      ':', '(', '{', '['
    };

    // Given the typed code and the current caret pos, calculates if the current input pos needs to
    // be auto indented.
    //
    // Returns true if the current input pos needs to be auto indented. The output arguments newCode
    // and newCaretPos contain the indented text and caret pos.
    //
    // Return false if the current input pos does not need auto-indention. In this case, newCode
    // will be set to null and newCaretPos will be set to 0.
    public static bool AutoIndent(string code, int caretPos,
                                  out string newCode, out int newCaretPos) {
      newCode = null;
      newCaretPos = 0;
      if (caretPos >= 1 && code[caretPos - 1] == _ret) {
        int lastLineEndPos = caretPos - 1;
        int lastLineStartPos = lastLineEndPos;
        while (lastLineStartPos >= 1 && code[lastLineStartPos - 1] != _ret) {
          lastLineStartPos--;
        }
        if (code[lastLineStartPos] == _ret) {
          lastLineStartPos++;
        }
        if (lastLineStartPos < lastLineEndPos) {
          string lastLine = code.Substring(lastLineStartPos, lastLineEndPos - lastLineStartPos);
          Debug.Log(lastLine);
          int lastLineLeadingSpacesEndPos = GetLeadingSpacesEndPos(lastLine);
          int lastLineLastCharPos = GetLastNonSpaceCharPos(lastLine);
          Debug.Log($"{caretPos} {lastLineLeadingSpacesEndPos} {lastLineLastCharPos}");

          var autoIndentText = new StringBuilder();
          autoIndentText.Append(code.Substring(0, caretPos));
          if (lastLineLeadingSpacesEndPos >= 0) {
            autoIndentText.Append(
                code.Substring(lastLineStartPos, lastLineLeadingSpacesEndPos + 1));
          }
          Debug.Log($"{autoIndentText.Length}");
          if (lastLineLastCharPos >= 0 &&
              GetAdditionalIndent(lastLine[lastLineLastCharPos], out string additionalIndent)) {
            autoIndentText.Append(additionalIndent);
          }
          Debug.Log($"{autoIndentText.Length}");
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

    private static bool GetAdditionalIndent(char lastLineLastChar, out string additionalIndent) {
      if (_endCharsToAddIndent.Contains(lastLineLastChar)) {
        additionalIndent = _tab.ToString();
        return true;
      } else {
        additionalIndent = null;
        return false;
      }
    }
  }
}
