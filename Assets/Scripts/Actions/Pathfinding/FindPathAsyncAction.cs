using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace BattleDrakeCreations.TacticalTurnBasedTemplate
{
    public class FindPathAsyncAction : ActionBase
    {
        private bool _isSearching = false;

        public override bool ExecuteAction(GridIndex index)
        {
            _playerActions.TacticsGrid.ClearAllTilesWithState(TileState.IsInPath);

            GridIndex previousTile = _playerActions.SelectedTile;
            if (previousTile != index)
            {
                if (!_isSearching)
                {
                    ExecuteActionAsync(index);
                    _isSearching = true;
                }
            }
            return false;
        }
        private async void ExecuteActionAsync(GridIndex index)
        {
            PathParams filter = _playerActions.TacticsGrid.GridPathfinder.CreateDefaultPathParams(Mathf.Infinity);

            PathfindingResult pathResult = await Task.Run(() => { return _playerActions.TacticsGrid.GridPathfinder.FindPath(_playerActions.SelectedTile, index, filter); });

            _playerActions.TacticsGrid.GridPathfinder.OnPathfindingCompleted?.Invoke();

            if (pathResult.Result != PathResult.SearchFail)
            {
                for (int i = 0; i < pathResult.Path.Count; i++)
                {
                    _playerActions.TacticsGrid.AddStateToTile(pathResult.Path[i], TileState.IsInPath);
                }
            }

            _isSearching = false;
        }

        private void OnDestroy()
        {
            _playerActions.TacticsGrid.ClearAllTilesWithState(TileState.IsInPath);
        }
    }
}