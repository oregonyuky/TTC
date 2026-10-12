(function (root, factory) {
  const api = factory();
  if (typeof module === 'object' && module.exports) module.exports = api;
  root.TtcMonaco = api;
}(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  function languageConfiguration(monaco) {
    return {
      brackets: [['{', '}'], ['[', ']'], ['(', ')']],
      colorizedBracketPairs: [['{', '}'], ['[', ']'], ['(', ')']],
      autoClosingPairs: [
        { open: '{', close: '}' }, { open: '[', close: ']' }, { open: '(', close: ')' },
        { open: '"', close: '"', notIn: ['string', 'comment'] },
        { open: "'", close: "'", notIn: ['string', 'comment'] }
      ],
      surroundingPairs: [
        { open: '{', close: '}' }, { open: '[', close: ']' }, { open: '(', close: ')' },
        { open: '"', close: '"' }, { open: "'", close: "'" }
      ],
      indentationRules: {
        increaseIndentPattern: /^((?!\/\/).)*(\{[^}"']*|\([^\)"']*|\[[^\]"']*)$/,
        decreaseIndentPattern: /^\s*[\}\]\)].*$/
      },
      onEnterRules: [{
        beforeText: /^.*\{\s*$/,
        afterText: /^\s*\}/,
        action: { indentAction: monaco.languages.IndentAction.IndentOutdent }
      }]
    };
  }

  function editorOptions(value) {
    return {
      value: value || '',
      language: 'csharp',
      theme: 'vs-dark',
      ariaLabel: 'Editor de código C#',
      automaticLayout: true,
      lineNumbers: 'on',
      lineNumbersMinChars: 3,
      tabSize: 4,
      insertSpaces: true,
      detectIndentation: false,
      useTabStops: true,
      autoIndent: 'full',
      autoIndentOnPaste: true,
      formatOnType: true,
      autoClosingBrackets: 'always',
      autoClosingQuotes: 'always',
      autoClosingDelete: 'always',
      autoClosingOvertype: 'always',
      autoSurround: 'languageDefined',
      matchBrackets: 'always',
      bracketPairColorization: { enabled: true, independentColorPoolPerBracketType: true },
      guides: { bracketPairs: true, bracketPairsHorizontal: true, highlightActiveBracketPair: true, indentation: true },
      folding: true,
      showFoldingControls: 'mouseover',
      minimap: { enabled: false },
      scrollBeyondLastLine: false,
      smoothScrolling: true,
      fontFamily: '"Cascadia Code", Consolas, "Courier New", monospace',
      fontSize: 13,
      lineHeight: 22,
      padding: { top: 14, bottom: 14 },
      renderWhitespace: 'selection',
      wordWrap: 'off'
    };
  }

  function submissionPayload(exerciseId, editor) {
    return { exerciseId: exerciseId, sourceCode: editor.getValue() };
  }

  return { languageConfiguration, editorOptions, submissionPayload };
}));
