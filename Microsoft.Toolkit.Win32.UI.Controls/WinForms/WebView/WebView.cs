// ******************************************************************
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THE CODE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
// IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
// TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH
// THE CODE OR THE USE OR OTHER DEALINGS IN THE CODE.
// ******************************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Toolkit.Win32.UI.Controls.Interop.Win32;
using Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT;
using Windows.Web.UI.Interop;
using WebViewControlDeferredPermissionRequest = Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT.WebViewControlDeferredPermissionRequest;
using WebViewControlMoveFocusReason = Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT.WebViewControlMoveFocusReason;
using WebViewControlProcess = Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT.WebViewControlProcess;
using WebViewControlSettings = Microsoft.Toolkit.Win32.UI.Controls.Interop.WinRT.WebViewControlSettings;

namespace Microsoft.Toolkit.Win32.UI.Controls.WinForms
{
    /// <summary>
    /// This class is an implementation of <see cref="IWebView" /> for Windows Forms. This class cannot be inherited.
    /// </summary>
    /// <seealso cref="Control" />
    /// <seealso cref="ISupportInitialize" />
    [Designer(typeof(WebViewDesigner))]
    [DefaultProperty(Constants.ComponentDefaultProperty)]
    [DefaultEvent(Constants.ComponentDefaultEvent)]
    [Docking(DockingBehavior.AutoDock)]
    [SecurityCritical]
    [PermissionSet(SecurityAction.InheritanceDemand, Name = Constants.SecurityPermissionSetName)]
    public sealed partial class WebView : Control, IWebView, ISupportInitialize
    {
        private bool _delayedIsIndexDbEnabled = WebViewDefaults.IsIndexedDBEnabled;
        private bool _delayedIsJavaScriptEnabled = WebViewDefaults.IsJavaScriptEnabled;
        private bool _delayedIsScriptNotifyAllowed = WebViewDefaults.IsScriptNotifyEnabled;
        private bool _delayedPrivateNetworkEnabled = WebViewDefaults.IsPrivateNetworkEnabled;
        private Uri _delayedSource;
        private WebViewControlHost _webViewControl;
        private bool _webViewControlClosed;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebView" /> class.
        /// </summary>
        public WebView()
        {
            Paint += OnWebViewPaint;
        }

        /// <summary>
        /// Gets a value indicating whether [contains full screen element].
        /// </summary>
        /// <value><see langword="true" /> if [contains full screen element]; otherwise, <see langword="false" />.</value>
        /// <inheritdoc />
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool ContainsFullScreenElement
        {
            get
            {
                Verify.IsFalse(IsDisposed);
                Verify.IsNotNull(_webViewControl);
                return _webViewControl?.ContainsFullScreenElement ?? false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether [design mode].
        /// </summary>
        /// <value><see langword="true" /> if [design mode]; otherwise, <see langword="false" />.</value>
        /// <inheritdoc cref="Control.DesignMode" />
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new bool DesignMode => IsInDesignMode();

        /// <summary>
        /// Gets the document title.
        /// </summary>
        /// <value>The document title.</value>
        /// <inheritdoc />
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string DocumentTitle
        {
            get
            {
                Verify.IsFalse(IsDisposed);
                Verify.IsNotNull(_webViewControl);
                return _webViewControl?.DocumentTitle;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="WebView" /> is focused.
        /// </summary>
        /// <value><see langword="true" /> if focused; otherwise, <see langword="false" />.</value>
        /// <inheritdoc />
        /// <remarks>Returns <see langword="true" /> if this or any of its child windows has focus.</remarks>
        public override bool Focused
        {
            get
            {
                if (base.Focused)
                {
                    return true;
                }

                var hwndFocus = UnsafeNativeMethods.GetFocus();
                var ret = hwndFocus != IntPtr.Zero
                       && NativeMethods.IsChild(new HandleRef(this, Handle), new HandleRef(null, hwndFocus));

                return ret;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is indexed database enabled.
        /// </summary>
        /// <value><see langword="true" /> if this instance is indexed database enabled; otherwise, <see langword="false" />.</value>
        /// <inheritdoc />
        [StringResourceCategory(Constants.CategoryBehavior)]
        [DefaultValue(WebViewDefaults.IsIndexedDBEnabled)]
        public bool IsIndexedDBEnabled
        {
            get
            {
                Verify.IsFalse(IsDisposed);
                Verify.Implies(Initializing, !Initialized);
                Verify.Implies(Initialized, WebViewControlInitialized);
                return WebViewControlInitialized
                    ? _webViewControl.Settings.IsIndexedDBEnabled
                    : _delayedIsIndexDbEnabled;
            }

            set
            {
                Verify.IsFalse(IsDisposed);
                _delayedIsIndexDbEnabled = value;
                if (!DesignMode)
                {
                    EnsureInitialized();
                    if (WebViewControlInitialized)
                    {
                        _webViewControl.Settings.IsIndexedDBEnabled = value;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the use of JavaScript is allowed.
        /// </summary>
        /// <value><c>true</c> if the use of JavaScript is allowed; otherwise, <c>false</c>.</value>
        /// <inheritdoc />
        [StringResourceCategory(Constants.CategoryBehavior)]
        [DefaultValue(WebViewDefaults.IsJavaScriptEnabled)]
        public bool IsJavaScriptEnabled
        {
            get
            {
                Verify.IsFalse(IsDisposed);
                Verify.Implies(Initializing, !Initialized);
                Verify.Implies(Initialized, WebViewControlInitialized);
                return WebViewControlInitialized
                    ? _webViewControl.Settings.IsJavaScriptEnabled
                    : _delayedIsJavaScriptEnabled;
            }

            set
            {
                Verify.IsFalse(IsDisposed);
                _delayedIsJavaScriptEnabled = value;
                if (!DesignMode)
                {
                    EnsureInitialized();
                    if (WebViewControlInitialized)
                    {
                        _webViewControl.Settings.IsJavaScriptEnabled = value;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="E:Microsoft.Toolkit.Win32.UI.Controls.WinForms.WebView.ScriptNotify" /> is allowed;
        /// </summary>
        /// <value><c>true</c> if <see cref="E:Microsoft.Toolkit.Win32.UI.Controls.WinForms.WebView.ScriptNotify" /> is allowed; otherwise, <c>false</c>.</value>
        /// <inheritdoc />
        [StringResourceCategory(Constants.CategoryBehavior)]
        [DefaultValue(WebViewDefaults.IsScriptNotifyEnabled)]
        public bool IsScriptNotifyAllowed
        {
            get
            {
                Verify.IsFalse(IsDisposed);
                Verify.Implies(Initializing, !Initialized);
                Verify.Implies(Initialized, WebViewControlInitialized);
                return WebViewControlInitialized
                    ? _webViewControl.Settings.IsScriptNotifyAllowed
                    : _delayedIsScriptNotifyAllowed;
            }

            set
            {
                Verify.IsFalse(IsDisposed);
                _delayedIsScriptNotifyAllowed = value;
                if (!DesignMode)
                {
                    EnsureInitialized();
                    if (WebViewControlInitialized)
                    {
                        _webViewControl.Settings.IsScriptNotifyAllowed = value;
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is private network client server capability enabled.
        /// </summary>
        /// <value><see langword="true" /> if this instance is private network client server capability enabled; otherwise, <see langword="false" />.</value>
        /// <exception cref="InvalidOperationException"></exception>
        [StringResourceCategory(Constants.CategoryBehavior)]
        [DefaultValue(WebViewDefaults.IsPrivateNetworkEnabled)]
        public bool IsPrivateNetworkClientServerCapabilityEnabled
        {
            get
            {
                Verify.IsFalse(IsDisposed);
                Verify.Implies(Initializing, !Initialized);
                Verify.Implies(Initialized, WebViewControlInitialized);
                return WebViewControlInitialized
                    ? _webViewControl.Process.IsPrivateNetworkClientServerCapabilityEnabled
                    : _delayedPrivateNetworkEnabled;
            }

            set
            {
                Verify.IsFalse(IsDisposed);
                _delayedPrivateNetworkEnabled = value;
                if (!DesignMode)
                {
                    EnsureInitialized();
                    if (WebViewControlInitialized
                        && _webViewControl.Process.IsPrivateNetworkClientServerCapabilityEnabled != _delayedPrivateNetworkEnabled)
                    {
                        throw new InvalidOperationException(DesignerUI.InvalidOp_Immutable);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a <see cref="WebViewControlProcess" /> object that the control is hosted in.
        /// </summary>
        /// <value>The <see cref="WebViewControlProcess" /> object that the control is hosted in.</value>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public WebViewControlProcess Process { get; private set; }

        /// <summary>
        /// Gets a <see cref="WebViewControlSettings" /> object that contains properties to enable or disable <see cref="WebView" /> features.
        /// </summary>
        /// <value>A <see cref="WebViewControlSettings" /> object that contains properties to enable or disable <see cref="WebView" /> features.</value>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public WebViewControlSettings Settings
        {
            get
            {
                Verify.IsFalse(IsDisposed);
                Verify.Implies(Initializing, !Initialized);
                Verify.Implies(Initialized, WebViewControlInitialized);
                return _webViewControl?.Settings;
            }
        }

        /// <summary>
        /// Gets or sets the Uniform Resource Identifier (URI) source of the HTML content to display in the <see cref="WebView" />.
        /// </summary>
        /// <value>The Uniform Resource Identifier (URI) source of the HTML content to display in the <see cref="WebView" />.</value>
        [Bindable(true)]
        [StringResourceCategory(Constants.CategoryBehavior)]
        [StringResourceDescription(Constants.DescriptionSource)]
        [TypeConverter(typeof(WebBrowserUriTypeConverter))]
        [DefaultValue((string)null)]
        public Uri Source
        {
            get
            {
                Verify.IsFalse(IsDisposed);
                Verify.Implies(Initializing, !Initialized);
                Verify.Implies(Initialized, WebViewControlInitialized);
                return WebViewControlInitialized
                    ? _webViewControl.Source
                    : _delayedSource;
            }

            set
            {
                Verify.IsFalse(IsDisposed);
                _delayedSource = value;
                if (!DesignMode)
                {
                    EnsureInitialized();
                    if (WebViewControlInitialized)
                    {
                        if (Initializing && value != null)
                        {
                            // During initialization if there is no Source set a navigation to "about:blank" will occur
                            _webViewControl.Source = value;
                        }
                        else if (Initialized)
                        {
                            // After the control is initialized send all values, regardless of if they are null
                            _webViewControl.Source = value;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the version of EDGEHTML.DLL used by the control.
        /// </summary>
        /// <value>The version of EDGEHTML.DLL used by the control.</value>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Version Version => _webViewControl?.Version;

        /// <summary>
        /// Closes this control.
        /// </summary>
        public void Close()
        {
            var webViewControlAlreadyClosed = _webViewControlClosed;
            _webViewControlClosed = true;

            // Unsubscribe all events:
            UnsubscribeEvents();

            if (!webViewControlAlreadyClosed)
            {
                _webViewControl?.Close();
                _webViewControl?.Dispose();
            }

            _webViewControl = null;
            Process = null;
        }

        /// <summary>
        /// Gets the deferred permission request with the specified Id.
        /// </summary>
        /// <param name="id">The Id of the deferred permission request.</param>
        /// <returns>A <see cref="WebViewControlDeferredPermissionRequest" /> object of the specified <paramref name="id" />.</returns>
        public WebViewControlDeferredPermissionRequest GetDeferredPermissionRequestById(uint id) => _webViewControl?.GetDeferredPermissionRequestById(id);

        /// <summary>
        /// Invokes the script.
        /// </summary>
        /// <param name="scriptName">Name of the script.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="InvalidOperationException">When the underlying &lt;
        /// <see cref="WebViewControl" /> is not yet initialized.</exception>
        /// <inheritdoc />

        public object InvokeScript(string scriptName) => _webViewControl?.InvokeScript(scriptName);

        /// <summary>
        /// Invokes the script.
        /// </summary>
        /// <param name="scriptName">Name of the script.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="InvalidOperationException">When the underlying &lt;&amp;&lt;
        /// <see cref="WebViewControl" /> is not yet initialized.</exception>
        /// <inheritdoc />

        public object InvokeScript(string scriptName, params string[] arguments) => _webViewControl?.InvokeScript(scriptName, arguments);

        /// <summary>
        /// Invokes the script.
        /// </summary>
        /// <param name="scriptName">Name of the script.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="InvalidOperationException">When the underlying <see cref="WebViewControl" /> is not yet initialized.</exception>
        public object InvokeScript(string scriptName, IEnumerable<string> arguments) => _webViewControl?.InvokeScript(scriptName, arguments);

        /// <summary>
        /// Invokes the script asynchronous.
        /// </summary>
        /// <param name="scriptName">Name of the script.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        /// <exception cref="InvalidOperationException">When the underlying <see cref="WebViewControl" /> is not yet initialized.</exception>
        public Task<string> InvokeScriptAsync(string scriptName) => _webViewControl?.InvokeScriptAsync(scriptName);

        /// <summary>
        /// Invokes the script asynchronous.
        /// </summary>
        /// <param name="scriptName">Name of the script.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        /// <exception cref="InvalidOperationException">When the underlying <see cref="WebViewControl" /> is not yet initialized.</exception>
        public Task<string> InvokeScriptAsync(string scriptName, params string[] arguments) =>
            _webViewControl?.InvokeScriptAsync(scriptName, arguments);

        /// <summary>
        /// Invokes the script asynchronous.
        /// </summary>
        /// <param name="scriptName">Name of the script.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        /// <exception cref="InvalidOperationException">When the underlying <see cref="WebViewControl" /> is not yet initialized.</exception>
        public Task<string> InvokeScriptAsync(string scriptName, IEnumerable<string> arguments)
        => _webViewControl?.InvokeScriptAsync(scriptName, arguments);

        /// <summary>
        /// Moves the focus.
        /// </summary>
        /// <param name="reason">The reason.</param>
        public void MoveFocus(WebViewControlMoveFocusReason reason) => _webViewControl?.MoveFocus(reason);

        /// <summary>
        /// Loads the document at the location indicated by the specified <see cref="Source" /> into the <see cref="WebView" /> control, replacing the previous document.
        /// </summary>
        /// <param name="source">A <see cref="Source" /> representing the URL of the document to load.</param>
        /// <exception cref="ArgumentException">The provided source is a relative URI.</exception>
        public void Navigate(Uri source) => _webViewControl?.Navigate(source);

        /// <summary>
        /// Navigates the specified source.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <exception cref="UriFormatException">In the .NET for Windows Store apps or the Portable Class Library, catch the base class exception, <see cref="T:System.FormatException" />, instead.
        /// <paramref name="source" /> is empty.-or- The scheme specified in <paramref name="source" /> is not correctly formed. See <see cref="M:System.Uri.CheckSchemeName(System.String)" />.-or-
        /// <paramref name="source" /> contains too many slashes.-or- The password specified in <paramref name="source" /> is not valid.-or- The host name specified in <paramref name="source" /> is not valid.-or- The file name specified in <paramref name="source" /> is not valid. -or- The user name specified in <paramref name="source" /> is not valid.-or- The host or authority name specified in <paramref name="source" /> cannot be terminated by backslashes.-or- The port number specified in <paramref name="source" /> is not valid or cannot be parsed.-or- The length of <paramref name="source" /> exceeds 65519 characters.-or- The length of the scheme specified in <paramref name="source" /> exceeds 1023 characters.-or- There is an invalid character sequence in <paramref name="source" />.-or- The MS-DOS path specified in <paramref name="source" /> must start with c:\\.</exception>
        public void Navigate(string source)
        {
            Verify.IsFalse(IsDisposed);
            Verify.IsNotNull(_webViewControl);
            _webViewControl?.Navigate(source);
        }

        /// <summary>
        /// Loads the specified HTML content as a new document.
        /// </summary>
        /// <param name="text">The HTML content to display in the control.</param>
        public void NavigateToString(string text) => _webViewControl?.NavigateToString(text);

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="T:System.Windows.Forms.Control" /> and its child controls and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><see langword="true" /> to release both managed and unmanaged resources; <see langword="false" /> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    Close();
                    _webViewControl?.Dispose();
                    _webViewControl = null;
                    Process = null;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Handles the <see cref="E:ClientSizeChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            UpdateBounds();
        }

        /// <summary>
        /// Handles the <see cref="E:DockChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected override void OnDockChanged(EventArgs e)
        {
            base.OnDockChanged(e);
            UpdateBounds();
        }

        /// <summary>
        /// Handles the <see cref="E:LocationChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected override void OnLocationChanged(EventArgs e)
        {
            base.OnLocationChanged(e);
            UpdateBounds();
        }

        /// <summary>
        /// Handles the <see cref="E:SizeChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateBounds();
        }

        private bool IsInDesignMode()
        {
            var wpfDesignMode = LicenseManager.UsageMode == LicenseUsageMode.Designtime;
            var formsDesignMode = System.Diagnostics.Process.GetCurrentProcess().ProcessName == "devenv";
            return wpfDesignMode || formsDesignMode;
        }

        private void OnWebViewPaint(object sender, PaintEventArgs e)
        {
            if (!DesignMode)
            {
                return;
            }

            using (var g = e.Graphics)
            {
                using (var hb = new HatchBrush(HatchStyle.ZigZag, Color.Black, BackColor))
                {
                    g.FillRectangle(hb, ClientRectangle);
                }
            }
        }

        private new void UpdateBounds()
        {
            try
            {
#if DEBUG_LAYOUT
                Debug.WriteLine("RECT:   X: {0}, Y: {1}, Height: {2}, Width: {3}", ClientRectangle.X, ClientRectangle.Y, ClientRectangle.Height, ClientRectangle.Width);
#endif

                _webViewControl?.UpdateBounds(ClientRectangle);
            }
            finally
            {
                base.UpdateBounds();
            }
        }
    }
}