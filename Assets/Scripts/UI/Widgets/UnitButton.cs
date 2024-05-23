using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BattleDrakeCreations.TacticalTurnBasedTemplate
{
    public class UnitButton : MonoBehaviour
    {
        public event Action<UnitId> OnUnitButtonToggled;

        [SerializeField] private Color _defaultBorderColor;
        [SerializeField] private Color _selectedBorderColor;
        [SerializeField] private Image _borderImage;
        [SerializeField] private Image _icon;

        private Toggle _buttonToggle;
        private UnitId _unitType;
        private PlayerActions _playerActions;

        private void Awake()
        {
            _buttonToggle = this.GetComponent<Toggle>();
        }

        private void OnEnable()
        {
            _buttonToggle.onValueChanged.AddListener(OnButtonToggleChanged);
        }

        private void OnDisable()
        {
            _buttonToggle.onValueChanged.RemoveListener(OnButtonToggleChanged);
        }

        private void OnButtonToggleChanged(bool isOn)
        {
            if (isOn)
            {
                _borderImage.color = _selectedBorderColor;
                _playerActions.SetLeftClickActionValue((int)_unitType);
            }
            else
            {
                _borderImage.color = _defaultBorderColor;
                _playerActions.SetLeftClickActionValue(-1);
            }
            OnUnitButtonToggled?.Invoke(_unitType);
        }

        public void InitializeButton(UnitId unitType, Sprite icon, PlayerActions playerActions)
        {
            _unitType = unitType;
            _icon.sprite = icon;
            _playerActions = playerActions;
        }

        public void DisableButton()
        {
            _buttonToggle.SetIsOnWithoutNotify(false);
            _borderImage.color = _defaultBorderColor;
        }
    }
}