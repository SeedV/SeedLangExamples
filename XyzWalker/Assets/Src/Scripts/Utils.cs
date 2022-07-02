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

using System;

public static class Utils {
  // Returns the modulo of a and b. C#'s % operator returns the signed reminder of a/b, which is
  // negative if a is a negative integer. This method implements the floored division algorithm
  // described at https://en.wikipedia.org/wiki/Modulo_operation.
  public static int Modulo(int a, int b) {
    return a - b * (int)Math.Floor((float)a / (float)b);
  }
}
