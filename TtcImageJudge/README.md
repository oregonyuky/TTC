# TTC Image Judge

Juiz local, em português, para praticar processamento de imagens em C#. O aluno lê o problema, escreve uma função, envia e recebe um veredito baseado nos pixels produzidos — o texto da solução nunca é comparado ao gabarito.

## Arquitetura

```text
Browser :5071 → Web estático
                 ↓ HTTP local
              API :5072 → SQLite (exercícios, testes, gabaritos e histórico)
                 ↓ HTTP local
         Executor :5073 → Docker Windows sem rede → compilação → processo julgado
```

- **Web**: ASP.NET Core servindo HTML/CSS/JS sem dependências externas. O editor tem destaque de C#, linhas, Tab e persiste o código em `localStorage`.
- **API**: endpoints de exercícios/submissões, SQLite e administração. A referência e imagens privadas são removidas da resposta pública. A administração exige loopback e `X-Admin-Key`.
- **Executor**: serviço separado, ligado somente a `127.0.0.1`. Compila a referência e a solução de verdade em um contêiner Windows descartável. O processo de execução recebe limite de CPU, memória, PIDs e tempo.
- **Core**: contratos, comparação de pixels e implementação independente usada nos testes unitários.

A separação mantém código não confiável fora da API. O executor não monta o repositório, segredos ou banco: somente uma pasta temporária exclusiva. O contêiner usa `--network none`, é removido ao terminar e sua árvore é encerrada no timeout. A pasta temporária é apagada em `finally`.

## Requisitos

- Windows 11/Windows Server compatível com contêineres Windows.
- SDK **.NET 10.0.401** ou compatível.
- Para julgamento real: Docker Desktop/Engine alternado para **Windows containers** e compatível com `windowsservercore-ltsc2025`.

`System.Drawing.Common 10` é suportado aqui somente no Windows. Por isso frontend/API poderiam ser movidos para outro host, mas o executor é `net10.0-windows` e deve permanecer em Windows. Não habilite o modo Docker em Linux e não publique a porta do executor.

## Início rápido seguro

Restaure e teste:

```powershell
dotnet restore .\TtcImageJudge.slnx
dotnet test .\TtcImageJudge.slnx
```

Sem isolamento configurado, a aplicação funciona para navegação/persistência, mas toda submissão termina em **Falha de infraestrutura**. Isso é intencional: não existe fallback para execução irrestrita.

Para habilitar julgamento real, primeiro construa a imagem em um terminal com Docker em modo Windows:

```powershell
docker build -t ttc-image-judge-runner:net10-windows .\executor-image
```

Copie os dois `appsettings.Local.example.json` para `appsettings.Local.json`, defina uma chave administrativa forte e altere `Isolation.Mode` para `DockerWindows`. Arquivos locais são ignorados pelo Git. Alternativamente, use variáveis de ambiente:

```powershell
$env:Isolation__Mode='DockerWindows'
$env:Isolation__Image='ttc-image-judge-runner:net10-windows'
$env:Admin__Key='uma-chave-longa'
```

Inicie em três terminais, a partir desta pasta:

```powershell
dotnet run --project .\src\TtcImageJudge.Executor --urls http://127.0.0.1:5073
dotnet run --project .\src\TtcImageJudge.Api --urls http://127.0.0.1:5072
dotnet run --project .\src\TtcImageJudge.Web --urls http://127.0.0.1:5071
```

Abra `http://127.0.0.1:5071`.

## Exercício inicial e testes

O seed inclui “Filtro passa-baixa por média 5 × 5”, com assinatura, formato 24bpp, bordas, divisão inteira, comparação exata, timeout, memória e acesso direto explicitados. A observação **K = 9** é registrada, mas não aplicada porque seu significado não foi esclarecido.

Os casos determinísticos cobrem constante, gradiente, impulso central, padrão alternado, ruído com semente fixa, imagem menor que 5 × 5 e largura 5 (stride 24bpp com padding). A referência usa `LockBits`, `Scan0`, ponteiros e o `Stride` real, preservando entrada e bordas. Ela é compilada e executada no mesmo ambiente isolado antes da solução; falhas nela são **Falha de infraestrutura**.

O diretório `samples/` contém uma solução correta com ponteiros e casos intencionalmente errados: divisor 9, janela 3 × 3, stride ignorado, bordas alteradas, erro de compilação e loop infinito.

O comparador trabalha sobre bitmaps decodificados, confere dimensões e R/G/B e produz contagem, percentual, primeira coordenada, delta máximo e mapa visual. Tolerância zero reprova com um único pixel divergente.

## Versionamento e gabaritos

Cada submissão grava as versões do exercício, referência e regras. Gabaritos gerados são armazenados em `expected_artifacts` com SHA-256 e a chave `(teste, versão da referência, versão das regras)`. Uma alteração administrativa remove caches incompatíveis; o executor também recalcula a referência no julgamento, evitando aceitar um artefato obsoleto. Imagens esperadas enviadas diretamente ficam no payload persistido do exercício.

Dois modos de teste são modelados por `ExpectedOutputMode`: `UploadedImage` e `ReferenceImplementation`. A versão inicial usa referência C#; a API aceita os dois na área administrativa.

## Segurança e limites conhecidos

- O modo padrão é `Blocked`. Se Docker, imagem ou gabarito falharem, o aluno recebe **Falha de infraestrutura**, nunca resposta incorreta.
- O compilador roda em um contêiner sem rede, com 768 MB; o binário é executado em outro contêiner com os limites do exercício. O host mata e remove o contêiner pelo nome ao atingir o timeout.
- O Docker identifica OOM como memória excedida quando retorna código 137 ou mensagem equivalente. Outros encerramentos ambíguos permanecem falha de infraestrutura.
- A análise Roslyn exige método `unsafe`, tipo ponteiro, par `LockBits/UnlockBits` e acessos `Scan0/Stride`. Isso é análise estrutural, não prova uso correto; a correção visual continua separada e a interface informa essa limitação.
- Para uma implantação multiusuário, adicione fila, identidade e um host executor dedicado. Esta versão é deliberadamente local.

## Validação neste ambiente

Foi validado no Windows com .NET 10:

- build completo sem avisos;
- testes unitários da referência independente, impulso `255 / 25 = 10`, preservação de bordas/imagens pequenas e comparação exata;
- variantes matemáticas erradas rejeitadas pelo comparador;
- análise estrutural não aceita apenas a palavra `unsafe`;
- seed e histórico sobrevivem à reabertura do SQLite;
- Web responde e contém o editor;
- executor bloqueado informa falha de infraestrutura.

Não havia Docker Windows disponível/configurado durante a implementação. Portanto, a compilação dentro do contêiner, os exemplos de erro de compilação/timeout e `System.Drawing.Common` dentro da imagem **não foram declarados como executados**. Rode a matriz abaixo após construir a imagem:

```powershell
# enviar, pela interface, cada arquivo de samples/ e confirmar:
# CorrectPointerSolution.cs → Resposta correta
# Wrong*.cs               → Resposta incorreta
# CompilationError.cs     → Erro de compilação
# TimeLimitExceeded.cs    → Tempo excedido
```
