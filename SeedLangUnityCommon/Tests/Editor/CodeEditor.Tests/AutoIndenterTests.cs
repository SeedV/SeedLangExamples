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

using NUnit.Framework;

namespace CodeEditor.Tests {
  public class AutoIndenterTests {
    [Test]
    public void TestAutoIndenter() {
      Assert.IsNull(AutoIndenter.GetIndention("", 0));
      Assert.IsNull(AutoIndenter.GetIndention("abc", 3));
      Assert.IsNull(AutoIndenter.GetIndention("\n", 1));
      Assert.IsNull(AutoIndenter.GetIndention("a\n", 2));

      Assert.AreEqual("\t", AutoIndenter.GetIndention("\t\n", 2));
      Assert.AreEqual("\t", AutoIndenter.GetIndention("\tabc\n", 5));
      Assert.AreEqual("  \t", AutoIndenter.GetIndention("  \t\n", 4));
      Assert.AreEqual("  \t", AutoIndenter.GetIndention("  \tabc\n", 7));
      Assert.AreEqual("\t\t", AutoIndenter.GetIndention("\n\n\t\ta\n", 6));
      Assert.AreEqual("  ", AutoIndenter.GetIndention("\n\n  a\n", 6));

      Assert.AreEqual("\t", AutoIndenter.GetIndention(":\n", 2));
      Assert.AreEqual("\t\t", AutoIndenter.GetIndention("\t:\n", 3));
      Assert.AreEqual("\t\t", AutoIndenter.GetIndention("\ta:\n", 4));
      Assert.AreEqual("\t", AutoIndenter.GetIndention("a {\n", 4));
      Assert.AreEqual("\t", AutoIndenter.GetIndention("a {\nb", 4));
      Assert.AreEqual("    ", AutoIndenter.GetIndention("    a\n    b\n", 12));
    }
  }
}
