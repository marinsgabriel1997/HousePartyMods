using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppEekCharacterEngine;
using Il2CppEekCharacterEngine.Components;
using Il2CppEekEvents;
using Il2CppEekEvents.Dialogues;
using Il2CppEekEvents.Stories;
using Il2CppList = Il2CppSystem.Collections.Generic.List<Il2CppEekEvents.GameEvent>;

namespace RelationshipHints
{
    // Calcula o efeito de uma resposta na relação com o Player, com o estado
    // atual do jogo. Hipóteses ainda não confirmadas no jogo estão marcadas.
    internal static class EffectCalculator
    {
        // GameEvents (EekEvents.GameEvents)
        private const int EventModifyValue = 0;
        private const int EventMatchValue = 1;
        private const int EventCombineValue = 2;
        private const int EventRandomizeIntValue = 3;
        private const int EventSocial = 30;
        private const int EventQuest = 80;
        private const int EventDialogue = 120;
        private const int EventState = 130;
        private const int EventProperty = 135;

        // SocialModTypes
        private const int SocialFriendship = 20;
        private const int SocialRomance = 30;

        // ModificationTypes
        private const int ModEquals = 0;
        private const int ModAdd = 1;

        // DialogueActions
        private const int DialogueTrigger = 0;
        private const int DialogueOverhear = 1;

        private const int HubDialogueId = 0;
        private const int MaxDepth = 4;

        public static Effect Calculate(Response response, string speaker)
        {
            var effect = new Effect();
            try
            {
                var visited = new HashSet<string>();
                bool redirected = WalkEvents(response.ResponseEvents, speaker, speaker, "ResponseEvents", effect, visited, 0);

                // Hipótese: um evento 120 que dispara um diálogo do próprio falante
                // substitui o Next.
                if (redirected)
                {
                    effect.Trace.Add($"Next {response.Next} ignorado: desvio 120 para o falante");
                }
                else if (response.Next >= 0)
                {
                    VisitDialogue(speaker, response.Next, speaker, effect, visited, 0, includeCloseEvents: true);
                }
            }
            catch (Exception ex)
            {
                effect.Trace.Add($"erro: {ex.Message}");
                effect.For(speaker).FriendshipUncertain = true;
                effect.For(speaker).RomanceUncertain = true;
            }
            return effect;
        }

        // Percorre uma lista de eventos. Retorna true se algum evento 120 disparou
        // um diálogo do próprio falante.
        private static bool WalkEvents(Il2CppList events, string owner, string speaker, string where,
            Effect effect, HashSet<string> visited, int depth)
        {
            if (events == null)
                return false;

            bool redirected = false;
            bool stateChanged = false;

            for (int i = 0; i < events.Count; i++)
            {
                GameEvent e = events[i];
                if (e == null || !e.Enabled)
                    continue;

                int type = e.EventType;
                bool conditional = e.UseConditions && e.Criteria != null && e.Criteria.Count > 0;

                bool uncertain = false;
                if (conditional)
                {
                    // Um evento anterior da mesma lista mudou estado que o critério
                    // pode ler; avaliar agora daria o resultado de antes da mudança.
                    if (stateChanged || HasRandomCriteria(e))
                    {
                        uncertain = true;
                    }
                    else
                    {
                        bool passes = CriteriaEvaluator.MeetsCritera(e.Criteria);
                        effect.Trace.Add($"{owner} {where}: evento {type} condicional -> {(passes ? "passa" : "falha")}");
                        if (!passes)
                            continue;
                    }
                }

                if (type == EventSocial)
                {
                    ApplySocial(e, owner, uncertain, effect, where);
                }
                else if (type == EventDialogue && (e.Option == DialogueTrigger || e.Option == DialogueOverhear))
                {
                    string target = string.IsNullOrEmpty(e.Character) ? owner : e.Character;
                    if (!int.TryParse(e.Value, out int id))
                        continue;

                    if (uncertain)
                    {
                        // Não dá para saber se o desvio acontece; o que ele faria
                        // vira incerteza.
                        var probe = new Effect();
                        VisitDialogue(target, id, speaker, probe, new HashSet<string>(visited), depth + 1, includeCloseEvents: false);
                        foreach (KeyValuePair<string, RelationDelta> pair in probe.ByCharacter)
                        {
                            if (pair.Value.IsEmpty)
                                continue;
                            effect.For(pair.Key).FriendshipUncertain |= pair.Value.Friendship != 0 || pair.Value.FriendshipUncertain || pair.Value.FriendshipSet != null;
                            effect.For(pair.Key).RomanceUncertain |= pair.Value.Romance != 0 || pair.Value.RomanceUncertain || pair.Value.RomanceSet != null;
                        }
                        effect.Trace.Add($"{owner} {where}: desvio 120 incerto para {target}#{id}");
                        if (target == speaker && e.Option == DialogueTrigger)
                            redirected = true;
                        continue;
                    }

                    effect.Trace.Add($"{owner} {where}: desvio 120 para {target}#{id}");
                    VisitDialogue(target, id, speaker, effect, visited, depth + 1, includeCloseEvents: false);
                    if (target == speaker && e.Option == DialogueTrigger)
                        redirected = true;
                }
                else if (IsStateChange(type))
                {
                    stateChanged = true;
                }
            }

            return redirected;
        }

        private static void VisitDialogue(string character, int id, string speaker, Effect effect,
            HashSet<string> visited, int depth, bool includeCloseEvents)
        {
            if (depth > MaxDepth || !visited.Add($"{character}#{id}"))
                return;

            Dialogue dialogue = FindDialogue(character, id);
            if (dialogue == null)
            {
                effect.Trace.Add($"diálogo {character}#{id} não encontrado");
                return;
            }

            if (id == HubDialogueId)
            {
                // O diálogo 0 é o hub ocioso: os eventos 120 dele escolhem o que o
                // NPC diz, independentemente da escolha. Só os eventos de relação contam.
                ApplySocialOnly(dialogue.StartEvents, character, effect, "StartEvents#0");
                return;
            }

            WalkEvents(dialogue.StartEvents, character, speaker, $"StartEvents#{id}", effect, visited, depth);

            // Hipótese: os CloseEvents do diálogo do Next disparam quando ele
            // fecha, e isso é consequência da resposta.
            if (includeCloseEvents)
                WalkEvents(dialogue.CloseEvents, character, speaker, $"CloseEvents#{id}", effect, visited, depth);
        }

        private static void ApplySocialOnly(Il2CppList events, string owner, Effect effect, string where)
        {
            if (events == null)
                return;

            for (int i = 0; i < events.Count; i++)
            {
                GameEvent e = events[i];
                if (e == null || !e.Enabled || e.EventType != EventSocial)
                    continue;

                bool conditional = e.UseConditions && e.Criteria != null && e.Criteria.Count > 0;
                bool uncertain = conditional && HasRandomCriteria(e);
                if (conditional && !uncertain && !CriteriaEvaluator.MeetsCritera(e.Criteria))
                    continue;

                ApplySocial(e, owner, uncertain, effect, where);
            }
        }

        private static void ApplySocial(GameEvent e, string owner, bool uncertain, Effect effect, string where)
        {
            if (e.Option != SocialFriendship && e.Option != SocialRomance)
                return;
            if (e.Character2 != "Player")
                return;
            if (!int.TryParse(e.Value, out int value))
                return;

            string character = string.IsNullOrEmpty(e.Character) ? owner : e.Character;
            RelationDelta delta = effect.For(character);
            bool friendship = e.Option == SocialFriendship;

            if (uncertain)
            {
                if (friendship) delta.FriendshipUncertain = true; else delta.RomanceUncertain = true;
            }
            else if (e.Option2 == ModAdd)
            {
                if (friendship) delta.Friendship += value; else delta.Romance += value;
            }
            else if (e.Option2 == ModEquals)
            {
                if (friendship) { delta.FriendshipSet = value; delta.Friendship = 0; }
                else { delta.RomanceSet = value; delta.Romance = 0; }
            }
            else
            {
                return;
            }

            effect.Trace.Add($"{owner} {where}: {character} {(friendship ? "A" : "R")} {(e.Option2 == ModAdd ? "+=" : "=")} {value}{(uncertain ? " (incerto)" : "")}");
        }

        private static bool HasRandomCriteria(GameEvent e)
        {
            for (int i = 0; i < e.Criteria.Count; i++)
            {
                Criteria c = e.Criteria[i];
                if (c != null && c.CompareType == CompareTypes.CoinFlip)
                    return true;
            }
            return false;
        }

        private static bool IsStateChange(int type) =>
            type == EventModifyValue || type == EventMatchValue || type == EventCombineValue
            || type == EventRandomizeIntValue || type == EventQuest || type == EventState
            || type == EventProperty;

        private static Dialogue FindDialogue(string character, int id)
        {
            CDialogue component = FindDialogueComponent(character);
            CharacterStoryData story = component?._story;
            if (story?.Dialogues == null)
                return null;

            for (int i = 0; i < story.Dialogues.Count; i++)
            {
                Dialogue dialogue = story.Dialogues[i];
                if (dialogue != null && dialogue.ID == id)
                    return dialogue;
            }
            return null;
        }

        public static CDialogue FindDialogueComponent(string character)
        {
            NonPlayerCharacter npc = CharacterManager.GetNonPlayerCharacter(character);
            if (npc != null)
                return npc.Dialogue;

            DialogueOnlyCharacter dialogueOnly = CharacterManager.GetDialogueOnlyCharacter(character);
            return dialogueOnly?.Dialogue;
        }
    }
}
