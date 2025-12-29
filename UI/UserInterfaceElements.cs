using EFT.UI;
using System;
using TMPro;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ChouUn.Iof.Reflection;
using ChouUn.Iof.Features;
using EFT.InventoryLogic;
using static ChouUn.Iof.Features.Organizer;

namespace ChouUn.Iof.UI
{
    internal static class UserInterfaceElements
    {
        public static Button OrganizeButtonStash { get; set; } = null;
        public static Button OrganizeButtonTrader { get; set; } = null;
        public static Sprite OrganizeSprite { get; set; } = null;
        public static Sprite TakeOutSprite { get; set; } = null;

        // Visual Constants
        private static readonly Color TextColor = new Color(0.75f, 0.75f, 0.75f, 1f);
        private const float ContainerButtonWidth = 35f;
        private const float FontSize = 12f;
        private const string FontName = "BenderBold";

        public static Button SetupOrganizeButton(Button sourceForCloneButton, CompoundItem item, InventoryController controller)
        {
            var clone = CloneAndCleanButton(sourceForCloneButton);

            clone.onClick.AddListener(new UnityEngine.Events.UnityAction(() =>
            {
                try
                {
                    var showMessageWindowArgs = new object[]
                    {
                        "Do you want to organize all items by tagged containers?",
                        new Action(() => Organize(item, controller)),
                        new Action(DoNothing),
                    };
                    
                    // Simple reflection invoke for ShowMessageWindow
                    ReflectionHelper.InvokeMethod(
                        ItemUiContext.Instance,
                        "ShowMessageWindow",
                        showMessageWindowArgs,
                        new Type[] { typeof(string), typeof(Action), typeof(Action), typeof(string), typeof(float), typeof(bool), typeof(TextAlignmentOptions) }
                    );
                }
                catch (Exception ex)
                {
                    throw Plugin.ShowErrorNotif(ex);
                }
            }));

            // Determine if this is the stash panel or container panel based on context or button parent?
            // The original logic checked for specific children or relied on context. 
            // However, the cleanest way based on previous code is:
            // Stash panel has a child named "Image" that we disabled.
            // Container panel has a child named "SortIcon".
            // We can try to handle both generically or check specific structure.
            
            // Let's look at the structure differences:
            // Stash: sourceForCloneButton -> Image (Icon)
            // Container: sourceForCloneButton -> SortIcon
            
            if (sourceForCloneButton.transform.Find("Image") != null) 
            {
                // Stash Panel Logic
                StyleButton(clone.gameObject, "ORG", null); // default width (native)
                
                // Stash specifically had a check for "Image" to disable it.
                var childImage = clone.transform.Find("Image");
                if (childImage != null) childImage.gameObject.SetActive(false);
            }
            else
            {
                // Container Panel Logic
                StyleButton(clone.gameObject, "ORG", ContainerButtonWidth);

                // Container specifically had a check for "SortIcon" to disable it.
                foreach (Transform child in clone.transform)
                {
                    if (child.name.Equals("SortIcon")) child.gameObject.SetActive(false);
                }
            }

            AddTooltip(clone.gameObject, "Organize items");
            clone.gameObject.SetActive(true);
            return clone;
        }

        public static Button SetupTakeOutButton(Button sourceForCloneButton, CompoundItem item, InventoryController controller)
        {
            var clone = CloneAndCleanButton(sourceForCloneButton);

            clone.onClick.AddListener(new UnityEngine.Events.UnityAction(() =>
            {
                try
                {
                     ItemUiContext.Instance.ShowMessageWindow(
                        "Do you want to take out all items from this container?",
                        new Action(() =>
                        {
                             // Check if parent is DefaultInventoryId. It's applicable on items which are equipped on PMC.
                             var parent = item.Parent.Container.ParentItem;
                             // Use reverse organizing with ignoreParams = true
                             new OrganizedContainer(parent.TemplateId == "55d7217a4bdc2d86028b456d" ? controller.Inventory.Stash : (CompoundItem)parent, item, controller).Organize(true);
                        }),
                        new Action(DoNothing)
                    );
                }
                catch (Exception ex)
                {
                    throw Plugin.ShowErrorNotif(ex);
                }
            }));

            // TakeOut button is only used in Container View, so we apply fixed width always.
            StyleButton(clone.gameObject, "OUT", ContainerButtonWidth);

             foreach (Transform child in clone.transform)
            {
                if (child.name.Equals("SortIcon")) child.gameObject.SetActive(false);
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
            // Enforce Width if requested
            if (forcedWidth.HasValue)
            {
                var layoutElement = buttonObj.GetComponent<LayoutElement>();
                if (layoutElement == null) layoutElement = buttonObj.AddComponent<LayoutElement>();
                layoutElement.minWidth = forcedWidth.Value;
                layoutElement.flexibleWidth = 0;
            }

            // Setup Text
            var textTrans = buttonObj.transform.Find("Text");
            GameObject textObj;
            if (textTrans == null)
            {
                textObj = new GameObject("Text");
                textObj.transform.SetParent(buttonObj.transform, false);
                var rect = textObj.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
            }
            else
            {
                textObj = textTrans.gameObject;
            }

            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            if (tmp == null) tmp = textObj.AddComponent<TextMeshProUGUI>();

            tmp.text = text;
            tmp.color = TextColor;
            tmp.fontSize = FontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = false;

            if (tmp.font == null)
            {
                var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
                var font = allFonts.FirstOrDefault(x => x.name.Equals(FontName));
                if (font == null) font = allFonts.FirstOrDefault(x => x.name.Contains("Bender"));
                if (font != null) tmp.font = font;
            }

            textObj.SetActive(true);
        }

        private static void AddTooltip(GameObject go, string message)
        {
            var trigger = go.GetComponent<EventTrigger>();
            if (trigger == null) trigger = go.AddComponent<EventTrigger>();

            var entryEnter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entryEnter.callback.AddListener((data) => {
                ItemUiContext.Instance.Tooltip.Show(message);
            });
            trigger.triggers.Add(entryEnter);

            var entryExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            entryExit.callback.AddListener((data) => {
                ItemUiContext.Instance.Tooltip.Close();
            });
            trigger.triggers.Add(entryExit);
        }

        private static void DoNothing()
        {
        }
    }
}
