let adminReferenceEditor;
let adminReferenceIndex = 0;
let updatingReferenceEditor = false;

const adminElement = id => document.getElementById(id);

function readAdminExercise() {
  return JSON.parse(adminElement('adminJson').value);
}

function writeAdminExercise(exercise) {
  adminElement('adminJson').value = JSON.stringify(exercise, null, 2);
}

function updateReferenceStatus(exercise, index) {
  const test = exercise.tests[index];
  adminElement('adminReferenceTest').textContent = test
    ? `Compartilhado pelos ${exercise.tests.length} testes · entrada selecionada: ${index}`
    : `Entrada selecionada: ${index}`;
}

function showReferenceForSelectedTest() {
  if (!adminReferenceEditor) return;
  try {
    const exercise = readAdminExercise();
    const index = Number(adminElement('adminTestIndex').value);
    updatingReferenceEditor = true;
    adminReferenceIndex = index;
    adminReferenceEditor.setValue(TtcAdmin.referenceCode(exercise, index));
    updateReferenceStatus(exercise, index);
  } catch (error) {
    adminElement('adminMessage').textContent = error.message;
  } finally {
    updatingReferenceEditor = false;
  }
}

function syncAdminReferenceToJson() {
  if (!adminReferenceEditor || updatingReferenceEditor) return;
  const exercise = readAdminExercise();
  TtcAdmin.setReferenceCode(exercise, adminReferenceIndex, adminReferenceEditor.getValue());
  writeAdminExercise(exercise);
  updateReferenceStatus(exercise, adminReferenceIndex);
}

async function openAdminReferenceEditor() {
  await editorReady;
  if (!adminReferenceEditor) {
    const options = TtcMonaco.editorOptions('');
    options.ariaLabel = 'Código C# correto usado para gerar a imagem esperada';
    options.minimap = { enabled: false };
    options.fontSize = 12;
    adminReferenceEditor = monaco.editor.create(adminElement('adminReferenceEditor'), options);
    adminReferenceEditor.onDidChangeModelContent(() => {
      if (updatingReferenceEditor) return;
      try {
        syncAdminReferenceToJson();
        adminElement('adminMessage').textContent =
          'Referência alterada para todos os testes. Salve a versão para invalidar os gabaritos antigos.';
      } catch (error) {
        adminElement('adminMessage').textContent = error.message;
      }
    });
  }
  showReferenceForSelectedTest();
  requestAnimationFrame(() => {
    adminReferenceEditor.layout();
    adminReferenceEditor.focus();
  });
}

window.openAdminReferenceEditor = openAdminReferenceEditor;
window.syncAdminReferenceToJson = syncAdminReferenceToJson;

window.addEventListener('DOMContentLoaded', () => {
  adminElement('adminKey').addEventListener('change', async event => {
    try {
      const response = await fetch(`http://127.0.0.1:5072/api/admin/exercises/${current.id}`, {
        headers: { 'X-Admin-Key': event.target.value }
      });
      if (!response.ok) throw new Error('Chave inválida.');
      const exercise = await response.json();
      writeAdminExercise(exercise);
      adminElement('adminTestIndex').max = Math.max(0, exercise.tests.length - 1);
      showReferenceForSelectedTest();
      adminElement('adminMessage').textContent = 'Dados privados e código de referência carregados.';
    } catch (error) {
      adminElement('adminMessage').textContent = error.message;
    }
  });

  adminElement('adminTestIndex').addEventListener('change', () => {
    try {
      syncAdminReferenceToJson();
      showReferenceForSelectedTest();
    } catch (error) {
      adminElement('adminMessage').textContent = error.message;
    }
  });

  adminElement('adminInputImage').addEventListener('change', event => {
    const file = event.target.files[0];
    if (!file) return;
    const reader = new FileReader();
    reader.onload = () => {
      try {
        syncAdminReferenceToJson();
        const exercise = readAdminExercise();
        const index = Number(adminElement('adminTestIndex').value);
        if (!exercise.tests?.[index]) throw new Error('Índice de teste inválido.');
        exercise.tests[index].inputImageBase64 = String(reader.result).split(',')[1];
        writeAdminExercise(exercise);
        adminElement('adminMessage').textContent = `${file.name} anexado como entrada do teste ${index}.`;
      } catch (error) {
        adminElement('adminMessage').textContent = error.message;
      }
    };
    reader.readAsDataURL(file);
  });
});
