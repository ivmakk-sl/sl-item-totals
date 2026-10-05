using System;
using System.Collections.Generic;
using GameCore.HotUpdate.ReduxUI;

namespace ItemTotals.Web
{
    // Runs the page script (the Vite bundle of src/Web/page) in the root page, which reaches the frame of each listed
    // window itself. Each call has a null callback: the page answers only by message (PageMessages), and a root page
    // with no script posts "no script", after which the schedule sends the script with the data.
    internal static class PageScript
    {
        private static string script;
        private static readonly HashSet<string> warned = new HashSet<string>();

        // The Vite bundle, embedded by ItemTotals.csproj under this name.
        private static string Script()
        {
            if (script != null) return script;
            using (var stream = typeof(PageScript).Assembly.GetManifestResourceStream("ItemTotals.page.js"))
            using (var reader = new System.IO.StreamReader(stream))
                script = reader.ReadToEnd();
            return script;
        }

        private static Vuplex.WebView.IWebView WebView() =>
            ReduxUISystem.Instance?.GetWebUILayer()?.canvasWebViewPrefab?.WebView;

        /// <summary>
        /// Runs one command in the root page, for a surface that the push schedule does not drive. Gives what
        /// happened, for the log behind Verbose; the call has no result callback, as every call of this mod.
        /// </summary>
        internal static string Run(string command)
        {
            var webView = WebView();
            if (webView == null) return "no web view";
            try
            {
                webView.ExecuteJavaScript(command, (Il2CppSystem.Action<string>)null);
                return "sent";
            }
            catch (Exception e)
            {
                if (warned.Add(e.Message)) Plugin.Log.LogWarning("Item Totals send failed: " + e.Message);
                return "failed";
            }
        }

        internal static void Send(PushSchedule.Step step)
        {
            var webView = WebView();
            if (webView == null)
            {
                if (warned.Add("no web view")) Plugin.Log.LogWarning("Item Totals: web view not found");
                return;
            }
            string command;
            switch (step.Kind)
            {
                case PushSchedule.Kind.Apply: command = PageJson.ApplyCommand; break;
                case PushSchedule.Kind.SetData: command = PageJson.SetDataCommand(step.Json); break;
                case PushSchedule.Kind.Full: command = PageJson.SetDataWithScriptCommand(Script(), step.Json); break;
                default: return;
            }
            try
            {
                webView.ExecuteJavaScript(command, (Il2CppSystem.Action<string>)null);
            }
            catch (Exception e)
            {
                // A web view that is being built again or disposed can throw at the call itself.
                if (warned.Add(e.Message)) Plugin.Log.LogWarning("Item Totals send failed: " + e.Message);
                return;
            }
            Plugin.Debug("send: " + step.Kind + (step.Retry ? " retry" : "") + " " + (step.Json == null ? 0 : step.Json.Length) + " chars");
        }
    }
}
