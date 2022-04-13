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

using System;
using System.Collections;

namespace CoroutineActions {
  // The common interface of a Unity coroutine task.
  public interface ITask {
    // Runs the task. See the concrete implementions in the following task classes.
    IEnumerator Run();
  }

  // A coroutine task that takes no argument.
  public class Task0 : ITask {
    // The entry function of the coroutine task.
    public Func<IEnumerator> TaskEntry { get; }

    public Task0(Func<IEnumerator> taskEntry) {
      TaskEntry = taskEntry;
    }

    // Runs the task.
    public IEnumerator Run() {
      return TaskEntry();
    }
  }

  // A coroutine task that takes 1 argument.
  public class Task1<T> : ITask {
    // The entry function of the coroutine task.
    public Func<T, IEnumerator> TaskEntry { get; }
    // The required argument.
    public T Arg { get; }

    public Task1(Func<T, IEnumerator> taskEntry, T arg) {
      TaskEntry = taskEntry;
      Arg = arg;
    }

    // Runs the task while passing its required argument in.
    public IEnumerator Run() {
      return TaskEntry(Arg);
    }
  }

  // A coroutine task that takes 2 arguments.
  public class Task2<T1, T2> : ITask {
    // The entry function of the coroutine task.
    public Func<T1, T2, IEnumerator> TaskEntry { get; }
    // The required arguments.
    public T1 Arg1 { get; }
    public T2 Arg2 { get; }

    public Task2(Func<T1, T2, IEnumerator> taskEntry, T1 arg1, T2 arg2) {
      TaskEntry = taskEntry;
      Arg1 = arg1;
      Arg2 = arg2;
    }

    // Runs the task while passing its required arguments in.
    public IEnumerator Run() {
      return TaskEntry(Arg1, Arg2);
    }
  }

  // A coroutine task that takes 3 arguments.
  public class Task3<T1, T2, T3> : ITask {
    // The entry function of the coroutine task.
    public Func<T1, T2, T3, IEnumerator> TaskEntry { get; }
    // The required arguments.
    public T1 Arg1 { get; }
    public T2 Arg2 { get; }
    public T3 Arg3 { get; }

    public Task3(Func<T1, T2, T3, IEnumerator> taskEntry, T1 arg1, T2 arg2, T3 arg3) {
      TaskEntry = taskEntry;
      Arg1 = arg1;
      Arg2 = arg2;
      Arg3 = arg3;
    }

    // Runs the task while passing its required arguments in.
    public IEnumerator Run() {
      return TaskEntry(Arg1, Arg2, Arg3);
    }
  }

  // A coroutine task that takes 4 arguments.
  public class Task4<T1, T2, T3, T4> : ITask {
    // The entry function of the coroutine task.
    public Func<T1, T2, T3, T4, IEnumerator> TaskEntry { get; }
    // The required arguments.
    public T1 Arg1 { get; }
    public T2 Arg2 { get; }
    public T3 Arg3 { get; }
    public T4 Arg4 { get; }

    public Task4(Func<T1, T2, T3, T4, IEnumerator> taskEntry, T1 arg1, T2 arg2, T3 arg3, T4 arg4) {
      TaskEntry = taskEntry;
      Arg1 = arg1;
      Arg2 = arg2;
      Arg3 = arg3;
      Arg4 = arg4;
    }

    // Runs the task while passing its required arguments in.
    public IEnumerator Run() {
      return TaskEntry(Arg1, Arg2, Arg3, Arg4);
    }
  }
}
