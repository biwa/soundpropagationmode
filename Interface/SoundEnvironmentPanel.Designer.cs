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
			this.soundenvironments = new BufferedTreeView();
			this.SuspendLayout();
			// 
			// soundenvironments
			// 
			this.soundenvironments.Dock = System.Windows.Forms.DockStyle.Fill;
			this.soundenvironments.Location = new System.Drawing.Point(0, 0);
			this.soundenvironments.Name = "soundenvironments";
			this.soundenvironments.Size = new System.Drawing.Size(214, 353);
			this.soundenvironments.TabIndex = 0;
			// 
			// SoundEnvironmentPanel
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.soundenvironments);
			this.Name = "SoundEnvironmentPanel";
			this.Size = new System.Drawing.Size(214, 353);
			this.ResumeLayout(false);

		}

		#endregion

		private BufferedTreeView soundenvironments;

	}
}
