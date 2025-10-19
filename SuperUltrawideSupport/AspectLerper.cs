using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperUltrawideSupport
{
	internal class OriginalRectTransform
	{
		// Cache by value so nothing else messes with them, including us
		public readonly float minX;
		public readonly float minY;
		public readonly float maxX;
		public readonly float maxY;
		public readonly WeakReference< RectTransform > original;

		public OriginalRectTransform( RectTransform rectTransform )
		{
			minX = rectTransform.anchorMin.x;
			minY = rectTransform.anchorMin.y;
			maxX = rectTransform.anchorMax.x;
			maxY = rectTransform.anchorMax.y;
			original = new WeakReference< RectTransform >( rectTransform );

			//Common.PrintRectTransform( "Original" , rectTransform );
		}
	}

	internal class AspectLerper
	{
		private Dictionary< string , OriginalRectTransform > originals = new Dictionary< string , OriginalRectTransform >();
		private int targetWidth;
		private int targetHeight;
		private float xBufferNormalized;
		private float yBufferNormalized;
		private bool enabled = true;

		public AspectLerper( int currentWidth , int currentHeight , float targetAspectWidth , float targetAspectHeight )
		{
			Update( currentWidth , currentHeight , targetAspectWidth , targetAspectHeight );
		}

		public void Update( int currentWidth , int currentHeight , float targetAspectWidth , float targetAspectHeight )
		{
			lock( originals )
			{
				float targetHeightF = Math.Min( currentHeight , ( currentWidth / targetAspectWidth ) * targetAspectHeight );
				float targetWidthF = ( targetHeightF / targetAspectHeight ) * targetAspectWidth;

				targetHeight = (int)Math.Floor( targetHeightF );
				targetWidth = (int)Math.Floor( targetWidthF );

				xBufferNormalized = ( ( currentWidth - targetWidth ) / 2.0f ) / (float)currentWidth;
				yBufferNormalized = ( ( currentHeight - targetHeight ) / 2.0f ) / (float)currentHeight;

				Update();
			}

			//System.Console.WriteLine( $"Current screen size: {currentWidth} x {currentHeight}" );
			//System.Console.WriteLine( $"Target HUD size: {targetWidth} x {targetHeight}" );
		}

		public void Update()
		{
			lock( originals )
			{
				HashSet< string > invalidReferences = new HashSet< string >();

				foreach( KeyValuePair< string , OriginalRectTransform > pair in originals )
					if( !pair.Value.original.TryGetTarget( out RectTransform rectTransform ) || !LerpCore( rectTransform , pair.Value ) )
						invalidReferences.Add( pair.Key );

				foreach( string invalidReference in invalidReferences )
					originals.Remove( invalidReference );
			}
		}

		public void Update( bool enable )
		{
			lock( originals )
			{
				if( enabled != enable )
				{
					enabled = enable;
					Update();
				}
			}
		}

		public void Register( RectTransform rectTransform )
		{
			lock( originals )
			{
				if( rectTransform == null )
					return;

				// We can't always track and remove things at the right time,
				// so we must handle replacing the old with the new
				string transformPath = AbsoluteTransformPath( rectTransform );
				if( originals.ContainsKey( transformPath ) )
				{
					Unregister( transformPath );
					originals.Remove( transformPath );
				}

				originals.Add( transformPath , new OriginalRectTransform( rectTransform ) );
			}
		}

		public void RegisterLerpAndUpdate( RectTransform rectTransform )
		{
			lock( originals )
			{
				if( rectTransform == null )
					return;

				Register( rectTransform );
				Lerp( rectTransform );
				Update(); // For unknown reasons after an unknown game update, Lerp() alone is insufficient
			}
		}

		protected void Unregister( string transformPath , RectTransform rectTransform , OriginalRectTransform original )
		{
			lock( originals )
			{
				if( rectTransform != null && original != null )
				{
					rectTransform.anchorMin = new Vector2( original.minX , original.minY );
					rectTransform.anchorMax = new Vector2( original.maxX , original.maxY );
				}

				originals.Remove( transformPath );
			}
		}

		public void Unregister( RectTransform rectTransform )
		{
			lock( originals )
			{
				if( rectTransform != null && originals.TryGetValue( rectTransform.name , out OriginalRectTransform original ) )
					Unregister( rectTransform.name , rectTransform , original );
			}
		}

		public void Unregister( string transformPath )
		{
			lock( originals )
			{
				if( transformPath != null
					&& originals.TryGetValue( transformPath , out OriginalRectTransform original )
					&& original.original.TryGetTarget( out RectTransform rectTransform ) )
				{
					Unregister( transformPath , rectTransform , original );
				}
			}
		}

		public void Lerp( RectTransform rectTransform )
		{
			lock( originals )
			{
				if( rectTransform != null && originals.TryGetValue( rectTransform.name , out OriginalRectTransform original ) )
					LerpCore( rectTransform , original );
			}
		}

		protected bool LerpCore( RectTransform rectTransform , OriginalRectTransform original )
		{
			lock( originals )
			{
				try
				{
					if( enabled )
					{
						rectTransform.anchorMin = new Vector2(
							Mathf.Lerp( xBufferNormalized , 1.0f - xBufferNormalized , original.minX ),
							Mathf.Lerp( yBufferNormalized , 1.0f - yBufferNormalized , original.minY ) );
						rectTransform.anchorMax = new Vector2(
							Mathf.Lerp( xBufferNormalized , 1.0f - xBufferNormalized , original.maxX ),
							Mathf.Lerp( yBufferNormalized , 1.0f - yBufferNormalized , original.maxY ) );
					}
					else
					{
						rectTransform.anchorMin = new Vector2( original.minX , original.minY );
						rectTransform.anchorMax = new Vector2( original.maxX , original.maxY );
					}

					//Common.PrintRectTransform( "New" , rectTransform );
					return true;
				}
				catch( NullReferenceException )
				{
					// [Error  : Unity Log] NullReferenceException
					// Stack trace:
					// UnityEngine.RectTransform.set_anchorMin (UnityEngine.Vector2 value)
					// SuperUltrawideSupport.AspectLerper.LerpCore (...)
					// SuperUltrawideSupport.AspectLerper.Update ()
					// SuperUltrawideSupport.SuperUltrawideSupport+InventoryGuiPatch.AwakePostfix (...)
					// (wrapper dynamic-method) InventoryGui.DMD<InventoryGui::Awake>(InventoryGui)
					// UnityEngine.GameObject:SetActive(GameObject, Boolean)
					// SetActiveOnAwake:Awake()

					// Observed twice after joining a game after leaving a game.
					// The first departure was due to a timeout, the second departure was deliberate.
					// But how are we REALLY getting here? rectTransform is not null.
					// Are we holding on to something that has been mostly GC'd or already deleted inside Unity?

					//System.Console.WriteLine( $"Failed to update \"{rectTransform.name}\"! Is it out-of-date?" );
				}

				return false;
			}
		}

		public void InverseLerp( RectTransform rectTransform )
		{
			lock( originals )
			{
				if( !enabled )
					return;

				float xOverflow = xBufferNormalized / ( 1.0f - ( xBufferNormalized * 2.0f ) );
				float yOverflow = yBufferNormalized / ( 1.0f - ( yBufferNormalized * 2.0f ) );

				rectTransform.anchorMin = new Vector2(
					rectTransform.anchorMin.x - xOverflow,
					rectTransform.anchorMin.y - yOverflow );
				rectTransform.anchorMax = new Vector2(
					rectTransform.anchorMax.x + xOverflow,
					rectTransform.anchorMax.y + yOverflow );
			}
		}

		// Statics

		public static string AbsoluteTransformPath( Transform transform )
		{
			List< string > names = new List< string >();
			for( ; transform != null ; transform = transform.parent?.gameObject?.transform )
				names.Add( transform.name );

			string absoluteName = string.Empty;
			for( int index = names.Count - 1 ; index >= 0 ; index-- )
				absoluteName += $"/{names[ index ]}";

			return absoluteName;
		}
	}
}
