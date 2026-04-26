using System.Windows.Forms;
using CashTracker.App.UI;

namespace CashTracker.App.Forms
{
    public sealed partial class KasaForm
    {
        private void ApplyResponsiveLayout()
        {
            if (_rootLayout is null || _leftPanel is null || _rightPanel is null)
                return;

            var stacked = ClientSize.Width < 920;
            var narrow = ClientSize.Width < 1250;

            _rootLayout.SuspendLayout();
            _rootLayout.Padding = narrow ? new Padding(18, 16, 18, 18) : new Padding(34, 30, 34, 26);
            _rootLayout.ColumnStyles.Clear();
            _rootLayout.RowStyles.Clear();

            if (stacked)
            {
                _rootLayout.ColumnCount = 1;
                _rootLayout.RowCount = 2;
                _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
                _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
                _rootLayout.SetColumn(_leftPanel, 0);
                _rootLayout.SetRow(_leftPanel, 0);
                _rootLayout.SetColumn(_rightPanel, 0);
                _rootLayout.SetRow(_rightPanel, 1);
                _leftPanel.Margin = new Padding(0, 0, 0, 10);
                _rightPanel.Margin = new Padding(0);
            }
            else
            {
                _rootLayout.ColumnCount = 2;
                _rootLayout.RowCount = 1;
                _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, narrow ? 54 : 58));
                _rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, narrow ? 46 : 42));
                _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                _rootLayout.SetColumn(_leftPanel, 0);
                _rootLayout.SetRow(_leftPanel, 0);
                _rootLayout.SetColumn(_rightPanel, 1);
                _rootLayout.SetRow(_rightPanel, 0);
                _leftPanel.Margin = new Padding(0, 0, narrow ? 8 : 12, 0);
                _rightPanel.Margin = new Padding(narrow ? 8 : 12, 0, 0, 0);
            }

            _rootLayout.ResumeLayout();
            ApplyGridColumnLayout();
        }
    }
}
