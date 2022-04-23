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

// Config info shared by all the visualization views.
public static class Config {
  // Number of cube stacks to be sorted.
  public const int StackCount = 10;

  // Maximum cubes per stack.
  public const int MaxCubesPerStack = 10;

  // The main color name of materials. E.g., the built-in pipeline uses "_Color" as the main color
  // name, while the URP pipeline uses "_BaseColor" as the main color name.
  public const string MainColorName = "_BaseColor";

  // Color definition for every stack state.
  private static readonly Dictionary<StackState, Color> _stackColors =
      new Dictionary<StackState, Color>() {
    [StackState.Normal] = new Color32(0x00, 0x99, 0xff, 0xff),
    [StackState.Highlighted] = new Color32(0xff, 0x99, 0x00, 0xff),
    [StackState.BeingCompared] = new Color32(0xff, 0xff, 0x00, 0xff),
    [StackState.BeingSwapped] = new Color32(0x00, 0xcc, 0x33, 0xff),
    [StackState.Sorted] = new Color32(0x00, 0xff, 0x33, 0xff),
  };

  public static Color GetStackColor(StackState state, float alpha = 1.0f) {
    var color = _stackColors[state];
    return new Color(color.r, color.g, color.b, alpha);
  }
}
