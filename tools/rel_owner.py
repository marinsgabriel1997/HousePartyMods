"""Quem é o dono (Character) dos eventos de relação com o Player, e dos 120."""
import collections, json, pathlib, sys

def load(f):
    raw = f.read_bytes()
    enc = "utf-16" if raw[:2] in (bytes([0xFF, 0xFE]), bytes([0xFE, 0xFF])) else "utf-8-sig"
    return json.loads(raw.decode(enc))

rel = collections.Counter(); d120 = collections.Counter(); examples = []
for f in pathlib.Path(sys.argv[1]).rglob("*.character"):
    ch = f.stem
    data = load(f)
    lists = []
    for d in data.get("Dialogues", []):
        lists += [d.get("StartEvents"), d.get("CloseEvents")] + [r.get("ResponseEvents") for r in d.get("Responses", [])]
    lists += [r.get("ResponseEvents") for k in ("GlobalResponses", "GlobalGoodbyeResponses") for r in data.get(k, [])]
    for evs in lists:
        for e in evs or []:
            if e.get("EventType") == 30 and e.get("Option") in (20, 30) and e.get("Character2") == "Player":
                own = "self" if e.get("Character") == ch else ("blank" if not e.get("Character") else "other")
                rel[own] += 1
                if own == "other" and len(examples) < 5:
                    examples.append((f.parent.name, ch, e.get("Character"), e.get("Option"), e.get("Value")))
            if e.get("EventType") == 120:
                own = "self" if e.get("Character") == ch else ("blank" if not e.get("Character") else "other")
                d120[(e.get("Option"), own)] += 1
print("relação com Player, dono:", rel)
print("exemplos de outro dono:", examples)
print("120 (Option, dono):", sorted(d120.items()))
