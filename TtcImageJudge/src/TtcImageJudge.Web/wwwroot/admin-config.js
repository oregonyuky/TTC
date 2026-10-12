(function (root, factory) {
  const api = factory();
  if (typeof module === 'object' && module.exports) module.exports = api;
  root.TtcAdmin = api;
})(typeof globalThis !== 'undefined' ? globalThis : this, function () {
  function testAt(exercise, index) {
    const test = exercise?.tests?.[index];
    if (!test) throw new Error('Índice de teste inválido.');
    return test;
  }

  function referenceCode(exercise, index) {
    testAt(exercise, index);
    return exercise.tests.find(test => test.referenceCode)?.referenceCode || '';
  }

  function setReferenceCode(exercise, index, code) {
    testAt(exercise, index);
    for (const test of exercise.tests) {
      test.referenceCode = String(code ?? '');
      test.expectedOutputMode = 'referenceImplementation';
      test.expectedImageBase64 = null;
    }
    return exercise;
  }

  return { referenceCode, setReferenceCode };
});
