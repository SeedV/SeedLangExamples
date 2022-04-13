using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace CoroutineActions {
  // An action contains one or more Unity coroutine tasks that need to be run together.
  public class Action {
    private readonly List<ITask> _tasks = new List<ITask>();
    private readonly List<Coroutine> _runningCoroutines = new List<Coroutine>();

    // The action is done if and only if all its task coroutines have finished.
    public bool IsRunning => _runningCoroutines.Any(coroutine => !(coroutine is null));

    private MonoBehaviour _hostObject;
    private Mutex _mutex = new Mutex();

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

    // Tries to start all the task coroutines in a thread-safe way. Returns true if all the task
    // coroutines have been started.
    public bool TryStart() {
      _mutex.WaitOne();
      if (!IsRunning) {
        for (int taskId = 0; taskId < _tasks.Count; taskId++) {
          _runningCoroutines[taskId] = _hostObject.StartCoroutine(CoroutineWrapper(taskId));
        }
      } else {
        return false;
      }
      _mutex.ReleaseMutex();
      return true;
    }

    // Stops all the task coroutines in a thread-safe way.
    public void Stop() {
      _mutex.WaitOne();
      if (IsRunning) {
        for (int taskId = 0; taskId < _tasks.Count; taskId++) {
          _hostObject.StopCoroutine(_runningCoroutines[taskId]);
          _runningCoroutines[taskId] = null;
        }
      }
      _mutex.ReleaseMutex();
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
