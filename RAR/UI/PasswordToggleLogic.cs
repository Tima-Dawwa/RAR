using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RAR.UI
{
    public class PasswordToggleLogic
    {
        private readonly Label _passwordLabel;
        private readonly TextBox _passwordTextBox;
        private readonly Button _passwordToggleBtn;

        public PasswordToggleLogic(Label passwordLabel, TextBox passwordTextBox, Button passwordToggleBtn)
        {
            _passwordLabel = passwordLabel;
            _passwordTextBox = passwordTextBox;
            _passwordToggleBtn = passwordToggleBtn;
        }

        public void EncryptionCheckBox_CheckedChanged(bool isChecked)
        {
            _passwordLabel.Visible = isChecked;
            _passwordTextBox.Visible = isChecked;
            _passwordToggleBtn.Visible = isChecked;

            if (isChecked)
            {
                _passwordTextBox.Focus();
            }
            else
            {
                _passwordTextBox.Clear();
                _passwordTextBox.UseSystemPasswordChar = true;
                _passwordToggleBtn.Text = "👁";
            }
        }

        public void PasswordToggleBtn_Click()
        {
            if (_passwordTextBox.UseSystemPasswordChar)
            {
                _passwordTextBox.UseSystemPasswordChar = false;
                _passwordToggleBtn.Text = "🙈";
            }
            else
            {
                _passwordTextBox.UseSystemPasswordChar = true;
                _passwordToggleBtn.Text = "👁";
            }
            _passwordTextBox.Focus();
            _passwordTextBox.SelectionStart = _passwordTextBox.Text.Length;
        }
    }

}
