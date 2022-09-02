﻿using System;
using Engine;
using Engine.Resources;
using Engine.Mathematics;
using Assimp;
using Assimp.Configs;
using AI = Assimp;
using ModelPart = Engine.Resources.ModelPart;
using Material = Engine.Resources.Material;
using System.Collections.Concurrent;
using Engine.Core;
using Mesh = Engine.Resources.Mesh;

namespace Basic.Loaders
{
	public class GLTFLoader : AssetLoader<Model>
	{
		public string Path;

		[ThreadStatic]
		private static AssimpContext importContext = new();

		public GLTFLoader(string path)
		{
			Path = path;
		}

		public override async Task<Model> Load()
		{
			// Create import context if needed.
			if (importContext == null)
			{
				importContext = new AssimpContext();

				importContext.SetConfig(new AppScaleConfig(1));
				importContext.SetConfig(new FavorSpeedConfig(true));
			}

			// Load GLTF file from thread-local context.
			PostProcessSteps steps = PostProcessSteps.GenerateNormals | PostProcessSteps.GenerateUVCoords;
			//steps |= PostProcessSteps.CalculateTangentSpace;
			AI.Scene importScene = importContext.ImportFile(Path, steps);

			// Load embedded textures.
			/*Texture2D[] textures = new Texture2D[importScene.TextureCount];
			Parallel.For(0, importScene.TextureCount, (i) =>
			{
				Texture2D tex = new Texture2D();
				tex.LoadData(importScene.Textures[i].CompressedData, TextureFormat.RGB);

				textures[i] = tex;
			});*/

			// Create embedded materials.
			Material[] materials = new Material[importScene.MaterialCount];
			for (int i = 0; i < importScene.MaterialCount; i++)
			{
				Shader shader = await Asset.GetAsync<Shader>("USER:Shaders/PBR");
				Material material = new Material(shader);
				material.SetColor("DebugColor", i % 2 == 0 ? new Color(0, 0, 1) : new Color(1, 0, 0));

				materials[i] = material;
			}

			// Create submeshes.
			ConcurrentBag<Mesh> meshes = new();
			Parallel.ForEach(importScene.Meshes, (importMesh, state) =>
			{
				Debug.Assert(importMesh.PrimitiveType == PrimitiveType.Triangle, "Engine does not support non-triangle geometry.");

				Vector3[] vertices = new Vector3[importMesh.VertexCount];
				Vector3[] normals = new Vector3[importMesh.VertexCount];

				// Interpret vertices/normals.
				for (int i = 0; i < importMesh.VertexCount; i++)
				{
					vertices[i] = new(importMesh.Vertices[i].X, importMesh.Vertices[i].Y, importMesh.Vertices[i].Z);
					normals[i] = new(importMesh.Normals[i].X, importMesh.Normals[i].Y, importMesh.Normals[i].Z);
				}

				// Create submesh.
				Mesh submesh = new Mesh();
				submesh.SetMaterial(materials[importMesh.MaterialIndex]);
				submesh.SetVertices(vertices);
				submesh.SetNormals(normals);
				submesh.SetIndices(importMesh.GetUnsignedIndices());

				meshes.Add(submesh);
			});

			// Create model.
			Model model = new Model();
			model.Parts = new[] { new ModelPart()
			{
				Meshes = meshes.ToArray()
			}};

			return model;
		}
	}
}
