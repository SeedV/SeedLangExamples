using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CodeEditor {

  public class EditorManager : MonoBehaviour {
    private static readonly HashSet<char> _endCharsToAddIndent = new HashSet<char> { ':', '{' };

    public TMP_InputField CodeEditor;

    private TMP_Text _inputText;
    private TMP_Text _lineNoText;
    private TMP_Text _overlayText;

    void Start() {
      var textArea = CodeEditor.gameObject.transform.Find("TextArea");
      _inputText = textArea.Find("InputText").GetComponent<TMP_Text>();
      Debug.Assert(!(_inputText is null));
      _lineNoText = _inputText.gameObject.transform.Find("LineNoText")?.GetComponent<TMP_Text>();
      Debug.Assert(!(_lineNoText is null));
      _overlayText = _inputText.gameObject.transform.Find("OverlayText")?.GetComponent<TMP_Text>();
      Debug.Assert(!(_overlayText is null));

      // Sets the input focus to the code editor.
      EventSystem.current.SetSelectedGameObject(CodeEditor.gameObject);

      // The color of the main input text is white (hidden) and the caret color is set to the main
      // text color by default. We need to specifically set the caret color to show the caret while
      // hiding the main text.
      CodeEditor.customCaretColor = true;
      CodeEditor.caretColor = Color.black;

      // The text change handler must run in a coroutine since _inputText.textInfo won't be updated
      // until the next frame.
      CodeEditor.onValueChanged.AddListener((newInputText) => {
        StartCoroutine(UpdateEditor(newInputText));
      });
    }

    private IEnumerator UpdateEditor(string newInputText) {
      string coloredText = ColorCode(newInputText);
      _overlayText.text = coloredText;
      // Checks the enter key state and uses the state in the next frame.
      bool enterKey = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
      // Waits for the next frame to calculate and update the line no list, since
      // InputText.textInfo.lineInfo won't be updated until the next frame.
      yield return null;
      UpdateLineNoText();
      // AutoIndent() could change the content and the caret position of the input field. It must be
      // invoked in the next frame, otherwise, the content change will generate recursive
      // onValueChanged events and hang the application up.
      if (enterKey) {
        AutoIndent();
      }
    }

    private void AutoIndent() {
      int caretPos = CodeEditor.caretPosition;
      string inputText = _inputText.text;
      if (caretPos >= 1 && inputText[caretPos - 1] == '\n') {
        int lastLineEndPos = caretPos - 1;
        int lastLineStartPos = lastLineEndPos;
        while (lastLineStartPos >= 1 &&
               (inputText[lastLineStartPos - 1] != '\n')) {
          lastLineStartPos--;
        }
        if (inputText[lastLineStartPos] == '\n') {
          lastLineStartPos++;
        }
        if (lastLineStartPos >= lastLineEndPos) {
          // The last line is empty.
          return;
        }
        string lastLine = inputText.Substring(lastLineStartPos, lastLineEndPos - lastLineStartPos);
        int lastLineLeadingSpacesEndPos = GetLeadingSpacesEndPos(lastLine);
        int lastLineLastCharPos = GetLastNonSpaceCharPos(lastLine);

        var autoIndentText = new StringBuilder();
        autoIndentText.Append(inputText.Substring(0, caretPos));
        if (lastLineLeadingSpacesEndPos >= 0) {
          autoIndentText.Append(
              inputText.Substring(lastLineStartPos, lastLineLeadingSpacesEndPos + 1));
        }
        if (lastLineLastCharPos >= 0 &&
            GetAdditionalIndent(lastLine[lastLineLastCharPos], out string additionalIndent)) {
          autoIndentText.Append(additionalIndent);
        }
        int newCaretPos = autoIndentText.Length;
        if (caretPos < inputText.Length) {
          autoIndentText.Append(inputText.Substring(caretPos, inputText.Length - caretPos));
        }
        string newInputText = autoIndentText.ToString();
        _inputText.text = newInputText;
        CodeEditor.caretPosition = newCaretPos;
      }
    }

    private int GetLastNonSpaceCharPos(string line) {
      int i = line.Length - 1;
      while (i >= 0 && char.IsWhiteSpace(line[i])) {
        i--;
      }
      return i;
    }

    private int GetLeadingSpacesEndPos(string line) {
      int i = 0;
      while (i < line.Length && char.IsWhiteSpace(line[i])) {
        i++;
      }
      return i - 1;
    }

    private bool GetAdditionalIndent(char lastLineLastChar, out string additionalIndent) {
      if (_endCharsToAddIndent.Contains(lastLineLastChar)) {
        additionalIndent = "\t";
        return true;
      } else {
        additionalIndent = null;
        return false;
      }
    }

    private void UpdateLineNoText() {
      // Note that InputField.text is not suitable for the following calculation, since InputText.text
      // contains an extra leading zero-width space '\u200B' for layout purposes. The text positions
      // managed by InputText.textInfo.lineInfo are specific for InputText.text, instead of
      // InputField.text.
      string inputText = _inputText.text;
      var lineNoString = new StringBuilder();
      int lineNo = 1;
      bool isNewLine = true;
      for (int i = 0; i < _inputText.textInfo.lineCount; i++) {
        var info = _inputText.textInfo.lineInfo[i];
        string currentLine = inputText.Substring(info.firstCharacterIndex, info.characterCount);
        if (isNewLine) {
          lineNoString.AppendLine($"{lineNo++,4}");
          isNewLine = false;
        } else {
          lineNoString.AppendLine("");
        }
        isNewLine = currentLine.EndsWith("\n");
      }
      _lineNoText.text = lineNoString.ToString();
    }

    private string ReplaceMultipleChars(
        string src,
        IReadOnlyDictionary<char, string> subStringToReplacementMap) {
      var dest = new StringBuilder();
      foreach (char c in src) {
        if (subStringToReplacementMap.ContainsKey(c)) {
          dest.Append(subStringToReplacementMap[c]);
        } else {
          dest.Append(c);
        }
      }
      return dest.ToString();
    }

    private string ColorCode(string code) {
      if (string.IsNullOrEmpty(code.TrimEnd())) {
        return "";
      }
      var lines = code.Split('\n');
      MockSyntaxParser.ParseSyntaxTokens(code, out IReadOnlyList<TokenInfo> syntaxTokens);
      int lastLineIndex = 0;
      int currentCol = 0;
      var coloredTextBuffer = new StringBuilder();
      foreach (var token in syntaxTokens)
      {
        string color = "#333";
        switch (token.Type)
        {
          case TokenType.Boolean:
          case TokenType.Nil:
            color = "#930";
            break;
          case TokenType.OpenBracket:
          case TokenType.CloseBracket:
            color = "#333";
            break;
          case TokenType.Function:
            color = "#039";
            break;
          case TokenType.Keyword:
            color = "#00c";
            break;
          case TokenType.Number:
            color = "#0c0";
            break;
          case TokenType.Operator:
            color = "#3c6";
            break;
          case TokenType.Parameter:
            color = "#0cc";
            break;
          case TokenType.OpenParenthesis:
          case TokenType.CloseParenthesis:
            color = "#066";
            break;
          case TokenType.String:
            color = "#9cc";
            break;
          case TokenType.Symbol:
            color = "#990";
            break;
          case TokenType.Variable:
            color = "#c33";
            break;
          case TokenType.Unknown:
          default:
            color = "#999";
            break;
        }
        int currentLineIndex = token.Range.Start.Line - 1;
        int startCol = token.Range.Start.Column;
        int endCol = token.Range.End.Column;
        while (lastLineIndex < currentLineIndex)
        {
          while (currentCol < lines[lastLineIndex].Length)
          {
            coloredTextBuffer.Append(lines[lastLineIndex][currentCol]);
            currentCol++;
          }
          coloredTextBuffer.Append('\n');
          lastLineIndex++;
          currentCol = 0;
        }
        while (currentCol < startCol)
        {
          coloredTextBuffer.Append(lines[currentLineIndex][currentCol]);
          currentCol++;
        }
        string tokeText = lines[currentLineIndex].Substring(startCol, endCol - startCol + 1);
        string escapedTokenText = ReplaceMultipleChars(tokeText, new Dictionary<char, string>()
        {
          ['<'] = "<noparse><</noparse>",
          ['>'] = "<noparse>></noparse>",
        });
        coloredTextBuffer.Append($"<color={color}>{escapedTokenText}</color>");
        lastLineIndex = currentLineIndex;
        currentCol = endCol + 1;
      }
      coloredTextBuffer.Append('\n');
      return coloredTextBuffer.ToString();

    }
  }
}
