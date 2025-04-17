namespace ChessOOP
{
    partial class PromotionForm : Form
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // PromotionForm
            // 
            this.ClientSize = new System.Drawing.Size(400, 100); // Размер формы
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "PromotionForm";
            this.Text = "Выберите фигуру";
            this.ResumeLayout(false);
        }

        #endregion
    }
}