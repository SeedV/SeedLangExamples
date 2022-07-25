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

using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using SeedLang.Common;

public class Inspector : MonoBehaviour {
  public ScrollRect ScrollView;
  public TMP_Text TextConsole;

  public void Clear() {
    TextConsole.text = "";
  }

  public IEnumerator OutputTextInfoTask(string info, bool append) {
    if (append) {
      AppendTextInfo(info);
    } else {
      OutputTextInfo(info);
    }
    yield return null;
    ScrollToBottom();
  }

  public IEnumerator OutputSeedLangDiagnosticsTask(DiagnosticCollection collection, bool append) {
    if (append) {
      AppendSeedLangDiagnostics(collection);
    } else {
      OutputSeedLangDiagnostics(collection);
    }
    yield return null;
    ScrollToBottom();
  }

  public void OutputTextInfo(string info) {
    TextConsole.text = info;
  }

  public void AppendTextInfo(string info) {
    if (TextConsole.text.Length > 0) {
      TextConsole.text += "\n";
    }
    TextConsole.text += info;
  }

  public void ScrollToTop() {
    ScrollView.verticalNormalizedPosition = 1;
  }

  public void ScrollToBottom() {
    ScrollView.verticalNormalizedPosition = 0;
  }

  public void OutputSeedLangDiagnostics(DiagnosticCollection collection) {
    OutputTextInfo(FormatDiagnosticCollection(collection));
  }

  public void AppendSeedLangDiagnostics(DiagnosticCollection collection) {
    AppendTextInfo(FormatDiagnosticCollection(collection));
  }

  private string FormatDiagnosticCollection(DiagnosticCollection collection) {
    var buf = new StringBuilder();
    foreach (var diagnostic in collection.Diagnostics) {
      buf.Append($"{diagnostic.ToString()}\n");
    }
    return buf.ToString();
  }
}
