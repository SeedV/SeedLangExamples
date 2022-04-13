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
using System.Collections.Concurrent;

namespace CoroutineActions {
  // A thread-safe queue to execute coroutine actions one by one. For each single action, the
  // coroutine tasks wrapped within the action will be executed in parallel.
  //
  // For example, given the following actions queued in an ActionQueue instance:
  //
  // - action_A (task_A1)
  // - action_B (task_B1, task_B2)
  // - action_C (task_C1)
  //
  // When the queue is executed, the execution order of the tasks will be:
  //
  // - Starts task_A1 and waits until task_A1 ends.
  // - Starts task_B1 and task_B2 and waits until both task_B1 and task_B2 end.
  // - Starts task_C1 and waits until task_C1 ends.
  public class ActionQueue {
    private ConcurrentQueue<Action> _queue = new ConcurrentQueue<Action>();
    private Action _currentAction = null;
    private bool _isStopping = false;

    public bool IsEmpty() => _queue.Count == 0 && _currentAction is null;

    // Enqueues an action to the tail of the queue. This method can be called from different
    // threads in parallel.
    public void Enqueue(Action action) {
      _queue.Enqueue(action);
    }

    // A coroutine to run the queued actions, and waits for new actions whenever the queue is empty.
    // This method must be called from the main Unity thread, with Unity's StartCoroutine() method.
    public IEnumerator Run() {
      while (!_isStopping) {
        OnUpdate();
        yield return null;
      }
    }

    // A coroutine to run the queued actions until the queue is empty. This method must be called
    // from the main Unity thread, with Unity's StartCoroutine() method.
    public IEnumerator RunUntilEmpty() {
      while (!_isStopping && !IsEmpty()) {
        OnUpdate();
        yield return null;
      }
    }

    // Stops the running queue.
    public void Stop() {
      if (!(_currentAction is null) && _currentAction.IsRunning) {
        _currentAction.Stop();
      }
      _isStopping = true;
    }

    // Updates the queue's state and keeps running the actions in the queue. This method must be
    // called from the main Unity thread repeatedly.
    //
    // Typically, you need to call this method in Update() or FixedUpdate() of a MonoBehaviour
    // object. You don't need this method if you run the queued actions with Run() or
    // RunUntilEmpty().
    public void OnUpdate() {
      if (!(_currentAction is null) && _currentAction.IsRunning) {
        return;
      }
      _currentAction = null;
      if (_queue.TryDequeue(out Action action)) {
        _currentAction = action;
        _currentAction.TryStart();
      }
    }
  }
}
