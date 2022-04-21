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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Heatmap : MonoBehaviour {
  private static readonly Color _emptyColor = new Color32(0xe0, 0xe0, 0xe0, 0xff);
  private const int _cols = 32;
  private const int _rows = 20;

  private List<List<GameObject>> _map = new List<List<GameObject>>();

  void Start() {
    var grid = transform.Find("HeatmapGrid")?.gameObject;
    Debug.Assert(!(grid is null));

    var refUnit = grid.transform.Find("Unit")?.gameObject;
    Debug.Assert(!(refUnit is null));
    refUnit.gameObject.SetActive(false);

    for (int i = 0; i < _rows; i++) {
      _map.Add(new List<GameObject>());
      for (int j = 0; j < _cols; j++) {
        var unit = Object.Instantiate(refUnit, grid.transform);
        unit.GetComponent<Image>().color = _emptyColor;
        unit.gameObject.SetActive(true);
        _map[i].Add(unit);
      }
    }
  }
}
