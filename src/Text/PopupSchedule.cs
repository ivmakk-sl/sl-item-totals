namespace ItemTotals.Text
{
    /// <summary>
    /// The sends of the line of the item detail popup.
    ///
    /// The reducer that opens the popup runs before the root page builds the frame of that web page, so the first
    /// send finds no frame. This keeps the line and sends it again on the next ticks until the page reports that it
    /// drew it, or until the window runs out, which is the same shape as the push schedule of the listed windows.
    ///
    /// Game-free on purpose: it takes the clock of the caller and gives the text to send.
    /// </summary>
    public class PopupSchedule
    {
        /// <summary>How long the sends go on after an open, in seconds.</summary>
        public const float Window = 2f;

        /// <summary>The least time between two sends, in seconds.</summary>
        public const float Gap = 0.1f;

        private string line;
        private bool pending;
        // True once the line of the current open went out. A report of the page before that is the answer to the
        // line of an earlier open, so it ends nothing.
        private bool sent;
        private float endAt;
        private float nextAt;

        /// <summary>The popup opened with this line. An empty line takes the line of an earlier open away again.</summary>
        public void Open(string text, float now)
        {
            line = text ?? "";
            pending = true;
            sent = false;
            endAt = now + Window;
            nextAt = now;
        }

        /// <summary>The page drew the line, so no more sends are needed until the next open.</summary>
        public void Shown()
        {
            if (!sent) return;
            pending = false;
        }

        /// <summary>The text to send now, or null when nothing is due.</summary>
        public string Tick(float now)
        {
            if (!pending) return null;
            if (now > endAt)
            {
                pending = false;
                return null;
            }
            if (now < nextAt) return null;
            nextAt = now + Gap;
            sent = true;
            return line;
        }
    }
}
