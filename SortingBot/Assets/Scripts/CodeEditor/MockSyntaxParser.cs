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

using System;
using System.Collections.Generic;
using UnityEngine;

// TODO: The following code is only used to mimic SeedLang's syntax parser API, for testing
// purposes. Delete this code once the SeedLang plugin is ready.

namespace CodeEditor {
  // BEGIN - code pieces copied from SeedLang.
  public enum TokenType {
    // VS Code standard token types.
    Class,             // For identifiers that declare or reference a class type.
    Comment,           // For tokens that represent a comment.
    Decorator,         // For identifiers that declare or reference decorators and annotations.
    Enum,              // For identifiers that declare or reference an enumeration type.
    EnumMember,        // For identifiers that declare or reference an enumeration property,
                       // constant, or member.
    Event,             // For identifiers that declare an event property.
    Function,          // For identifiers that declare a function.
    Interface,         // For identifiers that declare or reference an interface type.
    Keyword,           // For tokens that represent a language keyword.
    Label,             // For identifiers that declare a label.
    Macro,             // For identifiers that declare a macro.
    Method,            // For identifiers that declare a member function or method.
    Namespace,         // For identifiers that declare or reference a namespace, module, or package.
    Number,            // For tokens that represent a number literal.
    Operator,          // For tokens that represent an operator.
    Parameter,         // For identifiers that declare or reference a function or method parameters.
    Property,          // For identifiers that declare or reference a member property, member field,
                       // or member variable.
    Regexp,            // For tokens that represent a regular expression literal.
    String,            // For tokens that represent a string literal.
    Struct,            // For identifiers that declare or reference a struct type.
    Type,              // For identifiers that declare or reference a type that is not covered
                       // above.
    TypeParameter,     // For identifiers that declare or reference a type parameter.
    Variable,          // For identifiers that declare or reference a local or global variable.

    // Extended token types.
    Boolean,           // For tokens that represent a boolean literal.
    CloseBrace,        // For tokens that represent a curly bracket.
    CloseBracket,      // For tokens that represent a square bracket.
    CloseParenthesis,  // For tokens that represent a parenthesis.
    Nil,               // For tokens that represent a nil literal.
    OpenBrace,         // For tokens that represent a curly bracket.
    OpenBracket,       // For tokens that represent a square bracket.
    OpenParenthesis,   // For tokens that represent a parenthesis.
    Symbol,            // For tokens that represent an unclassified symbol.
    Unknown,           // Unknown token type.
  }

  // An immutable class to represent a position in a plaintext source code.
  public struct TextPosition : IComparable<TextPosition>, IEquatable<TextPosition> {
    public int Line { get; }
    public int Column { get; }

    public TextPosition(int line, int column) {
      Line = line;
      Column = column;
    }

    public override string ToString() {
      return $"Ln {Line}, Col {Column}";
    }

    public override int GetHashCode() {
      return Tuple.Create(Line, Column).GetHashCode();
    }

    public int CompareTo(TextPosition pos) {
      if (Line < pos.Line) {
        return -1;
      } else if (Line > pos.Line) {
        return 1;
      } else {
        if (Column < pos.Column) {
          return -1;
        } else if (Column > pos.Column) {
          return 1;
        } else {
          return 0;
        }
      }
    }

    public bool Equals(TextPosition pos) {
      return CompareTo(pos) == 0;
    }

    public override bool Equals(object obj) {
      return (obj is TextPosition objTextPosition) && Equals(objTextPosition);
    }

    public static bool operator ==(TextPosition pos1, TextPosition pos2) {
      return pos1.Equals(pos2);
    }

    public static bool operator !=(TextPosition pos1, TextPosition pos2) {
      return !(pos1 == pos2);
    }

    public static bool operator <(TextPosition pos1, TextPosition pos2) {
      return pos1.CompareTo(pos2) < 0;
    }

    public static bool operator <=(TextPosition pos1, TextPosition pos2) {
      return pos1.CompareTo(pos2) <= 0;
    }

    public static bool operator >(TextPosition pos1, TextPosition pos2) {
      return pos1.CompareTo(pos2) > 0;
    }

    public static bool operator >=(TextPosition pos1, TextPosition pos2) {
      return pos1.CompareTo(pos2) >= 0;
    }
  }

  // The base class of all the concrete code range classes.
  public abstract class Range : IEquatable<Range> {
    // All the subclasses must provide a customized ToString() method to return the display string.
    public abstract override string ToString();

    public abstract override int GetHashCode();

    public abstract bool Equals(Range range);

    public override bool Equals(object obj) {
      return Equals(obj as Range);
    }

    public static bool operator ==(Range range1, Range range2) {
      if (range1 is null) {
        return range2 is null;
      }
      return range1.Equals(range2);
    }

    public static bool operator !=(Range range1, Range range2) {
      return !(range1 == range2);
    }
  }

  // Represents a range in a plaintext source code.
  public class TextRange : Range {
    public TextPosition Start { get; }
    public TextPosition End { get; }

    // A text range is defined as [start, end], where both ends of the range are inclusive.
    public TextRange(TextPosition start, TextPosition end) {
      Start = start;
      End = end;
    }

    public TextRange(int startLine, int startColumn, int endLine, int endColumn) :
        this(new TextPosition(startLine, startColumn), new TextPosition(endLine, endColumn)) {
    }

    public override string ToString() {
      return $"[{Start} - {End}]";
    }

    public override int GetHashCode() {
      return Tuple.Create(Start.GetHashCode(), End.GetHashCode()).GetHashCode();
    }

    public override bool Equals(Range range) {
      if (range is null) {
        return false;
      }
      if (ReferenceEquals(this, range)) {
        return true;
      }
      if (GetType() != range.GetType()) {
        return false;
      }
      return Start == (range as TextRange).Start && End == (range as TextRange).End;
    }
  }

  // Represents syntax or semantic tokens parsed from source code.
  public class TokenInfo {
    public TokenType Type { get; }
    public TextRange Range { get; }

    public TokenInfo(TokenType type, TextRange range) {
      Type = type;
      Range = range;
    }

    public override string ToString() {
      return $"{Type} {Range}";
    }
  }
  // END - code pieces copied from SeedLang.

  public static class MockSyntaxParser {
    // A mock-up implementation to mimic SeedLang's syntax parsing, only returning two kinds of
    // token types: TokenType.Number for integer number tokens and TokenType.Label for other tokens.
    public static void ParseSyntaxTokens(string code, out IReadOnlyList<TokenInfo> syntaxTokens) {
      var tokens = new List<TokenInfo>();
      int line = 1;
      int col = 0;
      int endCol = 0;
      var tokenLen = 0;
      var tokenType = TokenType.Number;
      foreach (char c in code) {
        if (c == '\n') {
          if (tokenLen > 0) {
            OutputToken(tokenType, line, endCol - tokenLen + 1, endCol, tokens);
            tokenLen = 0;
            tokenType = TokenType.Number;
          }
          line += 1;
          col = 0;
        } else if (c == ' ' || c == '\t') {
          if (tokenLen > 0) {
            OutputToken(tokenType, line, endCol - tokenLen + 1, endCol, tokens);
            tokenLen = 0;
            tokenType = TokenType.Number;
          }
          col++;
        } else {
          tokenLen++;
          if (!char.IsDigit(c)) {
            tokenType = TokenType.Label;
          }
          endCol = col;
          col++;
        }
      }
      if (tokenLen > 0) {
        OutputToken(tokenType, line, endCol - tokenLen + 1, endCol, tokens);
      }
      syntaxTokens = tokens;
    }

    public static void OutputToken(TokenType type, int line, int startCol, int endCol,
                                   List<TokenInfo> tokens) {
      var range = new TextRange(line, startCol, line, endCol);
      var tokenInfo = new TokenInfo(type, range);
      tokens.Add(tokenInfo);
    }
  }
}
