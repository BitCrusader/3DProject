﻿using System;
using System.Collections.Immutable;
using System.Linq;
using Engine.GPU;
using Engine.Rendering;

namespace Engine.Resources
{
	/// <summary>
	/// A 3D model, composed of one or multiple parts and optionally a skeleton.
	/// </summary>
	public class Model : Resource
	{
		public ModelPart[] Parts { get; set; }
		public Bone Skeleton { get; set; } = null;

		public bool IsCommitted => Parts?.Any(o => o.Meshes.Any(o2 => o2.IsCommitted)) ?? false;
		public bool IsDeformable => Parts?.Any(o => o.Meshes.Any(o2 => o2.IsDeformable)) ?? false;

		public Model(params Mesh[] meshes)
		{
			Parts = new[]
			{
				new ModelPart()
				{
					Meshes = meshes
				}
			};
		}

		public Model(params ModelPart[] parts)
		{
			Parts = parts;
		}

		public override void Dispose()
		{
			foreach (var part in Parts)
			{
				part.Dispose();
			}

			base.Dispose();
		}
	}

	/// <summary>
	/// A group of one or multiple meshes. Can be toggled on/off within the editor, comparable to a bodygroup in SFM.
	/// </summary>
	public class ModelPart : IDisposable
	{
		public Mesh[] Meshes { get; set; }

		public ModelPart(params Mesh[] meshes)
		{
			Meshes = meshes;
		}

		public void Dispose()
		{
			foreach (var mesh in Meshes)
			{
				mesh.Dispose();
			}
		}
	}

	public class Bone
	{
		public string Name { get; }
		public ImmutableList<Bone> Children { get; }

		public Matrix4 InverseBind { get; }

		public Vector3 BasePosition { get; }
		public Vector3 BaseRotation { get; }
		public Vector3 BaseScale { get; }

		/// <param name="boneTransform">The transform of the bone in model space (not relative to other bones).</param>
		/// <param name="parentTransform">The transform of the bone's parent (in model space), or Matrix4.Identity if none exists.</param>
		public Bone(string name, Matrix4 boneTransform, Matrix4 parentTransform, IEnumerable<Bone> children)
		{
			Name = name;
			Children = children.ToImmutableList();
			InverseBind = boneTransform.Inverse();

			// Calculate base transforms.
			boneTransform = boneTransform * parentTransform.Inverse();
			BasePosition = boneTransform.Inverse().ExtractTranslation();
			BaseRotation = boneTransform.Inverse().ExtractRotation().EulerAngles.ToDegrees();
			BaseScale = boneTransform.Inverse().ExtractScale();
		}
	}
}