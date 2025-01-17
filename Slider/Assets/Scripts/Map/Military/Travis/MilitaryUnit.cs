using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MilitaryUnit : MonoBehaviour
{
    private static readonly List<MilitaryUnit> activeUnits = new();
    public static MilitaryUnit[] ActiveUnits { get => activeUnits.ToArray(); }

    [SerializeField] private Type _unitType;
    public Type UnitType { get => _unitType; }

    [SerializeField] private Team _unitTeam;
    public Team UnitTeam { get => _unitTeam; }

    [SerializeField] private Vector2Int _gridPosition;
    public Vector2Int GridPosition { 
        get => _gridPosition; 
        set
        {
            _gridPosition = value;
            transform.position = GridPositionToWorldPosition(value);
            StartCombatWithOverlappingEnemyUnit();
        }
    }

    [SerializeField] private MilitaryUnitCommander _commander;
    public MilitaryUnitCommander Commander
    {
        get => _commander;
        set
        {
            if (_commander != null)
            {
                _commander.RemoveUnit(this);
            }
            _commander = value;
            _commander.AddUnit(this);
        }
    }

    [SerializeField] private STile attachedSTile;
    [SerializeField] private UnityEvent onDeath;

    public static void RegisterUnit(MilitaryUnit unit)
    {
        activeUnits.Add(unit);
        Debug.Log($"Registered Unit '{unit.gameObject.name}'");
    }

    public static void UnregisterUnit(MilitaryUnit unit)
    {
        activeUnits.Remove(unit);
        Debug.Log($"Unregistered Unit '{unit.gameObject.name}'");
    }

    public static Vector2 GridPositionToWorldPosition(Vector2Int tilePosition)
    {
        return new Vector2(tilePosition.x * 13, tilePosition.y * 13);
    }

    private void Awake()
    {
        SGridAnimator.OnSTileMoveEnd += (object sender, SGridAnimator.OnTileMoveArgs e) =>
        {
            if (attachedSTile != null && e.stile == attachedSTile)
            {
                GridPosition = new Vector2Int(e.stile.x, e.stile.y);
            }
        };

        RegisterUnit(this);
        if (Commander != null)
        {
            Commander.AddUnit(this);
        }

        // TODO: Make this actually work
        STile parentSTile = SGrid.GetSTileUnderneath(gameObject);
        if (parentSTile != null)
        {
            GridPosition = new Vector2Int(parentSTile.x, parentSTile.y);
        }
    }

    public void KillUnit()
    {
        CoroutineUtils.ExecuteAfterEndOfFrame(() => Cleanup(), coroutineOwner: this);
        onDeath?.Invoke();
    }

    private void Cleanup()
    {
        UnregisterUnit(this);
        if (Commander != null)
        {
            Commander.RemoveUnit(this);
        }
        gameObject.SetActive(false);
    }

    private void StartCombatWithOverlappingEnemyUnit()
    {
        Debug.Log($"Checking for combat start...");
        activeUnits.ForEach(unit =>
        {
            if (unit.GridPosition == GridPosition && unit.UnitTeam != UnitTeam)
            {
                MilitaryCombat.ResolveBattle(this, unit);
            }
        });
    }

    public enum Type
    {
        Rock,
        Paper,
        Scissors
    }

    public enum Team
    {
        Player,
        Alien
    }

    public static Color ColorForUnitTeam(Team team)
    {
        return team switch
        {
            Team.Player => Color.white,
            Team.Alien => new Color(1, 0.5f, 0.5f),
            _ => Color.white,
        };
    }
}
