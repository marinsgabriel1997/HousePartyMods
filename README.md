# RelationshipHints: mod para House Party

*[English version](README.en.md)*

Mostra, antes de cada opção do menu de respostas, o efeito que ela terá na sua
relação com o personagem:

```
+1A -1R | I like your glasses. They make you look smart!
```

- **A** = amizade, **R** = romance.
- Valores positivos aparecem em **verde**, negativos em **vermelho**.
- Resposta sem efeito fica sem prefixo.

O efeito é calculado **durante o jogo**, com o estado atual. Assim, respostas
condicionais mostram o resultado certo para o momento. Exemplo real, com a
Katherine: "So how about letting me get you that drink now?" mostra `-1A -1R`
antes de você ver um certo diálogo dela e `+1A +1R` depois.

O mod não altera nenhum arquivo do jogo, nem `.character` nem os arquivos de
idioma. Ele só muda o texto exibido no menu, então funciona com qualquer
idioma, inclusive com traduções da comunidade.

## Marcadores

| Marcador | Significado |
|---|---|
| `+1A`, `-2R` | soma ou subtrai pontos de amizade ou romance |
| `?A`, `?R` | há efeito, mas não dá para prever com segurança (por exemplo, depende de sorteio) |
| `=5A` | define a relação com um valor fixo, em vez de somar |
| `Madison: -3A` | efeito na relação com outro personagem, não com quem está falando |

## Instalação

1. Instale o [MelonLoader](https://github.com/LavaGang/MelonLoader/releases)
   **0.7.x** na pasta do jogo
   (`...\steamapps\common\House Party`). Usando o zip, extraia
   `MelonLoader.x64.zip` direto na pasta do jogo.
2. Abra o jogo uma vez e espere chegar ao menu principal. Na primeira
   execução, o MelonLoader gera os assemblies, o que leva alguns minutos.
   - Se aparecer o erro **"Failed to Download DeobfuscationMap!"**, abra
     `UserData\Loader.cfg` na pasta do jogo, troque
     `force_offline_generation = false` por `true` e abra o jogo de novo. O
     mapa de desofuscação que o MelonLoader tenta baixar é de uma versão
     antiga do jogo e não é necessário.
3. Baixe o `RelationshipHints.dll` na página de
   [Releases](../../releases) e coloque na pasta `Mods` do jogo, a que fica na
   raiz da instalação, não a `HouseParty_Data\StreamingAssets\Mods`.
4. Abra o jogo e converse com alguém.

### Desinstalar

Apague o `Mods\RelationshipHints.dll`. Para remover também o MelonLoader,
apague o `version.dll` e as pastas `MelonLoader`, `Mods`, `Plugins`,
`UserData` e `UserLibs` da raiz do jogo.

## Configuração

Depois da primeira execução, o arquivo `UserData\MelonPreferences.cfg` ganha a
seção:

```toml
[RelationshipHints]
DiagnosticLog = false
```

Com `DiagnosticLog = true`, o mod registra em `MelonLoader\Latest.log`, a cada
menu, o efeito previsto de cada resposta e o caminho que levou a ele. A cada
escolha, compara a previsão com a mudança real de relação aplicada pelo jogo e
marca `OK`, `DIVERGE` ou `INCERTO`. Se encontrar um marcador errado, ligue essa
opção, reproduza o caso e anexe o log ao abrir uma issue.

## Como o efeito é calculado

Para cada resposta exibida, o mod percorre os mesmos dados que o jogo usa:

1. os eventos da própria resposta (`ResponseEvents`);
2. os desvios de diálogo (evento 120) que ela dispara, avaliando as condições
   com o avaliador de critérios do próprio jogo. Quando o desvio vai para o
   próprio personagem, ele substitui o `Next`, como no jogo;
3. o diálogo seguinte (`Next`): eventos de abertura (`StartEvents`) e de
   fechamento (`CloseEvents`).

No diálogo 0, a conversa "ociosa" do personagem, só contam os eventos de
relação. Os desvios dele escolhem o que o NPC diz, independentemente da sua
escolha.

O efeito vira `?` quando há critério aleatório (`CoinFlip`) ou quando a
condição depende de algo que a própria resposta muda antes de ser avaliada.

### Limitações conhecidas

- Os caminhos de `CloseEvents` e de efeito em outros personagens ainda foram
  pouco testados no jogo.
- Traduções que já trazem marcadores de relação embutidos no texto vão
  aparecer com o marcador duplicado.

## Compilar a partir do código

Requisitos: [.NET SDK](https://dotnet.microsoft.com/download) 6 ou mais novo, e
o jogo com o MelonLoader já instalado e executado uma vez, porque a compilação
usa os assemblies gerados por ele.

```sh
cd RelationshipHints
dotnet build -c Release
# se o jogo estiver em outra pasta:
dotnet build -c Release -p:GameDir="D:\Jogos\House Party"
```

A DLL sai em `RelationshipHints\bin\Release\net6.0\`.

A pasta `tools/` tem scripts Python usados para analisar os arquivos
`.character` durante o desenvolvimento.

## Aviso

Projeto de fã, sem relação com a Eek! Games. Este repositório não contém
binários nem código do jogo.

## Licença

[GPL-3.0](LICENSE)
