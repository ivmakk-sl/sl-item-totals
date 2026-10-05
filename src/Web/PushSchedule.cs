using System;
using System.Collections.Generic;

namespace ItemTotals.Web
{
    // Decides on each frame whether the plugin sends to the page script, and what. No game or BepInEx type here: the
    // caller gives the real time. A send has no result callback; the page answers only by message.
    //
    // - A refresh of a listed window sets a request. The next tick sends once, so many refreshes in one frame make
    //   one send. It sends the data when the mod holds a version that the page has not confirmed, else it only
    //   applies the line, which is what installs it into a frame that just opened.
    // - An item change sets a data request. It sends only while a listed window is open, which the page tells with
    //   the count of frames it installed into.
    // - A "no script" message (a new root page, or one the game built again) makes the next tick send the script with
    //   the data. One full send at a time: another "no script" in the next few real seconds is an answer of the same
    //   round and is dropped; a browser crash can drop the script, so after that time it can go again.
    // - A send that found no frame makes a retry one real second later, while the request is less than 3 real seconds
    //   old. WebUILayer queues a message for a page that has not loaded, so the first send after a window open can
    //   come before the page is there, and the retry is the expected path. A retry carries the data only when the
    //   page has not confirmed that version.
    // - A build that throws (for example while the window closes) makes a retry one real second later, and rethrows.
    public sealed class PushSchedule
    {
        public enum Kind { None, Apply, SetData, Full }

        public readonly struct Step
        {
            public readonly Kind Kind;
            // The data JSON of the send; null when the send only applies the line.
            public readonly string Json;
            // True for a send of the retry, false for the first send of a request.
            public readonly bool Retry;

            public Step(Kind kind, string json, bool retry)
            {
                Kind = kind; Json = json; Retry = retry;
            }
        }

        // The message of the root page when it has no page script of this mod.
        public const string NoScript = "no script";

        // The message of each pass: the count of frames the script installed the line into, and the data version the
        // page holds ("frames 2 version 7", or "frames 0" when no listed window is open).
        public const string Frames = "frames ";

        private const float RetrySeconds = 1f;
        private const float RequestSeconds = 3f;
        private const float FullSendWaitSeconds = 5f;

        private static readonly Step Nothing = new Step(Kind.None, null, false);

        private readonly HashSet<string> warned = new HashSet<string>();

        private bool requested;
        private float requestAt = float.NegativeInfinity;
        private float retryAt = float.NaN;
        private bool dataRequested;
        private int confirmedVersion;
        // True until a pass answers that it found no frame of a listed window.
        private bool windowOpen = true;
        private bool fullPending;
        private float fullSentAt = float.NegativeInfinity;

        /// <summary>A listed window refreshed, so the next tick applies the line to its frame.</summary>
        public void RequestPage(string page, float now)
        {
            requested = true;
            requestAt = now;
            retryAt = float.NaN;
            // The window of that page is open, whatever the last pass found.
            windowOpen = true;
        }

        /// <summary>The item totals changed, so the next tick sends them while a listed window is open.</summary>
        public void RequestData(int version, float now)
        {
            dataRequested = true;
            requestAt = now;
        }

        /// <summary>True when the next tick can send. The caller then builds the data, else it returns at once.</summary>
        public bool Pending(float now) =>
            fullPending
            || requested
            || (dataRequested && windowOpen)
            || (!float.IsNaN(retryAt) && now >= retryAt);

        public Step Tick(float now, int version, Func<string> build)
        {
            if (fullPending)
            {
                string data = Build(now, build);
                fullPending = false;
                fullSentAt = now;
                return Sent(Kind.Full, data, false);
            }

            bool retry = false;
            if (!requested && !(dataRequested && windowOpen))
            {
                if (float.IsNaN(retryAt) || now < retryAt) return Nothing;
                retryAt = float.NaN;
                if (now - requestAt >= RequestSeconds) return Nothing;
                retry = true;
            }

            if (version == confirmedVersion && !dataRequested)
            {
                // The page already holds this data: the send only applies the line, which installs it into a frame
                // that just opened. A retry is the same: it only looks for the frame again.
                requested = false;
                return new Step(Kind.Apply, null, retry);
            }
            string json = Build(now, build);
            return Sent(Kind.SetData, json, retry);
        }

        /// <summary>A message of the page script, with the prefix of the mod taken off.</summary>
        public void OnMessage(string text, float now)
        {
            if (text == NoScript)
            {
                if (now - fullSentAt < FullSendWaitSeconds) return;
                confirmedVersion = 0;
                fullPending = true;
                return;
            }
            if (text != null && text.StartsWith(Frames, StringComparison.Ordinal))
            {
                int frames = ReadNumber(text, Frames);
                windowOpen = frames > 0;
                int version = ReadNumber(text, " version ");
                if (version > 0) confirmedVersion = version;
                // A later refresh makes its own send, so only the answer of the last send sets a retry.
                if (frames == 0 && !requested && !dataRequested) retryAt = now + RetrySeconds;
            }
        }

        /// <summary>True the first time for a status text, so the log holds one warning for each distinct one.</summary>
        public bool ShouldWarn(string text)
        {
            if (text == null) return false;
            if (text == NoScript || text.StartsWith(Frames, StringComparison.Ordinal)) return false;
            return warned.Add(text);
        }

        private static int ReadNumber(string text, string after)
        {
            int at = text.IndexOf(after, StringComparison.Ordinal);
            if (at < 0) return 0;
            at += after.Length;
            int value = 0;
            bool any = false;
            while (at < text.Length && text[at] >= '0' && text[at] <= '9')
            {
                value = value * 10 + (text[at] - '0');
                any = true;
                at++;
            }
            return any ? value : 0;
        }

        private Step Sent(Kind kind, string json, bool retry)
        {
            requested = false;
            dataRequested = false;
            return new Step(kind, json, retry);
        }

        private string Build(float now, Func<string> build)
        {
            try { return build(); }
            catch
            {
                requested = false;
                dataRequested = false;
                retryAt = now + RetrySeconds;
                throw;
            }
        }
    }
}
