window.addEventListener('DOMContentLoaded', () => {
  document.getElementById('adminKey').addEventListener('change', async event => {
    try {
      const response = await fetch(`http://127.0.0.1:5072/api/admin/exercises/${current.id}`, { headers: { 'X-Admin-Key': event.target.value } });
      if (!response.ok) throw new Error('Chave inválida.');
      document.getElementById('adminJson').value = JSON.stringify(await response.json(), null, 2);
      document.getElementById('adminMessage').textContent = 'Dados privados e referência carregados.';
    } catch (error) { document.getElementById('adminMessage').textContent = error.message; }
  });
  for (const [inputId, property] of [['adminInputImage', 'inputImageBase64'], ['adminExpectedImage', 'expectedImageBase64']]) {
    document.getElementById(inputId).addEventListener('change', event => {
      const file = event.target.files[0]; if (!file) return;
      const reader = new FileReader();
      reader.onload = () => {
        try {
          const model = JSON.parse(document.getElementById('adminJson').value);
          const index = Number(document.getElementById('adminTestIndex').value);
          if (!model.tests?.[index]) throw new Error('Índice de teste inválido.');
          model.tests[index][property] = String(reader.result).split(',')[1];
          model.tests[index].expectedOutputMode = 'uploadedImage';
          document.getElementById('adminJson').value = JSON.stringify(model, null, 2);
          document.getElementById('adminMessage').textContent = `${file.name} anexado ao teste ${index}.`;
        } catch (error) { document.getElementById('adminMessage').textContent = error.message; }
      };
      reader.readAsDataURL(file);
    });
  }
});
