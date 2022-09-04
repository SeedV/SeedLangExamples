# SeedLangExamples

Example applications that demonstrate SeedLang's in-game scripting and
visualization features.

## AppleCalc

A simple project that shows the way how to embed SeedLang into a .Net console
application.

Here is an example run:

```shell
dotnet run --project AppleCalc
] 3+4*(5-3)-4
STEP 1: 🍎🍎🍎🍎🍎 - 🍎🍎🍎 = 🍎🍎
STEP 2: 🍎🍎🍎🍎 * 🍎🍎 = 🍎🍎🍎🍎🍎🍎🍎🍎
STEP 3: 🍎🍎🍎 + 🍎🍎🍎🍎🍎🍎🍎🍎 = 🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎
STEP 4: 🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎🍎 - 🍎🍎🍎🍎 = 🍎🍎🍎🍎🍎🍎🍎
Result: 🍎🍎🍎🍎🍎🍎🍎
] bye
```

## FuncCallTrace

A visualizer that traces the function call history of a script program then
draws the function call graph with Unity animations.

## SeedLangUnityCommon

Common libraries for integrating SeedLang with Unity games or applications:

- An in-game code editor for SeedLang. It supports modern IDE features such as
  syntax highlighting, auto indention, etc.
- A coroutine-based action queue framework to queue animations or other tasks
  into Unity's main thread. With this framework, we can run SeedLang in a
  separate thread and synchronize with Unity's main thread easily.

## SortingBot

A Unity project that visualizes common sorting algorithms with the visualization
framework of SeedLang.

## XyzWalker

A simple and interesting example that shows how to run a python script with
SeedLang and visualize the values of a set of variables during the runtime.
