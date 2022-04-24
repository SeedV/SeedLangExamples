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

using System.Threading;
using UnityEngine;

using CodeEditor;
using CoroutineActions;

public class GameManager : MonoBehaviour {

  private readonly ActionQueue _actionQueue = new ActionQueue();

  public Stacks3D Stacks3D;
  public Stacks2D Stacks2D;
  public EditorManager CodeEditor;

  // Event handler when user runs the program.
  public void OnRun() {
    CodeEditor.HighlightLine(1);
    RunDemo();
  }

  // Event handler when user stops the running of the program.
  public void OnStop() {
  }

  void Start() {
    // Starts and keeps the action queue running during the life cycle of the application.
    StartCoroutine(_actionQueue.Run());
  }

  // Temp method to demo queuing actions from a separate thread.
  //
  // TODO: Replace this with a method to run SeedLang engine in a separate thread.
  private void RunDemo() {
    Thread demoThread = new Thread(DemoThreadEntry);
    demoThread.Start();
  }

  private void DemoThreadEntry() {
    // Enqueues demo animations to set up stacks in both 3D and 2D views.
    for (int i = 0; i < Config.StackCount; i++) {
      var task3D = new Task2<int, int>(Stacks3D.Setup, i, i + 1);
      var task2D = new Task2<int, int>(Stacks2D.Setup, i, i + 1);
      _actionQueue.Enqueue(new Action(this, new ITask[] { task3D, task2D }));
    }
  }
}
