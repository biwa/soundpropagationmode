﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Editing;
using CodeImp.DoomBuilder.Geometry;

namespace CodeImp.DoomBuilder.SoundPropagationMode
{
	public partial class SoundEnvironmentPanel : UserControl
	{
		public SoundEnvironmentPanel()
		{
			InitializeComponent();

			soundenvironments.ImageList = new ImageList();
			soundenvironments.ImageList.Images.Add(global::SoundPropagationMode.Properties.Resources.Status0);
			soundenvironments.ImageList.Images.Add(global::SoundPropagationMode.Properties.Resources.Warning);
		}

		public void AddSoundEnvironment(SoundEnvironment se)
		{
			TreeNode topnode = new TreeNode("Sound environment " + se.ID.ToString());
			TreeNode thingsnode = new TreeNode("Things (" + se.Things.Count.ToString() + ")");
			TreeNode linedefsnode = new TreeNode("Linedefs (" + se.Linedefs.Count.ToString() + ")");
			int notdormant = 0;
			int topindex = 0;

			// Add things
			foreach (Thing t in se.Things)
			{
				TreeNode thingnode = new TreeNode("Thing " + t.Index.ToString());
				thingnode.Tag = t;
				thingsnode.Nodes.Add(thingnode);

				if (!ThingDormant(t))
					notdormant++;
				else
					thingnode.Text += " (dormant)";
			}

			if (notdormant > 1)
			{
				thingsnode.ImageIndex = 1;
				thingsnode.SelectedImageIndex = 1;
				topindex = 1;

				foreach (TreeNode tn in thingsnode.Nodes)
				{
					if (!ThingDormant((Thing)tn.Tag))
					{
						tn.ImageIndex = 1;
						tn.SelectedImageIndex = 1;
					}
				}
					
			}

			// Add linedefs
			foreach (Linedef ld in se.Linedefs)
			{
				TreeNode linedefnode = new TreeNode("Linedef " + ld.Index.ToString());
				linedefnode.Tag = ld;
				linedefsnode.Nodes.Add(linedefnode);
			}

			topnode.Nodes.Add(thingsnode);
			topnode.Nodes.Add(linedefsnode);

			topnode.Tag = se;

			topnode.ImageIndex = topindex;
			topnode.SelectedImageIndex = topindex;

			topnode.Expand();
			
			soundenvironments.Nodes.Add(topnode);
		}

		public void HighlightSoundEnvironment(SoundEnvironment se)
		{
			foreach (TreeNode tn in soundenvironments.Nodes)
			{
				if (se != null && tn.Text == "Sound environment " + se.ID.ToString())
				{
					tn.NodeFont = new Font(soundenvironments.Font.FontFamily, soundenvironments.Font.Size, FontStyle.Bold);
					tn.Text += string.Empty;
				}
				else
				{
					tn.NodeFont = new Font(soundenvironments.Font.FontFamily, soundenvironments.Font.Size);
				}
			}
		}

		private bool ThingDormant(Thing thing)
		{
			var flags = thing.GetFlags();

			if (General.Map.UDMF && flags.ContainsKey("dormant"))
				return flags["dormant"];
			else if (!General.Map.UDMF && flags.ContainsKey("16"))
				return flags["16"];

			return false;
		}

		private void soundenvironments_AfterSelect(object sender, TreeViewEventArgs e)
		{
			TreeNode node = soundenvironments.SelectedNode;

			if (node == null)
				return;

			List<Vector2D> points = new List<Vector2D>();
			RectangleF area = MapSet.CreateEmptyArea();

			if (node.Parent == null)
			{
				if (node.Text.StartsWith("Sound environment"))
				{
					SoundEnvironment se = (SoundEnvironment)node.Tag;

					foreach (Sector s in se.Sectors)
					{
						foreach (Sidedef sd in s.Sidedefs)
						{
							points.Add(sd.Line.Start.Position);
							points.Add(sd.Line.End.Position);
						}
					}
				}
				else
				{
					// Don't zoom if the wrong nodes are selected
					return;
				}
			}
			else
			{
				if (node.Parent.Text.StartsWith("Things"))
				{
					Thing t = (Thing)node.Tag;

					// We don't want to be zoomed too closely, so add somepadding
					points.Add(t.Position - 200);
					points.Add(t.Position + 200);
				}
				else if (node.Parent.Text.StartsWith("Linedefs"))
				{
					Linedef ld = (Linedef)node.Tag;

					points.Add(ld.Start.Position);
					points.Add(ld.End.Position);
				}
				else
				{
					// Don't zoom if the wrong nodes are selected
					return;
				}
			}

			area = MapSet.IncreaseArea(area, points);

			// Add padding
			area.Inflate(100f, 100f);

			// Zoom to area
			ClassicMode editmode = (General.Editing.Mode as ClassicMode);
			editmode.CenterOnArea(area, 0.0f);
		}
	}
}
