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

			//System.Console.WriteLine( $"Target HUD size: {TargetWidth} x {TargetHeight}" );
		}
		
		public void Update()
		{
			lock( originals )
			{
				HashSet< string > invalidReferences = new HashSet< string >();

				foreach( KeyValuePair< string , OriginalRectTransform > pair in originals )
				{
					if( pair.Value.original.TryGetTarget( out RectTransform rectTransform ) )
						LerpCore( rectTransform , pair.Value );
					else
						invalidReferences.Add( pair.Key );
				}

				foreach( string name in invalidReferences )
					originals.Remove( name );
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
				if( originals.ContainsKey( rectTransform.name ) )
				{
					Unregister( rectTransform.name );
					originals.Remove( rectTransform.name );
				}

				originals.Add( rectTransform.name , new OriginalRectTransform( rectTransform ) );
			}
		}

		protected void Unregister( string name , RectTransform rectTransform , OriginalRectTransform original )
		{
			lock( originals )
			{
				if( rectTransform != null && original != null )
				{
					rectTransform.anchorMin = new Vector2( original.minX , original.minY );
					rectTransform.anchorMax = new Vector2( original.maxX , original.maxY );
				}

				originals.Remove( name );
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
		
		public void Unregister( string name )
		{
			lock( originals )
			{
				if( name != null && originals.TryGetValue( name , out OriginalRectTransform original ) )
				{
					original.original.TryGetTarget( out RectTransform rectTransform );
					Unregister( name , rectTransform , original );
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

		protected void LerpCore( RectTransform rectTransform , OriginalRectTransform original )
		{
			lock( originals )
			{
				try
				{
					if( enabled )
					{
						rectTransform.anchorMin = new Vector2(
							Mathf.Lerp( xBufferNormalized , 1.0f - xBufferNormalized , original.minX ) ,
							Mathf.Lerp( yBufferNormalized , 1.0f - yBufferNormalized , original.minY ) );
						rectTransform.anchorMax = new Vector2(
							Mathf.Lerp( xBufferNormalized , 1.0f - xBufferNormalized , original.maxX ) ,
							Mathf.Lerp( yBufferNormalized , 1.0f - yBufferNormalized , original.maxY ) );
					}
					else
					{
						rectTransform.anchorMin = new Vector2( original.minX , original.minY );
						rectTransform.anchorMax = new Vector2( original.maxX , original.maxY );
					}
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
			}

			//Common.PrintRectTransform( "New" , rectTransform );
		}

		public void InverseLerp( RectTransform rectTransform )
		{
			lock( originals )
			{
				if( !enabled )
					return;

				float hOverflow = xBufferNormalized / ( 1.0f - ( xBufferNormalized * 2.0f ) );
				float yOverflow = yBufferNormalized / ( 1.0f - ( yBufferNormalized * 2.0f ) );

				rectTransform.anchorMin = new Vector2(
					rectTransform.anchorMin.x - hOverflow,
					rectTransform.anchorMin.y - yOverflow );
				rectTransform.anchorMax = new Vector2(
					rectTransform.anchorMax.x + hOverflow,
					rectTransform.anchorMax.y + yOverflow );
			}
		}
	}
}
