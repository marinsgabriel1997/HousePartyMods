# Investigação do jogo

Como levantar classes, métodos e comportamento do House Party antes de
escrever um mod. O jogo é IL2CPP: a lógica fica compilada no
`GameAssembly.dll`, e os assemblies gerados pelo MelonLoader
(`MelonLoader\Il2CppAssemblies`) só trazem as assinaturas e chamadas ao
código nativo.

## Ferramentas locais

Ficam fora do git (`.gitignore`) e não devem ser publicadas.

- `_tools/ilspycmd.exe`: descompilador ilspycmd 11.1. Para instalar de novo:
  `dotnet tool install ilspycmd --tool-path _tools`.
- `_decomp/<Assembly>/`: assemblies gerados descompilados, um projeto por
  assembly (`Il2CppEekCharacterEngine`, `Il2CppEekEvents`, `Il2CppEekUI`,
  `Il2CppHouseParty` e `Assembly-CSharp`), gerados em 2026-10-10 com o jogo
  1.5.2.14289. Depois de atualizar o jogo e rodar o MelonLoader, gerar de
  novo:
  `_tools/ilspycmd.exe -p -o _decomp/<Assembly> "<pasta do jogo>\MelonLoader\Il2CppAssemblies\<Assembly>.dll"`.

## O que o código descompilado mostra

- Classes, campos, propriedades, métodos e enums, com os tipos. Buscar com
  `grep` em `_decomp` é o caminho mais rápido para achar uma API.
- Os métodos só chamam `il2cpp_runtime_invoke`; o comportamento não aparece.
  `[CallerCount]` e `[CachedScanResults]` indicam quantos lugares chamam o
  método.
- Os nomes dos campos `NativeMethodInfoPtr_<Método>_<Acesso>_<Retorno>_<Parâmetros>`
  trazem a assinatura e servem para buscar com `grep -a` direto nas DLLs.

## Comportamento: só no jogo

O que o código faz, quando é chamado e com que valores só se confirma no
jogo, com a sonda `dev/Probe`: patches que registram chamadas e estados no
`MelonLoader\Latest.log` e testes ligados por opções `Probe.*`. Os planos
em `planos/executados/` registram os resultados de cada investigação.

## Armadilhas confirmadas

- Métodos com o mesmo código nativo são fundidos numa única função. Para
  conferir, comparar o endereço do código: o primeiro ponteiro do
  `Il2CppMethodInfo` lido do campo `NativeMethodInfoPtr_...` (plano `p50`).
  Patches por dois métodos fundidos derrubam o jogo; corrigir um só e
  conferir a classe real (`Guard`).
- Valores de propriedades e campos podem não significar o que o nome diz
  (`CCombat.CombatActive` fica `true` fora do combate; `CurrentMovementSpeed`
  fica 0 correndo, plano `p60`). Confirmar no log antes de usar.
