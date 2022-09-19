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
  private const int _cameraAnimSteps = 20;
  private const float _animInterval = .03f;
  private const float _cameraMinX = 200f;
  private const float _playSoundDelay = .8f;
  private readonly ActionQueue _actionQueue = new ActionQueue();

  public Camera Camera;
  public Button RunButton;
  public Button StopButton;
  public TMP_InputField CodeEditorInputField;
  public TMP_Text TextConsole;
  public Boy Boy;
  public Blocks Blocks;
  public AudioClip[] AudioClips;

  public bool IsActionQueueEmpty => _actionQueue.IsEmpty;

  private CodeExecutor _codeExecutor;
  private EditorManager _editor;
  private AudioSource _audioSource;

  // The (x, y, z) value to be visualized.
  private Vector3Int _currentValue;

  // The current ground size.
  private int _currentGroundSize;

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

    _audioSource = GetComponent<AudioSource>();

    Reset(true);

    // Starts and keeps the action queue running during the life cycle of the application.
    StartCoroutine(_actionQueue.Run());
  }

  public void OnRun() {
    Reset(true);
    _codeExecutor.Run(_editor.Text);
    UpdateButtons();
  }

  public void OnStop() {
    _codeExecutor.Stopping = true;
  }

  public void QueueVisualizeX(float x) {
    if (x < 0 || x > Blocks.Size - 1) {
      QueueOutputTextInfo($"x is clamped to [0, {Blocks.Size - 1}]");
    }
    int value = Utils.Modulo((int)x, Blocks.Size);
    if (_currentValue.x != value) {
      QueueMoveXY(value, _currentValue.y);
      _currentValue.x = value;
    }
  }

  public void QueueVisualizeY(float y) {
    if (y < 0 || y > Blocks.Size - 1) {
      QueueOutputTextInfo($"y is clamped to [0, {Blocks.Size - 1}]");
    }
    int value = Utils.Modulo((int)y, Blocks.Size);
    if (_currentValue.y != value) {
      QueueMoveXY(_currentValue.x, value);
      _currentValue.y = value;
    }
  }

  public void QueueVisualizeZ(float z) {
    if (z < 0 || z > Blocks.ColorNum - 1) {
      QueueOutputTextInfo($"z is clamped to [0, {Blocks.ColorNum - 1}]");
    }
    int colorIndex = Utils.Modulo((int)z, Blocks.ColorNum);
    int soundIndex = Utils.Modulo((int)z, AudioClips.Length);
    QueueSetColorAndPlaySound(_currentValue.x, _currentValue.y, colorIndex, soundIndex);
    _currentValue.z = colorIndex;
  }

  public void QueueVisualizeSize(float size) {
    if (size < Blocks.MinSize || size > Blocks.MaxSize) {
      QueueOutputTextInfo($"size is clamped to [{Blocks.MinSize}, {Blocks.MaxSize}]");
    }
    int newSize = Mathf.Clamp((int)size, Blocks.MinSize, Blocks.MaxSize);
    if (_currentGroundSize != newSize) {
      QueueResize(newSize);
      _currentGroundSize = newSize;
    }
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

  private void Reset(bool clearConsole) {
    if (clearConsole) {
      TextConsole.text = "";
    }
    _currentValue = Vector3Int.zero;
    _currentGroundSize = Blocks.Size;
    var pos = Blocks.GetBlockWorldPos(0, 0);
    Boy.MoveToWorldPos(pos.x, pos.z);
    Blocks.ResetColors();
  }

  private void QueueMoveXY(int x, int y) {
    var task = new Task2<int, int>(BoyMoveToTask, x, y);
    _actionQueue.Enqueue(new SingleTaskAction(this, task));
  }

  private void QueueSetColorAndPlaySound(int x, int y, int colorIndex, int soundIndex) {
    var taskBlocks = new Task3<int, int, int>(SetBlockColorTask, x, y, colorIndex);
    var taskBoy = new Task0(BoyJumpTask);
    var taskSound = new Task1<int>(PlaySoundTask, soundIndex);
    _actionQueue.Enqueue(new Action(this, new ITask[] {taskBlocks, taskBoy, taskSound}));
  }

  private void QueueResize(int size) {
    var task = new Task1<int>(ResizeTask, size);
    _actionQueue.Enqueue(new SingleTaskAction(this, task));
  }

  private IEnumerator BoyMoveToTask(int row, int col) {
    var worldPos = Blocks.GetBlockWorldPos(row, col);
    yield return Boy.MoveToWorldPosCoroutine(worldPos.x, worldPos.z);
  }

  private IEnumerator BoyJumpTask() {
    yield return Boy.JumpCoroutine();
  }

  private IEnumerator SetBlockColorTask(int row, int col, int colorIndex) {
    yield return Blocks.SetBlockColorCoroutine(row, col, colorIndex);
  }

  private IEnumerator PlaySoundTask(int soundIndex) {
    yield return new WaitForSeconds(_playSoundDelay);
    _audioSource.clip = AudioClips[soundIndex];
    _audioSource.Play();
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

  private IEnumerator ResizeTask(int size) {
    Blocks.Resize(size);
    yield return null;
    Reset(false);
    yield return null;

    // Zooms out the camera to meet the new scale of the ground size. The main camera's angle will
    // be kept as (45, -45, 0) in degrees, thus the yPos of the camera will be set to xPos *
    // sqrt(2).
    var currentCameraPos = Camera.transform.position;
    float cameraX = _cameraMinX / Blocks.MinSize * Blocks.Size;
    float cameraY = cameraX * Mathf.Sqrt(2f);
    float cameraZ = -cameraX;
    var targetCameraPos = new Vector3(cameraX, cameraY, cameraZ);

    for (int i = 0; i < _cameraAnimSteps; i++) {
      float t = (float)i / (float)_cameraAnimSteps;
      float x = Mathf.SmoothStep(currentCameraPos.x, targetCameraPos.x, t);
      float y = Mathf.SmoothStep(currentCameraPos.y, targetCameraPos.y, t);
      float z = Mathf.SmoothStep(currentCameraPos.z, targetCameraPos.z, t);
      Camera.transform.position = new Vector3(x, y, z);
      yield return new WaitForSeconds(_animInterval);
    }
  }
}
