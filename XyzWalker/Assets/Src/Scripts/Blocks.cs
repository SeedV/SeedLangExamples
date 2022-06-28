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

public class Blocks : MonoBehaviour {
  private const int _minSize = 10;
  private const int _maxSize = 100;
  private const float _unitSize = 1.0f;
  private const float _animInterval = .03f;
  private const int _animSteps = 60;
  private readonly Color _defaultBlockColor = Color.black;

  private static readonly List<Color> _blockColors = new List<Color> {
    new Color32(0xff, 0xff, 0xff, 0xff),
    new Color32(0xff, 0x99, 0x00, 0xff),
    new Color32(0xff, 0x00, 0x99, 0xff),
    new Color32(0x99, 0xff, 0x00, 0xff),
    new Color32(0x99, 0x00, 0xff, 0xff),
    new Color32(0x00, 0xff, 0x00, 0xff),
    new Color32(0x00, 0x99, 0xff, 0xff),
    new Color32(0xff, 0xff, 0x66, 0xff),
    new Color32(0xff, 0x66, 0xff, 0xff),
    new Color32(0x66, 0xff, 0xff, 0xff),
  };

  private readonly List<List<GameObject>> _blocks = new List<List<GameObject>>();

  public int Size => _size;

  private int _size = _minSize;
  private GameObject _blockRef;

  public void Start() {
    _blockRef = transform.Find("BlockRef")?.gameObject;
    Debug.Assert(!(_blockRef is null));
    _blockRef.SetActive(false);
    Resize(50);
    ResetColors();
  }

  public void ResetColors() {
    for (int row = 0; row < _size; row++) {
      for (int col = 0; col < _size; col++) {
        var block = _blocks[row][col];
        block.GetComponent<Renderer>().material.color = _defaultBlockColor;
      }
    }
  }

  public Vector3 GetBlockWorldPos(int row, int col) {
    return _blocks[row][col].transform.position;
  }

  public void Resize(int size) {
    Clear();
    _size = Mathf.Clamp(size, _minSize, _maxSize);
    Setup();
  }

  public IEnumerator SetBlockColor(int row, int col, int colorIndex) {
    var block = _blocks[row][col];
    var fromColor = block.GetComponent<Renderer>().material.color;
    var toColor = _blockColors[colorIndex % _blockColors.Count];
    if (fromColor != Color.black) {
      yield return ColorStep(block, fromColor, Color.black, _animSteps / 4);
    }
    yield return ColorStep(block, Color.black, toColor, _animSteps / 4);
    yield return ColorStep(block, toColor, Color.black, _animSteps / 4);
    yield return ColorStep(block, Color.black, toColor, _animSteps / 4);
  }

  private IEnumerator ColorStep(GameObject obj, Color fromColor, Color toColor, int steps) {
    for (int i = 1; i <= steps; i++) {
      var color = Vector4.Lerp(fromColor, toColor, (float)i / (float)steps);
      obj.GetComponent<Renderer>().material.color = color;
      yield return new WaitForSeconds(_animInterval);
    }
  }

  private void Clear() {
    foreach (var row in _blocks) {
      foreach (var block in row) {
        Object.Destroy(block);
      }
    }
    _blocks.Clear();
  }

  private void Setup() {
    var posRef = _blockRef.transform.localPosition;
    float startX = _size / 2.0f - 0.5f - (_size - 1) * _unitSize;
    float startZ = startX;
    for (int row = 0; row < _size; row++) {
      _blocks.Add(new List<GameObject>());
      for (int col = 0; col < _size; col++) {
        var block = Object.Instantiate(_blockRef, transform);
        float x = startX + _unitSize * col;
        float z = startZ + _unitSize * row;
        block.transform.localPosition = new Vector3(x, posRef.y, z);
        block.SetActive(true);
        _blocks[row].Add(block);
      }
    }
  }
}
