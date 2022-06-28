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
using UnityEngine;

public class Boy : MonoBehaviour {
  private const string _walkTrigger = "Walk";
  private const string _standTrigger = "Stand";
  private const string _jumpTrigger = "Jump";
  private const float _defaultAngleY = 135f;
  private const float _animInterval = .03f;
  private const int _rotateSteps = 20;
  private const float _walkStepsPerUnit = 3f;
  private const float _jumpTime = 2.1f;

  private Animator _animator;

  public void Start() {
    _animator = GetComponent<Animator>();
  }

  public void MoveToWorldPos(float toX, float toZ) {
    transform.localPosition = new Vector3(toX, transform.localPosition.y, toZ);
    transform.localEulerAngles = new Vector3(0, _defaultAngleY, 0);
  }

  public IEnumerator MoveToWorldPosCoroutine(float toX, float toZ) {
    var from = transform.position;
    var to = new Vector3(toX, from.y, toZ);
    float deltaX = toX - from.x;
    float deltaZ = toZ - from.z;
    float fromAngleY = (transform.eulerAngles.y + 360f) % 360f;
    float toAngleY = ((Mathf.Atan2(deltaX, deltaZ) * Mathf.Rad2Deg) + 360f) % 360f;

    // Rotates.
    _animator.SetTrigger(_walkTrigger);
    int steps = _rotateSteps;
    for (int i = 1; i <= steps; i++) {
      float angleY = Mathf.SmoothStep(fromAngleY, toAngleY, (float)i / (float)steps);
      transform.eulerAngles = new Vector3(0, angleY, 0);
      yield return new WaitForSeconds(_animInterval);
    }

    // Walks.
    float distance = Vector3.Distance(from, to);
    steps = (int)(_walkStepsPerUnit * distance) + 1;
    for (int i = 1; i <= steps; i++) {
      float t = (float)i / (float)steps;
      float x = Mathf.SmoothStep(from.x, to.x, t);
      float z = Mathf.SmoothStep(from.z, to.z, t);
      transform.position = new Vector3(x, from.y, z);
      yield return new WaitForSeconds(_animInterval);
    }

    _animator.SetTrigger(_standTrigger);
  }

  public IEnumerator JumpCoroutine() {
    _animator.SetTrigger(_jumpTrigger);
    yield return new WaitForSeconds(_jumpTime);
  }
}
