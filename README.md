# Mods para House Party

*[English version](README.en.md)*

Mods para [House Party](https://store.steampowered.com/app/611790/House_Party/)
(Eek! Games), feitos com [MelonLoader](https://github.com/LavaGang/MelonLoader).

Cada mod é uma DLL independente: instale só os que quiser.

## Mods

| Mod | O que faz | Download |
|---|---|---|
| [RelationshipHints](mods/RelationshipHints/README.md) | Mostra no menu de respostas o efeito de cada opção na amizade e no romance (`+1A -1R \| Texto`) | [Releases](../../releases?q=RelationshipHints&expanded=true) |
| [NewTalkMarker](mods/NewTalkMarker/README.md) | Mostra um "!" sobre o personagem quando surge uma fala nova para ele | [Releases](../../releases?q=NewTalkMarker&expanded=true) |
| [ItemHighlight](mods/ItemHighlight/README.md) | Mostra o nome dos itens à vista e faz brilhar os que podem ser pegos, cada um com sua tecla | [Releases](../../releases?q=ItemHighlight&expanded=true) |

## Compatibilidade

Os mods são desenvolvidos e testados neste ambiente:

| Componente | Versão |
|---|---|
| House Party (Steam, Windows) | 1.5.2.14289 |
| Unity | 2022.3.62f2 (IL2CPP) |
| MelonLoader | 0.7.3 Open-Beta (runtime net6) |

Outras versões do MelonLoader 0.7.x devem funcionar. Atualizações do jogo
podem quebrar os mods. Se isso acontecer, o mod registra o erro no log e o
jogo segue normalmente, só sem o recurso do mod. A versão do seu jogo aparece
no início do `MelonLoader\Latest.log`, na linha `Game Version`. O README de
cada mod informa em qual versão ele foi testado por último.

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
3. Baixe a DLL de cada mod que quiser (tabela acima) e coloque na pasta `Mods`
   do jogo, a que fica na raiz da instalação, não a
   `HouseParty_Data\StreamingAssets\Mods`.

As configurações dos mods ficam em `UserData\MelonPreferences.cfg`, criado
depois da primeira execução.

### Desinstalar

Para remover um mod, apague a DLL dele da pasta `Mods`. Para remover também o
MelonLoader, apague o `version.dll` e as pastas `MelonLoader`, `Mods`,
`Plugins`, `UserData` e `UserLibs` da raiz do jogo.

## Compilar a partir do código

Requisitos: [.NET SDK](https://dotnet.microsoft.com/download) 6 ou mais novo, e
o jogo com o MelonLoader já instalado e executado uma vez, porque a compilação
usa os assemblies gerados por ele.

```sh
dotnet build HousePartyMods.slnx -c Release
# se o jogo estiver em outra pasta:
dotnet build HousePartyMods.slnx -c Release -p:GameDir="D:\Jogos\House Party"
```

Cada DLL sai em `mods\<Mod>\bin\Release\net6.0\`.

Estrutura:

- `mods/`: um projeto por mod, cada um com seu README;
- `Directory.Build.props`: pasta do jogo e referências comuns a todos os mods;
- `tools/`: scripts Python usados para analisar os arquivos `.character`;
- [IDEIAS.md](IDEIAS.md): ideias de novos mods em avaliação.

## Aviso

Projeto de fã, sem relação com a Eek! Games. Este repositório não contém
binários nem código do jogo.

## Licença

[GPL-3.0](LICENSE)
