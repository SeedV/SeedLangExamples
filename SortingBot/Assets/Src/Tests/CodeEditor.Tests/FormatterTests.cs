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
using NUnit.Framework;

using SeedLang.Common;

namespace CodeEditor.Tests {
  public class FormatterTests {
    [Test]
    public void TestFormatEmpty() {
      CodeFomatter.FormatAndColor("", 0, null, 4, new List<TokenInfo>(),
                                   out string formatted,
                                   out bool changed,
                                   out int newCaretPos,
                                   out string formattedAndColored);
      Assert.AreEqual("", formatted);
      Assert.False(changed);
      Assert.AreEqual(0, newCaretPos);
      Assert.AreEqual(formatted, formattedAndColored);
    }

    [Test]
    public void TestTabToSpaces() {
      CodeFomatter.FormatAndColor("\ta\t\n\t \tb\n", 4, null, 4, new List<TokenInfo>(),
                                   out string formatted,
                                   out bool changed,
                                   out int newCaretPos,
                                   out string formattedAndColored);
      Assert.AreEqual("    a   \n        b\n", formatted);
      Assert.True(changed);
      Assert.AreEqual(9, newCaretPos);
      Assert.AreEqual(formatted, formattedAndColored);
    }

    [Test]
    public void TestAutoIndentionAndTabToSpaces() {
      string code = "\ta:\nbc\td\n123";
      int caretPost = 4;
      string indention = AutoIndenter.GetIndention(code, caretPost);
      Assert.AreEqual("\t\t", indention);
      CodeFomatter.FormatAndColor(code, caretPost, indention, 4, new List<TokenInfo>(),
                                   out string formatted,
                                   out bool changed,
                                   out int newCaretPos,
                                   out string formattedAndColored);
      Assert.AreEqual("    a:\n        bc  d\n123", formatted);
      Assert.True(changed);
      Assert.AreEqual(15, newCaretPos);
      Assert.AreEqual(formatted, formattedAndColored);
    }

    public void TestTokenColorsAndEscape() {
      string code = "\tabc 123\n456 <x>\n";
      int caretPost = 9;
      string indention = AutoIndenter.GetIndention(code, caretPost);
      Assert.AreEqual("\t", indention);
      var tokens = new List<TokenInfo>();
      tokens.Add(new TokenInfo(TokenType.Label, new TextRange(1, 1, 1, 3)));
      tokens.Add(new TokenInfo(TokenType.Number, new TextRange(1, 5, 1, 7)));
      tokens.Add(new TokenInfo(TokenType.Number, new TextRange(2, 0, 2, 2)));
      tokens.Add(new TokenInfo(TokenType.Number, new TextRange(2, 4, 2, 6)));
      CodeFomatter.FormatAndColor(code, caretPost, indention, 4, tokens,
                                   out string formatted,
                                   out bool changed,
                                   out int newCaretPos,
                                   out string formattedAndColored);
      Assert.AreEqual("    abc 123\n    456 <x>\n", formatted);
      Assert.True(changed);
      Assert.AreEqual(16, newCaretPos);
      string labelColor = EditorConfig.TokenColors[TokenType.Label];
      string numberColor = EditorConfig.TokenColors[TokenType.Number];
      Assert.AreEqual(
          "    <{labelColor}>abc</color> <{numberColor}>123</color>\n    " +
          "<{numberColor}456</color> " +
          "<{labelColor}><noparse><</noparse>x<noparse>></noparse></color>\n",
          formattedAndColored);
    }
  }
}
