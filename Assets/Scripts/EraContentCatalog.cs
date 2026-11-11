using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EraExpansionCatalog", menuName = "Bunker/Era expansion catalog")]
public class EraContentCatalog : ScriptableObject
{
    public int firstAdditionalEra = 7;
    public EntityRang[] additionalEras;
    public WeaponData[] additionalWeapons;

    public static void Install(GameCore game)
    {
        EraContentCatalog catalog = Resources.Load<EraContentCatalog>("EraExpansionCatalog");
        if (catalog != null) catalog.Apply(game);
    }

    public void Apply(GameCore game)
    {
        if (game == null || game.board == null || game.entitys == null || game.entitys.Length < firstAdditionalEra)
        {
            Debug.LogError("Era expansion: original era configuration is missing; scene was not changed.");
            return;
        }
        // Preserve all configured original eras and any custom entries already present.
        int extraCount = additionalEras == null ? 0 : additionalEras.Length;
        int originalCount = game.entitys.Length;
        if (originalCount < firstAdditionalEra + extraCount)
        {
            EntityRang[] expanded = new EntityRang[firstAdditionalEra + extraCount];
            Array.Copy(game.entitys, expanded, originalCount);
            for (int i = originalCount; i < expanded.Length; i++)
                expanded[i] = additionalEras[i - firstAdditionalEra];
            game.entitys = expanded;
        }
        // Never reorder existing weapon indices: saves store an index, not an asset GUID.
        List<WeaponData> combined = new List<WeaponData>(game.board.weapons ?? new WeaponData[0]);
        if (additionalWeapons != null)
            foreach (WeaponData weapon in additionalWeapons)
                if (weapon != null && !combined.Contains(weapon)) combined.Add(weapon);
        game.board.weapons = combined.ToArray();
    }
}
