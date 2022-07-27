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

// Executes a sorting code with the SeedLang engine by a coroutine and queues animation actions when
// needed.
public class CodeExecutor
    : IVisualizer<Event.SingleStep>,
      IVisualizer<Event.Assignment>,
      IVisualizer<Event.Comparison>,
      IVisualizer<Event.VTagEntered>,
      IVisualizer<Event.VTagExited> {
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
  private const int _minSleepInMilliSeconds = 30;
  private const float _singleStepWaitInSeconds = .5f;

  // Uses lower-case VTag names to support case-insensitive comparisons.
  private const string _dataVTag = "data";
  private const string _swapVTag = "swap";
  private const string _indexVTag = "index";

  private readonly GameManager _gameManager;
  private readonly ConsoleWriter _consoleWriter;
  private readonly Engine _engine = new Engine(SeedXLanguage.SeedPython, RunMode.Script);
  private readonly Dictionary<string, VTagInfo> _currentVTags = new Dictionary<string, VTagInfo>();

  // If the execution is running. It's treated as running if the engine is running or paused.
  public bool IsRunning => !_engine.IsStopped;

  // A switch for the caller to stop the code execution when the coroutine is running.
  public bool Stopping = false;

  private string _source;
  private string _dataVariableName;
  private string _indexVariableName;
  private int _currentIndexVariableValue = -1;

  public CodeExecutor(GameManager gameManager) {
    _gameManager = gameManager;
    _consoleWriter = new ConsoleWriter(gameManager);
    _engine.RedirectStdout(_consoleWriter);
    _engine.Register(this);
  }

  // Runs a SeedLang script. Returns false if the executor is already running.
  public bool Run(string source) {
    if (IsRunning) {
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

  public void On(Event.Assignment e, IVM vm) {
    if (_currentVTags.ContainsKey(_dataVTag) && e.RValue.IsTemporary && e.RValue.Value.IsList) {
      // Inside the data VTag, checks if the data list meets the requirements.
      if (e.RValue.Value.Length > Config.StackCount) {
        // TODO: makes all string messages localizable.
        _gameManager.QueueOutputTextInfo(
            $"The length of {e.Target.Variable.Name} exceeds the limit 0-{Config.StackCount}.");
        vm.Stop();
        return;
      }
      _dataVariableName = e.Target.Variable.Name;
      var intValueList = new List<int>();
      for (int i = 0; i < e.RValue.Value.Length; i++) {
        int intValue = (int)(e.RValue.Value[new Value(i)].AsNumber());
        if (intValue < 0 || intValue > Config.MaxCubesPerStack) {
          _gameManager.QueueOutputTextInfo(
              $"The value {e.Target.Variable.Name}[{i}] exceeds " +
              $"the limit 0-{Config.MaxCubesPerStack}.");
          vm.Stop();
          return;
        }
        intValueList.Add(intValue);
      }
      _gameManager.QueueOutputTextInfo($"Data to sort: {e.Target} = {e.RValue}");
      _gameManager.QueueSetupStacks(intValueList);
    } else if (_currentVTags.ContainsKey(_indexVTag) && e.RValue.Value.IsNumber) {
      // Inside the index VTag, records the index variable name and shows the index ball.
      _indexVariableName = e.Target.Variable.Name;
      UpdateIndexVariableValue((int)e.RValue.Value.AsNumber());
    } else if (e.Target.Variable.Name == _indexVariableName && e.RValue.Value.IsNumber) {
      // Otherwise, if the index variable is assigned, shows th index ball accordingly.
      UpdateIndexVariableValue((int)e.RValue.Value.AsNumber());
    } else {
      _gameManager.QueueOutputTextInfo($"Assigning: {e.Target} = {e.RValue}");
    }
  }

  public void On(Event.Comparison e, IVM vm) {
    if (e.Left.IsElement &&
        e.Left.Variable.Name == _dataVariableName &&
        e.Left.Keys.Count == 1 &&
        e.Right.IsElement &&
        e.Right.Variable.Name == _dataVariableName &&
        e.Right.Keys.Count == 1) {
      _gameManager.QueueOutputTextInfo($"Comparing: {e.Left} vs. {e.Right}");
      int index1 = (int)(e.Left.Keys[0].AsNumber());
      int index2 = (int)(e.Right.Keys[0].AsNumber());
      _gameManager.QueueCompare(index1, index2);
    }
  }

  public void On(Event.VTagEntered e, IVM vm) {
    foreach (var tag in e.VTags) {
      string name = tag.Name.ToLower();
      // For embedded VTags with the same name, only the last one matters.
      _currentVTags[name] = tag;
    }
  }

  public void On(Event.VTagExited e, IVM vm) {
    foreach (var tag in e.VTags) {
      // Handles the swap operation in the Exited event of the VTag.
      string name = tag.Name.ToLower();
      if (_currentVTags.ContainsKey(name)) {
        _currentVTags.Remove(name);
      }
      if (name == _swapVTag && tag.Values[0].IsNumber && tag.Values[1].IsNumber) {
        int index1 = (int)(tag.Values[0].AsNumber());
        int index2 = (int)(tag.Values[1].AsNumber());
        _gameManager.QueueSwap(index1, index2);
      }
    }
  }

  private void UpdateIndexVariableValue(int indexVariableValue) {
    _gameManager.QueueOutputTextInfo($"Index variable is set to {indexVariableValue}");
    _gameManager.QueueShowIndexBall(indexVariableValue, true);
    _currentIndexVariableValue = indexVariableValue;
  }

  // The coroutine to execute the source code.
  private IEnumerator RunProgram(string source) {
    var collection = new DiagnosticCollection();
    if (_engine.Compile(source, _defaultModuleName, collection)) {
      if (_engine.Run(collection)) {
        yield return new UnityEngine.WaitUntil(() => _gameManager.IsActionQueueEmpty);
        while (!Stopping && !_engine.IsStopped) {
          if (_engine.Continue(collection)) {
            yield return new UnityEngine.WaitUntil(() => _gameManager.IsActionQueueEmpty);
          } else {
            _gameManager.QueueOutputSeedLangDiagnostics(collection);
          }
        }
        if (Stopping) {
          _engine.Stop();
          Stopping = false;
        }
        _gameManager.QueueShowIndexBall(_currentIndexVariableValue, false);
        _gameManager.QueueHighlightCodeLineAndWait(-1, 0);
        _gameManager.QueueOutputTextInfo("Done.");
      }
    } else {
      _gameManager.QueueOutputSeedLangDiagnostics(collection);
    }
    _gameManager.QueueOnExecutorComplete();
  }
}
