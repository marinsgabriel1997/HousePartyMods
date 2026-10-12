# Instruções para agentes

## Projeto e ambiente

- Este repositório contém mods independentes para House Party: cada mod em `mods/` produz sua própria DLL.
- A stack atual é C# com alvo `net6.0`, MelonLoader, Harmony e interoperabilidade Unity IL2CPP. Os scripts em `tools/` usam Python para analisar arquivos `.character`.
- `Directory.Build.props` concentra configuração e referências comuns. Cada `.csproj` declara somente suas particularidades.
- A compilação depende da instalação local do jogo com MelonLoader executado ao menos uma vez para gerar os assemblies.
- O README declara como ambiente testado House Party 1.5.2.14289, Unity 2022.3.62f2 e MelonLoader 0.7.3. Não interpretar isso como verificação do ambiente local ou promessa de suporte a outras versões.

## Comandos

- Build documentado: `dotnet build HousePartyMods.slnx -c Release`.
- Para outra instalação: `dotnet build HousePartyMods.slnx -c Release -p:GameDir="D:\Jogos\House Party"`.
- A saída dos mods fica em `mods/<Mod>/bin/Release/net6.0/`.
- Confirmar que o SDK disponível suporta a solução `.slnx`; não presumir que qualquer SDK capaz de compilar `net6.0` aceita esse formato.
- Não inventar comandos oficiais de teste, lint, instalação ou geração. Relatar dependências ausentes e impedimentos de compilação.

## Fluxo de trabalho e entrega

- Em pedidos de revisão, trabalhar em leitura e apresentar os achados. Implementar correções somente quando a solicitação também autorizar edição.
- Antes de implementar, alinhar objetivo, escopo, proposta técnica, riscos e critérios de aceite; criar ou atualizar o plano correspondente e obter aprovação explícita desse plano.
- A aprovação do plano autoriza somente seu escopo. Mudanças materiais de escopo, contrato ou arquitetura exigem revisão do plano e nova aprovação.
- Implementar incrementalmente, acompanhar o plano e executar as validações previstas.
- Quando a validação depender do jogo, solicitar ao usuário a verificação no ambiente real com cenários e resultados esperados claros.
- Atualizar documentação e histórico do mod afetado quando aplicável.
- Antes de um commit, revisar o diff, apresentar a mensagem proposta e obter autorização explícita. Aplicar a política de versão e histórico pertinente e executar as validações finais antes de realizar o commit.
- Não realizar push ou publicação sem autorização explícita específica.

## Planos

- Manter planos ativos em `planos/p<prioridade>-<nome-do-plano>.md`, por exemplo `planos/p10-marcadores-de-dialogo.md`.
- Números menores indicam maior prioridade. Usar intervalos de 10 por padrão, permitindo prioridades intermediárias sem renomear os demais planos.
- Cada plano deve ser autocontido e registrar objetivo, contexto, escopo, estado desejado, riscos e premissas.
- Organizar a execução em fases numeradas, cada uma com tarefas `[ ]` e um critério objetivo de conclusão.
- Marcar `[x]` somente após confirmar a execução; não avançar de fase sem atender ao critério de conclusão da fase atual.
- Atualizar o plano conforme o estado técnico real. Não registrar conversa transitória, instruções para o próximo agente ou cópias das regras deste arquivo.
- Mover planos concluídos para `planos/executados/`, preservando nome e prioridade histórica.

## Arquitetura e qualidade

- Preservar a independência dos mods; não introduzir dependência entre DLLs por conveniência.
- Preservar as referências ao jogo e ao MelonLoader com `Private="false"`; essas dependências já pertencem à instalação do jogo.
- Manter nomenclatura e estilo locais: namespace por projeto, nomes claros e comentários que expliquem decisões ou particularidades do jogo.
- Preferir alterações limitadas, responsabilidades claras e código simples. Não adicionar funcionalidades paralelas, abstrações sem benefício ou reescritas alheias ao escopo aprovado.
- Nos patches IL2CPP pertinentes, preservar a verificação do tipo nativo real, conforme `mods/RelationshipHints/Guard.cs`: métodos nativos podem ser compartilhados entre classes.
- Preservar o isolamento de falhas dos mods e os mecanismos que evitam inundar o log. Falhas do recurso devem permitir que o jogo continue.
- Não tratar hipóteses sobre eventos e critérios como contratos confirmados. Preservar a indicação de incerteza nas previsões de relacionamento.
- Ao alterar lógica de carregamento, respeitar a restauração tardia do estado e não conservar referências a objetos do jogo entre carregamentos quando eles forem recriados.

## Documentação e autoridade das fontes

- Este `AGENTS.md` concentra as regras comuns do repositório. Não criar arquivos aninhados sem necessidade de regras locais distintas; verificar a descoberta e precedência do agente utilizado antes de adotar uma hierarquia.
- Preservar o significado de regras válidas em reorganizações e resolver conflitos explicitamente. Não gerar ou sincronizar `CLAUDE.md`.
- Código e configuração mostram o estado implementado; documentação especializada descreve contratos e domínio; o plano aprovado define o estado desejado da entrega.
- `IDEIAS.md` registra propostas e investigação; uma ideia registrada não autoriza sua implementação.
- Quando fontes divergirem, identificar a divergência e resolver o ponto relevante antes de implementar.
- Atualizar a documentação afetada e manter equivalentes as informações dos READMEs em português e inglês quando houver ambos.
- Distinguir hipóteses, observações de código e resultados confirmados no jogo. Documentar somente contexto permanente e evitar duplicações.
- Usar Markdown e UTF-8, preservando acentos e as convenções existentes.

## Validação e compatibilidade

- Definir no plano critérios de aceite e a estratégia de validação do comportamento afetado.
- Executar o build aplicável e as verificações oficiais existentes. Build bem-sucedido não substitui validação funcional no jogo.
- Para mudanças em diálogo ou marcadores, considerar conforme o escopo: condições variáveis, carregar saves, troca de personagem, textos traduzidos e ausência de duplicação visual.
- No RelationshipHints, usar o diagnóstico previsto versus real quando pertinente; os caminhos de `CloseEvents` e efeitos em outros personagens têm validação limitada documentada.
- Relatar exatamente o que foi executado, os resultados e o que permanece sem verificação. Não afirmar sucesso no jogo sem evidência.
- Não seguir para commit após validação reprovada nem considerar atendidos critérios ainda pendentes.
- Preservar contratos públicos, configurações e comportamentos explicitamente suportados. Documentar migrações, depreciações e remoções.
- Não adicionar camadas de compatibilidade sem requisito comprovado nem prometer compatibilidade com versões não verificadas.

## Versões e histórico por mod

- Manter versões independentes por mod, declaradas no atributo `MelonInfo` do respectivo `Mod.cs`.
- Não incrementar versões automaticamente em toda alteração ou commit. Se a entrega exigir uma nova versão, alinhar o incremento com o usuário; não há política formal de incremento definida.
- Manter o histórico de mudanças notáveis de cada mod em `mods/<Mod>/CHANGELOG.md`, criando-o quando uma entrega daquele mod precisar de registro.
- Durante o trabalho, usar `## [Unreleased]`. Ao fechar uma release autorizada, usar `## [versão] - YYYY-MM-DD`, com a versão do mod e a data local, em ordem da mais recente para a mais antiga.
- Descrever o efeito das mudanças para o usuário, sem copiar mensagens de commit. Usar somente categorias aplicáveis: `Added`, `Changed`, `Deprecated`, `Removed`, `Fixed`, `Security`.
- Não criar categorias vazias nem um changelog global por padrão. Alterações comuns devem ser registradas nos históricos dos mods afetados quando forem notáveis para eles.

## Commits

- Usar Conventional Commits em português brasileiro: `<tipo>[escopo opcional]: <descrição>`.
- Tipos: `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`. Usar escopo quando ajudar a identificar o mod ou área afetada.
- Escrever descrições curtas e objetivas. Para uma única alteração, usar uma linha; para mudanças distintas, registrar uma por linha, cada qual seguindo integralmente o formato.
- Usar `!` ou `BREAKING CHANGE:` somente para quebra de compatibilidade externa confirmada.
- Incluir no commit somente alterações pertencentes à entrega autorizada; preservar trabalho preexistente do usuário.

## Restrições

- Não versionar binários do jogo, assemblies gerados, código decompilado ou saídas de build; respeitar `.gitignore`.
- `dev/Probe` é ferramenta de desenvolvimento e não deve ser publicada como mod de usuário.
- Preservar alterações locais existentes. Não descartar, sobrescrever ou incluir em entregas trabalho alheio ao escopo aprovado.
- No RelationshipHints, preservar o contrato de alterar somente a exibição dos marcadores, sem modificar arquivos do jogo, `.character` ou arquivos de idioma.

## Referências situacionais

Consultar somente os documentos necessários à tarefa. Os gatilhos abaixo são instruções de leitura.

| Documento | Quando ler |
| --- | --- |
| `README.md` e `README.en.md` | Antes de alterar build, instalação, compatibilidade ou apresentação pública dos mods, ler os READMEs. |
| `Directory.Build.props` e `.csproj` afetado | Antes de alterar alvo, referências, caminhos ou configuração de compilação, ler esses arquivos. |
| `mods/RelationshipHints/README.md` e `README.en.md` no mesmo diretório | Antes de alterar cálculo, marcadores ou diagnóstico do RelationshipHints, ler ambos. |
| `IDEIAS.md` | Antes de investigar protótipos ou alterar premissas das ideias registradas, ler as entradas pertinentes. |
| `docs/teclas-do-jogo.md` | Antes de escolher ou mudar a tecla de um mod, ler para evitar conflito com o jogo. |
| `docs/investigacao-do-jogo.md` | Antes de procurar classes, métodos ou comportamento do jogo, ler para usar o código descompilado em `_decomp` e a sonda. |
| `.gitignore` | Antes de adicionar arquivos, dependências locais ou artefatos, conferir as exclusões. |
