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
using System.IO;
using System.Text;

using SeedLang;
using SeedLang.Common;
using SeedLang.Visualization;

// Executes python code with the SeedLang engine by a coroutine and queues animation actions when
// needed.
public class CodeExecutor
    : IVisualizer<Event.SingleStep>,
      IVisualizer<Event.FuncCalled>,
      IVisualizer<Event.FuncReturned> {
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
  private const float _singleStepWaitInSeconds = .3f;

  private readonly GameManager _gameManager;
  private readonly ConsoleWriter _consoleWriter;
  private readonly Engine _engine = new Engine(SeedXLanguage.SeedPython, RunMode.Script);
  private readonly HashSet<string> _excludeFuncNames = new HashSet<string> () {
    // TODO: Ignore all built-in function calls
    "print"
  };

  // If the execution is running. It's treated as running if the engine is running or paused.
  public bool IsRunning => !_engine.IsStopped;

  // A switch for the caller to stop the code execution when the coroutine is running.
  public bool Stopping = false;

  public CodeExecutor(GameManager gameManager) {
    _gameManager = gameManager;
    _consoleWriter = new ConsoleWriter(gameManager);
    _engine.RedirectStdout(_consoleWriter);
    _engine.Register(this);
  }

  // Runs a SeedLang script. Returns false if the executor is already running.
  public bool Run(string source) {
    if (IsRunning || string.IsNullOrEmpty(source)) {
      return false;
    }
    _gameManager.StartCoroutine(RunProgram(source));
    return true;
  }

  public void On(Event.SingleStep e, IVM vm) {
    // Highlights the current line.
    _gameManager.QueueHighlightCodeLineAndWait(e.Range.Start.Line, _singleStepWaitInSeconds);
    vm.Pause();
  }

  public void On(Event.FuncCalled e, IVM vm) {
    if (_excludeFuncNames.Contains(e.Name)) {
      return;
    }
    var args = new List<string>();
    foreach (var arg in e.Args) {
      args.Add(arg.ToString());
    }
    string label = $"{e.Name}({string.Join(',', args)})";
    _gameManager.QueueFuncCalled(label);
  }

  public void On(Event.FuncReturned e, IVM vm) {
    if (_excludeFuncNames.Contains(e.Name)) {
      return;
    }
    string label = $"return: {e.Result}";
    _gameManager.QueueFuncReturned(label);
  }

  // The coroutine to execute the source code.
  private IEnumerator RunProgram(string source) {
    _gameManager.QueueProgramStarted();
    var collection = new DiagnosticCollection();
    if (_engine.Compile(source, _defaultModuleName, collection) && _engine.Run(collection)) {
      yield return new UnityEngine.WaitUntil(() => _gameManager.IsActionQueueEmpty);
      while (!Stopping && !_engine.IsStopped) {
        if (_engine.Continue(collection)) {
          yield return new UnityEngine.WaitUntil(() => _gameManager.IsActionQueueEmpty);
        } else {
          _gameManager.QueueOutputSeedLangDiagnostics(collection);
          Stopping = true;
          break;
        }
      }
      if (Stopping) {
        _engine.Stop();
        Stopping = false;
      }
      _gameManager.QueueHighlightCodeLineAndWait(-1, 0);
      if (collection.Diagnostics.Count <= 0) {
        _gameManager.QueueOutputTextInfo("Done.");
      }
    } else {
      _gameManager.QueueOutputSeedLangDiagnostics(collection);
    }
    _gameManager.QueueOnExecutorComplete();
  }
}
