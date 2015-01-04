using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CodeImp.DoomBuilder;

namespace CodeImp.DoomBuilder.SoundPropagationMode
{
	public partial class MenusForm : Form
	{
		public ToolStripButton ColorConfiguration { get { return colorconfiguration; } }

		public MenusForm()
		{
			InitializeComponent();
		}

		// This invokes an action from control event
		private void InvokeTaggedAction(object sender, EventArgs e)
		{
			General.Interface.InvokeTaggedAction(sender, e);
		}
	}
}
