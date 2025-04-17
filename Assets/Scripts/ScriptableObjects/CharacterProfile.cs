using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Character Profile")]
public class CharacterProfile : ScriptableObject
{
    public string characterID; // ID interna, tipo "char_phoenix"
    public string displayName; // Lo que se muestra en pantalla
    public Sprite defaultPortrait;

    [System.Serializable]
    public class ExpressionEntry
    {
        public string expressionID;
        public Sprite sprite;
    }

    public List<ExpressionEntry> expressions;

    // Helper para obtener una expresión específica
    public Sprite GetExpressionSprite(string id)
    {
        foreach (var exp in expressions)
        {
            if (exp.expressionID == id)
                return exp.sprite;
        }
        Debug.LogWarning($"Expression '{id}' not found in {name}");
        return defaultPortrait;
    }
}