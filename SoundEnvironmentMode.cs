﻿
#region ================== Copyright (c) 2007 Pascal vd Heiden, 2014 Boris Iwanski

/*
 * Copyright (c) 2007 Pascal vd Heiden, www.codeimp.com
 * Copyright (c) 2014 Boris Iwanski
 * This program is released under GNU General Public License
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 */

#endregion

#region ================== Namespaces

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Linq;
using System.ComponentModel;
using CodeImp.DoomBuilder.Windows;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Geometry;
using CodeImp.DoomBuilder.Editing;
using System.Drawing;
using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.Types;
using CodeImp.DoomBuilder.BuilderModes;
using CodeImp.DoomBuilder.BuilderModes.Interface;
using CodeImp.DoomBuilder.Controls;

#endregion

namespace CodeImp.DoomBuilder.SoundPropagationMode
{
	[EditMode(DisplayName = "Sound Environment Mode",
			  SwitchAction = "soundenvironmentmode",		// Action name used to switch to this mode
			  ButtonImage = "ZDoomSoundEnvironment.png",	// Image resource name for the button
			  ButtonOrder = int.MinValue + 501,	// Position of the button (lower is more to the left)
			  ButtonGroup = "000_editing",
			  UseByDefault = true,
			  SafeStartMode = false,
			  Volatile = false)]

	public class SoundEnvironmentMode : ClassicMode
	{
		#region ================== Constants

		private const float LINE_THICKNESS = 1.0f;

		#endregion

		#region ================== Structs

		#endregion

		#region ================== Variables

		// Highlighted item
		protected Sector highlighted;
		private SoundEnvironment highlightedsoundenvironment;

		// Interface
		protected bool editpressed;
		private SoundEnvironmentPanel panel;
		private Docker docker;

		private BackgroundWorker worker;

		#endregion


		#region ================== Properties

		public override object HighlightedObject { get { return highlighted; } }

		#endregion

		#region ================== Constructor / Disposer

		// Constructor
		public SoundEnvironmentMode()
		{
		}

		// Disposer
		public override void Dispose()
		{
			// Not already disposed?
			if (!isdisposed)
			{
				// Dispose base
				base.Dispose();
			}
		}

		#endregion

		#region ================== Methods



		// This makes a CRC for the selection
		public int CreateSelectionCRC()
		{
			CRC crc = new CRC();
			ICollection<Sector> orderedselection = General.Map.Map.GetSelectedSectors(true);
			crc.Add(orderedselection.Count);
			foreach (Sector s in orderedselection)
			{
				crc.Add(s.FixedIndex);
			}
			return (int)(crc.Value & 0xFFFFFFFF);
		}

		// This updates the overlay
		/*
		private void UpdateOverlay()
		{
			if(renderer.StartOverlay(true))
			{
				foreach (Sector sector in General.Map.Map.Sectors)
					renderer.RenderHighlight(sector.FlatVertices, BuilderPlug.Me.NoSoundColor.WithAlpha(128).ToInt());

				if (BuilderPlug.Me.UseHighlight)
				{
					renderer.RenderHighlight(overlayGeometryLevel1, BuilderPlug.Me.Level1Color.WithAlpha(128).ToInt());
					renderer.RenderHighlight(overlayGeometryLevel2, BuilderPlug.Me.Level2Color.WithAlpha(128).ToInt());
				}

				if (BuilderPlug.Me.UseHighlight && highlighted != null)
				{
					renderer.RenderHighlight(highlighted.FlatVertices, BuilderPlug.Me.HighlightColor.WithAlpha(128).ToInt());
				}

				renderer.Finish();
			}
		}
		*/

		// Support function for joining and merging sectors
		private void JoinMergeSectors(bool removelines)
		{
			// Remove lines in betwen joining sectors?
			if (removelines)
			{
				// Go for all selected linedefs
				List<Linedef> selectedlines = new List<Linedef>(General.Map.Map.GetSelectedLinedefs(true));
				foreach (Linedef ld in selectedlines)
				{
					// Front and back side?
					if ((ld.Front != null) && (ld.Back != null))
					{
						// Both a selected sector, but not the same?
						if (ld.Front.Sector.Selected && ld.Back.Sector.Selected &&
						   (ld.Front.Sector != ld.Back.Sector))
						{
							// Remove this line
							ld.Dispose();
						}
					}
				}
			}

			// Find the first sector that is not disposed
			List<Sector> orderedselection = new List<Sector>(General.Map.Map.GetSelectedSectors(true));
			Sector first = null;
			foreach (Sector s in orderedselection)
				if (!s.IsDisposed) { first = s; break; }

			// Join all selected sectors with the first
			for (int i = 0; i < orderedselection.Count; i++)
				if ((orderedselection[i] != first) && !orderedselection[i].IsDisposed)
					orderedselection[i].Join(first);

			// Clear selection
			General.Map.Map.ClearAllSelected();

			// Update
			General.Map.Map.Update();
		}

		// This highlights a new item
		protected void Highlight(Sector s)
		{
			// Update display
			/*
			if(renderer.StartPlotter(false))
			{
				// Undraw previous highlight
				if((highlighted != null) && !highlighted.IsDisposed)
					renderer.PlotSector(highlighted);
				
				// Set new highlight
				highlighted = s;

				UpdateSoundPropagation();

				// Render highlighted item
				if((highlighted != null) && !highlighted.IsDisposed)
					renderer.PlotSector(highlighted, General.Colors.Highlight);
				
				// Done
				renderer.Finish();
			}

			// updateOverlaySurfaces();
			// UpdateOverlay();
			renderer.Present();
			*/

			// Set new highlight
			highlighted = s;
			highlightedsoundenvironment = null;

			if (highlighted != null)
			{
				foreach (SoundEnvironment se in BuilderPlug.Me.SoundEnvironments)
				{
					if (se.Sectors.Contains(highlighted))
					{
						highlightedsoundenvironment = se;
						break;
					}
				}
			}
			
			if (highlightedsoundenvironment != null)
			{
				panel.HighlightSoundEnvironment(highlightedsoundenvironment);
			}
			else
			{
				panel.HighlightSoundEnvironment(null);
			}

			// Show highlight info
			/*
			if ((highlighted != null) && !highlighted.IsDisposed)
				General.Interface.ShowSectorInfo(highlighted);
			else
				General.Interface.HideInfo();
			*/

			// General.Interface.RedrawDisplay();
		}

		// This selectes or deselects a sector
		/*
		protected void SelectSector(Sector s, bool selectstate, bool update)
		{
			bool selectionchanged = false;

			if(!s.IsDisposed)
			{
				// Select the sector?
				if(selectstate && !s.Selected)
				{
					s.Selected = true;
					selectionchanged = true;
				}
				// Deselect the sector?
				else if(!selectstate && s.Selected)
				{
					s.Selected = false;
					selectionchanged = true;
				}

				// Selection changed?
				if(selectionchanged)
				{
					// Make update lines selection
					foreach(Sidedef sd in s.Sidedefs)
					{
						bool front, back;
						if(sd.Line.Front != null) front = sd.Line.Front.Sector.Selected; else front = false;
						if(sd.Line.Back != null) back = sd.Line.Back.Sector.Selected; else back = false;
						sd.Line.Selected = front | back;
					}
				}

				if(update)
				{
					UpdateOverlay();
					renderer.Present();
				}
			}
		}
		*/

		#endregion

		#region ================== Events

		public override void OnHelp()
		{
			General.ShowHelp("e_sectors.html");
		}

		// Cancel mode
		public override void OnCancel()
		{
			base.OnCancel();

			// Return to this mode
			General.Editing.ChangeMode(new SectorsMode());
		}

		// Mode engages
		public override void OnEngage()
		{
			base.OnEngage();

			General.Interface.AddButton(BuilderPlug.Me.MenusForm.ColorConfiguration);

			panel = new SoundEnvironmentPanel();
			docker = new Docker("soundenvironments", "Sound Environments", panel);
			General.Interface.AddDocker(docker);
			General.Interface.SelectDocker(docker);

			worker = new BackgroundWorker();
			worker.WorkerReportsProgress = true;
			worker.WorkerSupportsCancellation = true;

			worker.DoWork += new DoWorkEventHandler(BuilderPlug.Me.UpdateSoundEnvironments);
			worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
			worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);

			// Only update if map has changed or the sound environments were never updated at all (i.e. first time engaging this mode)
			if (General.Map.IsChanged || !BuilderPlug.Me.SoundEnvironmentIsUpdated)
			{
				General.Interface.DisplayStatus(StatusType.Busy, "Updating sound environments");
				worker.RunWorkerAsync();
			}
			else
			{
				foreach (SoundEnvironment se in BuilderPlug.Me.SoundEnvironments)
					panel.AddSoundEnvironment(se);
			}

			CustomPresentation presentation = new CustomPresentation();
			// presentation.AddLayer(new PresentLayer(RendererLayer.Background, BlendingMode.Mask, General.Settings.BackgroundAlpha));

			presentation.AddLayer(new PresentLayer(RendererLayer.Background, BlendingMode.Mask, General.Settings.BackgroundAlpha));
			// presentation.AddLayer(new PresentLayer(RendererLayer.Surface, BlendingMode.Mask));
			presentation.AddLayer(new PresentLayer(RendererLayer.Grid, BlendingMode.Mask));
			presentation.AddLayer(new PresentLayer(RendererLayer.Overlay, BlendingMode.Alpha, 1f, true));
			presentation.AddLayer(new PresentLayer(RendererLayer.Things, BlendingMode.Alpha, 1.0f));
			presentation.AddLayer(new PresentLayer(RendererLayer.Geometry, BlendingMode.Alpha, 1f, true));

			renderer.SetPresentation(presentation);

			// renderer.SetPresentation(Presentation.Standard);
			renderer.SetPresentation(presentation);
			Presentation p = Presentation.Standard;

			// Convert geometry selection to sectors only
			General.Map.Map.ConvertSelection(SelectionType.Sectors);
		}

		void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			General.Interface.DisplayStatus(StatusType.Ready, "Finished updating sound environments");
		}

		void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			SoundEnvironment se = e.UserState as SoundEnvironment;

			General.Interface.DisplayStatus(StatusType.Busy, "Updating sound environments (" + e.ProgressPercentage.ToString() + "%)");

			panel.AddSoundEnvironment(se);

			General.Interface.RedrawDisplay();
		}

		// Mode disengages
		public override void OnDisengage()
		{
			base.OnDisengage();

			worker.CancelAsync();

			General.Interface.RemoveButton(BuilderPlug.Me.MenusForm.ColorConfiguration);
			General.Interface.RemoveDocker(docker);

			// Keep only sectors selected
			General.Map.Map.ClearSelectedLinedefs();

			// Going to EditSelectionMode?
			if (General.Editing.NewMode is EditSelectionMode)
			{
				// Not pasting anything?
				EditSelectionMode editmode = (General.Editing.NewMode as EditSelectionMode);
				if (!editmode.Pasting)
				{
					// No selection made? But we have a highlight!
					if ((General.Map.Map.GetSelectedSectors(true).Count == 0) && (highlighted != null))
					{
						// Make the highlight the selection
						//SelectSector(highlighted, true, false);
					}
				}
			}

			// Hide highlight info
			General.Interface.HideInfo();
		}

		// This redraws the display
		public override void OnRedrawDisplay()
		{
			// Render lines and vertices
			if (renderer.StartPlotter(true))
			{
				// Plot lines by hand, so that no coloring (line specials, 3D floors etc.) distracts from
				// the sound environments. Also don't draw the line's normal. They are not needed here anyway
				// and can make it harder to see the sound environment colors
				foreach (Linedef ld in General.Map.Map.Linedefs)
				{
					PixelColor c;
					
					if(ld.IsFlagSet(General.Map.Config.ImpassableFlag))
						c = General.Colors.Linedefs;
					else
						c = General.Colors.Linedefs.WithAlpha(General.Settings.DoubleSidedAlphaByte);

					renderer.PlotLine(ld.Start.Position, ld.End.Position, c);
				}

				// Since there will usually be way less blocking linedefs than total linedefs, it's presumably
				// faster to draw them on their own instead of checking if each linedef is in BlockingLinedefs
				lock (BuilderPlug.Me.BlockingLinedefs)
				{
					foreach (Linedef ld in BuilderPlug.Me.BlockingLinedefs)
					{
						renderer.PlotLine(ld.Start.Position, ld.End.Position, BuilderPlug.Me.BlockSoundColor);
					}
				}

				renderer.PlotVerticesSet(General.Map.Map.Vertices);

				renderer.Finish();
			}

			// Render things
			if (renderer.StartThings(true))
			{
				renderer.RenderThingSet(General.Map.ThingsFilter.HiddenThings, Presentation.THINGS_BACK_ALPHA);
				renderer.RenderThingSet(General.Map.ThingsFilter.VisibleThings, Presentation.THINGS_HIDDEN_ALPHA);

				lock (BuilderPlug.Me.SoundEnvironments)
				{
					foreach (SoundEnvironment se in BuilderPlug.Me.SoundEnvironments)
					{
						if (se.Things.Count > 0)
							renderer.RenderThingSet(se.Things, 1.0f);
					}
				}

				renderer.Finish();
			}

			// Render selection
			/*
			if(renderer.StartOverlay(true))
			{
				// if((highlighted != null) && !highlighted.IsDisposed) BuilderPlug.Me.RenderReverseAssociations(renderer, highlightasso);
				if(selecting) RenderMultiSelection();

				renderer.Finish();
			}
			*/

			// Render overlay geometry (sectors)
			if (BuilderPlug.Me.OverlayGeometry != null)
			{
				lock (BuilderPlug.Me.OverlayGeometry)
				{
					if (BuilderPlug.Me.OverlayGeometry.Length > 0 && renderer.StartOverlay(true))
					{
						renderer.RenderGeometry(BuilderPlug.Me.OverlayGeometry, General.Map.Data.WhiteTexture, true);

						renderer.Finish();
					}
				}
			}

			renderer.Present();
		}

		private void RenderColoredSector(Sector sector, PixelColor color)
		{
			RenderColoredSector(sector.FlatVertices, color);
		}

		private void RenderColoredSector(FlatVertex[] flatvertices, PixelColor color)
		{
			FlatVertex[] fv = new FlatVertex[flatvertices.Length];
			flatvertices.CopyTo(fv, 0);
			for (int i = 0; i < fv.Length; i++) fv[i].c = color.ToInt();
			renderer.RenderGeometry(fv, General.Map.Data.WhiteTexture, true);
		}

		// Selection
		/*
		protected override void OnSelectBegin()
		{
			// Item highlighted?
			if((highlighted != null) && !highlighted.IsDisposed)
			{
				// Flip selection
				SelectSector(highlighted, !highlighted.Selected, true);

				// Update display
				if(renderer.StartPlotter(false))
				{
					// Redraw highlight to show selection
					renderer.PlotSector(highlighted);
					renderer.Finish();
					renderer.Present();
				}
			}
			else
			{
				// Start making a selection
				StartMultiSelection();
			}

			base.OnSelectBegin();
		}

		// End selection
		protected override void OnSelectEnd()
		{
			// Not stopping from multiselection?
			if(!selecting)
			{
				// Item highlighted?
				if((highlighted != null) && !highlighted.IsDisposed)
				{
					// Update display
					if(renderer.StartPlotter(false))
					{
						// Render highlighted item
						renderer.PlotSector(highlighted, General.Colors.Highlight);
						renderer.Finish();
						renderer.Present();
					}

					// Update overlay
					updateOverlaySurfaces();
					UpdateOverlay();
					renderer.Present();
				}
			}

			UpdateSoundPropagation();
			updateOverlaySurfaces();

			General.Interface.RedrawDisplay();

			base.OnSelectEnd();
		}
		*/

		// Mouse moves
		public override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			// Not holding any buttons?
			if (e.Button == MouseButtons.None)
			{
				General.Interface.SetCursor(Cursors.Default);

				// Find the nearest linedef within highlight range
				Linedef l = General.Map.Map.NearestLinedef(mousemappos);
				if (l != null)
				{
					// Check on which side of the linedef the mouse is
					float side = l.SideOfLine(mousemappos);
					if (side > 0)
					{
						// Is there a sidedef here?
						if (l.Back != null)
						{
							// Highlight if not the same
							if (l.Back.Sector != highlighted) Highlight(l.Back.Sector);
						}
						else
						{
							// Highlight nothing
							Highlight(null);
						}
					}
					else
					{
						// Is there a sidedef here?
						if (l.Front != null)
						{
							// Highlight if not the same
							if (l.Front.Sector != highlighted) Highlight(l.Front.Sector);
						}
						else
						{
							// Highlight nothing
							Highlight(null);
						}
					}
				}
				else
				{
					// Highlight nothing
					Highlight(null);
				}
			}
		}

		// Mouse leaves
		public override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);

			// Highlight nothing
			Highlight(null);
		}

		#endregion

		#region ================== Actions

		// This clears the selection
		[BeginAction("clearselection", BaseAction = true)]
		public void ClearSelection()
		{
			// Clear selection
			General.Map.Map.ClearAllSelected();

			//mxd. Clear selection info
			General.Interface.DisplayStatus(StatusType.Selection, string.Empty);

			// Redraw
			General.Interface.RedrawDisplay();
		}

		#endregion
	}
}
