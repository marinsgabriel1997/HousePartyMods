# Dubbing

*[English version](README.en.md) · [Todos os mods](../../README.md)*

Toca dublagens em outros idiomas no lugar das vozes originais, a partir de
arquivos `.ogg` numa pasta. O mod troca a voz na hora de tocar: os arquivos
do jogo não são alterados.

- Vale para as falas dos diálogos e para as conversas de fundo com voz.
- A boca do personagem acompanha a voz dublada, e a conversa espera a fala
  dublada terminar, como no original.
- Fala sem arquivo na dublagem escolhida toca a voz original. Dá para dublar
  só parte do jogo.
- Trocar um arquivo na pasta vale na próxima vez que a fala tocar, sem
  reiniciar o jogo.
- Se um arquivo estiver com problema ou der erro, o mod registra uma vez no
  log e a fala toca a voz original.

O mod não traz dublagens: elas são instaladas à parte, numa pasta por
idioma. Ele não altera nenhum arquivo do jogo nem os saves.

## Compatibilidade

Testado por último em House Party 1.5.2.14289 com MelonLoader 0.7.3. Veja os
detalhes no [README principal](../../README.md#compatibilidade).

## Instalação

1. Instale o MelonLoader, conforme o [README principal](../../README.md#instalação).
2. Baixe o [`Dubbing.dll`](../../dist/1.0.0/Dubbing.dll) (na página do
   arquivo, botão de download) e coloque na pasta `Mods` da raiz do jogo.
3. Coloque a dublagem em `UserData\Dubbing\<idioma>` (veja abaixo).
4. Abra o jogo e escolha a dublagem na opção "Dublagem" do ModSettings
   (`F6`), ou em `Language` no arquivo de configuração (veja abaixo).

Para desinstalar, apague o `Mods\Dubbing.dll`. As dublagens ficam em
`UserData\Dubbing` e podem ser apagadas também.

## Arquivos da dublagem

Um `.ogg` (Ogg Vorbis) por fala, com o nome do clip de áudio original, numa
pasta por idioma, história e personagem:

```text
UserData\Dubbing\<idioma>\<história>\<personagem>\<clip>.ogg
```

Por exemplo:

```text
UserData\Dubbing\pt-BR\Original Story\Amy\base_dialogue_originalstory_amy_12.ogg
UserData\Dubbing\pt-BR\Original Story\Amy\base_backgroundchatter_amy_33.ogg
```

- `<idioma>`: nome livre, o mesmo escolhido na opção da dublagem.
- `<história>`: o nome da pasta da história em
  `HouseParty_Data\StreamingAssets\Mods\Stories`, por exemplo
  `Original Story`, `Date Night With Brittney` ou
  `A Vickie Vixen Valentine`.
- `<personagem>`: o nome do arquivo `.character` do personagem nessa pasta,
  sem a extensão, por exemplo `Amy`.
- `<clip>`: o nome do clip original nos bundles de áudio do jogo, como
  `base_dialogue_originalstory_amy_12` (fala 12 da Amy),
  `base_dialogue_originalstory_amy_0.1` (variante 1 da fala 0) ou
  `base_backgroundchatter_amy_33` (conversa de fundo 33 da Amy).

As conversas de fundo ficam na pasta do personagem que fala. Arquivo na
pasta de outro personagem ou de outra história não é usado.

## Configuração

Pela janela do [ModSettings](../ModSettings/README.md) (`F6`), com os nomes
das opções no idioma do jogo, ou em `UserData\MelonPreferences.cfg`:

```toml
[Dubbing]
Language = "pt-BR"
```

- `Language`: nome da pasta da dublagem em `UserData\Dubbing`. Vazio
  desliga o mod. A descrição da opção lista as pastas encontradas ao abrir o
  jogo. Se a pasta não existir, o mod registra uma vez no log e as falas
  tocam a voz original.

## Limitações conhecidas

- Falas que não têm áudio original no jogo continuam sem voz, mesmo com
  arquivo na pasta.
- Não dubla as falas soltas que o jogo toca de bundles genéricos (o peixe
  falante, a Britt e o motorista) nem as cenas.
- Testado com a dublagem completa em português só na Original Story. Nas
  outras histórias e na `Phone Call`, o nome da pasta do personagem não foi
  conferido no jogo.
- Cada fala é lida do disco quando começa, o que levou poucos milissegundos
  nos testes. Num disco muito lento, a fala pode demorar a começar.
