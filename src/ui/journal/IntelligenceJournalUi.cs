using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Core.Narrative.Dialogue;

namespace UI.Journal
{
    /// <summary>
    /// Intelligence Journal UI controller.
    ///
    /// Displays discovered clues grouped by category (Documents, Conversations, Evidence).
    /// Undiscovered clues are not shown — the journal only displays what the player has found.
    /// Accessed from the pause menu as a modal overlay.
    /// </summary>
    public class IntelligenceJournalUi : MonoBehaviour
    {
        #region Constants

        /// <summary>
        /// Category display names for the UI.
        /// </summary>
        private static readonly Dictionary<string, string> CategoryDisplayNames = new Dictionary<string, string>
        {
            { ClueCategory.Documents, "Documents" },
            { ClueCategory.Conversations, "Conversations" },
            { ClueCategory.Evidence, "Evidence" },
            { ClueCategory.Uncategorized, "Miscellaneous" }
        };

        #endregion

        #region Inspector References

        [Header("UI Document")]
        [SerializeField] private UIDocument _uiDocument;

        [Header("Panel Reference (optional — uses Template if not set)")]
        [SerializeField] private VisualTreeAsset _uxmlTemplate;

        #endregion

        #region Private Fields

        private VisualElement _root;
        private VisualElement _backdrop;
        private VisualElement _panel;
        private Label _titleLabel;
        private Button _closeButton;
        private VisualElement _documentsContent;
        private VisualElement _conversationsContent;
        private VisualElement _evidenceContent;
        private VisualElement _uncategorizedContent;
        private VisualElement _uncategorizedSection;
        private bool _isVisible;

        // Event for when the journal is closed
        public event Action OnClose;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeUI();
            Hide();
        }

        private void OnDestroy()
        {
            if (_closeButton != null)
                _closeButton.UnregisterCallback<ClickEvent>(HandleCloseClicked);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Shows the Intelligence Journal with current clue data.
        /// </summary>
        public void Show()
        {
            if (_isVisible) return;

            RefreshClueDisplay();
            SetVisibility(true);
            _isVisible = true;
        }

        /// <summary>
        /// Hides the Intelligence Journal.
        /// </summary>
        public void Hide()
        {
            if (!_isVisible) return;

            SetVisibility(false);
            _isVisible = false;
        }

        /// <summary>
        /// Toggles the journal visibility.
        /// </summary>
        public void Toggle()
        {
            if (_isVisible)
                Hide();
            else
                Show();
        }

        /// <summary>
        /// Returns whether the journal is currently visible.
        /// </summary>
        public bool IsVisible => _isVisible;

        #endregion

        #region Initialization

        private void InitializeUI()
        {
            // Load UXML from the template or use the document's tree
            if (_uxmlTemplate != null)
            {
                _root = _uxmlTemplate.CloneTree();
                _uiDocument.visualTree = _root;
            }
            else if (_uiDocument != null)
            {
                _root = _uiDocument.visualTree;
            }
            else
            {
                Debug.LogError("[IntelligenceJournalUi] No UIDocument or UXML template assigned.");
                return;
            }

            // Cache element references
            _backdrop = _root.Query<VisualElement>("journal-backdrop").First();
            _panel = _root.Query<VisualElement>("journal-panel").First();
            _titleLabel = _root.Query<Label>("journal-title").First();
            _closeButton = _root.Query<Button>("journal-close-button").First();
            _documentsContent = _root.Query<VisualElement>("documentsContent").First();
            _conversationsContent = _root.Query<VisualElement>("conversationsContent").First();
            _evidenceContent = _root.Query<VisualElement>("evidenceContent").First();
            _uncategorizedContent = _root.Query<VisualElement>("uncategorizedContent").First();
            _uncategorizedSection = _root.Query<VisualElement>("uncategorizedSection").First();

            // Apply stylesheet
            var stylesheet = Resources.Load<StyleSheet>("USS/IntelligenceJournalUi");
            if (stylesheet != null)
            {
                _root.styleSheets.Add(stylesheet);
            }

            // Register close button handler
            if (_closeButton != null)
            {
                _closeButton.RegisterCallback<ClickEvent>(HandleCloseClicked);
            }

            // Make backdrop clickable to close
            if (_backdrop != null)
            {
                _backdrop.RegisterCallback<ClickEvent>(HandleBackdropClicked);
            }
        }

        #endregion

        #region Clue Display

        /// <summary>
        /// Refreshes the clue display with current data from ClueSystem.
        /// </summary>
        private void RefreshClueDisplay()
        {
            ClearAllClueItems();

            var cluesByCategory = ClueSystem.GetDiscoveredCluesByCategory();

            PopulateCategorySection(_documentsContent, cluesByCategory, ClueCategory.Documents);
            PopulateCategorySection(_conversationsContent, cluesByCategory, ClueCategory.Conversations);
            PopulateCategorySection(_evidenceContent, cluesByCategory, ClueCategory.Evidence);
            PopulateCategorySection(_uncategorizedContent, cluesByCategory, ClueCategory.Uncategorized);

            // Hide uncategorized section if empty
            if (_uncategorizedSection != null)
            {
                bool hasUncategorized = cluesByCategory.TryGetValue(ClueCategory.Uncategorized, out var uncategorized)
                    && uncategorized.Count > 0;
                _uncategorizedSection.style.display = hasUncategorized ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void PopulateCategorySection(
            VisualElement container,
            Dictionary<string, List<ClueSystem.ClueMetadata>> cluesByCategory,
            string category)
        {
            if (container == null) return;

            if (!cluesByCategory.TryGetValue(category, out var clues) || clues.Count == 0)
            {
                AddEmptyLabel(container);
                return;
            }

            foreach (var clue in clues)
            {
                AddClueItem(container, clue);
            }
        }

        private void AddClueItem(VisualElement container, ClueSystem.ClueMetadata clue)
        {
            var item = new VisualElement();
            item.AddToClassList("journal-clue-item");
            item.style.flexDirection = FlexDirection.Row;
            item.style.alignItems = Align.Center;
            item.style.marginBottom = new StyleLength(4);

            var bullet = new VisualElement();
            bullet.AddToClassList("journal-clue-bullet");
            bullet.style.width = new StyleLength(6);
            bullet.style.height = new StyleLength(6);
            bullet.style.backgroundColor = new Color(0.79f, 0.64f, 0.15f); // --journal-accent
            bullet.style.borderRadius = new BorderRadius(new StyleLength(3));
            bullet.style.marginRight = new StyleLength(10);

            var label = new Label();
            label.AddToClassList("journal-clue-name");
            label.text = FormatClueName(clue.Id);
            label.style.fontSize = new StyleLength(15);
            label.style.color = new Color(0.91f, 0.91f, 0.94f); // --journal-text-primary

            item.Add(bullet);
            item.Add(label);
            container.Add(item);
        }

        private void AddEmptyLabel(VisualElement container)
        {
            var label = new Label();
            label.AddToClassList("journal-empty-text");
            label.text = "No clues discovered yet.";
            label.style.fontSize = new StyleLength(14);
            label.style.color = new Color(0.53f, 0.53f, 0.67f); // --journal-text-secondary
            label.style.unityFontStyleAndOptions = FontStyle.Italic;
            container.Add(label);
        }

        private void ClearAllClueItems()
        {
            ClearContainer(_documentsContent);
            ClearContainer(_conversationsContent);
            ClearContainer(_evidenceContent);
            ClearContainer(_uncategorizedContent);
        }

        private void ClearContainer(VisualElement container)
        {
            if (container == null) return;
            container.Clear();
        }

        /// <summary>
        /// Formats a clue ID into a human-readable display name.
        /// e.g. "clue_zhang_affair" -> "Zhang Affair"
        /// </summary>
        private string FormatClueName(string clueId)
        {
            if (string.IsNullOrEmpty(clueId)) return "Unknown";

            // Remove "clue_" prefix if present
            string name = clueId;
            if (name.StartsWith("clue_", StringComparison.OrdinalIgnoreCase))
                name = name.Substring(5);

            // Replace underscores with spaces
            name = name.Replace("_", " ");

            // Title case each word
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name.ToLower());
        }

        #endregion

        #region Visibility

        private void SetVisibility(bool visible)
        {
            if (_backdrop != null)
            {
                _backdrop.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        #endregion

        #region Event Handlers

        private void HandleCloseClicked(ClickEvent evt)
        {
            Hide();
            OnClose?.Invoke();
        }

        private void HandleBackdropClicked(ClickEvent evt)
        {
            // Only close if clicking the backdrop itself, not the panel
            if (evt.target == _backdrop)
            {
                Hide();
                OnClose?.Invoke();
            }
        }

        #endregion
    }
}