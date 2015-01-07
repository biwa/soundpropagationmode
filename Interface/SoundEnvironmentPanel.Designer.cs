namespace CodeImp.DoomBuilder.SoundPropagationMode
{
	partial class SoundEnvironmentPanel
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.tooltipscheckbox = new System.Windows.Forms.CheckBox();
			this.soundenvironments = new CodeImp.DoomBuilder.SoundPropagationMode.BufferedTreeView();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.tooltipscheckbox);
			this.groupBox1.Location = new System.Drawing.Point(3, 462);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(208, 47);
			this.groupBox1.TabIndex = 1;
			this.groupBox1.TabStop = false;
			// 
			// tooltipscheckbox
			// 
			this.tooltipscheckbox.AutoSize = true;
			this.tooltipscheckbox.Location = new System.Drawing.Point(11, 19);
			this.tooltipscheckbox.Name = "tooltipscheckbox";
			this.tooltipscheckbox.Size = new System.Drawing.Size(89, 17);
			this.tooltipscheckbox.TabIndex = 0;
			this.tooltipscheckbox.Text = "Show tooltips";
			this.tooltipscheckbox.UseVisualStyleBackColor = true;
			this.tooltipscheckbox.CheckedChanged += new System.EventHandler(this.tooltipscheckbox_CheckedChanged);
			// 
			// soundenvironments
			// 
			this.soundenvironments.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.soundenvironments.Location = new System.Drawing.Point(0, 0);
			this.soundenvironments.Name = "soundenvironments";
			this.soundenvironments.Size = new System.Drawing.Size(214, 456);
			this.soundenvironments.TabIndex = 0;
			this.soundenvironments.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.soundenvironments_AfterSelect);
			// 
			// SoundEnvironmentPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.soundenvironments);
			this.Name = "SoundEnvironmentPanel";
			this.Size = new System.Drawing.Size(214, 512);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private BufferedTreeView soundenvironments;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.CheckBox tooltipscheckbox;


	}
}
