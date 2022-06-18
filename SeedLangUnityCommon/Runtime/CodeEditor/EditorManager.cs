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
using UnityEngine.UI;
using UnityEngine.EventSystems;

using SeedLang;

namespace CodeEditor {
  // The main controller of the code editor.
  public class EditorManager {
    // The wrap info of a code line. In our editor, a code line can be wrapped to more than one
    // editor lines.
    private class LineWrapInfo {
      private readonly List<TMP_LineInfo> _infoOfEditorLines = new List<TMP_LineInfo>();

      // The code line No (1-based index).
      public int LineNo { get; set; }
      // The first (0-based index, inclusive) editor line that holds the code line.
      public int EditorLineFrom { get; set; }
      // The last (0-based index, inclusive) editor line that holds the code line.
      public int EditorLineTo { get; set; }
      // The info of editor lines.
      public List<TMP_LineInfo> InfoOfEditorLines => _infoOfEditorLines;
    }

    // SeedLang engine instance.
    private readonly Engine _engine = new Engine(SeedXLanguage.SeedPython, RunMode.Script);

    public string Text {
      get {
        return _inputField.text;
      }
      set {
        _inputField.text = value;
      }
    }

    // Enables or disables the auto-conversion from tab to spaces.
    public bool TabToSpaces = true;

    // Number of spaces that a tab is equal to.
    public int TabSize = EditorConfig.DefaultTabSize;

    // A MonoBehavior object that can start a Unity coroutine.
    private MonoBehaviour _gameManager;

    // The root UI object of the code editor. It must be a TextMeshPro InputField object.
    private TMP_InputField _inputField;

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

    // A transparent rectangle bar to highlight a code line.
    private Image _highlighter;

    // A flag to disable the onValueChanged handler for the current frame, in order to avoid
    // re-entry of the handler.
    private bool disableOnChangeHandler = false;

    public EditorManager(MonoBehaviour gameManager,
                         TMP_InputField inputField,
                         TMP_Text inputText,
                         TMP_Text overlayText,
                         TMP_Text lineNoText,
                         Image highlighter) {
      _gameManager = gameManager;
      _inputField = inputField;
      _inputText = inputText;
      _overlayText = overlayText;
      _lineNoText = lineNoText;
      _highlighter = highlighter;

      _inputText.text = "";
      _lineNoText.text = "";
      _overlayText.text = "";
      _highlighter.gameObject.SetActive(false);

      // Focuses on the input filed of the code editor.
      EventSystem.current.SetSelectedGameObject(_inputField.gameObject);

      // By default, _inputField sets caret color to the same color as the default text object.
      // Since we have hidden the default text object, the caret color must be set specifically
      // here.
      _inputField.customCaretColor = true;
      _inputField.caretColor = Color.black;

      _inputField.onValueChanged.AddListener((code) => {
        if (disableOnChangeHandler) {
          disableOnChangeHandler = false;
        } else {
          // The editor updating logic must run in a coroutine since _inputText.textInfo won't be
          // updated until the next frame.
          _gameManager.StartCoroutine(UpdateEditor(code));
        }
      });
    }

    // Highlights a code line. lineNo starts from 1. Hides the highlight bar if lineNo <= 0.
    public void HighlightLine(int lineNo) {
      if (lineNo <= 0) {
        _highlighter.gameObject.SetActive(false);
      } else if (GetLineHeightAndCenter(lineNo, out float height, out float yCenter)) {
        _highlighter.rectTransform.sizeDelta = new Vector2(0, height);
        _highlighter.rectTransform.anchoredPosition = new Vector2(0, yCenter);
        _highlighter.gameObject.SetActive(true);
      }
    }

    private IEnumerator UpdateEditor(string code) {
      var tokens = _engine.ParseSyntaxTokens(code, "");
      bool enterKey = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
      int caretPos = _inputField.caretPosition;
      string indention = enterKey ? AutoIndenter.GetIndention(code, caretPos) : null;
      string spacesPerTab = TabToSpaces ? new string(EditorConfig.Space, TabSize) : null;
      CodeFomatter.FormatAndColor(code,
                                  _inputField.caretPosition,
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
        _inputField.text = formatted;
        _inputField.caretPosition = newCaretPos;
      }
    }

    private void UpdateLineNoText() {
      // Note that _inputField.text is not suitable for the following calculation, since
      // _inputField.text contains an extra leading zero-width space '\u200B' for layout purposes.
      // _inputText.text and _inputText.textInfo must be used instead.
      var wrapInfoOfLines = GetWrapInfoOfLines(0, 0);
      var lineNoString = new StringBuilder();
      foreach (var wrapInfo in wrapInfoOfLines) {
        lineNoString.AppendLine($"{wrapInfo.LineNo,4}");
        for (int i = wrapInfo.EditorLineFrom + 1; i <= wrapInfo.EditorLineTo; i++) {
          lineNoString.AppendLine();
        }
      }
      _lineNoText.text = lineNoString.ToString();
    }

    // Gets the height and the Y center position of a code line. If the code line is wrapped to
    // multiple editor lines, returns the total height and the Y center of all the editor lines.
    // Returns false if lineNo is out of the range of the current code.
    private bool GetLineHeightAndCenter(int lineNo, out float height, out float yCenter) {
      var wrapInfoOfLines = GetWrapInfoOfLines(lineNo, lineNo);
      if (wrapInfoOfLines.Count > 0) {
        Debug.Assert(wrapInfoOfLines.Count == 1);
        int editorLineNum = wrapInfoOfLines[0].InfoOfEditorLines.Count;
        float yFrom = wrapInfoOfLines[0].InfoOfEditorLines[0].lineExtents.max.y;
        float yTo = wrapInfoOfLines[0].InfoOfEditorLines[editorLineNum - 1].lineExtents.min.y;
        height = Mathf.Abs(yTo - yFrom);
        yCenter = (yTo + yFrom) / 2.0f;
        return true;
      } else {
        height = 0;
        yCenter = 0;
        return false;
      }
    }

    // A code line can be wrapped to more than one editor lines. This method gets the wrap info of
    // code lines. Both fromLineNo and toLineNo are 1-based and inclusive. Iterates from the first
    // code line if fromLineNo <= 0. Iterates to the last code line if toLineNo <= 0.
    private IReadOnlyList<LineWrapInfo> GetWrapInfoOfLines(int fromLineNo, int toLineNo) {
      var wrapInfoOfLines = new List<LineWrapInfo>();
      string inputText = _inputText.text;
      int lineNo = 1;
      int editorLineFrom = 0;
      for (int i = 0; i < _inputText.textInfo.lineCount; i++) {
        var info = _inputText.textInfo.lineInfo[i];
        var lineEndingChar = inputText[info.firstCharacterIndex + info.characterCount - 1];
        if (lineEndingChar == EditorConfig.Ret || i == _inputText.textInfo.lineCount - 1) {
          if (toLineNo > 0 && lineNo > toLineNo) {
            break;
          }
          if (lineNo >= fromLineNo) {
            var wrapInfo = new LineWrapInfo {
              LineNo = lineNo,
              EditorLineFrom = editorLineFrom,
              EditorLineTo = i,
            };
            for (int j = editorLineFrom; j <= i; j++) {
              wrapInfo.InfoOfEditorLines.Add(_inputText.textInfo.lineInfo[j]);
            }
            wrapInfoOfLines.Add(wrapInfo);
          }
          lineNo++;
          editorLineFrom = i + 1;
        }
      }
      return wrapInfoOfLines;
    }
  }
}
