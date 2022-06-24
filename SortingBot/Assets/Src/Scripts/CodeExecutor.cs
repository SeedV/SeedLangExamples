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

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

using SeedLang;
using SeedLang.Common;
using SeedLang.Visualization;

// Executes a sorting code with the SeedLang engine in a separate thread and queues animation
// actions to Unity's main thread when needed.
public class CodeExecutor
    : IVisualizer<Event.SingleStep>,
      IVisualizer<Event.Assignment>,
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
  private const string _compareVTag = "compare";
  private const string _swapVTag = "swap";

  private readonly GameManager _gameManager;
  private readonly ConsoleWriter _consoleWriter;
  private readonly Dictionary<string, VTagInfo> _currentVTags = new Dictionary<string, VTagInfo>();
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
    // Only checks the stopping flag in the SingleStep callback.
    if (Stopping) {
      vm.Stop();
      Stopping = false;
    } else {

      // Highlights the current line.
      _gameManager.QueueHighlightCodeLineAndWait(e.Range.Start.Line, _singleStepWaitInSeconds);

      // A temporary solution to check the compare VTag.
      //
      // TODO: Migrate to the Compare event when the event supports getting semantic variable info.
      if (_currentVTags.TryGetValue(_compareVTag, out VTagInfo tag)) {
        if (tag.Values[0].IsNumber && tag.Values[1].IsNumber) {
          int index1 = (int)(tag.Values[0].AsNumber());
          int index2 = (int)(tag.Values[1].AsNumber());
          _gameManager.QueueCompare(index1, index2);
          _currentVTags.Remove(_compareVTag);
        }
      }
      WaitForActionQueueComplete();
    }
  }

  public void On(Event.Assignment e, IVM vm) {
    if (_currentVTags.ContainsKey(_dataVTag) && e.Value.IsList) {
      // Inside the data VTag, checks if the data list meets the requirements.
      if (e.Value.Length > Config.StackCount) {
        // TODO: makes all string messages localizable.
        _gameManager.QueueOutputTextInfo(
            $"The length of {e.Name} exceeds the limit 0-{Config.StackCount}.");
        vm.Stop();
        return;
      }
      var intValueList = new List<int>();
      for (int i = 0; i < e.Value.Length; i++) {
        int intValue = (int)(e.Value[new Value(i)].AsNumber());
        if (intValue < 0 || intValue > Config.MaxCubesPerStack) {
          _gameManager.QueueOutputTextInfo(
              $"The value {e.Name}[{i}] exceeds the limit 0-{Config.MaxCubesPerStack}.");
          vm.Stop();
          return;
        }
        intValueList.Add(intValue);
      }
      _gameManager.QueueOutputTextInfo($"Data to sort: {e.Name} = {e.Value}");
      _gameManager.QueueSetupStacks(intValueList);
      WaitForActionQueueComplete();
    } else {
      _gameManager.QueueOutputTextInfo($"Assigning: {e.Name} = {e.Value}");
    }
  }

  public void On(Event.VTagEntered e, IVM vm) {
    foreach (var tag in e.VTags) {
      string name = tag.Name.ToLower();
      _currentVTags.Add(name, tag);
    }
  }

  public void On(Event.VTagExited e, IVM vm) {
    foreach (var tag in e.VTags) {
      // Handles the swap operation in the Exited event of the VTag.
      string name = tag.Name.ToLower();
      _currentVTags.Remove(name);
      if (name == _swapVTag && tag.Values[0].IsNumber && tag.Values[1].IsNumber) {
        int index1 = (int)(tag.Values[0].AsNumber());
        int index2 = (int)(tag.Values[1].AsNumber());
        _gameManager.QueueSwap(index1, index2);
        WaitForActionQueueComplete();
      }
    }
  }

  // This method is used to synchronize the executor thread and the main UI thread. For example, it
  // will be confusing if the main UI thread is still playing the swapping animation while the
  // executor thread has finished the program.
  //
  // TODO: design and implement a better synchronizing solution between the UI and the executor.
  private void WaitForActionQueueComplete() {
    while (!_gameManager.IsActionQueueEmpty) {
      Thread.Sleep(_minSleepInMilliSeconds);
    }
  }

  // The main thread of the executor.
  private void ThreadEntry() {
    _gameManager.QueueOutputTextInfo("");
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
