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
  private const float _startLineWidth = .08f;
  private const float _endLineWidth = .08f;
  private const float _cameraViewExpandRatio = 1.3f;
  private const int _cameraMoveSteps = 10;
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
  private int _maxCallDepth;

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
    if (_codeExecutor.Run(_editor.Text)) {
      UpdateButtons();
    }
  }

  public void OnStop() {
    _codeExecutor.Stopping = true;
  }

  public void OnLoadExample() {
    _editor.Text = ExampleCode.Code;
  }

  public void QueueProgramStarted() {
    var task = new Task2<LineDirection, string>(DrawLine, LineDirection.Down, null);
    _actionQueue.Enqueue(new SingleTaskAction(this, task));
  }

  public void QueueFuncCalled(string label) {
    var task = new Task2<LineDirection, string>(DrawLine, LineDirection.Right, label);
    _actionQueue.Enqueue(new SingleTaskAction(this, task));

    task = new Task2<LineDirection, string>(DrawLine, LineDirection.Down, null);
    _actionQueue.Enqueue(new SingleTaskAction(this, task));
  }

  public void QueueFuncReturned(string label) {
    var task = new Task2<LineDirection, string>(DrawLine, LineDirection.Left, label);
    _actionQueue.Enqueue(new SingleTaskAction(this, task));

    task = new Task2<LineDirection, string>(DrawLine, LineDirection.Down, null);
    _actionQueue.Enqueue(new SingleTaskAction(this, task));
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
    _maxCallDepth = 0;
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
    yield return AutoPositionCamera(direction);

    var points = CalcLinePosition(direction);
    var line = Object.Instantiate(_lineRef, TraceGraph.transform);
    line.SetActive(true);
    _graphElements.Add(line);
    var lineRenderer = line.GetComponent<LineRenderer>();
    lineRenderer.SetPosition(0, points.start);

    int steps = (int)(Vector3.Distance(points.start, points.end) / _deltaLengthPerStep);
    for (int i = 0; i <= steps; i++) {
      float t = Mathf.SmoothStep(0, 1, (float)i / (float)steps);
      lineRenderer.SetPosition(1, Vector3.Lerp(points.start, points.end, t));
      yield return null;
    }
    lineRenderer.SetPosition(1, points.end);

    if (!string.IsNullOrEmpty(labelText)) {
      DrawLabel(labelText, points.start.x, points.end.x);
    }

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
    if (_currentCallDepth > _maxCallDepth) {
      _maxCallDepth = _currentCallDepth;
    }
  }

  // Moves the main camera to the center of the graph and tries to cover the whole graph in the
  // camera view by adjusting the camera's z position.
  private IEnumerator AutoPositionCamera(LineDirection direction) {
    float steps = direction == LineDirection.Down ? _currentStep + 1 : _currentStep;
    float maxDepth =
        (direction == LineDirection.Right && _currentCallDepth == _maxCallDepth) ?
            _maxCallDepth + 1 :
            _maxCallDepth;
    float graphWidth = maxDepth * _horizontalLength;
    float graphHeight = steps * _verticalLength;
    float expectedHeight = graphWidth / Camera.aspect;
    float viewHeight = graphHeight >=  expectedHeight ? graphHeight : expectedHeight;
    viewHeight *= _cameraViewExpandRatio;
    var start = Camera.transform.position;
    float endX = graphWidth / 2;
    float endY = - graphHeight / 2;
    float endZ = - viewHeight / 2 / Mathf.Tan(Camera.fieldOfView / 2 * Mathf.Deg2Rad);
    var end = new Vector3(endX, endY, endZ);
    for (int i = 0; i <= _cameraMoveSteps; i++) {
      float t = Mathf.SmoothStep(0, 1, (float)i / (float)_cameraMoveSteps);
      Camera.transform.position = Vector3.Lerp(start, end, t);
      yield return null;
    }
    Camera.transform.position = end;
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
