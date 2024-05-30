using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BattleDrakeCreations.TacticalTurnBasedTemplate
{
    public enum CombatTurnMode
    {
        Team,
        Unit,
        Stat
    }

    [Serializable]
    public struct TeamColorData
    {
        public int index;
        public Color color;
    }

    public struct CombatStartParams
    {
        public bool canStartCombat;
        public bool hasEnoughUnits;
        public bool hasEnoughTeams;
        public bool isNotInCombat;
    }

    public class CombatManager : MonoBehaviour
    {
        public static CombatManager Instance;

        public event Action<Unit, GridIndex> OnUnitGridIndexChanged;
        public event Action OnUnitTeamChanged;
        public event Action OnCombatStarted;
        public event Action OnCombatEnded;
        public event Action<Unit> OnUnitTurnStarted;
        public event Action<Unit> OnUnitTurnEnded;

        [SerializeField] private List<TeamColorData> _teamColors;

        [Header("Dependencies")]
        [SerializeField] private TacticsGrid _tacticsGrid;


        public List<Unit> UnitsInCombat { get => _unitsInCombat; }
        public Dictionary<int, HashSet<Unit>> UnitTeams { get => _unitTeams; }
        public int NumberOfTeams { get => _teamColors.Count; }
        public bool IsInCombat { get => _isInCombat; }
        public bool ShowEnemyMoveRange { get => _showEnemyMoveRange; set => _showEnemyMoveRange = value; }

        private List<Unit> _unitsInCombat = new List<Unit>();
        private Dictionary<int, HashSet<Unit>> _unitTeams = new Dictionary<int, HashSet<Unit>>();

        private bool _isInCombat = false;
        private bool _showEnemyMoveRange = false;

        private Unit _activeUnit;
        private int _actionPointDeduction = 0;

        private void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(this);
            else
                Instance = this;

            DontDestroyOnLoad(this.gameObject);
        }

        public Color GetTeamColor(int teamIndex)
        {
            return _teamColors.FirstOrDefault(d => d.index == teamIndex).color;
        }

        private void OnEnable()
        {
            _tacticsGrid.OnGridGenerated += TacticsGrid_OnGridGenerated;
            _tacticsGrid.OnTileDataUpdated += TacticsGrid_OnTileDataUpdated;
            _tacticsGrid.OnTileHeightChanged += TacticsGrid_OnTileHeightChanged;
        }

        private void OnDisable()
        {
            _tacticsGrid.OnGridGenerated -= TacticsGrid_OnGridGenerated;
            _tacticsGrid.OnTileDataUpdated -= TacticsGrid_OnTileDataUpdated;
            _tacticsGrid.OnTileHeightChanged -= TacticsGrid_OnTileHeightChanged;
        }
        public CombatStartParams CanStartCombat()
        {
            CombatStartParams combatStartParams;
            combatStartParams.isNotInCombat = !_isInCombat;
            combatStartParams.hasEnoughTeams = _unitTeams.Count(kvp => kvp.Value.Count > 0) >= 2;
            combatStartParams.hasEnoughUnits = _unitsInCombat.Count >= 2;
            combatStartParams.canStartCombat = !_isInCombat && _unitTeams.Count(kvp => kvp.Value.Count > 0) >= 2 && _unitsInCombat.Count >= 2;

            return combatStartParams;
        }

        public void StartCombat()
        {
            for (int i = 0; i < _unitsInCombat.Count; i++)
            {
                _unitsInCombat[i].CombatStarted();
            }

            _isInCombat = true;
            OnCombatStarted?.Invoke();

            _activeUnit = null;
            NextUnit();
        }

        public void EndCombat()
        {
            for (int i = 0; i < _unitsInCombat.Count; i++)
            {
                _unitsInCombat[i].CombatEnded();
            }

            _activeUnit = null;
            _isInCombat = false;
            OnCombatEnded?.Invoke();
        }

        public void NextUnit()
        {
            if (_activeUnit)
            {
                _activeUnit.TurnEnded();
                OnUnitTurnEnded?.Invoke(_activeUnit);
            }

            int _currentIndex = _unitsInCombat.IndexOf(_activeUnit);
            _currentIndex++;
            _activeUnit = _unitsInCombat[_currentIndex % _unitsInCombat.Count];
            _activeUnit.TurnStarted();
            OnUnitTurnStarted?.Invoke(_activeUnit);

            GameObject.Find("[CameraControllers]").GetComponent<CameraController>().SetMoveToTarget(_activeUnit.transform.position);
        }

        public void MoveUnit(Unit unit, List<GridIndex> path)
        {
            GridMovement gridMoveComp = unit.GetComponent<GridMovement>();
            if (gridMoveComp)
            {
                unit.OnUnitReachedDestination += Unit_OnUnitReachedDestination;
                gridMoveComp.SetPathAndMove(path);
            }

            _actionPointDeduction = path.Count <= unit.MoveRange ? 1 : 2;

            _tacticsGrid.RemoveUnitFromTile(unit.UnitGridIndex);
            _tacticsGrid.AddUnitToTile(path.Last(), unit, false);
            OnUnitGridIndexChanged?.Invoke(unit, path.Last());
        }

        private void Unit_OnUnitReachedDestination(Unit unit)
        {
            unit.OnUnitReachedDestination -= Unit_OnUnitReachedDestination;
            unit.RemoveActionPoints(_actionPointDeduction);
            _actionPointDeduction = 0;
        }

        public void AddUnitToCombat(Vector3 worldPosition, Unit unit, int teamIndex = 0)
        {
            GridIndex unitIndex = _tacticsGrid.GetTileIndexFromWorldPosition(worldPosition);
            AddUnitToCombat(unitIndex, unit, teamIndex);
        }

        public void AddUnitToCombat(GridIndex gridIndex, Unit unit, int teamIndex = 0)
        {
            if (_tacticsGrid.AddUnitToTile(gridIndex, unit))
            {
                _unitsInCombat.Add(unit);
                unit.OnUnitDied += Unit_OnUnitDied;
            }
            else
            {
                Debug.LogWarning("Unable to add unit to tile. Invalid index or unit already exists at GridIndex");
            }
            SetUnitTeamIndex(unit, teamIndex);
        }

        public void SetUnitTeamIndex(Unit unit, int teamIndex)
        {
            int previousIndex = unit.TeamIndex;
            if (previousIndex != -1)
            {
                _unitTeams.TryGetValue(previousIndex, out HashSet<Unit> unitsInTeam);
                if (unitsInTeam != null)
                    unitsInTeam.Remove(unit);
            }
            if (teamIndex != -1)
            {
                _unitTeams.TryGetValue(teamIndex, out HashSet<Unit> newTeam);
                if (newTeam == null)
                    newTeam = new HashSet<Unit>();
                newTeam.Add(unit);
                _unitTeams[teamIndex] = newTeam;
            }

            unit.TeamIndex = teamIndex;

            OnUnitTeamChanged?.Invoke();
        }

        private void Unit_OnUnitDied(Unit unit, bool shouldDestroy = false)
        {
            RemoveUnitFromCombat(unit, shouldDestroy);
        }

        /// <summary>
        /// Remove unit from combat and place at a desired position. Removes unit from grid.
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="newPosition"></param>
        public void RemoveUnitFromCombat(Unit unit, Vector3 newPosition)
        {
            unit.OnUnitDied -= Unit_OnUnitDied;

            _unitsInCombat.Remove(unit);
            _tacticsGrid.RemoveUnitFromTile(unit.UnitGridIndex);

            unit.transform.position = newPosition;

            SetUnitTeamIndex(unit, -1);
        }

        /// <summary>
        /// Remove unit from combat with optional choice to destroy gameobject. If not destroyed, unit remains at position but is taken off the grid and out of the combat units list.
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="shouldDestroy"></param>
        public void RemoveUnitFromCombat(Unit unit, bool shouldDestroy = true)
        {
            _unitsInCombat.Remove(unit);
            _tacticsGrid.RemoveUnitFromTile(unit.UnitGridIndex);
            SetUnitTeamIndex(unit, -1);

            if (shouldDestroy)
            {
                Destroy(unit.gameObject, 2f);
            }
        }

        public bool TryActivateAbility(Ability ability, Unit instigator, GridIndex origin, GridIndex target)
        {
            if (GetAbilityRange(origin, ability.RangeData).Contains(target))
            {
                //In the future, we'll want to check if there's already an ability object.
                Ability abilityObject = Instantiate(ability, _tacticsGrid.GetWorldPositionFromGridIndex(origin), Quaternion.identity);

                List<GridIndex> impactIndexes = new List<GridIndex>();
                if (ability.AreaOfEffectData.rangePattern == AbilityRangePattern.None)
                {
                    impactIndexes.Add(target);
                    abilityObject.InitializeAbility(_tacticsGrid, instigator, origin, target);
                }
                else
                {
                    impactIndexes = GetAbilityRange(target, ability.AreaOfEffectData);
                    abilityObject.InitializeAbility(_tacticsGrid, instigator, origin, target, impactIndexes);
                }

                if (abilityObject.TryActivateAbility())
                {
                    abilityObject.OnBehaviorComplete += Ability_OnBehaviorComplete;
                    //TODO: Ability point deduction logic.
                    _actionPointDeduction = abilityObject.AbilityCost;
                    //TODO: Maybe add an OnAbilityActivated or something here for the AbilityBar.
                    return true;
                }
                else
                {
                    //Ability Failed to activate. Check conditions;
                    abilityObject.EndAbility();
                }
            }
            return false;
        }

        private void Ability_OnBehaviorComplete(Ability ability)
        {
            ability.OnBehaviorComplete -= Ability_OnBehaviorComplete;
            ability.Instigator.RemoveActionPoints(_actionPointDeduction);
            _actionPointDeduction = 0;
        }

        public void ApplyEffectsToUnit(Unit instigator, Unit receiver, List<AbilityEffect> effectsToApply)
        {
            if (receiver == null)
                return;

            List<AbilityEffectReal> effectsRealList = new List<AbilityEffectReal>();
            for (int i = 0; i < effectsToApply.Count; i++)
            {
                AbilityEffectReal effectReal;
                effectReal.attributeType = effectsToApply[i].attributeType;
                effectReal.modifier = StaticUtilities.MinMaxRandom(effectsToApply[i].minMaxModifier);
                effectsRealList.Add(effectReal);
            }
            receiver.ApplyEffects(effectsRealList);
        }

        public bool IsValidTileForUnit(Unit unit, GridIndex index)
        {
            if (!_tacticsGrid.IsIndexValid(index))
                return false;

            List<TileType> tileTypes = unit.UnitData.unitStats.validTileTypes;
            return tileTypes != null && tileTypes.Contains(_tacticsGrid.GridTiles[index].tileType);
        }

        public List<GridIndex> RemoveIndexesWithoutLineOfSight(GridIndex origin, List<GridIndex> tiles, float height)
        {
            List<GridIndex> returnList = new List<GridIndex>();
            for (int i = 0; i < tiles.Count; i++)
            {
                if (HasLineOfSight(origin, tiles[i], height))
                {
                    returnList.Add(tiles[i]);
                }
            }
            return returnList;
        }

        public List<GridIndex> RemoveNotWalkableIndexes(List<GridIndex> targetIndexes)
        {
            List<GridIndex> validIndexes = new List<GridIndex>();
            for (int i = 0; i < targetIndexes.Count; i++)
            {
                if (_tacticsGrid.IsTileWalkable(targetIndexes[i]))
                    validIndexes.Add(targetIndexes[i]);
            }
            return validIndexes;
        }

        public bool HasLineOfSight(GridIndex origin, GridIndex target, float height)
        {
            if (!_tacticsGrid.GetTileDataFromIndex(origin, out TileData originData))
            {
                return false;
            }
            if (!_tacticsGrid.GetTileDataFromIndex(target, out TileData targetData))
            {
                return false;
            }


            Vector3 startPosition = originData.tileMatrix.GetPosition();
            startPosition.y += height;

            Vector3 targetPosition = targetData.tileMatrix.GetPosition();
            targetPosition.y += height;

            Vector3 direction = targetPosition - startPosition;

            if (Physics.Raycast(startPosition, direction, out RaycastHit hitInfo, direction.magnitude))
            {
                Unit abilityUnit = originData.unitOnTile;
                Unit targetUnit = targetData.unitOnTile;
                Unit hitUnit = hitInfo.collider.GetComponent<Unit>();
                if (hitUnit != null)
                {
                    if (hitUnit != abilityUnit && hitUnit != targetUnit)
                        return false;
                    else
                        return true;
                }
                else
                {
                    //Allows corner line of sights.
                    Vector2[] offsets = new Vector2[]
                    {
                        new Vector2(-.5f, 0f),
                        new Vector2(0f, .5f),
                        new Vector2(.5f, 0f),
                        new Vector2(0f, -.5f)
                    };
                    for(int i = 0; i < offsets.Length; i++)
                    {
                        Vector3 startOffset = startPosition + new Vector3(offsets[i].x, 0f, offsets[i].y);
                        if(!Physics.Raycast(startOffset, direction, out hitInfo, direction.magnitude))
                        {
                            return true;
                        }
                        else
                        {
                            if (hitUnit = hitInfo.collider.GetComponent<Unit>())
                            {
                                if (hitUnit != abilityUnit && hitUnit != targetUnit)
                                    return false;
                                else
                                    return true;
                            }
                        }
                    }
                    return false;
                }
            }
            return true;
        }

        private void TacticsGrid_OnGridGenerated()
        {
            List<Unit> copyList = new List<Unit>(_unitsInCombat);
            for (int i = 0; i < copyList.Count; i++)
            {
                Unit unit = copyList[i];
                //GridIndex unitIndex = unit.UnitGridIndex;
                GridIndex positionIndex = _tacticsGrid.GetTileIndexFromWorldPosition(unit.transform.position);
                if (IsValidTileForUnit(unit, positionIndex))
                {
                    _tacticsGrid.AddUnitToTile(positionIndex, unit);
                }
                else
                {
                    RemoveUnitFromCombat(copyList[i], false);
                }
            }
        }

        private void TacticsGrid_OnTileDataUpdated(GridIndex index)
        {
            Unit unit = _unitsInCombat.FirstOrDefault(u => u.UnitGridIndex == index);
            if (unit)
            {
                if (IsValidTileForUnit(unit, index))
                {
                    return;
                }
                else
                {
                    RemoveUnitFromCombat(unit, false);
                }
            }
        }

        private void TacticsGrid_OnTileHeightChanged(GridIndex index)
        {
            Unit unit = _unitsInCombat.FirstOrDefault(u => u.UnitGridIndex == index);
            if (unit)
            {
                _tacticsGrid.GetTileDataFromIndex(index, out TileData tileData);
                unit.transform.position = new Vector3(unit.transform.position.x, tileData.tileMatrix.GetPosition().y, unit.transform.position.z);
            }
        }

        public List<GridIndex> GetAbilityRange(GridIndex originIndex, AbilityRangeData rangeData)
        {
            List<GridIndex> indexesInRange = RemoveNotWalkableIndexes(AbilityStatics.GetIndexesFromPatternAndRange(originIndex, _tacticsGrid.GridShape, rangeData.rangeMinMax, rangeData.rangePattern));

            if (rangeData.lineOfSightData.requireLineOfSight)
            {
                indexesInRange = RemoveIndexesWithoutLineOfSight(originIndex, indexesInRange, rangeData.lineOfSightData.height);
            }
            return indexesInRange;
        }
    }
}