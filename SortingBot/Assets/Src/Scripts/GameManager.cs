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
using TMPro;
using UnityEngine;

using CodeEditor;
using CoroutineActions;
using SeedLang.Common;

public class GameManager : MonoBehaviour {
  private const float _robotGotoStackZPos = -4f;
  private readonly ActionQueue _actionQueue = new ActionQueue();
  private readonly CodeExecutor _codeExecutor;

  public Robot Robot;
  public Stacks3D Stacks3D;
  public Stacks2D Stacks2D;
  public EditorManager CodeEditor;
  public Inspector Inspector;
  public TMP_Dropdown ExamplesDropdown;

  public bool IsActionQueueEmpty => _actionQueue.IsEmpty;

  public GameManager() {
    _codeExecutor = new CodeExecutor(this);
    // TODO: overload and redirect SeedLang runtime's print() function.
  }

  // Event handler when user runs the program.
  public void OnRun() {
    // TODO: disable the "Run" button when a program is running.
    Inspector.Clear();
    _codeExecutor.Run(CodeEditor.InputField.text);
  }

  // Event handler when user stops the running of the program.
  public void OnStop() {
    _codeExecutor.Stopping = true;
  }

  // Event handler when user loads an example code.
  public void OnLoadExample() {
    if (ExamplesDropdown.interactable &&
        ExamplesDropdown.value >= 0 &&
        ExamplesDropdown.value < ExampleCode.Examples.Count) {
      string code = ExampleCode.Examples[ExamplesDropdown.value].code;
      CodeEditor.InputField.text = code;
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

  public void QueueSetupStacks(IReadOnlyList<int> data) {
    int count = Mathf.Min(Config.StackCount, data.Count);
    for (int i = 0; i < Config.StackCount; i++) {
      int num = i < data.Count ? data[i] : 0;
      if (Stacks3D.GetStackCubeNum(i) > 0 || num > 0) {
        QueueRobotGotoStack(i);
        var task3D = new Task2<int, int>(Stacks3D.Setup, i, num);
        var task2D = new Task2<int, int>(Stacks2D.Setup, i, num);
        _actionQueue.Enqueue(new Action(this, new ITask[] { task3D, task2D }));
      }
    }
    QueueRobotGoHome();
  }

  public void QueueCompare(int stackIndex1, int stackIndex2) {
    if (stackIndex1 >= 0 && stackIndex1 < Config.StackCount &&
        stackIndex2 >= 0 && stackIndex2 < Config.StackCount) {
      var task3D = new Task2<int, int>(Stacks3D.Compare, stackIndex1, stackIndex2);
      var task2D = new Task2<int, int>(Stacks2D.Compare, stackIndex1, stackIndex2);
      _actionQueue.Enqueue(new Action(this, new ITask[] { task3D, task2D }));
    }
  }

  public void QueueSwap(int stackIndex1, int stackIndex2) {
    if (stackIndex1 >= 0 && stackIndex1 < Config.StackCount &&
        stackIndex2 >= 0 && stackIndex2 < Config.StackCount) {
      QueueRobotGotoCenterOfTwoStacks(stackIndex1, stackIndex2);
      var task3D = new Task2<int, int>(Stacks3D.Swap, stackIndex1, stackIndex2);
      var task2D = new Task2<int, int>(Stacks2D.Swap, stackIndex1, stackIndex2);
      _actionQueue.Enqueue(new Action(this, new ITask[] { task3D, task2D }));
      QueueRobotGoHome();
    }
  }

  void Start() {
    SetupExamplesDropdown();
    // Starts and keeps the action queue running during the life cycle of the application.
    StartCoroutine(_actionQueue.Run());
  }

  private void QueueRobotGoHome() {
    var task = new Task0(Robot.GoHome);
    _actionQueue.Enqueue(new SingleTaskAction(this, task));
  }

  private void QueueRobotGotoStack(int stackIndex) {
    var targetPos = Stacks3D.GetStackBasePos(stackIndex);
    targetPos.z = _robotGotoStackZPos;
    var task = new Task1<Vector3>(Robot.Goto, targetPos);
    _actionQueue.Enqueue(new SingleTaskAction(this, task));
  }

  private void QueueRobotGotoCenterOfTwoStacks(int stackIndex1, int stackIndex2) {
    var pos1 = Stacks3D.GetStackBasePos(stackIndex1);
    var pos2 = Stacks3D.GetStackBasePos(stackIndex2);
    var targetPos = (pos1 + pos2) / 2f;
    targetPos.z = _robotGotoStackZPos;
    var task = new Task1<Vector3>(Robot.Goto, targetPos);
    _actionQueue.Enqueue(new SingleTaskAction(this, task));
  }

  private void SetupExamplesDropdown() {
    var options = new List<string>();
    foreach (var example in ExampleCode.Examples) {
      options.Add(example.name);
    }
    ExamplesDropdown.AddOptions(options);
  }

  private IEnumerator OutputTextInfoTask(string info, bool append) {
    if (append) {
      Inspector.AppendTextInfo(info);
    } else {
      Inspector.OutputTextInfo(info);
    }
    yield return null;
  }

  private IEnumerator OutputSeedLangDiagnosticsTask(DiagnosticCollection collection, bool append) {
    if (append) {
      Inspector.AppendSeedLangDiagnostics(collection);
    } else {
      Inspector.OutputSeedLangDiagnostics(collection);
    }
    yield return null;
  }

  private IEnumerator HighlightCodeLineTask(int lineNo, float secondsToWait) {
    CodeEditor.HighlightLine(lineNo);
    yield return new WaitForSeconds(secondsToWait);
  }
}
