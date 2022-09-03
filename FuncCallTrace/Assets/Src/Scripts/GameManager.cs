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
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

using CodeEditor;
using CoroutineActions;
using SeedLang.Common;

public class GameManager : MonoBehaviour {
  private enum LineDirection {
    Left,
    Right,
    Down,
  }

  private const float _horizontalLength = 5.0f;
  private const float _verticalLength = 1.0f;
  private const float _labelOffsetY = 0.4f;
  private const float _playSoundDelay = 0.1f;
  private const float _deltaLengthPerStep = 0.1f;
  private const float _startLineWidth = .1f;
  private const float _endLineWidth = .15f;
  private readonly ActionQueue _actionQueue = new ActionQueue();
  private readonly List<GameObject> _graphElements = new List<GameObject>();

  public Camera Camera;
  public Button RunButton;
  public Button StopButton;
  public Button ExampleButton;
  public TMP_InputField CodeEditorInputField;
  public TMP_Text TextConsole;
  public GameObject TraceGraph;

  public bool IsActionQueueEmpty => _actionQueue.IsEmpty;

  private CodeExecutor _codeExecutor;
  private EditorManager _editor;
  private GameObject _lineRef;
  private GameObject _labelRef;

  private int _currentStep;
  private int _currentCallDepth;
  private float _initCameraZ;

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

    _lineRef = TraceGraph.transform.Find("LineRef")?.gameObject;
    Debug.Assert(!(_lineRef is null));
    _lineRef.GetComponent<LineRenderer>().startWidth = _startLineWidth;
    _lineRef.GetComponent<LineRenderer>().endWidth = _endLineWidth;
    _lineRef.SetActive(false);
    _labelRef = TraceGraph.transform.Find("LabelRef")?.gameObject;
    Debug.Assert(!(_labelRef is null));
    _labelRef.SetActive(false);

    _initCameraZ = Camera.transform.position.z;

    RunButton.onClick.AddListener(OnRun);
    StopButton.onClick.AddListener(OnStop);
    ExampleButton.onClick.AddListener(OnLoadExample);
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

  public void OnLoadExample() {
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

    foreach (var obj in _graphElements) {
      Object.Destroy(obj);
    }
    _graphElements.Clear();

    _currentStep = 0;
    _currentCallDepth = 0;

    var task = new Task2<LineDirection, string>(DrawLine, LineDirection.Down, "Start");
    _actionQueue.Enqueue(new SingleTaskAction(this, task));

    task = new Task2<LineDirection, string>(DrawLine, LineDirection.Right, "Foo (1)");
    _actionQueue.Enqueue(new SingleTaskAction(this, task));

    task = new Task2<LineDirection, string>(DrawLine, LineDirection.Down, null);
    _actionQueue.Enqueue(new SingleTaskAction(this, task));

    task = new Task2<LineDirection, string>(DrawLine, LineDirection.Right, "Foo (2)");
    _actionQueue.Enqueue(new SingleTaskAction(this, task));

    task = new Task2<LineDirection, string>(DrawLine, LineDirection.Down, null);
    _actionQueue.Enqueue(new SingleTaskAction(this, task));

    task = new Task2<LineDirection, string>(DrawLine, LineDirection.Left, "Return (3)");
    _actionQueue.Enqueue(new SingleTaskAction(this, task));

    task = new Task2<LineDirection, string>(DrawLine, LineDirection.Down, null);
    _actionQueue.Enqueue(new SingleTaskAction(this, task));

    task = new Task2<LineDirection, string>(DrawLine, LineDirection.Left, "Return (4)");
    _actionQueue.Enqueue(new SingleTaskAction(this, task));

    task = new Task2<LineDirection, string>(DrawLine, LineDirection.Down, null);
    _actionQueue.Enqueue(new SingleTaskAction(this, task));
  }

  private void DrawLabel(string labelText, float x0, float x1) {
    var label = Object.Instantiate(_labelRef, TraceGraph.transform);
    float x = (x0 + x1) / 2;
    float y = - _currentStep * _verticalLength + _labelOffsetY;
    label.transform.position = new Vector3(x, y, 0);
    label.SetActive(true);
    label.GetComponent<TMP_Text>().text = labelText;
    _graphElements.Add(label);
  }

  private IEnumerator DrawLine(LineDirection direction, string labelText) {
    var points = CalcLinePosition(direction);
    var line = Object.Instantiate(_lineRef, TraceGraph.transform);
    line.SetActive(true);
    _graphElements.Add(line);
    var lineRenderer = line.GetComponent<LineRenderer>();
    lineRenderer.SetPosition(0, points.start);

    int steps = (int)(Vector3.Distance(points.start, points.end) / _deltaLengthPerStep);
    var pointsBuffer = new List<Vector3>();
    for (int i = 0; i <= steps; i++) {
      float t = Mathf.SmoothStep(0, 1, (float)i / (float)steps);
      var p = Vector3.Lerp(points.start, points.end, t);
      pointsBuffer.Add(p);
      lineRenderer.SetPosition(1, p);
      yield return null;
    }
    lineRenderer.SetPosition(1, points.end);

    if (!string.IsNullOrEmpty(labelText)) {
      DrawLabel(labelText, points.start.x, points.end.x);
    }

    foreach (var p in pointsBuffer) {
      Camera.transform.position = new Vector3(p.x, p.y, _initCameraZ);
      yield return null;
    }
    Camera.transform.position = new Vector3(points.end.x, points.end.y, _initCameraZ);
    switch (direction) {
      case LineDirection.Down:
        _currentStep++;
        break;
      case LineDirection.Right:
        _currentCallDepth++;
        break;
      case LineDirection.Left:
        _currentCallDepth--;
        break;
    }
  }

  private (Vector3 start, Vector3 end) CalcLinePosition(LineDirection direction) {
    if (direction == LineDirection.Left || direction == LineDirection.Right) {
      float x0 = _currentCallDepth * _horizontalLength;
      float x1 = x0 + _horizontalLength * (direction == LineDirection.Left ? -1 : 1);
      float y = - _currentStep * _verticalLength;
      return (new Vector3(x0, y, 0), new Vector3(x1, y, 0));
    } else if (direction == LineDirection.Down) {
      float x = _currentCallDepth * _horizontalLength;
      float y0 = - _currentStep * _verticalLength;
      float y1 = y0 - _verticalLength;
      return (new Vector3(x, y0, 0), new Vector3(x, y1, 0));
    } else {
      throw new System.ArgumentException();
    }
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
    ExampleButton.interactable = !_codeExecutor.IsRunning;
  }

  private IEnumerator UpdateButtonsTask() {
    UpdateButtons();
    yield return null;
  }
}
