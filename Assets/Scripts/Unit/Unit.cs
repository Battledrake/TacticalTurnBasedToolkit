using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

namespace BattleDrakeCreations.TacticalTurnBasedTemplate
{
    [RequireComponent(typeof(GridMovement), typeof(Health), typeof(AbilitySystem))]
    public class Unit : MonoBehaviour, IPlayAnimation, IAbilitySystem, IHaveHealth
    {
        public static event Action<Unit, GridIndex> OnAnyUnitReachedNewTile;
        public static event Action<Unit> OnAnyUnitDied;
        public static event Action<Unit> OnAnyUnitHealthChanged;
        public event Action<bool> OnUnitHoveredChanged;
        public event Action<bool> OnUnitSelectedChanged;
        public event Action<Unit> OnUnitReachedDestination;
        public event Action<Unit> OnUnitStartedMovement;
        public event Action<Unit> OnUnitMovementStopped;
        public event Action<Unit, bool> OnUnitDied;
        public event Action<Unit> OnUnitRespawn;
        public event Action OnTeamIndexChanged;

        [SerializeField] private UnitId _unitType = UnitId.Ranger;
        [SerializeField] private Transform _lookAtTransform;
        [SerializeField] private Color _hoverColor;
        [SerializeField] private Color _selectedColor = Color.green;

        public TacticsGrid TacticsGrid { get => _tacticsGrid; }
        public Transform LookAtTransform { get => _lookAtTransform; }
        public GridIndex UnitGridIndex { get => _gridIndex; set => _gridIndex = value; }
        public UnitData UnitData { get => _unitData; }
        public bool IsMoving { get => _gridMovement.IsMoving; }
        public bool IsAlive { get => _isAlive; }
        public int MoveRange { get => _moveRange; set => _moveRange = value; }
        public int TeamIndex { get => _teamIndex; }
        public int PreviousTeamIndex { get => _prevTeamIndex; }
        public int CurrentHealth { get => _healthComponent.CurrentHealth; }
        public int MaxHealth { get => _healthComponent.MaxHealth; }
        public int Agility { get => _agility; }

        private GameObject _unitVisual;
        private Animator _unitAnimator;
        private AnimationEventHandler _animEventHandler;
        private UnitData _unitData;
        private GridIndex _gridIndex = GridIndex.Invalid();
        private int _teamIndex = -1;
        private int _prevTeamIndex = -1;
        private bool _isAlive = true;

        //TODO: Should we move these to a set or somewhere?
        private int _currentHealth;
        private int _maxHealth;
        private int _moveRange;
        private int _agility;

        private TacticsGrid _tacticsGrid;
        private Collider _collider;
        private GridMovement _gridMovement;
        private Health _healthComponent;
        private AbilitySystem _abilitySystem;

        //Outline Stuff
        private Outline _unitOutline;
        private float _defaultOutlineWidth = 2f;
        private float _hoverSelectedWidth = 3f;
        private bool _isHovered = false;
        private bool _isSelected = false;

        private void Awake()
        {
            _collider = this.GetComponent<Collider>();
            _gridMovement = this.GetComponent<GridMovement>();
            _healthComponent = this.GetComponent<Health>();
            _abilitySystem = this.GetComponent<AbilitySystem>();

            _healthComponent.OnHealthChanged += HealthComponent_OnHealthChanged;
            _healthComponent.OnHealthReachedZero += HealthComponent_OnHealthReachedZero;
        }

        //Used for initial team setting or permanent team changes.
        public void SetTeamIndex(int index)
        {
            _prevTeamIndex = _teamIndex;
            _teamIndex = index;
            OnTeamIndexChanged?.Invoke();
            _healthComponent.SetHealthUnitColor(CombatManager.Instance.GetTeamColor(index));
        }

        //Used for team swaps due to abilities and allow prevTeamIndex to be grabbed later.
        public void ChangeTeam(int teamIndex)
        {
            _teamIndex = teamIndex;
            OnTeamIndexChanged?.Invoke();
        }

        public AnimationEventHandler GetAnimationEventHandler()
        {
            return _animEventHandler;
        }

        public AbilitySystem GetAbilitySystem()
        {
            return _abilitySystem;
        }

        public int GetCurrentHealth()
        {
            return _currentHealth;
        }

        public int GetMaxHealth()
        {
            return _maxHealth;
        }

        private void HealthComponent_OnHealthChanged()
        {
            OnAnyUnitHealthChanged?.Invoke(this);
        }

        private void OnEnable()
        {
            _gridMovement.OnMovementStarted += GridMovement_OnMovementStarted;
            _gridMovement.OnMovementStopped += GridMovement_OnMovementStopped;
            _gridMovement.OnReachedNewTile += GridMovement_OnReachedNewTile;
            _gridMovement.OnReachedDestination += GridMovement_OnReachedDestination;
        }

        public void CombatStarted()
        {
        }

        public void CombatEnded()
        {
        }

        public void TurnStarted()
        {
            _abilitySystem.ResetActionPoints();
        }

        public void TurnEnded()
        {
            //What we doing here eh?
        }

        private void OnDisable()
        {
            _gridMovement.OnMovementStarted -= GridMovement_OnMovementStarted;
            _gridMovement.OnMovementStopped -= GridMovement_OnMovementStopped;
            _gridMovement.OnReachedNewTile -= GridMovement_OnReachedNewTile;
            _gridMovement.OnReachedDestination -= GridMovement_OnReachedDestination;
        }

        private void GridMovement_OnMovementStarted()
        {
            OnUnitStartedMovement?.Invoke(this);
            _unitAnimator.SetTrigger(AnimationType.Run.ToString());
        }

        private void GridMovement_OnMovementStopped()
        {
            OnUnitMovementStopped?.Invoke(this);
        }

        private void GridMovement_OnReachedNewTile(GridIndex index)
        {
            OnAnyUnitReachedNewTile?.Invoke(this, index);
        }

        private void GridMovement_OnReachedDestination()
        {
            OnUnitReachedDestination?.Invoke(this);
            _unitAnimator.SetTrigger(AnimationType.Idle.ToString());
        }

        public void SetUnitsGrid(TacticsGrid grid)
        {
            _tacticsGrid = grid;
            _gridMovement.SetPathingGrid(grid);
        }

        public void InitUnit(UnitId unitType)
        {
            if (_unitVisual != null)
                Destroy(_unitVisual);

            _unitType = unitType;
            _unitData = DataManager.GetUnitDataFromType(_unitType);
            if (_unitVisual != null)
                Destroy(_unitVisual);
            _unitVisual = Instantiate(_unitData.assetData.unitVisual, this.transform);

            _maxHealth = _unitData.unitStats.maxHealth;
            _currentHealth = _maxHealth;
            _moveRange = _unitData.unitStats.moveRange;
            _agility = _unitData.unitStats.agility;

            _isAlive = true;

            InitComponents();
        }

        private void InitComponents()
        {
            _healthComponent.InitHealth(this);
            _abilitySystem.InitAbilitySystem(this, _unitData.unitStats.abilities);

            _unitAnimator = _unitVisual.GetComponent<Animator>();
            _unitOutline = _unitVisual.AddComponent<Outline>();
            _animEventHandler = _unitVisual.AddComponent<AnimationEventHandler>();

            Transform headTransform = FindTransform(_unitVisual, "Head");
            if (headTransform)
            {
                _healthComponent.HealthBar.position = headTransform.position + Vector3.up;
            }
        }

        private Transform FindTransform(GameObject parentObject, string transformName)
        {
            return parentObject.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == transformName);
        }

        [ContextMenu("ChangeType")]
        public void ChangeUnitType()
        {
            InitUnit(_unitType);
        }

        [ContextMenu("ResetUnit")]
        public void ResetUnit()
        {
            if (!_isAlive)
            {
                _isAlive = true;

                CombatManager.Instance.AddUnitToCombat(this.transform.position, this, _prevTeamIndex);

                _unitAnimator.ResetTrigger(AnimationType.Hit.ToString());
                _unitAnimator.SetTrigger(AnimationType.Respawn.ToString());
                _collider.enabled = true;
            }
            _currentHealth = _maxHealth;
            _healthComponent.UpdateHealth(_currentHealth);
        }

        public void SetIsHovered(bool isHovered)
        {
            _isHovered = isHovered;
            UpdateOutlineVisual();
            OnUnitHoveredChanged?.Invoke(_isHovered);
        }

        public void SetIsSelected(bool isSelected)
        {
            _isSelected = isSelected;
            UpdateOutlineVisual();
            OnUnitSelectedChanged?.Invoke(_isSelected);
        }

        public void UpdateOutlineVisual()
        {
            if (!_unitOutline)
                return;

            if (!_isHovered)
            {
                _unitOutline.enabled = false;
                return;
            }
            _unitOutline.enabled = true;

            //if (_isSelected)
            //{
            //    _unitOutline.OutlineColor = _selectedColor;

            //    if (_isHovered)
            //        _unitOutline.OutlineWidth = _hoverSelectedWidth;
            //    else
            //        _unitOutline.OutlineWidth = _defaultOutlineWidth;
            //}
            //else
            //{
            //    _unitOutline.OutlineColor = CombatManager.Instance.GetTeamColor(_teamIndex);
            //    _unitOutline.OutlineWidth = _defaultOutlineWidth;
            //}
            _unitOutline.OutlineColor = CombatManager.Instance.GetTeamColor(_teamIndex);
            _unitOutline.OutlineWidth = _defaultOutlineWidth;
        }

        public void ModifyCurrentHealth(int effectModifier)
        {
            _healthComponent.UpdateHealth(effectModifier);

            if (effectModifier < 0)
                PlayAnimationType(AnimationType.Hit);
        }

        private void HealthComponent_OnHealthReachedZero()
        {
            //TODO: we'll want to run a check to see if we want to destroy.
            //If it's a summoned unit, can check future effectTypes or something.
            //Game Design choices as well.
            Die();
        }

        public void Die(bool shouldDestroy = false)
        {
            _isAlive = false;
            _gridMovement.Stop();
            _collider.enabled = false;
            PlayAnimationType(AnimationType.Death);
            OnUnitDied?.Invoke(this, shouldDestroy);
            OnAnyUnitDied?.Invoke(this);
        }

        public void LookAtTarget(GridIndex targetIndex)
        {
            Vector3 lookAtVector = _tacticsGrid.GetTilePositionFromIndex(targetIndex);
            lookAtVector.y = this.transform.position.y;
            this.transform.LookAt(lookAtVector);
        }

        public void PlayAnimationType(AnimationType animationType)
        {
            _unitAnimator.SetTrigger(animationType.ToString());
        }
    }
}
