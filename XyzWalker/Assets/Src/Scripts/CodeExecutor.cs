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

using System.IO;
using System.Text;
using System.Threading;

using SeedLang;
using SeedLang.Common;
using SeedLang.Visualization;

// Executes python code with the SeedLang engine in a separate thread and queues animation actions
// to Unity's main thread when needed.
public class CodeExecutor
    : IVisualizer<Event.SingleStep>,
      IVisualizer<Event.Assignment> {
  private class ConsoleWriter : TextWriter {
    private readonly GameManager _gameManager;
    private readonly StringBuilder _buffer = new StringBuilder();

    public override System.Text.Encoding Encoding => Encoding.Default;

    public ConsoleWriter(GameManager gameManager) {
      _gameManager = gameManager;
    }

    public override void Write(char c) {
      if (c == '\n') {
        if (_buffer.Length > 0) {
          _gameManager.QueueOutputTextInfo(_buffer.ToString());
          _buffer.Clear();
        }
      } else {
        _buffer.Append(c);
      }
    }
  }

  private const string _defaultModuleName = "Program";
  private const int _minSleepInMilliSeconds = 10;
  private const float _singleStepWaitInSeconds = .1f;

  private readonly GameManager _gameManager;
  private readonly ConsoleWriter _consoleWriter;
  private readonly object _threadLock = new object();

  // If the executor thread is running.
  public bool IsRunning => !(_thread is null);

  // A switch for the caller to stop the code execution when the thread is running. It's safe for
  // another thread to flip this bool flag directly.
  public bool Stopping = false;

  private Thread _thread;
  private string _source;

  public CodeExecutor(GameManager gameManager) {
    _gameManager = gameManager;
    _consoleWriter = new ConsoleWriter(gameManager);
  }

  // Runs a SeedLang script. Returns false if the executor is already running.
  public bool Run(string source) {
    bool ret = true;
    lock (_threadLock) {
      if (_thread is null) {
        _source = source;
        Stopping = false;
        _thread = new Thread(ThreadEntry);
        _thread.Start();
      } else {
        ret = false;
      }
    }
    return ret;
  }

  public void On(Event.SingleStep e, IVM vm) {
    // Checks the stopping flag in the SingleStep callback.
    if (Stopping) {
      vm.Stop();
      Stopping = false;
    } else {
      // Highlights the current line.
      _gameManager.QueueHighlightCodeLineAndWait(e.Range.Start.Line, _singleStepWaitInSeconds);
      WaitForActionQueueComplete();
    }
  }

  public void On(Event.Assignment e, IVM vm) {
    // Monitors the assignment events for the variables "x", "y", "z", and "size".
    switch (e.Target.Variable.Name) {
      case "x":
        _gameManager.QueueVisualizeX((float)e.Value.Value.AsNumber());
        break;
      case "y":
        _gameManager.QueueVisualizeY((float)e.Value.Value.AsNumber());
        break;
      case "z":
        _gameManager.QueueVisualizeZ((float)e.Value.Value.AsNumber());
        break;
      case "size":
        _gameManager.QueueVisualizeSize((float)e.Value.Value.AsNumber());
        break;
    }
    WaitForActionQueueComplete();
  }

  // This method is used to synchronize the executor thread and the main UI thread.
  //
  // TODO: design and implement a better synchronizing solution between the UI and the executor.
  private void WaitForActionQueueComplete() {
    while (!_gameManager.IsActionQueueEmpty) {
      Thread.Sleep(_minSleepInMilliSeconds);
    }
  }

  // The main thread of the executor.
  private void ThreadEntry() {
    var engine = new Engine(SeedXLanguage.SeedPython, RunMode.Script);
    engine.RedirectStdout(_consoleWriter);
    engine.Register(this);
    var collection = new DiagnosticCollection();
    if (!engine.Compile(_source, _defaultModuleName, collection)) {
      _gameManager.QueueOutputSeedLangDiagnostics(collection);
    } else if (!engine.Run(collection)) {
      _gameManager.QueueOutputSeedLangDiagnostics(collection);
    } else {
      _gameManager.QueueHighlightCodeLineAndWait(-1, 0);
      _gameManager.QueueOutputTextInfo("Done.");
    }
    _thread = null;
    _gameManager.QueueOnExecutorComplete();
  }
}
