public enum DialogueNodeType
{
    // ── Generic ───────────────────────────────────────────────────────────────
    Start,
    End,
    Dialogue,
    Choice,
    Condition,  // Generic flag/variable check -> True / False
    Branch,     // Generic multi-output with editable labels

    // ── Ace Attorney ──────────────────────────────────────────────────────────
    EvidenceCheck,      // Was a specific piece of evidence presented?
    StatementPressed,   // Was a specific statement pressed?
    TestimonyContradicted, // Was testimony contradicted with specific evidence?
    SceneAlreadySeen,   // Has the player already seen a scene?
    TalkedToCharacter   // Has the player talked to a specific character?
}
