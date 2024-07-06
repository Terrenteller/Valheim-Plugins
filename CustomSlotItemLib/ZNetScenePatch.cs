using BepInEx;
using HarmonyLib;
using System;
using UnityEngine;

namespace CustomSlotItemLib
{
	public partial class CustomSlotItemLib
	{
		private static string[] ValidateItemSlotPair( string rawPair )
		{
			if( rawPair == null )
				throw new ArgumentNullException( "rawPair" );

			string[] keyValue = rawPair.Split( ',' );
			if( keyValue.Length < 2 )
				throw new ArgumentException( "Item slot pair does not name a slot!" );
			else if( keyValue.Length > 2 )
				throw new ArgumentException( "Item slot pair lists more than a Item and a slot!" );
			else if( keyValue[ 0 ].IsNullOrWhiteSpace() )
				throw new ArgumentException( "Item name is null or whitespace!" );
			else if( keyValue[ 1 ].IsNullOrWhiteSpace() )
				throw new ArgumentException( "Slot name is null or whitespace!" );
			else if( ZNetScene.instance.GetPrefab( keyValue[ 0 ] ) == null )
				throw new NullReferenceException( $"Item \"{keyValue[ 0 ]}\" is NULL!" );

			return keyValue;
		}
		
		[HarmonyPatch( typeof( ZNetScene ) )]
		[HarmonyPriority( Priority.High )]
		public class ZNetScenePatch
		{
			[HarmonyPatch( "Awake" )]
			[HarmonyPostfix]
			static void AwakePostfix( ref ZNetScene __instance )
			{
				if( ItemSlotPairs.Value.IsNullOrWhiteSpace() )
					return;

				try
				{
					foreach( string pair in ItemSlotPairs.Value.Split( ';' ) )
					{
						string[] keyValue = ValidateItemSlotPair( pair );
						GameObject gameObject = __instance.GetPrefab( keyValue[ 0 ] );
						CustomSlotManager.ApplyCustomSlotItem( gameObject , keyValue[ 1 ] );
					}
				}
				catch( Exception e )
				{
					System.Console.WriteLine( e );
				}
			}
		}
	}
}
