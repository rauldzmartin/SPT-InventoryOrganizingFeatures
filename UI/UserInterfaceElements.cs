using System;
using System.Linq;
using ChouUn.Iof.Features;
using ChouUn.Iof.Reflection;
using EFT.InventoryLogic;
using EFT.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChouUn.Iof.UI
{
    internal static class UserInterfaceElements
    {
        private static readonly Color TextColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        private const float ContainerButtonWidth = 35f;
        private const float FontSize = 12f;
        private const string FontName = "BenderBold";

        public static Button OrganizeButtonStash { get; set; } = null;
        public static Button OrganizeButtonTrader { get; set; } = null;
        public static Sprite OrganizeSprite { get; set; } = null;
        public static Sprite TakeOutSprite { get; set; } = null;

        public static Button SetupOrganizeButton(Button sourceForCloneButton, CompoundItem item, InventoryController controller)
        {
            var clone = CloneAndCleanButton(sourceForCloneButton);

            clone.onClick.AddListener(new UnityAction(() =>
            {
                try
                {
                    var showMessageWindowArgs = new object[]
                    {
                        "Do you want to organize all items by tagged containers?",
                        new Action(() =>
                        {
                            Organizer.Organize(item, controller);
                        }),
                        new Action(DoNothing),
                    };
                    var showMessageWindowArgTypes = new Type[]
                    {
                        typeof(string),
                        typeof(Action),
                        typeof(Action),
                        typeof(string),
                        typeof(float),
                        typeof(bool),
                        typeof(TextAlignmentOptions),
                    };
                    ReflectionHelper.InvokeMethod(
                        ItemUiContext.Instance,
                        "ShowMessageWindow",
                        showMessageWindowArgs,
                        showMessageWindowArgTypes
                    );
                }
                catch (Exception ex)
                {
                    throw Plugin.ShowErrorNotif(ex);
                }
            }));

            // For stash panel - hide image and use text
            var childImage = clone.transform.Find("Image");
            if (childImage != null)
            {
                StyleButton(clone.gameObject, "ORG", null);
                childImage.gameObject.SetActive(false);
            }
            else
            {
                // For container view panel
                StyleButton(clone.gameObject, "ORG", ContainerButtonWidth);
                foreach (Transform child in clone.transform)
                {
                    if (child.name.Equals("SortIcon"))
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }

            AddTooltip(clone.gameObject, "Organize items");
            clone.gameObject.SetActive(true);
            return clone;
        }

        private const string DefaultInventoryId = "55d7217a4bdc2d86028b456d";

        public static Button SetupTakeOutButton(Button sourceForCloneButton, CompoundItem item, InventoryController controller)
        {
            var clone = CloneAndCleanButton(sourceForCloneButton);

            clone.onClick.AddListener(new UnityAction(() =>
            {
                try
                {
                    ItemUiContext.Instance.ShowMessageWindow(
                        "Do you want to take out all items from this container?",
                        new Action(() =>
                        {
                            var parent = item.Parent.Container.ParentItem;
                            var targetContainer = parent.TemplateId == DefaultInventoryId
                                ? controller.Inventory.Stash
                                : (CompoundItem)parent;
                            new OrganizedContainer(targetContainer, item, controller).Organize(true);
                        }),
                        new Action(DoNothing),
                        null,
                        0f,
                        false,
                        TextAlignmentOptions.Center
                    );
                }
                catch (Exception ex)
                {
                    throw Plugin.ShowErrorNotif(ex);
                }
            }));

            StyleButton(clone.gameObject, "OUT", ContainerButtonWidth);

            foreach (Transform child in clone.transform)
            {
                if (child.name.Equals("SortIcon"))
                {
                    child.gameObject.SetActive(false);
                }
            }

            AddTooltip(clone.gameObject, "Take all items out");
            clone.gameObject.SetActive(true);
            return clone;
        }

        private static Button CloneAndCleanButton(Button source)
        {
            var clone = GameObject.Instantiate(source, source.transform.parent);
            clone.onClick.RemoveAllListeners();
            return clone;
        }

        private static void StyleButton(GameObject buttonObj, string text, float? forcedWidth)
        {
            // Set fixed width if specified
            if (forcedWidth.HasValue)
            {
                var layoutElement = buttonObj.GetComponent<LayoutElement>();
                if (layoutElement == null)
                {
                    layoutElement = buttonObj.AddComponent<LayoutElement>();
                }
                layoutElement.minWidth = forcedWidth.Value;
                layoutElement.flexibleWidth = 0f;
            }

            // Find or create text element
            var textTransform = buttonObj.transform.Find("Text");
            GameObject textObj;
            if (textTransform == null)
            {
                textObj = new GameObject("Text");
                textObj.transform.SetParent(buttonObj.transform, false);
                var rectTransform = textObj.AddComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.sizeDelta = Vector2.zero;
            }
            else
            {
                textObj = textTransform.gameObject;
            }

            // Configure TextMeshPro
            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                tmp = textObj.AddComponent<TextMeshProUGUI>();
            }
            tmp.text = text;
            tmp.color = TextColor;
            tmp.fontSize = FontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = false;

            // Find and set font
            if (tmp.font == null)
            {
                var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                var font = fonts.FirstOrDefault(x => x.name.Equals(FontName))
                        ?? fonts.FirstOrDefault(x => x.name.Contains("Bender"));
                if (font != null)
                {
                    tmp.font = font;
                }
            }

            textObj.SetActive(true);
        }

        private static void AddTooltip(GameObject go, string message)
        {
            var trigger = go.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = go.AddComponent<EventTrigger>();
            }

            // Pointer Enter - show tooltip
            var enterEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enterEntry.callback.AddListener(_ =>
            {
                ItemUiContext.Instance.Tooltip.Show(message, null, 0f, null);
            });
            trigger.triggers.Add(enterEntry);

            // Pointer Exit - hide tooltip
            var exitEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exitEntry.callback.AddListener(_ =>
            {
                ItemUiContext.Instance.Tooltip.Close();
            });
            trigger.triggers.Add(exitEntry);
        }

        private static void DoNothing()
        {
            // Empty method used to close message window
        }
    }
}
