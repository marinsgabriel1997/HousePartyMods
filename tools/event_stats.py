"""Estatísticas dos eventos 120 (Dialogue) e 30 (Social) nos .character."""
import collections
import json
import pathlib
import sys

root = pathlib.Path(sys.argv[1])
opt120 = collections.Counter()
opt120_by_place = collections.Counter()
social = collections.Counter()
sample = None

def walk_events(events, place, ch):
    global sample
    for e in events or []:
        t = e.get("EventType")
        if t == 120:
            key = (e.get("Option"), e.get("Option2"), e.get("Character") == ch or e.get("Character"))
            opt120[(e.get("Option"), e.get("Option2"))] += 1
            opt120_by_place[(place, e.get("Option"))] += 1
            if sample is None and place == "ResponseEvents" and e.get("UseConditions"):
                sample = e
        elif t == 30:
            social[(e.get("Option"), e.get("Option2"), e.get("Character2"))] += 1

for f in root.rglob("*.character"):
    raw = f.read_bytes()
    enc = "utf-16" if raw[:2] in (bytes([0xFF, 0xFE]), bytes([0xFE, 0xFF])) else "utf-8-sig"
    data = json.loads(raw.decode(enc))
    ch = data.get("CharacterName") or f.stem
    for d in data.get("Dialogues", []):
        walk_events(d.get("StartEvents"), "StartEvents", ch)
        walk_events(d.get("CloseEvents"), "CloseEvents", ch)
        for r in d.get("Responses", []):
            walk_events(r.get("ResponseEvents"), "ResponseEvents", ch)
    for key in ("GlobalResponses", "GlobalGoodbyeResponses"):
        for r in data.get(key, []):
            walk_events(r.get("ResponseEvents"), "ResponseEvents", ch)

print("EventType 120 (Option, Option2):", opt120.most_common())
print("EventType 120 por lugar:", sorted(opt120_by_place.items()))
print("EventType 30 (Option, Option2, Character2) top:", social.most_common(15))
print("Exemplo 120 condicional em ResponseEvents:")
print(json.dumps(sample, indent=1, ensure_ascii=False)[:2500])
