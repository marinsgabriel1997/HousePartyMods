using UnityEngine;
using UnityEngine.UI;

namespace RelationshipHints
{
    // O destaque rosa do jogo multiplica a cor do texto inteiro do botão, o que
    // estraga o verde e o vermelho. Os números coloridos ficam numa camada de
    // texto filha, com o mesmo layout e fora do alcance dessa tinta; no texto do
    // botão, o mesmo trecho fica transparente, ocupando o espaço.
    internal static class ColorOverlay
    {
        private const string ObjectName = "RelationshipHintsColors";

        public static void Sync(Text label, string overlayText)
        {
            Text overlay = Find(label);
            if (overlayText == null)
            {
                if (overlay != null)
                    overlay.text = string.Empty;
                return;
            }

            if (overlay == null)
                overlay = Create(label);

            CopyLayout(label, overlay);
            overlay.text = overlayText;
        }

        private static Text Find(Text label)
        {
            Transform child = label.transform.Find(ObjectName);
            return child != null ? child.GetComponent<Text>() : null;
        }

        private static Text Create(Text label)
        {
            var go = new GameObject(ObjectName);
            go.layer = label.gameObject.layer;
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.SetParent(label.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = label.rectTransform.pivot;

            Text overlay = go.AddComponent<Text>();
            overlay.raycastTarget = false;
            overlay.supportRichText = true;
            overlay.color = Color.white;
            return overlay;
        }

        // Mesmas configurações de texto, para que as quebras de linha e as
        // posições dos caracteres coincidam com as do texto do botão.
        private static void CopyLayout(Text label, Text overlay)
        {
            overlay.font = label.font;
            overlay.fontSize = label.fontSize;
            overlay.fontStyle = label.fontStyle;
            overlay.lineSpacing = label.lineSpacing;
            overlay.alignment = label.alignment;
            overlay.alignByGeometry = label.alignByGeometry;
            overlay.horizontalOverflow = label.horizontalOverflow;
            overlay.verticalOverflow = label.verticalOverflow;
            overlay.resizeTextForBestFit = label.resizeTextForBestFit;
            overlay.resizeTextMinSize = label.resizeTextMinSize;
            overlay.resizeTextMaxSize = label.resizeTextMaxSize;
            overlay.material = label.material;
        }
    }
}
