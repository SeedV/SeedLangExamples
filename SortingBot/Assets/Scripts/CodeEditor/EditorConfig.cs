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

namespace CodeEditor {
  // Common config info of the code editor.
  public static class EditorConfig {
    public const char Ret = '\n';
    public const char Tab = '\t';

    // The line-ending character that may increase the indention level of the next code line.
    public static readonly HashSet<char> EndCharsToIncreaseIndent = new HashSet<char> {
      ':', '(', '{', '[', '='
    };

    // Special characters like "<" and ">" must be escaped, otherwise they will be treated as
    // Unity's rich text formatters.
    public static Dictionary<char, string> CharEscapeTable = new Dictionary<char, string>() {
      ['<'] = "<noparse><</noparse>",
      ['>'] = "<noparse>></noparse>",
    };

    // Unity rich text colors use CSS-like notations.
    public const string DefaultTokenColor = "#666";

    // TODO: Complete the color list once the real SeedLang plugin is ready.
    public static readonly Dictionary<TokenType, string> TokenColors =
        new Dictionary<TokenType, string>() {
          [TokenType.Label] = "#09F",
          [TokenType.Number] = "#0C6",
        };
  }
}
