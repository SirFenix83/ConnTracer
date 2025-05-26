using System;
using System.Drawing;
using System.Windows.Forms;

namespace ConnTracer.UI
{
    public static class Prompt
    {
        public static string ShowDialog(string text, string caption)
        {
            using Form prompt = new()
            {
                Width = 400,
                Height = 170,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen,
                MinimizeBox = false,
                MaximizeBox = false,
                Font = new Font("Segoe UI", 9),
            };

            Label lblText = new()
            {
                Left = 10,
                Top = 20,
                Text = text,
                Width = 360,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            TextBox textBox = new()
            {
                Left = 10,
                Top = 50,
                Width = 360,
            };

            Button btnOk = new()
            {
                Text = "OK",
                Left = 200,
                Width = 80,
                Top = 90,
                DialogResult = DialogResult.OK,
            };

            Button btnCancel = new()
            {
                Text = "Abbrechen",
                Left = 290,
                Width = 80,
                Top = 90,
                DialogResult = DialogResult.Cancel,
            };

            btnOk.Click += (sender, e) => prompt.Close();
            btnCancel.Click += (sender, e) => prompt.Close();

            prompt.Controls.Add(lblText);
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(btnOk);
            prompt.Controls.Add(btnCancel);

            prompt.AcceptButton = btnOk;
            prompt.CancelButton = btnCancel;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : string.Empty;
        }
    }
}
