using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace EnableIE
{

    /// <summary>
    /// 带有PlaceHolder的Textbox
    /// </summary>
    /// <creator>marc</creator>
    public class PlaceHolderTextBox : TextBox
    {
        private bool _isPlaceHolder = true;
        private string _placeHolderText;
        /// <summary>
        /// 提示文本
        /// </summary>
        public string PlaceHolderText
        {
            get { return _placeHolderText; }
            set
            {
                _placeHolderText = value;
                SetPlaceholder();
            }
        }

        /// <summary>
        /// 文本
        /// </summary>
        public new string Text
        {
            get
            {
                return _isPlaceHolder ? string.Empty : base.Text;
            }
            set
            {
                base.Text = value;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public PlaceHolderTextBox()
        {
            GotFocus += RemovePlaceHolder;
            LostFocus += SetPlaceholder;
        }

        /// <summary>
        /// 当焦点失去的时候，将清空提示文本
        /// </summary>
        private void SetPlaceholder()
        {
            if (string.IsNullOrEmpty(base.Text))
            {
                base.Text = PlaceHolderText;
                this.ForeColor = Color.Gray;
                this.Font = new Font(this.Font, FontStyle.Regular);
                _isPlaceHolder = true;
            }
        }

        /// <summary>
        /// 当焦点获得的时候，将显示提示文本
        /// </summary>
        private void RemovePlaceHolder()
        {
            if (_isPlaceHolder)
            {
                base.Text = "";
                this.ForeColor = SystemColors.GradientActiveCaption;
                this.Font = new Font(this.Font, FontStyle.Bold);
                _isPlaceHolder = false;
            }
        }

        /// <summary>
        /// 失去焦点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetPlaceholder(object sender, EventArgs e)
        {
            SetPlaceholder();
        }

        /// <summary>
        /// 获得焦点
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemovePlaceHolder(object sender, EventArgs e)
        {
            RemovePlaceHolder();
        }
    }
}
