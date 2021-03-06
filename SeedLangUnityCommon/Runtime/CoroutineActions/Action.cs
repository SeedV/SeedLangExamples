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
using System.Linq;
using UnityEngine;

namespace CoroutineActions {
  // An action contains one or more Unity coroutine tasks that need to be run together.
  public class Action {
    private readonly List<ITask> _tasks = new List<ITask>();
    private readonly List<Coroutine> _runningCoroutines = new List<Coroutine>();

    // The action is done if and only if all its task coroutines have finished.
    public bool IsRunning => _runningCoroutines.Any(coroutine => !(coroutine is null));

    private MonoBehaviour _hostObject;

    // hostObject must be a Unity MonoBehaviour object that can start a coroutine. tasks may contain
    // one or more coroutine tasks.
    public Action(MonoBehaviour hostObject, IEnumerable<ITask> tasks) {
      Debug.Assert(!(hostObject is null) && !(tasks is null));
      _hostObject = hostObject;
      foreach (var task in tasks) {
        if (!(task is null)) {
          _tasks.Add(task);
          _runningCoroutines.Add(null);
        }
      }
      Debug.Assert(_tasks.Count > 0);
    }

    // Tries to start all the task coroutines. Returns true if all the task coroutines have been
    // started.
    public bool TryStart() {
      if (!IsRunning) {
        for (int taskId = 0; taskId < _tasks.Count; taskId++) {
          _runningCoroutines[taskId] = _hostObject.StartCoroutine(CoroutineWrapper(taskId));
        }
      } else {
        return false;
      }
      return true;
    }

    // Stops all the task coroutines.
    public void Stop() {
      if (IsRunning) {
        for (int taskId = 0; taskId < _tasks.Count; taskId++) {
          _hostObject.StopCoroutine(_runningCoroutines[taskId]);
          _runningCoroutines[taskId] = null;
        }
      }
    }

    private IEnumerator CoroutineWrapper(int taskId) {
      var task = _tasks[taskId];
      yield return task.Run();
      // Sets the coroutine reference to null when the coroutine has finished.
      _runningCoroutines[taskId] = null;
    }
  }

  // Helper class to initialize an action with only one coroutine task.
  public class SingleTaskAction : Action {
    public SingleTaskAction(MonoBehaviour hostObject, ITask task) :
        base(hostObject, new ITask[] { task }) {
    }
  }
}
