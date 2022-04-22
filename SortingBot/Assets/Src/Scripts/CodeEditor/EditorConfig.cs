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

using SeedLang.Common;

namespace CodeEditor {
  // Common config info of the code editor.
  public static class EditorConfig {
    public const char Ret = '\n';
    public const char Tab = '\t';
    public const char Space = ' ';

    // Default number of spaces that a tab is equal to.
    public const int DefaultTabSize = 4;

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

    // See https://github.com/SeedV/SeedLang/blob/main/csharp/src/SeedLang/Common/TokenInfo.cs for a
    // complete list of token types.
    //
    // TODO: support multiple color themes.
    public static readonly Dictionary<TokenType, string> TokenColors =
        new Dictionary<TokenType, string>() {
          [TokenType.Class] = "#09c",
          [TokenType.Comment] = "#ccc",
          [TokenType.Decorator] = "#c93",
          [TokenType.Enum] = "#09c",
          [TokenType.EnumMember] = "#09c",
          [TokenType.Event] = "#09c",
          [TokenType.Function] = "#09c",
          [TokenType.Interface] = "#09c",
          [TokenType.Keyword] = "#009",
          [TokenType.Label] = "#333",
          [TokenType.Macro] = "#333",
          [TokenType.Method] = "#09c",
          [TokenType.Namespace] = "#09c",
          [TokenType.Number] = "#093",
          [TokenType.Operator] = "#666",
          [TokenType.Parameter] = "#09c",
          [TokenType.Property] = "#09c",
          [TokenType.Regexp] = "#960",
          [TokenType.String] = "#960",
          [TokenType.Struct] = "#09c",
          [TokenType.Type] = "#09c",
          [TokenType.TypeParameter] = "#09c",
          [TokenType.Variable] = "#900",
          [TokenType.Boolean] = "#f9f",
          [TokenType.CloseBrace] = "#000",
          [TokenType.CloseBracket] = "#000",
          [TokenType.CloseParenthesis] = "#000",
          [TokenType.Nil] = "#f9f",
          [TokenType.OpenBrace] = "#000",
          [TokenType.OpenBracket] = "#000",
          [TokenType.OpenParenthesis] = "#000",
          [TokenType.Symbol] = "#000",
          [TokenType.Unknown] = "#666",
        };
  }
}
