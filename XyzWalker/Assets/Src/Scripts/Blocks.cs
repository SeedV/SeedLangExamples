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
  public const int MinSize = 10;
  public const int MaxSize = 40;

  private const float _unitSize = 1.0f;
  private const float _animInterval = .03f;
  private const int _animSteps = 15;
  private readonly Color _defaultBlockColor = new Color32(0xd8, 0xd8, 0xd8, 0xff);

  // The 16 basic web colors. See https://en.wikipedia.org/wiki/Web_colors
  private static readonly List<Color> _blockColors = new List<Color> {
    new Color32(0xff, 0xff, 0xff, 0xff),  // White
    new Color32(0xc0, 0xc0, 0xc0, 0xff),  // Silver
    new Color32(0x80, 0x80, 0x80, 0xff),  // Gray
    new Color32(0x00, 0x00, 0x00, 0xff),  // Black
    new Color32(0xff, 0x00, 0x00, 0xff),  // Red
    new Color32(0x80, 0x00, 0x00, 0xff),  // Maroon
    new Color32(0xff, 0xff, 0x00, 0xff),  // Yellow
    new Color32(0x80, 0x80, 0x00, 0xff),  // Olive
    new Color32(0x00, 0xff, 0x00, 0xff),  // Lime
    new Color32(0x00, 0x80, 0x00, 0xff),  // Green
    new Color32(0x00, 0xff, 0xff, 0xff),  // Aqua
    new Color32(0x00, 0x80, 0x80, 0xff),  // Teal
    new Color32(0x00, 0x00, 0xff, 0xff),  // Blue
    new Color32(0x00, 0x00, 0x80, 0xff),  // Navy
    new Color32(0xff, 0x00, 0xff, 0xff),  // Fuchsia
    new Color32(0x80, 0x00, 0x80, 0xff),  // Purple
  };

  private readonly List<List<GameObject>> _blocks = new List<List<GameObject>>();

  public int Size => _size;
  public int ColorNum => _blockColors.Count;

  private int _size = MinSize;
  private GameObject _blockRef;

  public void Start() {
    _blockRef = transform.Find("BlockRef")?.gameObject;
    Debug.Assert(!(_blockRef is null));
    _blockRef.SetActive(false);
    Setup();
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
    Debug.Assert(size >= MinSize & size <= MaxSize);
    Clear();
    _size = size;
    Setup();
  }

  public IEnumerator SetBlockColorCoroutine(int row, int col, int colorIndex) {
    Debug.Assert(colorIndex >= 0 && colorIndex < ColorNum);
    var block = _blocks[row][col];
    var fromColor = block.GetComponent<Renderer>().material.color;
    var toColor = _blockColors[colorIndex];
    if (fromColor != Color.black) {
      yield return ColorStepCoroutine(block, fromColor, _defaultBlockColor);
    }
    yield return ColorStepCoroutine(block, _defaultBlockColor, toColor);
  }

  private IEnumerator ColorStepCoroutine(GameObject obj,
                                         Color fromColor,
                                         Color toColor) {
    for (int i = 1; i <= _animSteps; i++) {
      var color = Vector4.Lerp(fromColor, toColor, (float)i / (float)_animSteps);
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
