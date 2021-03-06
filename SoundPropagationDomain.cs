﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;

namespace CodeImp.DoomBuilder.SoundPropagationMode
{
	public class SoundPropagationDomain
	{
		private List<Sector> sectors;
		private List<Sector> adjacentsectors;
		private List<Linedef> blockinglines;
		private FlatVertex[] level1geometry;
		private FlatVertex[] level2geometry;

		public List<Sector> Sectors { get { return sectors; } set { sectors = value; } }
		public List<Sector> AdjacentSectors { get { return adjacentsectors; } set { adjacentsectors = value; } }
		public List<Linedef> BlockingLines { get { return blockinglines; } set { blockinglines = value; } }
		public FlatVertex[] Level1Geometry { get { return level1geometry; } }
		public FlatVertex[] Level2Geometry { get { return level2geometry; } }

		public SoundPropagationDomain(Sector sector)
		{
			sectors = new List<Sector>();
			adjacentsectors = new List<Sector>();
			blockinglines = new List<Linedef>();

			CreateSoundPropagationDomain(sector);
		}

		private void CreateSoundPropagationDomain(Sector sourcesector)
		{
			List<Sector> sectorstocheck = new List<Sector>();

			sectorstocheck.Add(sourcesector);

			while (sectorstocheck.Count > 0)
			{
				// Make sure to first check all sectors that are not behind a sound blocking line
				Sector sector = sectorstocheck[0];

				foreach (Sidedef sd in sector.Sidedefs)
				{
					bool blocksound = BuilderPlug.Me.LinedefBlocksSounds(sd.Line);
					bool blockheight = false;
					Sector oppositesector = null;

					if (blocksound)
						blockinglines.Add(sd.Line);

					if (sd.Line.Back == null || blocksound) // If the line is one sided, the sound can travel nowhere, so try the next one
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
								sector.CeilHeight <= oppositesector.FloorHeight ||
								sector.FloorHeight >= oppositesector.CeilHeight ||
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
					if (oppositesector == null || blockheight)
						continue;

					// If the opposite sector was not regarded at all yet...
					if (!sectors.Contains(oppositesector) && !sectorstocheck.Contains(oppositesector))
					{
						sectorstocheck.Add(oppositesector);
					}
				}

				sectorstocheck.Remove(sector);
				sectors.Add(sector);
			}

			foreach (Linedef ld in blockinglines)
			{
				if (!sectors.Contains(ld.Front.Sector))
					adjacentsectors.Add(ld.Front.Sector);

				if (ld.Back != null && !sectors.Contains(ld.Back.Sector))
					adjacentsectors.Add(ld.Back.Sector);
			}

			List<FlatVertex> vertices = new List<FlatVertex>();

			foreach (Sector s in sectors)
			{
				vertices.AddRange(s.FlatVertices);
			}

			level1geometry = vertices.ToArray();
			level2geometry = vertices.ToArray();

			for (int i = 0; i < level1geometry.Length; i++)
			{
				level1geometry[i].c = BuilderPlug.Me.Level1Color.WithAlpha(128).ToInt();
				level2geometry[i].c = BuilderPlug.Me.Level2Color.WithAlpha(128).ToInt();
			}

		}
	}
}
