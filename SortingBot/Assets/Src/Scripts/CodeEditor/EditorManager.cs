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
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

using SeedLang;

namespace CodeEditor {
  // The main controller of the code editor.
  public class EditorManager : MonoBehaviour {
    // The root UI object of the code editor. It must be a TextMeshPro InputField object.
    public TMP_InputField InputField;

    // Enables or disables the auto-conversion from tab to spaces.
    public bool TabToSpaces = true;

    // SeedLang engine instance.
    private readonly Engine engine = new Engine(SeedXLanguage.SeedPython, RunMode.Script);

    // Number of spaces that a tab is equal to.
    public int TabSize = EditorConfig.DefaultTabSize;

    // The default text object of the InputField. Typically a TextMeshPro InputField uses one text
    // object to show the typed text. In Unity we set the color of this default text object to the
    // same color as the editor background, in order to hide the original text.
    private TMP_Text _inputText;

    // We set up an extra text object to show the formatted colorful text. It is called the overlay
    // text object. The overlay text object shares the same font and style settings with the default
    // text object, and is located on top of the default text object.
    //
    // We turn off the rich text feature for the InputField itself, while turning on the rich text
    // feature for the overlay text, so that the code editor accepts plain text input and outputs
    // formatted colorful text via the overlay text object.
    private TMP_Text _overlayText;

    // The text object to show the line numbers on the left side of the code editor. The InputField
    // object must have a left padding setting to make the line number text visible.
    //
    // Both the overlay text object and the line number text object must be the children of the text
    // area object, so that they can be scrolled together with the default text object when the
    // vertical scroll bar is valid.
    private TMP_Text _lineNoText;

    // A flag to disable the onValueChanged handler for the current frame, in order to avoid
    // re-entry of the handler.
    private bool disableOnChangeHandler = false;

    void Start() {
      var textArea = InputField.gameObject.transform.Find("TextArea");
      _inputText = textArea.Find("InputText").GetComponent<TMP_Text>();
      Debug.Assert(!(_inputText is null));
      _inputText.text = "";
      _lineNoText = _inputText.gameObject.transform.Find("LineNoText")?.GetComponent<TMP_Text>();
      Debug.Assert(!(_lineNoText is null));
      _lineNoText.text = "";
      _overlayText = _inputText.gameObject.transform.Find("OverlayText")?.GetComponent<TMP_Text>();
      Debug.Assert(!(_overlayText is null));
      _overlayText.text = "";

      // Focuses on the input filed of the code editor.
      EventSystem.current.SetSelectedGameObject(InputField.gameObject);

      // By default, InputField sets caret color to the same color as the default text object. Since
      // we have hidden the default text object, the caret color must be set specifically here.
      InputField.customCaretColor = true;
      InputField.caretColor = Color.black;

      InputField.onValueChanged.AddListener((code) => {
        if (disableOnChangeHandler) {
          disableOnChangeHandler = false;
        } else {
          // The editor updating logic must run in a coroutine since _inputText.textInfo won't be
          // updated until the next frame.
          StartCoroutine(UpdateEditor(code));
        }
      });
    }

    private IEnumerator UpdateEditor(string code) {
      var tokens = engine.ParseSyntaxTokens(code, "");
      bool enterKey = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
      int caretPos = InputField.caretPosition;
      string indention = enterKey ? AutoIndenter.GetIndention(code, caretPos) : null;
      string spacesPerTab = TabToSpaces ? new string(EditorConfig.Space, TabSize) : null;
      CodeFomatter.FormatAndColor(code,
                                  InputField.caretPosition,
                                  indention,
                                  TabToSpaces ? TabSize : 0,
                                  tokens,
                                  out string formatted,
                                  out bool changed,
                                  out int newCaretPos,
                                  out string formattedAndColored);

      _overlayText.text = formattedAndColored;

      // Waits for the next frame.
      yield return null;

      // Calculates and updates the line no list in the next frame, since
      // InputText.textInfo.lineInfo won't be updated until the next frame.
      UpdateLineNoText();

      // Updates the input text and the caret pos in the next frame.
      if (changed) {
        disableOnChangeHandler = true;
        InputField.text = formatted;
        InputField.caretPosition = newCaretPos;
      }
    }

    private void UpdateLineNoText() {
      // Note that InputField.text is not suitable for the following calculation, since
      // InputField.text contains an extra leading zero-width space '\u200B' for layout purposes.
      // _inputText.text and _inputText.textInfo must be used instead.
      string inputText = _inputText.text;
      var lineNoString = new StringBuilder();
      int lineNo = 1;
      bool isNewLine = true;
      for (int i = 0; i < _inputText.textInfo.lineCount; i++) {
        if (isNewLine) {
          lineNoString.AppendLine($"{lineNo++,4}");
          isNewLine = false;
        } else {
          lineNoString.AppendLine();
        }
        var info = _inputText.textInfo.lineInfo[i];
        var lineEndingChar = inputText[info.firstCharacterIndex + info.characterCount - 1];
        isNewLine = (lineEndingChar == EditorConfig.Ret);
      }
      _lineNoText.text = lineNoString.ToString();
    }
  }
}
