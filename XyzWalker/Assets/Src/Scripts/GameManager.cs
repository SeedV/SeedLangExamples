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

using CodeEditor;
using CoroutineActions;
using SeedLang.Common;

public class GameManager : MonoBehaviour {
  private readonly ActionQueue _actionQueue = new ActionQueue();

  public Button RunButton;
  public Button StopButton;
  public TMP_InputField CodeEditorInputField;
  public TMP_Text TextConsole;
  public Boy Boy;
  public Blocks Blocks;

  public bool IsActionQueueEmpty => _actionQueue.IsEmpty;

  private CodeExecutor _codeExecutor;
  private EditorManager _editor;

  // The (x, y, z) value to be visualized.
  private Vector3 _currentValue;

  public void Start() {
    _codeExecutor = new CodeExecutor(this);

    var textArea = CodeEditorInputField.gameObject.transform.Find("TextArea");
    var inputText = textArea.Find("InputText").GetComponent<TMP_Text>();
    Debug.Assert(!(inputText is null));
    var lineNoText = inputText.gameObject.transform.Find("LineNoText")?.GetComponent<TMP_Text>();
    Debug.Assert(!(lineNoText is null));
    var overlayText = inputText.gameObject.transform.Find("OverlayText")?.GetComponent<TMP_Text>();
    Debug.Assert(!(overlayText is null));
    var highlighter = inputText.gameObject.transform.Find("Highlighter")?.GetComponent<Image>();
    Debug.Assert(!(highlighter is null));
    _editor = new EditorManager(this,
                                CodeEditorInputField,
                                inputText,
                                overlayText,
                                lineNoText,
                                highlighter);

    RunButton.onClick.AddListener(OnRun);
    StopButton.onClick.AddListener(OnStop);
    UpdateButtons();

    Reset();

    // Starts and keeps the action queue running during the life cycle of the application.
    StartCoroutine(_actionQueue.Run());
  }

  public void OnRun() {
    Reset();
    _codeExecutor.Run(_editor.Text);
    UpdateButtons();
  }

  public void OnStop() {
    _codeExecutor.Stopping = true;
  }

  public void QueueVisualizeX(float x) {
    if (_currentValue.x != x) {
      QueueMoveXY(x, _currentValue.y);
      _currentValue.x = x;
    }
  }

  public void QueueVisualizeY(float y) {
    if (_currentValue.y != y) {
      QueueMoveXY(_currentValue.x, y);
      _currentValue.y = y;
    }
  }

  public void QueueVisualizeZ(float z) {
    QueueSetColor(_currentValue.x, _currentValue.y, z);
    _currentValue.z = z;
  }

  public void QueueOutputTextInfo(string info) {
    var task = new Task2<string, bool>(OutputTextInfoTask, info, true);
    _actionQueue.Enqueue(new SingleTaskAction(this, task));
  }

  public void QueueOutputSeedLangDiagnostics(DiagnosticCollection collection) {
    var task =
        new Task2<DiagnosticCollection, bool>(OutputSeedLangDiagnosticsTask, collection, true);
    _actionQueue.Enqueue(new SingleTaskAction(this, task));
  }

  public void QueueHighlightCodeLineAndWait(int lineNo, float secondsToWait) {
    var task = new Task2<int, float>(HighlightCodeLineTask, lineNo, secondsToWait);
    _actionQueue.Enqueue(new SingleTaskAction(this, task));
  }

  public void QueueOnExecutorComplete() {
    var task = new Task0(UpdateButtonsTask);
    _actionQueue.Enqueue(new SingleTaskAction(this, task));
  }

  private void Reset() {
    TextConsole.text = "";
    _currentValue = Vector3.zero;
    Boy.Reset();
    Blocks.ResetColors();
  }

  private void QueueMoveXY(float x, float y) {
    var task = new Task2<int, int>(BoyMoveToTask, (int)x % 10, (int)y % 10);
    _actionQueue.Enqueue(new SingleTaskAction(this, task));
  }

  private void QueueSetColor(float x, float y, float z) {
    var taskBlocks = new Task3<int, int, int>(SetBlockColorTask,
                                              (int)x % 10,
                                              (int)y % 10,
                                              (int)z % 10);
    var taskBoy = new Task0(BoyJumpTask);
    _actionQueue.Enqueue(new Action(this, new ITask[] {taskBlocks, taskBoy}));
  }

  private IEnumerator BoyMoveToTask(int row, int col) {
    var worldPos = Blocks.GetBlockWorldPos(row, col);
    yield return Boy.MoveToWorldPos(worldPos.x, worldPos.z);
  }

  private IEnumerator BoyJumpTask() {
    yield return Boy.Jump();
  }

  private IEnumerator SetBlockColorTask(int row, int col, int colorIndex) {
    yield return Blocks.SetBlockColor(row, col, colorIndex);
  }

  private IEnumerator OutputTextInfoTask(string info, bool append) {
    if (append) {
      AppendTextInfo(info);
    } else {
      OutputTextInfo(info);
    }
    yield return null;
  }

  private IEnumerator OutputSeedLangDiagnosticsTask(DiagnosticCollection collection, bool append) {
    if (append) {
      AppendTextInfo(FormatDiagnosticCollection(collection));
    } else {
      OutputTextInfo(FormatDiagnosticCollection(collection));
    }
    yield return null;
  }

  private void OutputTextInfo(string info) {
    TextConsole.text = info;
  }

  private void AppendTextInfo(string info) {
    if (TextConsole.text.Length > 0) {
      TextConsole.text += "\n";
    }
    TextConsole.text += info;
  }

  private string FormatDiagnosticCollection(DiagnosticCollection collection) {
    var buf = new StringBuilder();
    foreach (var diagnostic in collection.Diagnostics) {
      buf.Append($"{diagnostic.ToString()}\n");
    }
    return buf.ToString();
  }

  private IEnumerator HighlightCodeLineTask(int lineNo, float secondsToWait) {
    _editor.HighlightLine(lineNo);
    yield return new WaitForSeconds(secondsToWait);
  }

  private void UpdateButtons() {
    RunButton.interactable = !_codeExecutor.IsRunning;
    StopButton.interactable = _codeExecutor.IsRunning;
  }

  private IEnumerator UpdateButtonsTask() {
    UpdateButtons();
    yield return null;
  }
}
