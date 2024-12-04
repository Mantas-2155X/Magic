using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace World
{
	public class TerrainGenerator : MonoBehaviour
	{
		[Serializable]
		public struct STerrainLayer
		{
			[SerializeField]
			public float StartingHeight;

			[SerializeField]
			public float Overlay;
		}
		
		[SerializeField]
		public Terrain Terrain;

		[SerializeField]
		public Transform Water;

		[SerializeField]
		public int Resolution = 256;
		
		[SerializeField]
		public int Width = 256;
		[SerializeField]
		public int Height = 256;
		[SerializeField]
		public int Depth = 16;
		
		[SerializeField]
		public float Scale = 4f;

		[SerializeField]
		public float WaterLevel = 8f;

		[SerializeField]
		public float PaintNoise = 0.001f;
		
		[SerializeField]
		public STerrainLayer[] Layers;
		
		[SerializeField, HideInInspector]
		public float[] Seed = { 0, 0 };
		
		public void Generate()
		{
			var terrainData = Terrain.terrainData;

			terrainData.heightmapResolution = Resolution + 1;
			terrainData.alphamapResolution = Resolution + 1;
			terrainData.baseMapResolution = Resolution + 1;
			
			terrainData.size = new Vector3(Width, Depth, Height);
			terrainData.SetHeights(0, 0, generateHeights());
			
			Terrain.terrainData = terrainData;
			
			Water.localPosition = new Vector3(Width / 2f, WaterLevel / 2f, Height / 2f);
			Water.localScale = new Vector3(Width, WaterLevel, Height);
		}

		public void Paint()
		{
			var terrainData = Terrain.terrainData;
			var map = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

			for (var y = 0; y < terrainData.alphamapHeight; y++)
			{
				for (var x = 0; x < terrainData.alphamapWidth; x++)
				{
					var height = terrainData.GetHeight(y, x);
					var blend = new float[Layers.Length];
					
					for (var i = 0; i < Layers.Length; i++)
					{
						var layer = Layers[i];

						var noise = remap(Mathf.PerlinNoise(x * PaintNoise, y * PaintNoise), 0f, 1f, 0.5f, 1f);
						
						var startingHeight = (layer.StartingHeight * noise) - (layer.Overlay * noise);
						var nextStartingHeight = i == Layers.Length - 1 ? 0f : (Layers[i].StartingHeight * noise) + (Layers[i].Overlay * noise);
						
						if (height >= startingHeight)
						{
							if (i == Layers.Length - 1)
								blend[i] = 1;
							else if (height <= nextStartingHeight)
								blend[i] = 1;
						}
					}

					blend = normalize(blend);
					
					for (var i = 0; i < Layers.Length; i++)
						map[x, y, i] = blend[i];
				}
			}
			
			terrainData.SetAlphamaps(0, 0, map);
			Terrain.terrainData = terrainData;
		}

		public void RandomizeSeed()
		{
			Seed[0] = Random.Range(-10000f, 10000f);
			Seed[1] = Random.Range(-10000f, 10000f);
		}

		private float[,] generateHeights()
		{
			var heights = new float[Width, Height];

			for (var x = 0; x < Width; x++)
				for (var y = 0; y < Height; y++)
					heights[x, y] = randomNoise(x, y);
			
			return heights;
		}

		private float randomNoise(int x, int y)
		{
			var xCoord = (x + Seed[0]) / Width * Scale;
			var yCoord = (y + Seed[1]) / Height * Scale;

			return Mathf.PerlinNoise(xCoord, yCoord);
		}

		private float[] normalize(float[] values)
		{
			var total = 0f;
			
			for (var i = 0; i < values.Length; i++)
				total += values[i];

			for (var i = 0; i < values.Length; i++)
				values[i] /= total;
			
			return values;
		}

		private float remap(float value, float startingMin, float startingMax, float targetMin, float targetMax)
		{
			return (value - startingMin) * (targetMax - targetMin) / (startingMax - startingMin) + targetMin;
		}
	}
}