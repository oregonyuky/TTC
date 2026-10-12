const API = 'http://127.0.0.1:5072/api';
const $ = id => document.getElementById(id);
const editorModels = new Map();
let current;
var codeEditor;
let initialEditorModel;

const verdicts = {
  accepted: 'Resposta correta', wrongAnswer: 'Resposta incorreta', compilationError: 'Erro de compilação',
  runtimeError: 'Erro de execução', timeLimitExceeded: 'Tempo excedido', memoryLimitExceeded: 'Memória excedida',
  requirementNotMet: 'Requisito não atendido', infrastructureFailure: 'Falha de infraestrutura'
};

const editorReady = new Promise((resolve, reject) => {
  if (typeof window.require !== 'function') {
    reject(new Error('O carregador do Monaco Editor não foi encontrado.'));
    return;
  }
  window.require.config({ paths: { vs: '/vendor/monaco/vs' } });
  window.require(['vs/editor/editor.main'], () => {
    monaco.languages.setLanguageConfiguration('csharp', TtcMonaco.languageConfiguration(monaco));
    $('editor').textContent = '';
    codeEditor = monaco.editor.create($('editor'), TtcMonaco.editorOptions(''));
    initialEditorModel = codeEditor.getModel();
    codeEditor.onDidChangeModelContent(() => {
      if (current) localStorage.setItem('ttc-code-' + current.id, codeEditor.getValue());
    });
    codeEditor.onDidChangeCursorPosition(updateCursor);
    codeEditor.addCommand(monaco.KeyMod.CtrlCmd | monaco.KeyCode.Enter, submit);
    resolve(codeEditor);
  }, error => reject(new Error('Não foi possível carregar o Monaco Editor: ' + error)));
});

async function start() {
  try {
    const exercises = await json('/exercises');
    $('exerciseCount').textContent = exercises.length;
    $('exerciseList').innerHTML = exercises.map((exercise, index) =>
      `<button class="exercise ${index ? '' : 'active'}" data-id="${exercise.id}"><span>${exercise.difficulty} · v${exercise.version}</span><strong>${esc(exercise.title)}</strong></button>`
    ).join('');
    document.querySelectorAll('.exercise').forEach(button => {
      button.onclick = () => load(button.dataset.id, button);
    });
    await editorReady;
    if (exercises.length) await load(exercises[0].id, document.querySelector('.exercise'));
    $('executorState').innerHTML = '<i></i><span>API conectada · executor isolado sob demanda</span>';
    $('executorState').classList.add('ok');
  } catch (error) {
    $('loading').textContent = error.message || 'Não foi possível conectar à API em 127.0.0.1:5072.';
  }
}

async function load(id, button) {
  await editorReady;
  current = await json('/exercises/' + id);
  document.querySelectorAll('.exercise').forEach(item => item.classList.remove('active'));
  button?.classList.add('active');
  $('loading').hidden = true;
  $('problem').hidden = false;
  $('codeSection').hidden = false;
  $('title').textContent = current.title;
  $('statement').textContent = current.statement;
  $('notes').textContent = current.notes;
  $('difficulty').textContent = current.difficulty;
  $('version').textContent = `V${current.version} · REF ${current.referenceVersion} · REGRAS ${current.rulesVersion}`;
  const rules = current.rules;
  $('rules').innerHTML = [
    ['Assinatura', rules.functionSignature], ['Entrada', rules.inputFormat], ['Bordas', rules.borderHandling],
    ['Aritmética', rules.roundingAndSaturation],
    ['Comparação', rules.exactComparison ? 'Exata — um pixel divergente reprova' : `Tolerância de ${rules.channelTolerance} por canal`],
    ['Limites', `${rules.timeLimitMs} ms · ${rules.memoryLimitMb} MB`],
    ['Memória direta', rules.requiresDirectMemoryAccess ? 'Obrigatória; análise estrutural + testes' : 'Não exigida']
  ].map(([label, value]) => `<div class="rule"><small>${label}</small>${esc(value)}</div>`).join('');
  $('tests').innerHTML = current.tests.map(test => `<span>${esc(test.name)} · ${test.width}×${test.height}</span>`).join('');
  selectExerciseModel(current);
  $('submitButton').disabled = false;
  history(false);
}

function selectExerciseModel(exercise) {
  let model = editorModels.get(exercise.id);
  if (!model) {
    const savedCode = localStorage.getItem('ttc-code-' + exercise.id) ?? exercise.starterCode;
    const uri = monaco.Uri.parse(`inmemory://ttc-image-judge/${exercise.id}/Solution.cs`);
    model = monaco.editor.createModel(savedCode, 'csharp', uri);
    model.updateOptions({ tabSize: 4, insertSpaces: true, trimAutoWhitespace: true });
    editorModels.set(exercise.id, model);
  }
  codeEditor.setModel(model);
  if (initialEditorModel) {
    initialEditorModel.dispose();
    initialEditorModel = null;
  }
  updateCursor({ position: codeEditor.getPosition() });
  requestAnimationFrame(() => { codeEditor.layout(); codeEditor.focus(); });
}

function updateCursor(event) {
  const position = event?.position || codeEditor?.getPosition();
  if (position) $('cursor').textContent = `Ln ${position.lineNumber}, Col ${position.column}`;
}

$('resetButton').onclick = async () => {
  if (current && confirm('Restaurar o código inicial deste exercício?')) {
    await editorReady;
    codeEditor.getModel().setValue(current.starterCode);
    codeEditor.focus();
  }
};
$('submitButton').onclick = submit;

async function submit() {
  if (!current) return;
  await editorReady;
  const button = $('submitButton');
  button.disabled = true;
  button.textContent = 'Julgando…';
  $('result').hidden = false;
  $('result').innerHTML = '<div class="verdict">Enviando ao executor isolado…</div>';
  try {
    // Monaco cuida apenas da edição/colorização; o backend continua sendo a autoridade do julgamento.
    const submission = await json('/submissions', {
      method: 'POST', headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(TtcMonaco.submissionPayload(current.id, codeEditor))
    });
    render(submission);
    history(false);
  } catch (error) {
    $('result').innerHTML = `<div class="verdict wrong">Falha ao enviar</div><p>${esc(error.message)}</p>`;
  } finally {
    button.disabled = false;
    button.innerHTML = 'Enviar solução <kbd>Ctrl↵</kbd>';
  }
}

function render(submission) {
  const name = verdicts[submission.verdict] ?? submission.verdict;
  const cssClass = submission.verdict === 'accepted' ? 'accepted' : submission.verdict === 'infrastructureFailure' ? '' : 'wrong';
  $('result').innerHTML = `<div class="verdict ${cssClass}">${name}</div><p>${esc(submission.message)}</p><small>Exercício v${submission.exerciseVersion} · referência v${submission.referenceVersion} · regras v${submission.rulesVersion}</small>${submission.compilerOutput ? `<pre class="output">${esc(submission.compilerOutput)}</pre>` : ''}`;
  const test = submission.tests?.find(item => item.difference) || submission.tests?.[0];
  if (test?.inputImageBase64) {
    $('visuals').hidden = false;
    [['inputImage', test.inputImageBase64], ['expectedImage', test.expectedImageBase64], ['actualImage', test.actualImageBase64], ['diffImage', test.difference?.differenceImageBase64]]
      .forEach(([id, data]) => $(id).src = data ? 'data:image/png;base64,' + data : '');
    const difference = test.difference;
    $('metrics').textContent = difference
      ? `${difference.differentPixels} de ${difference.totalPixels} pixels divergentes (${difference.percentage.toFixed(4)}%). Primeira divergência: ${difference.firstX === null ? '—' : `(${difference.firstX}, ${difference.firstY})`}. Delta máximo: ${difference.maxChannelDelta}.`
      : '';
  } else $('visuals').hidden = true;
}

async function history(show = true) {
  if (!current) return;
  const submissions = await json('/submissions?exerciseId=' + current.id);
  $('historyList').innerHTML = submissions.length ? submissions.map(submission =>
    `<button class="history-item" data-id="${submission.id}"><span><strong>${verdicts[submission.verdict] ?? submission.verdict}</strong><small>${new Date(submission.submittedAt).toLocaleString('pt-BR')}</small></span><small>v${submission.exerciseVersion}/r${submission.referenceVersion}</small></button>`
  ).join('') : '<p>Nenhuma submissão ainda.</p>';
  document.querySelectorAll('.history-item').forEach(item => {
    item.onclick = async () => render(await json('/submissions/' + item.dataset.id));
  });
  if (show) { $('history').hidden = false; $('history').scrollIntoView({ behavior: 'smooth' }); }
}

$('historyButton').onclick = () => history(true);
$('closeHistory').onclick = () => $('history').hidden = true;
$('adminButton').onclick = () => { $('adminJson').value = JSON.stringify(current, null, 2); $('adminDialog').showModal(); };
$('saveAdmin').onclick = async event => {
  event.preventDefault();
  try {
    const exercise = JSON.parse($('adminJson').value);
    exercise.version++;
    const response = await fetch(API + '/admin/exercises/' + exercise.id, {
      method: 'PUT', headers: { 'Content-Type': 'application/json', 'X-Admin-Key': $('adminKey').value },
      body: JSON.stringify(exercise)
    });
    if (!response.ok) throw Error('Acesso negado ou dados inválidos.');
    $('adminMessage').textContent = 'Versão salva; gabaritos incompatíveis foram invalidados.';
    setTimeout(() => location.reload(), 700);
  } catch (error) { $('adminMessage').textContent = error.message; }
};

async function json(path, options) {
  const response = await fetch(API + path, options);
  if (!response.ok) throw Error((await response.json().catch(() => ({}))).message || `HTTP ${response.status}`);
  return response.json();
}
function esc(value) {
  return String(value ?? '').replace(/[&<>"']/g, character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[character]));
}

start();
