namespace Project05.UI.Hud
{
    /// <summary>What the board controller should do after an input event.</summary>
    public enum HoldAction
    {
        /// <summary>Nothing.</summary>
        None,

        /// <summary>Pick the clicked item up.</summary>
        Pickup,

        /// <summary>Place the held item (or send it back if the target is invalid).</summary>
        Drop,
    }

    /// <summary>
    /// The click rules for holding an item, kept apart from the engine so they can be tested. Picking up and placing by
    /// click both happen when the mouse button is released, and the click event the UI sends for that same release must
    /// never be counted twice: otherwise placing an item would pick it straight up again, which looks like one click too
    /// many. An event that arrives in the frame an item was dropped is ignored whatever the order in which the engine
    /// delivers the mouse state and the click.
    /// </summary>
    public sealed class HoldGesture
    {
        private bool _pickedByClick;
        private int _pickFrame = -1;
        private int _dropFrame = -1;

        /// <summary>True while an item is held.</summary>
        public bool Held { get; private set; }

        /// <summary>Records that an item was picked up.</summary>
        /// <param name="frame">The frame number.</param>
        /// <param name="byClick">True when picked by a click rather than a drag.</param>
        public void Picked(int frame, bool byClick)
        {
            Held = true;
            _pickedByClick = byClick;
            _pickFrame = frame;
        }

        /// <summary>Records that the held item was placed or sent back.</summary>
        public void Dropped(int frame)
        {
            Held = false;
            _dropFrame = frame;
        }

        /// <summary>A plate was clicked (the button click event) or submitted with the keyboard.</summary>
        /// <param name="frame">The frame number.</param>
        /// <param name="fromMouseRelease">True when the click came from releasing the mouse button.</param>
        public HoldAction Click(int frame, bool fromMouseRelease)
        {
            if (frame == _dropFrame) return HoldAction.None;          // the release that just placed the item
            if (!Held) return HoldAction.Pickup;
            if (fromMouseRelease) return HoldAction.None;             // a mouse release is handled by MouseRelease
            return frame != _pickFrame ? HoldAction.Drop : HoldAction.None;
        }

        /// <summary>The left mouse button was released, seen by the controller each frame.</summary>
        public HoldAction MouseRelease(int frame)
        {
            return Held && _pickedByClick && frame != _pickFrame ? HoldAction.Drop : HoldAction.None;
        }

        /// <summary>Enter or Space was pressed.</summary>
        public HoldAction ConfirmKey(int frame)
        {
            return Held && frame != _pickFrame ? HoldAction.Drop : HoldAction.None;
        }
    }
}
