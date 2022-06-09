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
using TMPro;
using UnityEngine;

using SeedLang.Common;

public class Inspector : MonoBehaviour {
  private TMP_Text _textConsole;

  public void OutputTextInfo(string info) {
    _textConsole.text = info;
  }

  public void AppendTextInfo(string info) {
    _textConsole.text = $"{_textConsole.text}\n{info}";
  }

  public void OutputSeedLangDiagnostics(DiagnosticCollection collection) {
    OutputTextInfo(FormatDiagnosticCollection(collection));
  }

  public void AppendSeedLangDiagnostics(DiagnosticCollection collection) {
    AppendTextInfo(FormatDiagnosticCollection(collection));
  }

  void Start() {
    var obj = transform.Find("InspectorOutput")?.Find("TextConsole").gameObject;
    Debug.Assert(!(obj is null));
    _textConsole = obj.GetComponent<TMP_Text>();
  }

  private string FormatDiagnosticCollection(DiagnosticCollection collection) {
    var buf = new StringBuilder();
    foreach (var diagnostic in collection.Diagnostics) {
      buf.Append($"{diagnostic.ToString()}\n");
    }
    return buf.ToString();
  }
}
