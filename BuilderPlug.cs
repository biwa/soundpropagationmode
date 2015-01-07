
#region ================== Copyright (c) 2014 Boris Iwanski

/*
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
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Linq;
using CodeImp.DoomBuilder.Windows;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Geometry;
using System.Drawing;
using CodeImp.DoomBuilder.Editing;
using CodeImp.DoomBuilder.Plugins;
using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.Types;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.BuilderModes;
using CodeImp.DoomBuilder.GZBuilder.Tools;
using CodeImp.DoomBuilder.GZBuilder.Geometry;

#endregion

namespace CodeImp.DoomBuilder.SoundPropagationMode
{
	//
	// MANDATORY: The plug!
	// This is an important class to the Doom Builder core. Every plugin must
	// have exactly 1 class that inherits from Plug. When the plugin is loaded,
	// this class is instantiated and used to receive events from the core.
	// Make sure the class is public, because only public classes can be seen
	// by the core.
	//

	public class BuilderPlug : Plug
	{
		#region ================== Variables

		private bool additiveselect;
		private bool autoclearselection;
		private MenusForm menusform;
		private bool usehighlight;
		private bool viewselectionnumbers;
		private float stitchrange;

		// Colors
		private PixelColor highlightcolor;
		private PixelColor level1color;
		private PixelColor level2color;
		private PixelColor blocksoundcolor;
		private PixelColor nosoundcolor;

		private List<PixelColor> distinctcolors;
		private List<SoundEnvironment> soundenvironments;
		private List<Linedef> blockinglinedefs;
		private FlatVertex[] overlayGeometry;
		private bool soundenvironmentisupdated;

		#endregion

		#region ================== Properties

		public bool AdditiveSelect { get { return additiveselect; } }
		public bool AutoClearSelection { get { return autoclearselection; } }
		public MenusForm MenusForm { get { return menusform; } }
		public bool ViewSelectionNumbers { get { return viewselectionnumbers; } set { viewselectionnumbers = value; } }
		public bool UseHighlight
		{
			get
			{
				return usehighlight;
			}
			set
			{
				usehighlight = value;
				General.Map.Renderer3D.ShowSelection = usehighlight;
				General.Map.Renderer3D.ShowHighlight = usehighlight;
			}
		}

		public float StitchRange { get { return stitchrange; } }

		// Colors
		public PixelColor HighlightColor { get { return highlightcolor; } set { highlightcolor = value; } }
		public PixelColor Level1Color { get { return level1color; } set { level1color = value; } }
		public PixelColor Level2Color { get { return level2color; } set { level2color = value; } }
		public PixelColor BlockSoundColor { get { return blocksoundcolor; } set { blocksoundcolor = value; } }
		public PixelColor NoSoundColor { get { return nosoundcolor; } set { nosoundcolor = value; } }

		public List<SoundEnvironment> SoundEnvironments { get { return soundenvironments; } set { soundenvironments = value; } }
		public List<Linedef> BlockingLinedefs { get { return blockinglinedefs; } set { blockinglinedefs = value; } }
		public FlatVertex[] OverlayGeometry { get { return overlayGeometry; } set { overlayGeometry = value; } }
		public bool SoundEnvironmentIsUpdated { get { return soundenvironmentisupdated; } }

		#endregion

 		// Static instance. We can't use a real static class, because BuilderPlug must
		// be instantiated by the core, so we keep a static reference. (this technique
		// should be familiar to object-oriented programmers)
		private static BuilderPlug me;

		// Static property to access the BuilderPlug
		public static BuilderPlug Me { get { return me; } }

      	// This plugin relies on some functionality that wasn't there in older versions
		public override int MinimumRevision { get { return 1310; } }

		// This event is called when the plugin is initialized
		public override void OnInitialize()
		{
			base.OnInitialize();

			usehighlight = true;

			highlightcolor = PixelColor.FromInt(General.Settings.ReadPluginSetting("highlightcolor", new PixelColor(255, 0, 192, 0).ToInt()));
			level1color = PixelColor.FromInt(General.Settings.ReadPluginSetting("level1color", new PixelColor(255, 0, 255, 0).ToInt()));
			level2color = PixelColor.FromInt(General.Settings.ReadPluginSetting("level2color", new PixelColor(255, 255, 255, 0).ToInt()));
			nosoundcolor = PixelColor.FromInt(General.Settings.ReadPluginSetting("nosoundcolor", new PixelColor(255, 160, 160, 160).ToInt()));
			blocksoundcolor = PixelColor.FromInt(General.Settings.ReadPluginSetting("blocksoundcolor", new PixelColor(255, 255, 0, 0).ToInt()));

			distinctcolors = new List<PixelColor> {
				PixelColor.FromColor(Color.Blue), 
				PixelColor.FromColor(Color.Orange), 
				PixelColor.FromColor(Color.ForestGreen), 
				PixelColor.FromColor(Color.Sienna), 
				PixelColor.FromColor(Color.LightPink), 
				PixelColor.FromColor(Color.Purple),
				PixelColor.FromColor(Color.Cyan), 
				PixelColor.FromColor(Color.LawnGreen), 
				PixelColor.FromColor(Color.PaleGoldenrod), 
				PixelColor.FromColor(Color.Red), 
				PixelColor.FromColor(Color.Yellow), 
				PixelColor.FromColor(Color.LightSkyBlue), 
				PixelColor.FromColor(Color.Magenta)
			};

			soundenvironments = new List<SoundEnvironment>();
			blockinglinedefs = new List<Linedef>();
			soundenvironmentisupdated = false;

			//controlsectorarea = new ControlSectorArea(-512, 0, 512, 0, -128, -64, 128, 64, 64, 56);

			// This binds the methods in this class that have the BeginAction
			// and EndAction attributes with their actions. Without this, the
			// attributes are useless. Note that in classes derived from EditMode
			// this is not needed, because they are bound automatically when the
			// editing mode is engaged.
            General.Actions.BindMethods(this);

			menusform = new MenusForm();

  			// TODO: Add DB2 version check so that old DB2 versions won't crash
			// General.ErrorLogger.Add(ErrorType.Error, "zomg!");

			// Keep a static reference
            me = this;
		}

		public override void OnMapOpenBegin()
		{
			base.OnMapOpenBegin();

			soundenvironmentisupdated = false;
		}

		public override void OnMapNewBegin()
		{
			base.OnMapNewBegin();

			soundenvironmentisupdated = false;
		}

		// This is called when the plugin is terminated
		public override void Dispose()
		{
			base.Dispose();

			// This must be called to remove bound methods for actions.
            General.Actions.UnbindMethods(this);
        }

		public void UpdateSoundEnvironments()
		{
			List<Sector> sectorstocheck = new List<Sector>();
			List<Sector> checkedsectors = new List<Sector>();
			List<Sector> allsectors = new List<Sector>();
			List<Thing> soundenvironmenthings = new List<Thing>();
			uint colorcounter = 0;

			General.Interface.DisplayStatus(StatusType.Busy, "Updating sound environments");

			soundenvironments.Clear();
			blockinglinedefs.Clear();

			foreach (Sector s in General.Map.Map.Sectors)
				allsectors.Add(s);

			soundenvironmenthings = GetSoundEnvironmentThings(General.Map.Map.Sectors.ToList());

			while (soundenvironmenthings.Count > 0)
			{
				Thing thing = soundenvironmenthings[0];

				if (thing.Sector == null)
					thing.DetermineSector();

				if (thing.Sector == null)
				{
					soundenvironmenthings.Remove(thing);
					continue;
				}

				SoundEnvironment environment = new SoundEnvironment(colorcounter + 1);

				sectorstocheck.Add(thing.Sector);

				while (sectorstocheck.Count > 0)
				{
					Sector sector = sectorstocheck[0];
					Sector oppositesector = null;

					if (!environment.Sectors.Contains(sector))
						environment.Sectors.Add(sector);

					if (!checkedsectors.Contains(sector))
						checkedsectors.Add(sector);

					sectorstocheck.Remove(sector);
					allsectors.Remove(sector);

					foreach (Sidedef sd in sector.Sidedefs)
					{
						if (LinedefBlocksSoundEnvironment(sd.Line))
						{
							if (!environment.Linedefs.Contains(sd.Line))
								environment.Linedefs.Add(sd.Line);

							continue;
						}

						if (sd.Line.Back == null)
							continue;

						if (sd.Line.Front.Sector == sector)
							oppositesector = sd.Line.Back.Sector;
						else
							oppositesector = sd.Line.Front.Sector;

						if (!sectorstocheck.Contains(oppositesector) && !checkedsectors.Contains(oppositesector))
							sectorstocheck.Add(oppositesector);
					}
				}

				environment.Things = GetSoundEnvironmentThings(environment.Sectors);

				foreach (Thing t in environment.Things)
				{
					if (soundenvironmenthings.Contains(t))
						soundenvironmenthings.Remove(t);
				}

				environment.Color = distinctcolors[(int)(colorcounter % distinctcolors.Count)];

				colorcounter++;

				environment.Things = environment.Things.OrderBy(o => o.Index).ToList();
				environment.Linedefs = environment.Linedefs.OrderBy(o => o.Index).ToList();

				soundenvironments.Add(environment);
			}

			// Create the overlay geometry from the sound environments
			int i = 0;
			List<FlatVertex> vertsList = new List<FlatVertex>();

			foreach (SoundEnvironment se in soundenvironments)
			{
				PixelColor color = BuilderPlug.Me.NoSoundColor;

				if (se.Things.Count > 0)
				{
					color = distinctcolors[i % distinctcolors.Count];
					i++;
				}

				foreach (Sector s in se.Sectors)
				{
					FlatVertex[] fv = new FlatVertex[s.FlatVertices.Length];
					s.FlatVertices.CopyTo(fv, 0);
					for (int j = 0; j < fv.Length; j++) fv[j].c = se.Color.WithAlpha(128).ToInt();
					vertsList.AddRange(fv);
				}
			}

			// Create overlay geometry for sectors that don't belong to a sound environment
			foreach (Sector s in allsectors)
			{
				FlatVertex[] fv = new FlatVertex[s.FlatVertices.Length];
				s.FlatVertices.CopyTo(fv, 0);
				for (int j = 0; j < fv.Length; j++) fv[j].c = BuilderPlug.Me.NoSoundColor.WithAlpha(128).ToInt();
				vertsList.AddRange(fv);
			}

			overlayGeometry = vertsList.ToArray();

			// Get all Linedefs that will block sound environments
			foreach (Linedef ld in General.Map.Map.Linedefs)
			{
				if (LinedefBlocksSoundEnvironment(ld))
					blockinglinedefs.Add(ld);
			}

			General.Interface.DisplayStatus(StatusType.Ready, "Done updating sound environments");

			soundenvironmentisupdated = true;
		}

		private List<Thing> GetSoundEnvironmentThings(List<Sector> sectors)
		{
			List<Thing> things = new List<Thing>();

			foreach (Thing thing in General.Map.Map.Things)
			{
				// SoundEnvironment thing, see http://zdoom.org/wiki/Classes:SoundEnvironment
				if (thing.Type != 9048)
					continue;

				if (thing.Sector == null)
					thing.DetermineSector();

				if (thing.Sector != null && sectors.Contains(thing.Sector))
					things.Add(thing);
			}

			return things;
		}

		private bool LinedefBlocksSoundEnvironment(Linedef linedef)
		{
			var flags = linedef.GetFlags();

			if (General.Map.UDMF && flags.ContainsKey("zoneboundary"))
				return flags["zoneboundary"];
			// In Hexen format the line must have action 121 (Line_SetIdentification) and bit 1 of
			// the second argument set (see http://zdoom.org/wiki/Line_SetIdentification)
			else if (!General.Map.UDMF && linedef.Action == 121 && (linedef.Args[1] & 1) == 1)
				return true;

			return false;
		}

        #region ================== Actions
		
        #endregion

		#region ================== Methods


		#endregion
	}

	public static class Extensions
	{
		public static PixelColor FromInt(this PixelColor color, int colorInt)
		{
			byte a = (byte)((colorInt & (0xff << 24)) >> 24);
			byte r = (byte)((colorInt & (0xff << 16)) >> 16);
			byte g = (byte)((colorInt & (0xff << 8)) >> 8);
			byte b = (byte)(colorInt & 0xff);

			return new PixelColor(a, r, g, b);
		}
	}
}
