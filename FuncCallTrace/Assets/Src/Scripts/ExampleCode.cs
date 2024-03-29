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

public static class ExampleCode {
  public static string Code = @"# Tower of Hanoi Problem

def move(n, src, dest, tmp):
    if n <= 0:
        return
    move(n - 1, src, tmp, dest)
    print(src + ' -> ' + dest)
    move(n - 1, tmp, dest, src)

num = 2
move(num, 'A', 'C', 'B')";
}
