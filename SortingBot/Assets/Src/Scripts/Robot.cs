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

public class Robot : MonoBehaviour {
  private const float _epsilon = 0.01f;
  private const float _transitionTime = .25f;
  private Vector3 _originPos;

  public IEnumerator Goto(Vector3 targetPos) {
    // Ignores the y value that is passed in.
    targetPos.y = _originPos.y;
    var currentPos = transform.localPosition;
    var speed = Vector3.zero;
    while (Vector3.Distance(transform.localPosition, targetPos) > _epsilon) {
      transform.localPosition =
          Vector3.SmoothDamp(transform.localPosition, targetPos, ref speed, _transitionTime);
      yield return null;
    }
    transform.localPosition = targetPos;
    yield return null;
  }

  public IEnumerator GoHome() {
    yield return Goto(_originPos);
  }

  void Start() {
    _originPos = transform.localPosition;
  }
}
