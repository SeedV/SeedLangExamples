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
using UnityEngine;

public class Stacks3D : MonoBehaviour {
  private const float _cubeIntervalY = 11f;
  private const int _markersPerStack = 3;
  private const int _stackSwapSteps = 30;

  private readonly List<GameObject> _stackBases = new List<GameObject>();
  private readonly List<Vector3> _stackBasePositions = new List<Vector3>();
  private readonly List<List<GameObject>> _stackCubes = new List<List<GameObject>>();
  private readonly List<GameObject> _indexBalls = new List<GameObject>();
  private readonly List<GameObject> _inLineMarkers = new List<GameObject>();
  private readonly List<GameObject> _connectorMarkers = new List<GameObject>();

  private GameObject _cubeRef;
  private GameObject _indexBallRef;
  private float _cubeInitialY;

  // Gets the current cube number of a stack.
  public int GetStackCubeNum(int stackIndex) {
    Debug.Assert(stackIndex >= 0 && stackIndex < Config.StackCount);
    return _stackCubes[stackIndex].Count;
  }

  // Returns the world position of a stack's base position.
  public Vector3 GetStackBasePos(int stackIndex) {
    Debug.Assert(stackIndex >= 0 && stackIndex < Config.StackCount);
    return _stackBasePositions[stackIndex];
  }

  public void Reset() {
    foreach (var stackCubeList in _stackCubes) {
      foreach (var cube in stackCubeList) {
        Object.Destroy(cube);
      }
      stackCubeList.Clear();
    }

    for (int i = 0; i < _indexBalls.Count; i++) {
      Object.Destroy(_indexBalls[i]);
      _indexBalls[i] = null;
    }
  }

  // Clears a stack with animations.
  public IEnumerator Clear(int stackIndex) {
    Debug.Assert(stackIndex >= 0 && stackIndex < Config.StackCount);
    for (int i = _stackCubes[stackIndex].Count - 1; i >= 0; i--) {
      yield return new WaitForSeconds(Config.StackCubeSetupInterval);
      var cube = _stackCubes[stackIndex][i];
      Object.Destroy(cube);
    }
    _stackCubes[stackIndex].Clear();
    _indexBalls[stackIndex] = null;
  }

  // Fills a stack with the given number of cubes with animations.
  public IEnumerator Setup(int stackIndex, int cubeCount) {
    Debug.Assert(stackIndex >= 0 && stackIndex < Config.StackCount);
    Debug.Assert(cubeCount >= 0 && cubeCount <= Config.MaxCubesPerStack);
    yield return Clear(stackIndex);
    for (int i = 0; i < cubeCount; i++) {
      yield return new WaitForSeconds(Config.StackCubeSetupInterval);
      var cube = Object.Instantiate(_cubeRef, _stackBases[stackIndex].transform);
      cube.transform.localPosition = new Vector3(0, _cubeInitialY + i * _cubeIntervalY, 0);
      _stackCubes[stackIndex].Add(cube);
      var color = Config.GetStackColor(StackState.Normal);
      cube.GetComponent<Renderer>().material.SetColor(Config.MainColorName, color);
      cube.SetActive(true);
    }
    var indexBall = Object.Instantiate(_indexBallRef, _stackBases[stackIndex].transform);
    indexBall.transform.localPosition =
        new Vector3(0, _cubeInitialY + cubeCount * _cubeIntervalY, 0);
    _indexBalls[stackIndex] = indexBall;
    indexBall.SetActive(false);
  }

  public IEnumerator Compare(int stackIndex1, int stackIndex2) {
    Debug.Assert(stackIndex1 >= 0 && stackIndex1 < Config.StackCount);
    Debug.Assert(stackIndex2 >= 0 && stackIndex2 < Config.StackCount);
    yield return FlashTwoStacks(stackIndex1, stackIndex2,
                                StackState.BeingCompared, StackState.Normal);
  }

  public IEnumerator Swap(int stackIndex1, int stackIndex2) {
    Debug.Assert(stackIndex1 >= 0 && stackIndex1 < Config.StackCount);
    Debug.Assert(stackIndex2 >= 0 && stackIndex2 < Config.StackCount);
    yield return RotateTwoStacksAroundEachOther(stackIndex1, stackIndex2, StackState.BeingSwapped);
    SetStackState(stackIndex1, StackState.Normal);
    SetStackState(stackIndex2, StackState.Normal);
    Utils.SwapListItems(_stackBases, stackIndex1, stackIndex2);
    Utils.SwapListItems(_stackCubes, stackIndex1, stackIndex2);
    Utils.SwapListItems(_indexBalls, stackIndex1, stackIndex2);
  }

  public IEnumerator ShowIndexBall(int stackIndex, bool show) {
    Debug.Assert(stackIndex >= 0 && stackIndex < Config.StackCount);
    // Shows or hides the specified index ball while hiding all other balls.
    for (int i = 0; i < _indexBalls.Count; i++) {
      if (!(_indexBalls[i] is null)) {
        _indexBalls[i].SetActive(i == stackIndex ? show : false);
      }
    }
    yield return null;
  }

  void Start() {
    SetupStacks();
    SetupMarkers();
  }

  private void SetupStacks() {
    for (int i = 0; i < Config.StackCount; i++) {
      var stackBase = transform.Find($"Stack{i}")?.gameObject;
      Debug.Assert(!(stackBase is null));
      _stackBases.Add(stackBase);
      _stackBasePositions.Add(stackBase.transform.position);
      _stackCubes.Add(new List<GameObject>());
      _indexBalls.Add(null);
    }

    _cubeRef = _stackBases[0].transform.Find("Cube")?.gameObject;
    Debug.Assert(!(_cubeRef is null));
    _cubeInitialY = _cubeRef.transform.localPosition.y;
    _cubeRef.SetActive(false);

    _indexBallRef = _stackBases[0].transform.Find("Index")?.gameObject;
    Debug.Assert(!(_indexBallRef is null));
    _indexBallRef.SetActive(false);
  }

  private void SetupMarkers() {
    var markers = transform.Find("Markers")?.gameObject;
    Debug.Assert(!(markers is null));
    var markerRef = markers.transform.Find("Marker")?.gameObject;
    Debug.Assert(!(markerRef is null));
    var refPos = markerRef.transform.localPosition;
    markerRef.SetActive(false);

    float stackDistance = _stackBasePositions[1].x - _stackBasePositions[0].x;
    float markerSize = stackDistance / _markersPerStack;
    float fromX = _stackBasePositions[0].x;
    float toX = _stackBasePositions[_stackBasePositions.Count - 1].x;
    for (float x = fromX; x < toX + markerSize * .5f; x += markerSize) {
      var marker = Object.Instantiate(markerRef, markers.transform);
      marker.transform.localPosition = new Vector3(x, refPos.y, refPos.z);
      _inLineMarkers.Add(marker);
      marker.SetActive(false);
    }
    foreach (var stackPos in _stackBasePositions) {
      var marker = Object.Instantiate(markerRef, markers.transform);
      marker.transform.localPosition = new Vector3(stackPos.x, refPos.y, refPos.z + 1.0f);
      _connectorMarkers.Add(marker);
      marker.SetActive(false);
    }
  }

  private IEnumerator FlashTwoStacks(int stackIndex1, int stackIndex2,
                                     StackState state1, StackState state2) {
    if (stackIndex1 != stackIndex2) {
      for (int i = 0; i < Config.StackCubeFlashTimes; i++) {
        SetStackState(stackIndex1, state1);
        SetStackState(stackIndex2, state1);
        ShowConnectionMarkers(stackIndex1, stackIndex2, state1);
        yield return new WaitForSeconds(Config.StackCubeFlashInterval);
        SetStackState(stackIndex1, state2);
        SetStackState(stackIndex2, state2);
        // Markers never use the state 2 as their second color. They always switch between the first
        // state and the invisible state.
        ShowConnectionMarkers(stackIndex1, stackIndex2, StackState.Normal);
        yield return new WaitForSeconds(Config.StackCubeFlashInterval);
      }
    }
  }

  private IEnumerator RotateTwoStacksAroundEachOther(int stackIndex1, int stackIndex2,
                                                     StackState state) {
    if (stackIndex1 != stackIndex2) {
      SetStackState(stackIndex1, state);
      SetStackState(stackIndex2, state);
      var base1 = _stackBases[stackIndex1];
      var base2 = _stackBases[stackIndex2];
      var base1Pos = base1.transform.localPosition;
      var base2Pos = base2.transform.localPosition;
      var base1WorldPos = base1.transform.position;
      var base2WorldPos = base2.transform.position;
      var center = (base1WorldPos + base2WorldPos) / 2f;
      var axis = new Vector3(0, 1, 0);
      for (int i = 0; i < _stackSwapSteps; i++) {
        base1.transform.RotateAround(center, axis, 180.0f / _stackSwapSteps);
        base2.transform.RotateAround(center, axis, 180.0f / _stackSwapSteps);
        yield return null;
      }
      base1.transform.localPosition = base2Pos;
      base1.transform.eulerAngles = Vector3.zero;
      base2.transform.localPosition = base1Pos;
      base2.transform.eulerAngles = Vector3.zero;
      SetStackState(stackIndex1, StackState.Normal);
      SetStackState(stackIndex2, StackState.Normal);
    }
  }

  private void SetStackState(int stackIndex, StackState state) {
    var color = Config.GetStackColor(state);
    foreach (var cube in _stackCubes[stackIndex]) {
      cube.GetComponent<Renderer>().material.SetColor(Config.MainColorName, color);
    }
  }

  private void ShowConnectionMarkers(int stackIndex1, int stackIndex2, StackState state) {
    int indexMin = Mathf.Min(stackIndex1, stackIndex2);
    int indexMax = Mathf.Max(stackIndex1, stackIndex2);
    var markers = new List<GameObject>();
    markers.Add(_connectorMarkers[indexMin]);
    markers.Add(_connectorMarkers[indexMax]);
    for (int i = indexMin * _markersPerStack; i <= indexMax * _markersPerStack; i++) {
      markers.Add(_inLineMarkers[i]);
    }
    foreach (var marker in markers) {
      if (state == StackState.Normal) {
        marker.SetActive(false);
      } else {
        var color = Config.GetStackColor(state);
        marker.GetComponent<Renderer>().material.SetColor(Config.MainColorName, color);
        marker.SetActive(true);
      }
    }
  }
}
