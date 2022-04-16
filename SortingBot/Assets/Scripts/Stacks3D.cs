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
  private const float _cubeAnimationInterval = .05f;
  private const float _cubeAlpha = 0.7f;

  private readonly List<GameObject> _stackBases = new List<GameObject>();
  private readonly List<List<GameObject>> _stackCubes = new List<List<GameObject>>();

  private GameObject _cubeRef;
  private float _cubeInitialY;
  private GameObject _markerRef;

  // Clears a stack with animations.
  public IEnumerator Clear(int stackIndex) {
    Debug.Assert(stackIndex >= 0 && stackIndex < Config.StackCount);
    for (int i = _stackCubes[stackIndex].Count - 1; i >= 0; i--) {
      yield return new WaitForSeconds(_cubeAnimationInterval);
      var cube = _stackCubes[stackIndex][i];
      cube.SetActive(false);
      Object.Destroy(cube);
    }
    _stackCubes[stackIndex].Clear();
  }

  // Fills a stack with the given number of cubes with animation.
  public IEnumerator Setup(int stackIndex, int cubeCount) {
    Debug.Assert(stackIndex >= 0 && stackIndex < Config.StackCount);
    Debug.Assert(cubeCount >= 0 && cubeCount <= Config.MaxCubesPerStack);
    yield return Clear(stackIndex);
    for (int i = 0; i < cubeCount; i++) {
      yield return new WaitForSeconds(_cubeAnimationInterval);
      var cube = Object.Instantiate(_cubeRef, _stackBases[stackIndex].transform);
      cube.transform.localPosition = new Vector3(0, _cubeInitialY + i * _cubeIntervalY, 0);
      var color = Config.GetStackColor(StackState.Normal, _cubeAlpha);
      cube.GetComponent<Renderer>().material.SetColor(Config.MainColorName, color);
      _stackCubes[stackIndex].Add(cube);
      cube.SetActive(true);
    }
  }

  void Start() {
    for (int i = 0; i < Config.StackCount; i++) {
      var stackBase = transform.Find($"Stack{i}")?.gameObject;
      Debug.Assert(!(stackBase is null));
      _stackBases.Add(stackBase);
      _stackCubes.Add(new List<GameObject>());
    }

    _cubeRef = _stackBases[0].transform.Find("Cube")?.gameObject;
    Debug.Assert(!(_cubeRef is null));
    _cubeInitialY = _cubeRef.transform.localPosition.y;
    _cubeRef.SetActive(false);

    _markerRef = transform.Find("Markers")?.Find("Marker")?.gameObject;
    Debug.Assert(!(_markerRef is null));
    _markerRef.SetActive(false);
  }
}
