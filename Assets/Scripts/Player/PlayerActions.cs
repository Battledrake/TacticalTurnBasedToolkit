using System;
using Unity.VisualScripting;
using UnityEngine;

namespace BattleDrakeCreations.TacticalTurnBasedTemplate
{
    public class PlayerActions : MonoBehaviour
    {
        public event Action<ActionBase, ActionBase> OnSelectedActionsChanged;

        [SerializeField] private TacticsGrid _tacticsGrid;
        [SerializeField] private CombatSystem _combatSystem;

        public TacticsGrid TacticsGrid { get => _tacticsGrid; }
        public CombatSystem CombatSystem { get => _combatSystem; }
        public GridIndex HoveredTile { get => _hoveredTile; set => _selectedTile = value; }
        public GridIndex SelectedTile { get => _selectedTile; set => _selectedTile = value; }
        public Unit HoveredUnit { get => _hoveredUnit; set => _hoveredUnit = value; }
        public Unit SelectedUnit { get => _selectedUnit; set => _selectedUnit = value; }
        public ActionBase LeftClickAction { get => _leftClickAction; }
        public ActionBase RightClickAction { get => _rightClickAction; }

        private GridIndex _hoveredTile = new GridIndex(int.MinValue, int.MinValue);
        private GridIndex _selectedTile = new GridIndex(int.MinValue, int.MinValue);
        private Unit _hoveredUnit = null;
        private Unit _selectedUnit = null;

        private ActionBase _leftClickAction;
        private ActionBase _rightClickAction;

        private Action HoverTileChanged;

        private bool _isLeftClickDown = false;
        private bool _isRightClickDown = false;


        private void Awake()
        {
            HoverTileChanged += OnHoverTileChanged;
        }

        private void OnHoverTileChanged()
        {
            if (_isLeftClickDown)
            {
                TryLeftClickAction();
            }
            if (_isRightClickDown)
            {
                TryRightClickAction();
            }
        }

        private void Update()
        {
            UpdatedHoveredTileAndUnit();

            if (Input.GetMouseButtonDown(0))
            {
                _isLeftClickDown = true;
                TryLeftClickAction();
            }
            if (Input.GetMouseButtonUp(0))
            {
                _isLeftClickDown = false;
            }
            if (Input.GetMouseButtonDown(1))
            {
                _isRightClickDown = true;
                TryRightClickAction();
            }
            if (Input.GetMouseButtonUp(1))
            {
                _isRightClickDown = false;
            }


        }

        private void TryLeftClickAction()
        {
            if (_leftClickAction)
                _leftClickAction.ExecuteAction(_hoveredTile);
        }
        private void TryRightClickAction()
        {
            if (_rightClickAction)
                _rightClickAction.ExecuteAction(_hoveredTile);
        }

        private void UpdatedHoveredTileAndUnit()
        {
            Unit unit = GetUnitUnderCursor();
            if (_hoveredUnit != unit)
            {
                if (_hoveredUnit != null)
                    _hoveredUnit.SetIsHovered(false);

                if (unit != null)
                    unit.SetIsHovered(true);

                _hoveredUnit = unit;
            }

            GridIndex newIndex;
            if (_hoveredUnit)
            {
                newIndex = _hoveredUnit.UnitGridIndex;
            }
            else
            {
                newIndex = _tacticsGrid.GetTileIndexUnderCursor();
            }

            if (newIndex != _hoveredTile)
            {
                _tacticsGrid.RemoveStateFromTile(_hoveredTile, TileState.Hovered);
                _hoveredTile = newIndex;
                _tacticsGrid.AddStateToTile(_hoveredTile, TileState.Hovered);
                HoverTileChanged?.Invoke();
            }
        }

        public void SetSelectedTileAndUnit(GridIndex index)
        {
            GridIndex previousTile = _selectedTile;
            if (previousTile != index)
            {
                _tacticsGrid.RemoveStateFromTile(previousTile, TileState.Selected);
                _selectedTile = index;
                _tacticsGrid.AddStateToTile(index, TileState.Selected);

            }
            else //Clicked on a tile that was already selected
            {
                _tacticsGrid.RemoveStateFromTile(index, TileState.Selected);
                _selectedTile = new GridIndex(int.MinValue, int.MinValue);
                if (_selectedUnit != null)
                {
                    _selectedUnit.SetIsSelected(false);
                    _selectedUnit = null;
                    return;
                }
            }

            _tacticsGrid.GridTiles.TryGetValue(index, out TileData tileData);

            if (tileData.unitOnTile != _selectedUnit)
            {
                if (_selectedUnit != null)
                {
                    _selectedUnit.SetIsSelected(false);
                }
                if (tileData.unitOnTile != null)
                {
                    tileData.unitOnTile.SetIsSelected(true);
                }
                _selectedUnit = tileData.unitOnTile;
            }
        }

        public void SetLeftClickActionValue(int value)
        {
            if (_leftClickAction != null)
                _leftClickAction.actionValue = value;
        }

        public void SetRightClickActionValue(int value)
        {
            if (_rightClickAction != null)
                _rightClickAction.actionValue = value;
        }

        public void ClearSelectedActions()
        {
            Destroy(_leftClickAction.gameObject);
            _leftClickAction = null;
            Destroy(_rightClickAction.gameObject);
            _rightClickAction = null;
        }

        public void SetSelectedActions(ActionBase leftClickAction, ActionBase rightClickAction)
        {
            if (_leftClickAction != null)
                ClearSelectedActions();

            _leftClickAction = GameObject.Instantiate(leftClickAction);
            _leftClickAction.InitializeAction(this);
            _rightClickAction = GameObject.Instantiate(rightClickAction);
            _rightClickAction.InitializeAction(this);

            OnSelectedActionsChanged?.Invoke(_leftClickAction, _rightClickAction);
        }

        public Unit GetUnitUnderCursor()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            LayerMask unitLayer = LayerMask.NameToLayer("Unit");

            if (Physics.Raycast(ray, out RaycastHit hitInfo, 1000f, unitLayer))
            {
                return hitInfo.transform.GetComponent<Unit>();
            }
            else
            {
                GridIndex tileIndex = _tacticsGrid.GetTileIndexUnderCursor();
                _tacticsGrid.GridTiles.TryGetValue(tileIndex, out TileData tileData);

                return tileData.unitOnTile;
            }
        }
    }
}
