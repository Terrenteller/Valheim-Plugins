using System.Collections.Generic;
using UnityEngine;

namespace SafetyNet
{
	public class WireframeMeshHelper
	{
		public int latitudeFidelity { get; set; }
		public int longitudeFidelity { get; set; }

		public void RegenerateMeshForSphere( Mesh mesh )
		{
			if( !mesh )
				return;

			WireframeCapsule wireframe = new WireframeCapsule(
				Vector3.zero,
				1.0f,
				0.0f,
				latitudeFidelity,
				longitudeFidelity );
			List< Vector3 > points = wireframe.linePoints();
			// Draw from top (the last point) to the bottom to help identify pieces
			points.Add( wireframe.bottom );
			RegenerateMesh( mesh , points );
		}

		public void RegenerateMeshForCapsule( Mesh mesh , CapsuleCollider collider )
		{
			if( mesh )
			{
				if( collider )
				{
					WireframeCapsule wireframe = new WireframeCapsule(
						Vector3.zero,
						collider.radius,
						collider.height - ( 2.0f * collider.radius ),
						latitudeFidelity,
						longitudeFidelity );
					List< Vector3 > points = wireframe.linePoints();
					// Draw from top (the last point) to the bottom to help identify pieces
					points.Add( wireframe.bottom );
					RegenerateMesh( mesh , points );
				}
				else
					mesh.Clear();
			}
		}

		protected void RegenerateMesh( Mesh mesh , List< Vector3 > points )
		{
			// TODO: Get indices from the wireframe. We should not know how to generate it.
			// Address this when we have a wireframe with multiple line strips.
			int[] indices = new int[ points.Count ];
			for( int index = 0 ; index < points.Count ; index++ )
				indices[ index ] = index;

			mesh.Clear( false );
			mesh.vertices = points.ToArray();
			mesh.SetIndices( indices , MeshTopology.LineStrip , 0 );
			mesh.RecalculateNormals();
		}
	}
}
