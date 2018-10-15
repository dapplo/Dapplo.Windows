namespace Dapplo.Windows.Example.FormsExample
{
    partial class FormExtendsDpiAwareForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormExtendsDpiAwareForm));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.somethingMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.something2MenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.button1 = new System.Windows.Forms.Button();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.halloToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(16, 16);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.somethingMenuItem,
            this.something2MenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Padding = new System.Windows.Forms.Padding(8, 2, 0, 2);
            this.menuStrip1.Size = new System.Drawing.Size(379, 28);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // somethingMenuItem
            // 
            this.somethingMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("somethingMenuItem.Image")));
            this.somethingMenuItem.Name = "somethingMenuItem";
            this.somethingMenuItem.Size = new System.Drawing.Size(113, 24);
            this.somethingMenuItem.Text = "Something";
            // 
            // something2MenuItem
            // 
            this.something2MenuItem.Image = ((System.Drawing.Image)(resources.GetObject("something2MenuItem.Image")));
            this.something2MenuItem.Name = "something2MenuItem";
            this.something2MenuItem.Size = new System.Drawing.Size(121, 24);
            this.something2MenuItem.Text = "Something2";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(163, 163);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "button1";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.Button1_Click);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.halloToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(180, 58);
            // 
            // halloToolStripMenuItem
            // 
            this.halloToolStripMenuItem.Image = ((System.Drawing.Image)(resources.GetObject("halloToolStripMenuItem.Image")));
            this.halloToolStripMenuItem.Name = "halloToolStripMenuItem";
            this.halloToolStripMenuItem.Size = new System.Drawing.Size(179, 26);
            this.halloToolStripMenuItem.Text = "Hallo";
            // 
            // FormExtendsDpiAwareForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(379, 322);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "FormExtendsDpiAwareForm";
            this.Text = "FormExtendsDpiAwareForm";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem somethingMenuItem;
        private System.Windows.Forms.ToolStripMenuItem something2MenuItem;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem halloToolStripMenuItem;
    }
}

