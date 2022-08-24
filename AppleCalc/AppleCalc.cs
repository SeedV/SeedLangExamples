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

using SeedLang;
using SeedLang.Common;
using SeedLang.Runtime;
using SeedLang.Visualization;

public class AppleCalc {
  private class Visualizer : IVisualizer<Event.Binary> {
    private int _step = 0;

    public void Reset() {
      _step = 0;
    }

    public void On(Event.Binary e, IVM vm) {
      _step++;
      string operand1 = NumberInApples(e.Left.Value.AsNumber());
      string operand2 = NumberInApples(e.Right.Value.AsNumber());
      string op = _binaryOperatorToString[e.Op];
      string result = NumberInApples(e.Result.AsNumber());
      Console.WriteLine($"STEP {_step}: {operand1} {op} {operand2} = {result}");
    }
  }

  private const string _moduleName = "Apples";

  private const string _welcome =
@"Enter arithmetic expressions to calculate. The integer numbers ranging from 1 to 20 will be
displayed as red apples, unless your console doesn't support Unicode encoding or Unicode fonts.
Enter ""bye"" to exit.";

  private const string _prompt = "] ";
  private const string _bye = "bye";

  private static readonly Dictionary<BinaryOperator, string> _binaryOperatorToString =
      new Dictionary<BinaryOperator, string> {
    { BinaryOperator.Add, "+" },
    { BinaryOperator.Subtract, "-" },
    { BinaryOperator.Multiply, "*" },
    { BinaryOperator.Divide, "/" },
  };

  private static readonly Dictionary<UnaryOperator, string> _unaryOperatorToString =
      new Dictionary<UnaryOperator, string> {
    { UnaryOperator.Positive, "+" },
    { UnaryOperator.Negative, "-" },
  };

  static void Main(string[] args) {
    Console.WriteLine(_welcome);
    Console.Write(_prompt);
    string? line;
    var engine = new Engine(SeedXLanguage.SeedCalc, RunMode.Script);
    var visualizer = new Visualizer();
    engine.Register(visualizer);
    while ((line = Console.ReadLine()) != null) {
      if (line.Length <= 0) {
        continue;
      }
      string input = line.ToLower();
      if (input == _bye) {
        break;
      }
      var collection = new DiagnosticCollection();
      if (!engine.Compile(input, _moduleName, collection) ||
          !engine.Run(collection)) {
        Console.WriteLine(collection.Diagnostics[0]?.ToString());
      }
      Console.Write(_prompt);
    }
    engine.Unregister(visualizer);
  }

  private static string GetApple() {
    string encodingName = Console.OutputEncoding.WebName.ToLower();
    if (encodingName.StartsWith("utf") || encodingName.StartsWith("unicode")) {
      return "\uD83C\uDF4E";  // U+1F34E: Unicode character red apple.
    } else {
      return "@";
    }
  }

  private static string NumberInApples(double number) {
    if (number % 1 == 0 && number >= 1 && number <= 20) {
      StringBuilder sb = new StringBuilder();
      for (int i = 0; i < (int)number; i++) {
        sb.Append(GetApple());
      }
      return sb.ToString();
    } else {
      return number.ToString();
    }
  }
}
