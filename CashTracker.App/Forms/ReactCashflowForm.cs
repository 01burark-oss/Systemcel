using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using CashTracker.App.Services;
using CashTracker.App.UI;
using CashTracker.Core.Services;
using Microsoft.Web.WebView2.WinForms;

namespace CashTracker.App.Forms
{
    internal sealed class ReactCashflowForm : Form
    {
        private readonly GelirGiderLocalApiServer _server;
        private readonly WebView2 _webView;
        private readonly Label _statusLabel;
        private bool _isStarted;

        public ReactCashflowForm(
            IKasaService kasaService,
            IIsletmeService isletmeService,
            IKalemTanimiService kalemTanimiService,
            IUrunHizmetService urunHizmetService,
            IStokService stokService)
        {
            _server = new GelirGiderLocalApiServer(
                kasaService,
                isletmeService,
                kalemTanimiService,
                urunHizmetService,
                stokService);

            Text = "Gelir & Gider Yönetimi";
            BackColor = BrandTheme.AppBackground;
            Font = BrandTheme.CreateFont(10f);
            UiMetrics.ApplyFormDefaults(this);

            _statusLabel = new Label
            {
                Dock = DockStyle.Fill,
                Text = "Gelir/Gider ekranı hazırlanıyor...",
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = BrandTheme.MutedText,
                Font = BrandTheme.CreateFont(11.5f)
            };
            Controls.Add(_statusLabel);

            _webView = new WebView2
            {
                Dock = DockStyle.Fill,
                Visible = false,
                DefaultBackgroundColor = BrandTheme.AppBackground
            };
            Controls.Add(_webView);
        }

        protected override async void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (_isStarted)
                return;

            _isStarted = true;
            await StartReactScreenAsync();
        }

        protected override async void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            await _server.DisposeAsync();
            _webView.Dispose();
        }

        private async Task StartReactScreenAsync()
        {
            try
            {
                await _server.StartAsync();
                await _webView.EnsureCoreWebView2Async();
                _webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                _webView.Source = _server.BaseUri;
                _webView.Visible = true;
                _webView.BringToFront();
                _statusLabel.Visible = false;
            }
            catch (Exception ex)
            {
                _statusLabel.Text =
                    "React Gelir/Gider ekranı açılamadı.\n\n" +
                    ex.Message;
                _statusLabel.ForeColor = Color.FromArgb(173, 59, 56);
                _statusLabel.BringToFront();
            }
        }
    }
}
