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

public static class ExampleCode {
  public static readonly List<(string name, string code)> Examples =
      new List<(string name, string code)> {

    ( "Bubble Sort",

@"# [[ Data ]]
a = [8, 1, 0, 5, 6, 3, 2, 4, 7, 1]

for i in range(len(a)):
    for j in range(len(a) - i - 1):
        if a[j] > a[j + 1]:
            # [[ Swap(j, j+1) ]]
            a[j], a[j + 1] = a[j + 1], a[j]

print(a)
"),

    ( "Selection Sort",

@"# [[ Data ]]
a = [8, 1, 0, 5, 6, 3, 2, 4, 7, 1]

for i in range(len(a)):
    # [[ Index ]]
    min = i
    for j in range(i + 1, len(a)):
        if a[min] > a[j]:
            min = j
    # [[ Swap(i, min) ]]
    a[i], a[min] = a[min], a[i]

print(a)
"),

      };
}
