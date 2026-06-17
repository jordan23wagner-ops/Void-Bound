# Void Bound — Coding Standards

## Unity Version & APIs
- Target: **Unity 6.5 (6000.5.0f1)**, URP
- Use the **new Input System** (`UnityEngine.InputSystem`) — never the legacy `Input.GetAxis()` / `Input.GetKey()` pattern
- Prefer `Awake()`/`OnEnable()` for initialization, `Start()` only when dependent on other objects' `Awake()`
- Use `[SerializeField] private` for Inspector-exposed fields, not public fields, unless there's a specific reason

## ScriptableObject Pattern (all game data)
Every data-driven system (gear, enemies, zones, skills, recipes, loot tables) follows this pattern:

```csharp
[CreateAssetMenu(fileName = "New GearItem", menuName = "VoidBound/Gear Item")]
public class GearItemSO : ScriptableObject
{
    public string itemId;
    public string displayName;
    public EquipmentSlot slot;
    public WeaponType weaponType; // only relevant if slot == Weapon
    public RarityTier rarity;
    public StatModifiers statModifiers;
    public GameObject visualPrefab;
    public string setId; // empty if not part of a set
}
```

Use `enum` types for fixed categories (EquipmentSlot, WeaponType, RarityTier, EnemyTier) — defined once in `Scripts/Data/Enums.cs`, referenced everywhere.

## Naming Conventions
- Classes/Scripts: `PascalCase` (e.g. `PlayerController`, `GearItemSO`)
- Private fields: `camelCase` with no underscore prefix unless project convention says otherwise
- Public properties: `PascalCase`
- ScriptableObject asset files: match the `itemId`/`displayName` for easy searching (e.g. `Sword_Legendary_Voidreaver.asset`)

## Folder/Namespace Discipline
- All scripts live under `Assets/Scripts/[Category]/`
- Use namespaces matching folder structure: `VoidBound.Core`, `VoidBound.Data`, `VoidBound.Combat`, etc.

## Compilation Verification
After every code change, run Unity in batch mode to confirm zero compile errors before moving to the next task:
```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.5.0f1\Editor\Unity.exe" -batchmode -projectPath "C:\Users\Jordon\Void Bound" -quit -logFile compile_check.log
```
Then check `compile_check.log` for `error CS` strings. If found, fix before proceeding — never leave a phase with known compile errors.

## Things to Avoid
- No `FindObjectOfType` in hot paths (use direct references or a registry/service locator pattern)
- No magic numbers — use named constants or ScriptableObject-configured values
- No deprecated Unity APIs (if unsure whether an API is current for Unity 6.x, flag it rather than guessing)
- No giant monolithic scripts — split responsibilities (e.g. `PlayerCombat`, `PlayerMovement`, `PlayerInventory` as separate components, not one `PlayerController` god-class)

## Git Commit Discipline
- One commit per completed task/feature, not one giant commit per phase
- Commit message format: `[Phase X] Brief description of what changed`
- Never commit with known compile errors
