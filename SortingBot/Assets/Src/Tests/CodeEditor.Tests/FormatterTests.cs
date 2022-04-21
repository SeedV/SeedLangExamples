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
  }
}
