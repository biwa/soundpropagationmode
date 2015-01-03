
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
using CodeImp.DoomBuilder.GZBuilder.Geometry;

#endregion

namespace CodeImp.DoomBuilder.SoundPropagationMode
{
	[EditMode(DisplayName = "Sound Propagation Mode",
			  SwitchAction = "soundpropagationmode",		// Action name used to switch to this mode
			  ButtonImage = "SoundPropagationIcon.png",	// Image resource name for the button
			  ButtonOrder = int.MinValue + 501,	// Position of the button (lower is more to the left)
			  ButtonGroup = "000_editing",
			  UseByDefault = true,
			  SafeStartMode = false,
			  Volatile = false)]

	public class SoundPropagationMode : ClassicMode
	{
		#region ================== Constants

		private const float LINE_THICKNESS = 1.0f;

		#endregion

		#region ================== Variables
		
		// Highlighted item
		protected Sector highlighted;
		private Association highlightasso = new Association();
		private FlatVertex[] overlayGeometryLevel1;
		private FlatVertex[] overlayGeometryLevel2;

		// Interface
		protected bool editpressed;

		// Labels
		private Dictionary<Sector, TextLabel[]> labels;

		private Dictionary<Sector, int> noisysectors;

		#endregion

		#region ================== Properties

		public override object HighlightedObject { get { return highlighted; } }

		#endregion

		#region ================== Constructor / Disposer

		// Constructor
		public SoundPropagationMode()
		{
			noisysectors = new Dictionary<Sector, int>();
		}

		// Disposer
		public override void Dispose()
		{
			// Not already disposed?
			if(!isdisposed)
			{
				// Dispose old labels
				foreach(KeyValuePair<Sector, TextLabel[]> lbl in labels)
					foreach(TextLabel l in lbl.Value) l.Dispose();

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
			foreach(Sector s in orderedselection)
			{
				crc.Add(s.FixedIndex);
			}
			return (int)(crc.Value & 0xFFFFFFFF);
		}

		// This sets up new labels
		private void SetupLabels()
		{
			if(labels != null)
			{
				// Dispose old labels
				foreach(KeyValuePair<Sector, TextLabel[]> lbl in labels)
					foreach(TextLabel l in lbl.Value) l.Dispose();
			}

			// Make text labels for sectors
			labels = new Dictionary<Sector, TextLabel[]>(General.Map.Map.Sectors.Count);
			foreach(Sector s in General.Map.Map.Sectors)
			{
				// Setup labels
				TextLabel[] labelarray = new TextLabel[s.Labels.Count];
				for(int i = 0; i < s.Labels.Count; i++)
				{
					Vector2D v = s.Labels[i].position;
					labelarray[i] = new TextLabel(20);
					labelarray[i].TransformCoords = true;
					labelarray[i].Rectangle = new RectangleF(v.x, v.y, 0.0f, 0.0f);
					labelarray[i].AlignX = TextAlignmentX.Center;
					labelarray[i].AlignY = TextAlignmentY.Middle;
					labelarray[i].Scale = 14f;
					labelarray[i].Color = General.Colors.Highlight.WithAlpha(255);
					labelarray[i].Backcolor = General.Colors.Background.WithAlpha(255);
				}
				labels.Add(s, labelarray);
			}
		}

		// This updates the overlay
		private void UpdateOverlay()
		{
			if(renderer.StartOverlay(true))
			{
				foreach (Sector sector in General.Map.Map.Sectors)
					renderer.RenderHighlight(sector.FlatVertices, new PixelColor(128, 160, 160, 160).ToInt());

				if (BuilderPlug.Me.UseHighlight)
				{
					renderer.RenderHighlight(overlayGeometryLevel1, new PixelColor(128, 0, 255, 0).ToInt());
					renderer.RenderHighlight(overlayGeometryLevel2, new PixelColor(128, 255, 255, 0).ToInt());
				}

				if (BuilderPlug.Me.UseHighlight && highlighted != null)
				{
					renderer.RenderHighlight(highlighted.FlatVertices, new PixelColor(128, 0, 192, 0).ToInt());
				}

				foreach(Linedef ld in General.Map.Map.Linedefs)
				{
					if(LinedefBlocksSounds(ld))
						renderer.RenderLine(ld.Start.Position, ld.End.Position, LINE_THICKNESS, new PixelColor(255, 255, 0, 0), true);
				}


				if(BuilderPlug.Me.ViewSelectionNumbers)
				{
					// Go for all selected sectors
					ICollection<Sector> orderedselection = General.Map.Map.GetSelectedSectors(true);
					foreach(Sector s in orderedselection)
					{
						// Render labels
						TextLabel[] labelarray = labels[s];
						for(int i = 0; i < s.Labels.Count; i++)
						{
							TextLabel l = labelarray[i];

							// Render only when enough space for the label to see
							float requiredsize = (l.TextSize.Height / 2) / renderer.Scale;
							if(requiredsize < s.Labels[i].radius) renderer.RenderText(l);
						}
					}
				}

				renderer.Finish();
			}
		}

		private void updateOverlaySurfaces()
		{
			ICollection<Sector> orderedselection = General.Map.Map.GetSelectedSectors(true);
			List<FlatVertex> vertsList = new List<FlatVertex>();

			// Go for all selected sectors
			// foreach (Sector s in orderedselection) vertsList.AddRange(s.FlatVertices);
			foreach (Sector s in noisysectors.Keys) if(noisysectors[s] == 1) vertsList.AddRange(s.FlatVertices);
			overlayGeometryLevel1 = vertsList.ToArray();

			vertsList.Clear();
			foreach (Sector s in noisysectors.Keys) if (noisysectors[s] == 2) vertsList.AddRange(s.FlatVertices);

			overlayGeometryLevel2 = vertsList.ToArray();
		}
		
		// Support function for joining and merging sectors
		private void JoinMergeSectors(bool removelines)
		{
			// Remove lines in betwen joining sectors?
			if(removelines)
			{
				// Go for all selected linedefs
				List<Linedef> selectedlines = new List<Linedef>(General.Map.Map.GetSelectedLinedefs(true));
				foreach(Linedef ld in selectedlines)
				{
					// Front and back side?
					if((ld.Front != null) && (ld.Back != null))
					{
						// Both a selected sector, but not the same?
						if(ld.Front.Sector.Selected && ld.Back.Sector.Selected &&
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
			foreach(Sector s in orderedselection)
				if(!s.IsDisposed) { first = s; break; }
			
			// Join all selected sectors with the first
			for(int i = 0; i < orderedselection.Count; i++)
				if((orderedselection[i] != first) && !orderedselection[i].IsDisposed)
					orderedselection[i].Join(first);

			// Clear selection
			General.Map.Map.ClearAllSelected();
			
			// Update
			General.Map.Map.Update();
			
			// Make text labels for sectors
			SetupLabels();
			UpdateSelectedLabels();
		}

		// This highlights a new item
		protected void Highlight(Sector s)
		{
			bool completeredraw = false;

			// Often we can get away by simply undrawing the previous
			// highlight and drawing the new highlight. But if associations
			// are or were drawn we need to redraw the entire display.

			// Previous association highlights something?
			if((highlighted != null) && (highlighted.Tag > 0)) completeredraw = true;

			// Set highlight association
			if (s != null)
			{
				Vector2D center = (s.Labels.Count > 0 ? s.Labels[0].position : new Vector2D(s.BBox.X + s.BBox.Width / 2, s.BBox.Y + s.BBox.Height / 2));
				highlightasso.Set(center, s.Tag, UniversalType.SectorTag);
			}
			else
				highlightasso.Set(new Vector2D(), 0, 0);

			// New association highlights something?
			if((s != null) && (s.Tag > 0)) completeredraw = true;

			// Change label color
			if((highlighted != null) && !highlighted.IsDisposed)
			{
				TextLabel[] labelarray = labels[highlighted];
				foreach(TextLabel l in labelarray) l.Color = General.Colors.Selection;
			}
			
			// Change label color
			if((s != null) && !s.IsDisposed)
			{
				TextLabel[] labelarray = labels[s];
				foreach(TextLabel l in labelarray) l.Color = General.Colors.Highlight;
			}

			// Update display
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

			updateOverlaySurfaces();
			UpdateOverlay();
			renderer.Present();

			// Show highlight info
			if((highlighted != null) && !highlighted.IsDisposed)
				General.Interface.ShowSectorInfo(highlighted);
			else
				General.Interface.HideInfo();
		}

		// This selectes or deselects a sector
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
					
					// Setup labels
					ICollection<Sector> orderedselection = General.Map.Map.GetSelectedSectors(true);
					TextLabel[] labelarray = labels[s];
					foreach(TextLabel l in labelarray)
					{
						l.Text = orderedselection.Count.ToString();
						l.Color = General.Colors.Selection;
					}
				}
				// Deselect the sector?
				else if(!selectstate && s.Selected)
				{
					s.Selected = false;
					selectionchanged = true;

					// Clear labels
					TextLabel[] labelarray = labels[s];
					foreach(TextLabel l in labelarray) l.Text = "";

					// Update all other labels
					UpdateSelectedLabels();
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

		// This updates labels from the selected sectors
		private void UpdateSelectedLabels()
		{
			// Go for all labels in all selected sectors
			ICollection<Sector> orderedselection = General.Map.Map.GetSelectedSectors(true);
			int index = 0;
			foreach(Sector s in orderedselection)
			{
				TextLabel[] labelarray = labels[s];
				foreach(TextLabel l in labelarray)
				{
					// Make sure the text and color are right
					int labelnum = index + 1;
					l.Text = labelnum.ToString();
					l.Color = General.Colors.Selection;
				}
				index++;
			}
		}

		private void UpdateSoundPropagation()
		{
			List<Sector> sectorstocheck = new List<Sector>();
			List<Sector> checkedsectors = new List<Sector>();

			noisysectors.Clear();

			if (highlighted == null || highlighted.IsDisposed)
				return;

			sectorstocheck.Add(highlighted);
			noisysectors[highlighted] = 1;

			while (sectorstocheck.Count > 0)
			{
				// Make sure to first check all sectors that are not behind a sound blocking line
				Sector sector = sectorstocheck.Find(s => noisysectors[s] == 1);

				// If there are only sectors behind a sound blocking line are left, just take
				// the first available
				if (sector == null)
					sector = sectorstocheck[0];

				foreach (Sidedef sd in sector.Sidedefs)
				{
					bool blocksound = LinedefBlocksSounds(sd.Line);
					bool blockheight = false;
					Sector oppositesector = null;

					if (sd.Line.Back == null) // If the line is one sided, the sound can travel nowhere, so try the next one
					{
						continue;
					}
					else
					{
						// Get the sector on the other side of the line we're checking right now
						if (sd.Line.Front.Sector == sector)
							oppositesector = sd.Line.Back.Sector;
						else
							oppositesector = sd.Line.Front.Sector;

						// Check if the sound will be blocked because of sector floor and ceiling heights
						// (like closed doors, raised lifts etc.)
						if (
							(
								//sector.CeilHeight <= oppositesector.FloorHeight ||
								//sector.FloorHeight >= oppositesector.CeilHeight ||
								oppositesector.CeilHeight <= oppositesector.FloorHeight ||
								sector.CeilHeight <= sector.FloorHeight
							) &&
							(
								true // sector.Selected == false && oppositesector.Selected == false
							)
						)
						{
							
								blockheight = true;
						}
					}

					// Try next line if sound will not pass through the current one. The last check makes
					// sure that the next line is tried if the current line is blocking sound, and the current
					// sector is already behind a sound blocking line
					if (oppositesector == null || blockheight || (blocksound && noisysectors[sector] == 2))
						continue;

					// If the opposite sector was not regarded at all yet...
					if (!checkedsectors.Contains(oppositesector) && !sectorstocheck.Contains(oppositesector))
					{
						sectorstocheck.Add(oppositesector);

						// Set the correct propagation level of the opposite sector. If the line is not blocking
						// sounds, set the same level as the current sector
						if(blocksound)
							noisysectors[oppositesector] = 2;
						else
							noisysectors[oppositesector] = noisysectors[sector];
					}
					// ... otherwise make sure that an already checked sector gets the correct propagation level.
					// This is required, since a sector might have sound blocking lines on one side, but the sound
					// could still travel freely from another side
					else if(!blocksound && noisysectors[sector] == 1)
					{
						noisysectors[oppositesector] = 1;
					}
				}

				sectorstocheck.Remove(sector);
				checkedsectors.Add(sector);
			}
		}

		private bool LinedefBlocksSounds(Linedef linedef)
		{
			var flags = linedef.GetFlags();

			if (General.Map.UDMF && flags.ContainsKey("blocksound"))
				return flags["blocksound"];
			else if (!General.Map.UDMF && flags.ContainsKey("64"))
				return flags["64"];

			return false;
		}

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

			CustomPresentation presentation = new CustomPresentation();
			// presentation.AddLayer(new PresentLayer(RendererLayer.Background, BlendingMode.Mask, General.Settings.BackgroundAlpha));

			/*
			presentation.AddLayer(new PresentLayer(RendererLayer.Background, BlendingMode.Alpha, 1f, true));
			presentation.AddLayer(new PresentLayer(RendererLayer.Grid, BlendingMode.Mask));
			presentation.AddLayer(new PresentLayer(RendererLayer.Overlay, BlendingMode.Alpha, 1f, true));
			presentation.AddLayer(new PresentLayer(RendererLayer.Geometry, BlendingMode.Alpha, 1f, true));
			*/

			presentation.AddLayer(new PresentLayer(RendererLayer.Background, BlendingMode.Mask, General.Settings.BackgroundAlpha));
			// presentation.AddLayer(new PresentLayer(RendererLayer.Surface, BlendingMode.Mask));
			presentation.AddLayer(new PresentLayer(RendererLayer.Things, BlendingMode.Alpha, 1f));
			presentation.AddLayer(new PresentLayer(RendererLayer.Grid, BlendingMode.Mask));
			presentation.AddLayer(new PresentLayer(RendererLayer.Geometry, BlendingMode.Alpha, 1f, true));
			presentation.AddLayer(new PresentLayer(RendererLayer.Overlay, BlendingMode.Alpha, 1f, true));

			renderer.SetPresentation(presentation);

			// renderer.SetPresentation(Presentation.Standard);
			renderer.SetPresentation(presentation);
			Presentation p = Presentation.Standard;

			// Convert geometry selection to sectors only
			General.Map.Map.ConvertSelection(SelectionType.Sectors);

			// Make text labels for sectors
			SetupLabels();
			
			// Update
			UpdateSelectedLabels();
			updateOverlaySurfaces();
			UpdateOverlay();
		}

		// Mode disengages
		public override void OnDisengage()
		{
			base.OnDisengage();

			// Keep only sectors selected
			General.Map.Map.ClearSelectedLinedefs();
			
			// Going to EditSelectionMode?
			if(General.Editing.NewMode is EditSelectionMode)
			{
				// Not pasting anything?
				EditSelectionMode editmode = (General.Editing.NewMode as EditSelectionMode);
				if(!editmode.Pasting)
				{
					// No selection made? But we have a highlight!
					if((General.Map.Map.GetSelectedSectors(true).Count == 0) && (highlighted != null))
					{
						// Make the highlight the selection
						SelectSector(highlighted, true, false);
					}
				}
			}

			// Hide highlight info
			General.Interface.HideInfo();
		}

		// This redraws the display
		public override void OnRedrawDisplay()
		{
			// renderer.RedrawSurface();
			base.OnRedrawDisplay();
			
			// Render lines and vertices
			if(renderer.StartPlotter(true))
			{
				renderer.PlotLinedefSet(General.Map.Map.Linedefs);
				renderer.PlotVerticesSet(General.Map.Map.Vertices);

				// foreach(Sector sector in General.Map.Map.Sectors)
				//	renderer.PlotSector(sector, new PixelColor(255, 255, 160, 160));

				if((highlighted != null) && !highlighted.IsDisposed)
				{
					renderer.PlotSector(highlighted, General.Colors.Highlight);
					// BuilderPlug.Me.PlotReverseAssociations(renderer, highlightasso);
				}

				renderer.Finish();
			}

			// Render things
			if(renderer.StartThings(true))
			{
				renderer.RenderThingSet(General.Map.ThingsFilter.HiddenThings, Presentation.THINGS_HIDDEN_ALPHA);
				renderer.RenderThingSet(General.Map.ThingsFilter.VisibleThings, 1.0f);
				renderer.Finish();
			}

			// Render selection
			if(renderer.StartOverlay(true))
			{
				// if((highlighted != null) && !highlighted.IsDisposed) BuilderPlug.Me.RenderReverseAssociations(renderer, highlightasso);
				if(selecting) RenderMultiSelection();

				renderer.Finish();
			}

			// Render overlay
			UpdateOverlay();
			
			renderer.Present();
		}

		// Selection
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
					TextLabel[] labelarray = labels[highlighted];
					foreach(TextLabel l in labelarray) l.Color = General.Colors.Highlight;
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

			// Clear labels
			foreach (TextLabel[] labelarray in labels.Values)
				foreach (TextLabel l in labelarray) l.Text = "";

			updateOverlaySurfaces();

			// Redraw
			General.Interface.RedrawDisplay();
		}

		#endregion
	}
}
