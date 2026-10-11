using UnityEngine.UIElements;

namespace IdleClinic.App
{
    /// <summary>While a modal sheet is open, only the sheet itself can be read or activated: every control behind it is hidden
    /// from screen readers and refuses its action, so a stale background node can never spend, travel or open a purchase.</summary>
    public static class ClinicModalPolicy
    {
        /// <summary>Open as soon as it is shown: the sheet's own display style, not a later layout pass.</summary>
        public static bool IsOpen(VisualElement modal) => modal != null && modal.panel != null && modal.style.display == DisplayStyle.Flex;

        /// <summary>True when <paramref name="element"/> may be read or activated given the modal state.</summary>
        public static bool Allows(VisualElement modal, VisualElement element)
            => element != null && (!IsOpen(modal) || element == modal || modal.Contains(element));
    }
}
