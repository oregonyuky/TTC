const test = require('node:test');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
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

test('versiona os arquivos do Monaco e informa falhas de carregamento', () => {
  const webRoot = path.join(__dirname, '..', 'src', 'TtcImageJudge.Web', 'wwwroot');
  const html = fs.readFileSync(path.join(webRoot, 'index.html'), 'utf8');
  const app = fs.readFileSync(path.join(webRoot, 'app.js'), 'utf8');
  assert.match(html, /loader\.js\?v=0\.52\.2-2/);
  assert.match(app, /urlArgs: 'v=0\.52\.2-2'/);
  assert.match(app, /O carregamento excedeu 15 segundos/);
  assert.match(app, /showEditorLoadError/);
});
