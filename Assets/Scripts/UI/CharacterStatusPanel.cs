using MineCraftUnity.Player;
using UnityEngine;
using UnityEngine.UI;

namespace MineCraftUnity.UI
{
    /// <summary>
    /// Controls visibility, animation and data binding for the world-space character status panel.
    /// Subscribe to PlayerStats.StatsChanged — never polls per frame.
    /// </summary>
    public sealed class CharacterStatusPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerStats stats;

        [Header("Animation")]
        [SerializeField] private float openDuration = 0.2f;
        [SerializeField] private float closeDuration = 0.15f;

        private CharacterStatusPanelBuilder _builder;
        private CanvasGroup _canvasGroup;
        private Transform _panelRoot;

        private enum PanelState { Closed, Opening, Open, Closing }
        private PanelState _state = PanelState.Closed;
        private float _animT;

        public bool IsVisible => _state == PanelState.Open || _state == PanelState.Opening;

        private Vector3 _baseLocalScale;

        // Button References
        private Button _hpButton;
        private Button _mpButton;
        private Button _staButton;
        private Button _strButton;
        private Button _defButton;
        private Button _agiButton;
        private Button _dexButton;
        private Button _intButton;
        private bool _buttonsBound;

        // ─── Lifecycle ───

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void Initialize(PlayerStats playerStats)
        {
            _builder = GetComponent<CharacterStatusPanelBuilder>();
            if (_builder != null)
            {
                _builder.Build();
            }

            // Cache the scale (either from Prefab or from Builder)
            _baseLocalScale = transform.localScale;

            // Hide the panel initially
            _canvasGroup.alpha = 0f;
            transform.localScale = Vector3.zero;

            stats = playerStats;
            stats.StatsChanged += RefreshLabels;
            
            if (!_buttonsBound)
            {
                BindButtons();
                _buttonsBound = true;
            }

            RefreshLabels();
        }

        private void OnDestroy()
        {
            if (stats != null)
                stats.StatsChanged -= RefreshLabels;
        }

        // ─── Toggle ───

        public void Toggle()
        {
            if (_state == PanelState.Closed || _state == PanelState.Closing)
                SetVisible(true);
            else
                SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (visible)
            {
                RefreshLabels();
                _state = PanelState.Opening;
                _animT = 0f;
            }
            else
            {
                _state = PanelState.Closing;
                _animT = 0f;
            }
        }

        // ─── Update Animation ───

        private void Update()
        {
            switch (_state)
            {
                case PanelState.Opening:
                {
                    _animT += Time.deltaTime / Mathf.Max(openDuration, 0.01f);
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_animT));
                    _canvasGroup.alpha = t;
                    transform.localScale = _baseLocalScale * t;
                    if (_animT >= 1f)
                        _state = PanelState.Open;
                    break;
                }
                case PanelState.Closing:
                {
                    _animT += Time.deltaTime / Mathf.Max(closeDuration, 0.01f);
                    float t = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_animT));
                    _canvasGroup.alpha = t;
                    transform.localScale = _baseLocalScale * t;
                    if (_animT >= 1f)
                    {
                        _state = PanelState.Closed;
                        _canvasGroup.alpha = 0f;
                        transform.localScale = _baseLocalScale * 0f;
                    }
                    break;
                }
            }
        }

        // ─── Data Binding ───

        private void RefreshLabels()
        {
            if (stats == null || _builder == null) return;
            _builder.SetLabels(
                "Player",
                stats.Level,
                stats.CurrentClass,
                stats.UnspentStatPoints,
                Mathf.RoundToInt(stats.MaxHealth),
                stats.Strength,
                stats.Defense,
                stats.Agility,
                stats.Dexterity,
                stats.Intelligence,
                Mathf.RoundToInt(stats.MaxMana),
                Mathf.RoundToInt(stats.MaxStamina)
            );

            bool canSpend = stats.UnspentStatPoints > 0;
            if (_hpButton != null) _hpButton.interactable = canSpend;
            if (_mpButton != null) _mpButton.interactable = canSpend;
            if (_staButton != null) _staButton.interactable = canSpend;
            if (_strButton != null) _strButton.interactable = canSpend;
            if (_defButton != null) _defButton.interactable = canSpend;
            if (_agiButton != null) _agiButton.interactable = canSpend;
            if (_dexButton != null) _dexButton.interactable = canSpend;
            if (_intButton != null) _intButton.interactable = canSpend;
        }

        private void BindButtons()
        {
            _hpButton = SetupButton("HPLabel/HPButton", StatType.Health);
            _mpButton = SetupButton("MPLabel/MPButton", StatType.Mana);
            _staButton = SetupButton("STALabel/STAButton", StatType.Stamina);
            _strButton = SetupButton("STRLabel/STRButton", StatType.Strength);
            _defButton = SetupButton("DEFLabel/DEFButton", StatType.Defense);
            _agiButton = SetupButton("AGILabel/AGIButton", StatType.Agility);
            _dexButton = SetupButton("DEXLabel/DEXButton", StatType.Dexterity);
            _intButton = SetupButton("INTLabel/INTButton", StatType.Intelligence);
        }

        private Button SetupButton(string path, StatType stat)
        {
            var btnTrans = transform.Find(path);
            if (btnTrans == null) return null;

            var btn = btnTrans.GetComponent<Button>();
            if (btn == null)
            {
                btn = btnTrans.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
            }

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SpendPoint(stat));
            return btn;
        }

        private void SpendPoint(StatType stat)
        {
            if (stats != null)
            {
                stats.SpendStatPoint(stat);
            }
        }
    }
}
