using System;
using System.Collections.Generic;
using UnityEngine;

namespace SafetyNet
{
	public class WireframeCapsule
	{
		public Vector3 top { get; protected set; }
		public Vector3[][] latitudes { get; protected set; }
		public Vector3 bottom { get; protected set; }
		public bool isSphere { get; protected set; } = false;

		public WireframeCapsule( Vector3 center , float radius , float cylinderHeight , int latitudeFidelity , int longitudeFidelity )
		{
			if( radius <= 0.0f )
				throw new ArgumentException( "Radius is less than or equal to zero!" );

			if( cylinderHeight < 0.0f )
				throw new ArgumentException( "Capsule height is smaller than twice the radius!" );
			else if( cylinderHeight == 0.0f )
				isSphere = true;

			// Four, or three for a sphere, for a minimum of one upper, one lower, and one or two equators
			latitudeFidelity = ( isSphere ? 3 : 4 ) + ( 2 * Mathf.Max( 0 , latitudeFidelity ) );
			// Eight, for a minimum of each horizontal axis direction and a point between
			longitudeFidelity = 8 + ( 4 * Mathf.Max( 0 , longitudeFidelity ) );

			float halfCylinderHeight = cylinderHeight / 2.0f;
			float halfTotalHeight = halfCylinderHeight + radius;
			top = new Vector3( center.x , center.y + halfTotalHeight , center.z );
			bottom = new Vector3( center.x , center.y - halfTotalHeight , center.z );

			latitudes = new Vector3[ latitudeFidelity ][];
			for( int index = 0 ; index < latitudes.Length ; index++ )
				latitudes[ index ] = new Vector3[ longitudeFidelity ];

			// Is it worth it to merge these further?
			if( isSphere )
			{
				int equatorIndex = ( latitudes.Length - 1 ) / 2;
				computeLatitude( ref center , radius , 0.0f , 0.0f , ref latitudes[ equatorIndex ] );

				float anglePerLatitude = 180.0f / ( latitudes.Length + 1 );
				for( int latitudeIndex = equatorIndex - 1 ; latitudeIndex >= 0 ; latitudeIndex-- )
				{
					Vector3[] latitude = latitudes[ latitudeIndex ];
					int equatorOffset = equatorIndex - latitudeIndex;
					computeLatitude( ref center , radius , anglePerLatitude * equatorOffset , 0.0f , ref latitude );

					Vector3[] otherLatitude = latitudes[ ( latitudes.Length - 1 ) - latitudeIndex ];
					mirrorLatitude( ref latitude , ref otherLatitude , center );
				}
			}
			else
			{
				int equatorIndex = ( latitudes.Length / 2 ) - 1;
				float anglePerLatitude = 180.0f / latitudes.Length;
				for( int latitudeIndex = equatorIndex ; latitudeIndex >= 0 ; latitudeIndex-- )
				{
					Vector3[] latitude = latitudes[ latitudeIndex ];
					int equatorOffset = equatorIndex - latitudeIndex;
					computeLatitude( ref center , radius , anglePerLatitude * equatorOffset , halfCylinderHeight , ref latitude );

					Vector3[] otherLatitude = latitudes[ ( latitudes.Length - 1 ) - latitudeIndex ];
					mirrorLatitude( ref latitude , ref otherLatitude , center );
				}
			}
		}

		protected float heightForLatitude( float latitudeAngle , float equatorRadius )
		{
			return Mathf.Sin( latitudeAngle * Mathf.Deg2Rad ) * equatorRadius;
		}

		protected float radiusForLatitude( float latitudeAngle , float equatorRadius )
		{
			return Mathf.Cos( latitudeAngle * Mathf.Deg2Rad ) * equatorRadius;
		}

		protected void computeLatitude(
			ref Vector3 center,
			float equatorRadius,
			float latitudeAngle,
			float halfCylinderHeight,
			ref Vector3[] latitude )
		{
			float radius = radiusForLatitude( latitudeAngle , equatorRadius );
			float y = center.y + heightForLatitude( latitudeAngle , equatorRadius ) + halfCylinderHeight;
			float anglePerLongitude = 360.0f / latitude.Length;

			// Easy mode
			int axisModulo = latitude.Length / 4;
			{
				float x = center.x , z = center.z;
				latitude[ 0 * axisModulo ] = new Vector3( x - radius , y , z );
				latitude[ 1 * axisModulo ] = new Vector3( x , y , z + radius );
				latitude[ 2 * axisModulo ] = new Vector3( x + radius , y , z );
				latitude[ 3 * axisModulo ] = new Vector3( x , y , z - radius );
			}

			// Minimal trigonometry
			float[] xOffsets = new float[ axisModulo - 1 ];
			float[] zOffsets = new float[ axisModulo - 1 ];
			for( int index = 0 ; index < xOffsets.Length ; index++ )
			{
				float angle = 90.0f - ( anglePerLongitude * ( index + 1 ) );
				xOffsets[ index ] = radius * Mathf.Sin( angle * Mathf.Deg2Rad );
				zOffsets[ index ] = radius * Mathf.Cos( angle * Mathf.Deg2Rad );
			}

			// Left semicircle
			for( int index = 1 ; index < axisModulo ; index++ )
			{
				float x = center.x - xOffsets[ index - 1 ];
				float zOffset = zOffsets[ index - 1 ];

				latitude[ index ] = new Vector3( x , y , center.z + zOffset );
				latitude[ latitude.Length - index ] = new Vector3( x , y , center.z - zOffset );
			}

			// Right semicircle
			for( int index = axisModulo + 1 ; index < ( 2 * axisModulo ) ; index++ )
			{
				int offsetsIndex = ( latitude.Length / 2 ) - ( index + 1 );
				float x = center.x + xOffsets[ offsetsIndex ];
				float zOffset = zOffsets[ offsetsIndex ];

				latitude[ index ] = new Vector3( x , y , center.z + zOffset );
				latitude[ latitude.Length - index ] = new Vector3( x , y , center.z - zOffset );
			}
		}

		protected void mirrorLatitude( ref Vector3[] srcLatitude , ref Vector3[] dstLatitude , Vector3 center )
		{
			float y = center.y - ( srcLatitude[ 0 ].y - center.y );
			for( int index = 0 ; index < srcLatitude.Length ; index++ )
				dstLatitude[ index ] = new Vector3( srcLatitude[ index ].x , y , srcLatitude[ index ].z );
		}

		public List< Vector3 > linePoints()
		{
			int longitudes = latitudes[ 0 ].Length;
			// +1 for starting on top.
			// Top and bottom get hit longitudes / 2 times each.
			// Every latitude point gets hit twice.
			List< Vector3 > points = new List< Vector3 >( 1 + longitudes + ( latitudes.Length * longitudes * 2 ) );

			// Draw the left-most longitude going down while drawing each latitude
			points.Add( top );
			foreach( Vector3[] latitude in latitudes )
			{
				points.AddRange( latitude );
				points.Add( latitude[ 0 ] );
			}
			points.Add( bottom );

			// Go back up the next (odd) longitude so the loop can draw in pairs
			for( int latitude = latitudes.Length - 1 ; latitude >= 0 ; latitude-- )
				points.Add( latitudes[ latitude ][ 1 ] );
			points.Add( top );

			// Draw longitudes in down/up pairs until complete
			for( int longitude = 2 ; longitude < longitudes ; )
			{
				for( int latitude = 0 ; latitude < latitudes.Length ; latitude++ )
					points.Add( latitudes[ latitude ][ longitude ] );
				points.Add( bottom );
				longitude++;

				for( int latitude = latitudes.Length - 1 ; latitude >= 0 ; latitude-- )
					points.Add( latitudes[ latitude ][ longitude ] );
				points.Add( top );
				longitude++;
			}

			return points;
		}
	}
}
