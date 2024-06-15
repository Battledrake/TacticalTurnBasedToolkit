using System.Collections.Generic;
using UnityEngine;

namespace BattleDrakeCreations.TacticalTurnBasedTemplate
{
    public class MoveUnitOnGridAction : ActionBase
    {
        private Unit _currentUnit;

        public override bool ExecuteAction(GridIndex index)
        {
            _playerActions.TacticsGrid.ClearAllTilesWithState(TileState.IsInPath);

            if (_currentUnit != null)
            {
                if (_currentUnit != _playerActions.SelectedUnit)
                {
                    _currentUnit.OnUnitReachedDestination -= SelectedUnit_OnUnitReachedDestination;
                }
                else
                {
                    if (_currentUnit.GetComponent<GridMovement>().IsMoving)
                        return false;
                }
            }

            _currentUnit = _playerActions.SelectedUnit;
            if (_currentUnit != null)
            {
                PathParams pathParams = GridPathfinding.CreatePathParamsFromUnit(_currentUnit, _currentUnit.MoveRange);

                PathfindingResult pathResult = _playerActions.TacticsGrid.Pathfinder.FindPath(_currentUnit.GridIndex, index, pathParams);
                if (pathResult.Result == PathResult.SearchSuccess)
                {
                    List<GridIndex> pathIndexes = PathfindingStatics.ConvertPathNodesToGridIndexes(pathResult.Path);
                    for (int i = 0; i < pathIndexes.Count; i++)
                    {
                        _playerActions.TacticsGrid.AddStateToTile(pathIndexes[i], TileState.IsInPath);
                    }
                    CombatManager.Instance.MoveUnit(_currentUnit, pathIndexes, pathResult.Length);

                    _playerActions.TacticsGrid.ClearAllTilesWithState(TileState.IsInMoveRange);
                    _currentUnit.OnUnitReachedDestination += SelectedUnit_OnUnitReachedDestination;
                    return true;
                }
            }
            return false;
        }

        private void SelectedUnit_OnUnitReachedDestination(Unit unit)
        {
            unit.OnUnitReachedDestination -= SelectedUnit_OnUnitReachedDestination;
            _playerActions.TacticsGrid.ClearAllTilesWithState(TileState.IsInPath);
        }

        private void OnDestroy()
        {
            if (_currentUnit != null)
                _currentUnit.OnUnitReachedDestination -= SelectedUnit_OnUnitReachedDestination;

            _playerActions.TacticsGrid.ClearAllTilesWithState(TileState.IsInPath);
        }
    }
}