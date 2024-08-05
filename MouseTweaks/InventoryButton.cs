using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine.UI;

namespace MouseTweaks
{
	public class InventoryButton
	{
		private static readonly PropertyInfo CurrentSelectionStateProperty;
		private static readonly MethodInfo DoStateTransitionMethod;

		// Protected in UnityEngine.UI.Selectable
		public enum SelectionState
		{
			Normal,
			Highlighted,
			Pressed,
			Selected,
			Disabled
		}

		public UIInputHandler inputHandler;
		public InventoryGrid grid;
		public Vector2i gridPos;
		public bool representsExistingItem;
		public bool considerForDrag;
		public ItemDrop.ItemData curItem => grid != null ? grid.GetInventory().GetItemAt( gridPos.x , gridPos.y ) : null;

		static InventoryButton()
		{
			CurrentSelectionStateProperty = AccessTools.Property( typeof( Selectable ) , "currentSelectionState" );
			DoStateTransitionMethod = AccessTools.Method(
				typeof( Selectable ),
				"DoStateTransition",
				new Type[ 2 ] { CurrentSelectionStateProperty.PropertyType , typeof( bool ) } );
		}

		public void SetSelectionState( SelectionState state , bool instant )
		{	
			DoStateTransitionMethod.Invoke( inputHandler.GetComponent< Button >() , new object[] { (int)state , instant } );
		}
	}
}
