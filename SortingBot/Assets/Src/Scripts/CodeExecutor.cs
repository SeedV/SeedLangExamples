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
  private const string _defaultModuleName = "Program";
  private const int _minSleepInMilliSeconds = 30;
  private const int _singleStepWaitInMilliSeconds = 1000;

  private static Mutex _mutex = new Mutex();
  private readonly GameManager _gameManager;
  private Thread _thread;

  private string _source;
  private Dictionary<string, VTagInfo> _currentVTags = new Dictionary<string, VTagInfo>();

  // Uses lower-case VTag names to support case-insensitive comparisons.
  private string _dataVTag = "data";
  private string _compareVTag = "compare";
  private string _swapVTag = "swap";

  public CodeExecutor(GameManager gameManager) {
    _gameManager = gameManager;
  }

  public bool Run(string source) {
    bool ret = true;
    _mutex.WaitOne();
    if (_thread is null) {
      _source = source;
      _thread = new Thread(ThreadEntry);
      _thread.Start();
    } else {
      ret = false;
    }
    _mutex.ReleaseMutex();
    return ret;
  }

  public void On(Event.SingleStep e) {
    _gameManager.QueueHighlightCodeLine(e.Range.Start.Line);
    if (_currentVTags.TryGetValue(_compareVTag, out VTagInfo tag)) {
      if (tag.Values[0].IsNumber && tag.Values[1].IsNumber) {
        int index1 = (int)(tag.Values[0].AsNumber());
        int index2 = (int)(tag.Values[1].AsNumber());
        _gameManager.QueueCompare(index1, index2);
        _currentVTags.Remove(_compareVTag);
      }
    }
    Thread.Sleep(_singleStepWaitInMilliSeconds);
  }

  public void On(Event.Assignment e) {
    if (_currentVTags.ContainsKey(_dataVTag) && e.Value.IsList) {
      _gameManager.QueueOutputTextInfo($"Data to sort: {e.Name} = {e.Value}");
      var intValueList = new List<int>();
      for (int i = 0; i < e.Value.Length; i++) {
        // TODO: Checks if the length of the array and the value of each item exceed the limit. If
        // so, reports the issue and stops the program.
        int intValue = (int)(e.Value[new Value(i)].AsNumber());
        intValueList.Add(intValue);
      }
      _gameManager.QueueSetupStacks(intValueList);
      while (!_gameManager.IsActionQueueEmpty) {
        Thread.Sleep(_minSleepInMilliSeconds);
      }
    } else {
      _gameManager.QueueOutputTextInfo($"Assigning: {e.Name} = {e.Value}");
    }
  }

  public void On(Event.VTagEntered e) {
    foreach (var tag in e.VTags) {
      string name = tag.Name.ToLower();
      _currentVTags.Add(name, tag);
    }
  }

  public void On(Event.VTagExited e) {
    foreach (var tag in e.VTags) {
      string name = tag.Name.ToLower();
      _currentVTags.Remove(name);
      if (name == _swapVTag && tag.Values[0].IsNumber && tag.Values[1].IsNumber) {
        int index1 = (int)(tag.Values[0].AsNumber());
        int index2 = (int)(tag.Values[1].AsNumber());
        _gameManager.QueueSwap(index1, index2);
      }
    }
  }

  private void ThreadEntry() {
    _gameManager.QueueOutputTextInfo("");
    var engine = new Engine(SeedXLanguage.SeedPython, RunMode.Script);
    engine.Register(this);
    var collection = new DiagnosticCollection();
    if (!engine.Compile(_source, _defaultModuleName, collection)) {
      _gameManager.QueueOutputSeedLangDiagnostics(collection);
    } else if (!engine.Run(collection)) {
      _gameManager.QueueOutputSeedLangDiagnostics(collection);
    } else {
      _gameManager.QueueHighlightCodeLine(-1);
      _gameManager.QueueOutputTextInfo("Done.");
    }
    _thread = null;
  }

}
