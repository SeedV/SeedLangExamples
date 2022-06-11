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
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Stacks2D : MonoBehaviour {
  private static readonly Color _emptyColor = new Color32(0xf0, 0xf0, 0xf0, 0xff);

  private List<List<GameObject>> _stackCubes = new List<List<GameObject>>();
  private List<int> _stackCubeNums = new List<int>();
  private List<TMP_Text> _stackTags = new List<TMP_Text>();

  // Clears a stack with animations.
  public IEnumerator Clear(int stackIndex) {
    Debug.Assert(stackIndex >= 0 && stackIndex < Config.StackCount);
    for (int i = _stackCubeNums[stackIndex] - 1; i >= 0; i--) {
      yield return new WaitForSeconds(Config.StackCubeSetupInterval);
      var cube2D = _stackCubes[stackIndex][i];
      cube2D.GetComponent<Image>().color = _emptyColor;
      SetTag(stackIndex, i);
    }
    _stackCubeNums[stackIndex] = 0;
  }

  // Fills a stack with the given number of cubes with animations.
  public IEnumerator Setup(int stackIndex, int cubeCount) {
    Debug.Assert(stackIndex >= 0 && stackIndex < Config.StackCount);
    Debug.Assert(cubeCount >= 0 && cubeCount <= Config.MaxCubesPerStack);
    yield return Clear(stackIndex);
    for (int i = 0; i < cubeCount; i++) {
      yield return new WaitForSeconds(Config.StackCubeSetupInterval);
      var cube2D = _stackCubes[stackIndex][i];
      cube2D.GetComponent<Image>().color = Config.GetStackColor(StackState.Normal);
      SetTag(stackIndex, i + 1);
    }
    _stackCubeNums[stackIndex] = cubeCount;
  }

  public IEnumerator Compare(int stackIndex1, int stackIndex2) {
    Debug.Assert(stackIndex1 >= 0 && stackIndex1 < Config.StackCount);
    Debug.Assert(stackIndex2 >= 0 && stackIndex2 < Config.StackCount);
    yield return FlashTwoStacks(stackIndex1, stackIndex2, StackState.BeingCompared);
  }

  public IEnumerator Swap(int stackIndex1, int stackIndex2) {
    Debug.Assert(stackIndex1 >= 0 && stackIndex1 < Config.StackCount);
    Debug.Assert(stackIndex2 >= 0 && stackIndex2 < Config.StackCount);
    yield return FlashTwoStacks(stackIndex1, stackIndex2, StackState.BeingSwapped);
    Utils.SwapListItems(_stackCubeNums, stackIndex1, stackIndex2);
    DrawStackCubes(stackIndex1, _stackCubeNums[stackIndex1]);
    DrawStackCubes(stackIndex2, _stackCubeNums[stackIndex2]);
    SetTag(stackIndex1, _stackCubeNums[stackIndex1]);
    SetTag(stackIndex2, _stackCubeNums[stackIndex2]);
  }

  void Start() {
    var grid = transform.Find("StackGrid")?.gameObject;
    Debug.Assert(!(grid is null));

    var refCube2D = grid.transform.Find("Cube2D")?.gameObject;
    Debug.Assert(!(refCube2D is null));
    refCube2D.gameObject.SetActive(false);

    var stackTagLine = transform.Find("StackTagLine")?.gameObject;
    Debug.Assert(!(stackTagLine is null));

    for (int i = 0; i < Config.StackCount; i++) {
      var tag = stackTagLine.transform.Find($"Tag{i}")?.GetComponent<TMP_Text>();
      Debug.Assert(!(tag is null));
      _stackTags.Add(tag);
      SetTag(i, 0);
    }

    for (int i = 0; i < Config.StackCount; i++) {
      _stackCubes.Add(new List<GameObject>(new GameObject[10]));
      _stackCubeNums.Add(0);
      for (int j = Config.MaxCubesPerStack - 1; j >= 0; j--) {
        var cube2D = Object.Instantiate(refCube2D, grid.transform);
        cube2D.GetComponent<Image>().color = _emptyColor;
        cube2D.gameObject.SetActive(true);
        _stackCubes[i][j] = cube2D;
      }
    }
  }

  private void DrawStackCubes(int stackIndex, int cubeNum) {
    for (int i = 0; i < Config.MaxCubesPerStack; i++) {
      var cube2D = _stackCubes[stackIndex][i];
      var color = i < cubeNum ? Config.GetStackColor(StackState.Normal) : _emptyColor;
      cube2D.GetComponent<Image>().color = color;
    }
  }

  private void SetTag(int stackIndex, int cubeNum) {
    Debug.Assert(stackIndex >= 0 && stackIndex < Config.StackCount);
    var tag = _stackTags[stackIndex];
    tag.text = $"{cubeNum:D2}";
  }

  private IEnumerator FlashTwoStacks(int stackIndex1, int stackIndex2, StackState state) {
    for (int i = 0; i < Config.StackCubeFlashTimes; i++) {
      SetStackState(stackIndex1, state);
      SetStackState(stackIndex2, state);
      yield return new WaitForSeconds(Config.StackCubeFlashInterval);
      SetStackState(stackIndex1, StackState.Normal);
      SetStackState(stackIndex2, StackState.Normal);
      yield return new WaitForSeconds(Config.StackCubeFlashInterval);
    }
  }

  private void SetStackState(int stackIndex, StackState state) {
    for (int i = 0; i < _stackCubeNums[stackIndex]; i++) {
      var cube2D = _stackCubes[stackIndex][i];
      cube2D.GetComponent<Image>().color = Config.GetStackColor(state);
    }
  }
}
