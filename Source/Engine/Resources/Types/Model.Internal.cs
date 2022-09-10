﻿using System;
using Engine.GPU;
using MeshOptimizer;

namespace Engine.Resources
{
	public partial class Mesh
	{
		internal static GraphicsBuffer<uint> PrimBuffer = new(2000000);
		internal static GraphicsBuffer<Vertex> VertBuffer = new(2000000);
		internal static GraphicsBuffer<Meshlet> MeshletBuffer = new(2000000);
		internal static GraphicsBuffer<GPUMesh> MeshBuffer = new(1000000);

		internal BufferAllocation<uint> PrimHandle;
		internal BufferAllocation<Vertex> VertHandle;
		internal BufferAllocation<Meshlet> MeshletHandle;
		internal BufferAllocation<GPUMesh> MeshHandle;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct GPUMesh
	{
		public uint VertOffset; // Start of submesh in vertex buffer.
		public uint PrimOffset; // Start of submesh in primitive buffer.
		public uint MeshletOffset; // Start of submesh in meshlet buffer.
		public uint MeshletCount;   // Number of meshlets used
	}

	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct Vertex
	{
		public Vector3 Position;
		public Vector3 Normal;
		public Vector2 UV0;
	}
}