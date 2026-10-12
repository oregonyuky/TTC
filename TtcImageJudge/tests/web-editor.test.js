const test = require('node:test');
const assert = require('node:assert/strict');
const editorConfig = require('../src/TtcImageJudge.Web/wwwroot/editor-config.js');

test('configura o Monaco para C# com quatro espaços e layout responsivo', () => {
  const options = editorConfig.editorOptions('public class Teste {}');
  assert.equal(options.language, 'csharp');
  assert.equal(options.tabSize, 4);
  assert.equal(options.insertSpaces, true);
  assert.equal(options.detectIndentation, false);
  assert.equal(options.automaticLayout, true);
  assert.equal(options.lineNumbers, 'on');
  assert.equal(options.matchBrackets, 'always');
  assert.equal(options.bracketPairColorization.enabled, true);
});

test('fecha delimitadores e indenta uma linha criada entre chaves', () => {
  const monaco = { languages: { IndentAction: { IndentOutdent: 2 } } };
  const language = editorConfig.languageConfiguration(monaco);
  const pairs = language.autoClosingPairs.map(pair => pair.open + pair.close);
  assert.deepEqual(pairs, ['{}', '[]', '()', '""', "''"]);
  assert.match('for (;;) {', language.onEnterRules[0].beforeText);
  assert.match('}', language.onEnterRules[0].afterText);
  assert.equal(language.onEnterRules[0].action.indentAction, 2);
});

test('monta a submissão com o valor atual obtido diretamente do editor', () => {
  let reads = 0;
  const editor = { getValue() { reads++; return 'public static void Main() {}'; } };
  assert.deepEqual(editorConfig.submissionPayload('exercise-1', editor), {
    exerciseId: 'exercise-1', sourceCode: 'public static void Main() {}'
  });
  assert.equal(reads, 1);
});
