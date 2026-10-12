# TTC Image Judge

Juiz local, em português, para praticar processamento de imagens em C#. O aluno lê o problema, escreve uma função, envia e recebe um veredito baseado nos pixels produzidos — o texto da solução nunca é comparado ao gabarito.

## Arquitetura

```text
Browser :5071 → Web estático
                  ↓ HTTP local
               API :5072 → SQLite (exercícios, testes, gabaritos e histórico)
                  ↓ HTTP local
          Executor :5073 → AppContainer + Job Object → compilador/solução
```

- **Web**: ASP.NET Core servindo HTML/CSS/JS e Monaco Editor 0.52.2 versionado localmente, sem dependência de CDN em execução. O editor C# oferece indentação, pares automáticos, linhas, undo/redo e persiste o código em `localStorage`.
- **API**: endpoints de exercícios/submissões, SQLite e administração. A referência e imagens privadas são removidas da resposta pública. A administração exige loopback e `X-Admin-Key`.
- **Executor**: serviço Windows separado, ligado somente a `127.0.0.1`. Copia um worker .NET autocontido para uma pasta temporária exclusiva e executa compilador, referência e submissão dentro de um AppContainer descartável.
- **Worker**: usa Roslyn para compilar o wrapper real e carrega o assembly resultante dentro do processo isolado.
- **Core**: contratos, comparação de pixels e implementação independente usada nos testes unitários.

Essa separação mantém código não confiável fora da API. O AppContainer não recebe capacidades de rede, segredos, banco ou acesso aos arquivos da aplicação. Um Job Object limita memória e processos e encerra toda a árvore no timeout. A pasta temporária, perfil, ACL e desktop privado são removidos ao final.

## Requisitos

- Windows 10 ou 11 x64, inclusive **Windows Home**.
- SDK **.NET 10.0.401** ou versão compatível.
- Não é necessário Hyper-V, Windows Containers nem Docker para julgar.

`System.Drawing.Common 10` é suportado aqui somente no Windows. Por isso o executor tem alvo `net10.0-windows`. O Docker Desktop pode continuar em modo Linux para outros projetos; ele não participa do fluxo do juiz.

## Início rápido

Restaure, compile e teste a solução inteira. Compilar a solução também gera o worker autocontido `win-x64` usado pelo executor:

```powershell
dotnet restore .\TtcImageJudge.slnx
dotnet build .\TtcImageJudge.slnx
dotnet test .\TtcImageJudge.slnx --no-build
```

Copie os arquivos `appsettings.Local.example.json` da API e do executor para `appsettings.Local.json`. Defina uma chave administrativa forte apenas no arquivo local da API; esses arquivos são ignorados pelo Git.

Inicie, em três terminais e a partir desta pasta:

```powershell
dotnet run --project .\src\TtcImageJudge.Executor --urls http://127.0.0.1:5073
dotnet run --project .\src\TtcImageJudge.Api --urls http://127.0.0.1:5072
dotnet run --project .\src\TtcImageJudge.Web --urls http://127.0.0.1:5071
```

Abra `http://127.0.0.1:5071`. O endpoint `http://127.0.0.1:5073/health` deve informar `configured`, `AppContainer + Job Object` e `worker: autocontido`.

Em uma publicação, coloque a saída autocontida do worker em uma pasta `worker` ao lado do executor ou configure `Isolation:WorkerPath` com seu caminho. Se o worker ou as APIs nativas não estiverem disponíveis, o executor bloqueia a submissão e retorna **Falha de infraestrutura**; não existe fallback irrestrito.

## Isolamento no Windows Home

O executor usa recursos presentes no Windows Home:

1. cria um perfil AppContainer único por submissão, sem capacidade de rede;
2. cria um desktop privado para o processo LowBox e concede ao SID efêmero somente os acessos necessários;
3. concede acesso total apenas à pasta temporária exclusiva da tarefa;
4. inicia o worker suspenso, atribui-o ao Job Object e só então libera a execução;
5. aplica limites de memória, quantidade de processos e tempo e mata o Job Object inteiro no timeout;
6. remove a ACL temporária, fecha o desktop, apaga o perfil e limpa os arquivos em `finally`.

Na primeira tarefa, o executor habilita `HKCU\Console\LowBoxConsoleEnabled=1`, configuração recomendada pela ferramenta de referência da Microsoft para processos de console AppContainer. Nenhuma permissão administrativa é necessária. AppContainer é uma barreira de segurança do Windows, mas não é uma VM; esta primeira versão é deliberadamente local e não deve ser exposta como serviço público multiusuário.

## Exercício inicial e testes

O seed inclui “Filtro passa-baixa por média 5 × 5” com assinatura, formato 24bpp, bordas, divisão inteira, comparação exata, timeout, memória e acesso direto explicitados. A observação **K = 9** é registrada, mas não aplicada porque seu significado não foi esclarecido.

Os casos determinísticos cobrem constante, gradiente, impulso central, padrão alternado, ruído com semente fixa, imagem menor que 5 × 5 e largura 5 (stride 24bpp com padding). A referência usa `LockBits`, `Scan0`, ponteiros e o `Stride` real, preservando entrada e bordas. Ela é compilada e executada dentro do mesmo AppContainer; falhas nela são **Falha de infraestrutura**.

O diretório `samples/` contém uma solução correta com ponteiros e casos intencionalmente errados: divisor 9, janela 3 × 3, stride ignorado, bordas alteradas, erro de compilação e loop infinito.

O comparador trabalha sobre bitmaps decodificados, confere dimensões e R/G/B e produz contagem, percentual, primeira coordenada, delta máximo e mapa visual. Tolerância zero reprova com um único pixel divergente.

## Versionamento e gabaritos

Cada submissão grava as versões do exercício, referência e regras. Gabaritos gerados são armazenados em `expected_artifacts` com SHA-256 e a chave `(teste, versão da referência, versão das regras)`. Uma alteração administrativa remove caches incompatíveis; o executor também recalcula a referência no julgamento. Imagens esperadas enviadas diretamente ficam no payload persistido do exercício.

Os dois modos de teste são `UploadedImage` e `ReferenceImplementation`. A versão inicial usa referência C#; a API aceita ambos na área administrativa.

## Segurança e limites conhecidos

- O compilador e o código submetido executam no AppContainer; restauração de pacotes pelo código do aluno não é permitida.
- O AppContainer não recebe `internetClient`; além disso, executor e API escutam somente no loopback.
- A detecção de memória excedida usa o pico do Job Object. Encerramentos que não podem ser atribuídos com segurança ao aluno permanecem falha de infraestrutura.
- A análise Roslyn exige método `unsafe`, tipo ponteiro, par `LockBits/UnlockBits` e acessos `Scan0/Stride`. Isso é análise estrutural, não prova uso correto; a correção visual continua separada.
- Para implantação pública/multiusuário, ainda seriam necessários host dedicado, fila, autenticação de usuários, limitação de taxa e defesa operacional adicional.

## Validação neste ambiente

Validado no Windows 11 Home, build 26200, com .NET 10 em 12/10/2026:

- solução completa compilada sem avisos ou erros;
- testes unitários da referência independente, preservação de bordas/imagens pequenas, comparação exata, análise estrutural e persistência SQLite;
- `CorrectPointerSolution.cs` → **Resposta correta**;
- `WrongDivideBy9.cs`, `Wrong3x3.cs`, `WrongIgnoreStride.cs` e `WrongBorders.cs` → **Resposta incorreta**;
- `CompilationError.cs` → **Erro de compilação** com diagnóstico Roslyn;
- `TimeLimitExceeded.cs` → **Tempo excedido**, com encerramento da árvore pelo Job Object;
- indisponibilidade do executor → **Falha de infraestrutura**, não resposta incorreta;
- histórico e gabaritos persistidos no SQLite.

O Docker Linux instalado na máquina não foi usado por nenhuma dessas validações.
