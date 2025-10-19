using System.Text;
using UnityEngine;

namespace SuperUltrawideSupport
{
	public static class Common
	{
		public static RectTransform FindChildOfParent( Transform transform , string childName , string parentName )
		{
			return Common.FindParentOrSelf( transform , parentName )?.Find( childName ) as RectTransform;
		}

		public static Transform FindParentOrSelf( Transform transform , string name )
		{
			while( transform != null )
			{
				if( transform.name == name )
					return transform;

				transform = transform.parent?.gameObject?.transform;
			}

			System.Console.WriteLine( $"Unable to find parent or self transform \"{name}\"!" );
			return null;
		}

#if !PACKAGE
		public static void PrintRectTransform( string text , RectTransform rectTransform )
		{
			System.Console.WriteLine( $"{text} {rectTransform.name}:" );
			System.Console.WriteLine( $"\t{rectTransform.anchorMin} <-> {rectTransform.anchorMax} @ {rectTransform.anchoredPosition}" );
		}

		public static void PrintParents( Transform transform )
		{
			System.Console.WriteLine( $"Parents of {transform.name}:" );

			while( transform != null )
			{
				System.Console.WriteLine( $"{transform.name} is {transform.GetType()}" );
				transform = transform.parent?.gameObject?.transform;
			}
		}

		public static void PrintChildren( Transform transform )
		{
			System.Console.WriteLine( $"Children of {transform.name}:" );

			for( int index = 0 ; index < transform.childCount ; index++ )
			{
				Transform child = transform.GetChild( index );
				System.Console.WriteLine( $"{child.name} is {child.GetType()}" );
			}
		}

		public static void DumpTransformTree( Transform transform , int indent = 0 )
		{
			if( transform == null )
				return;

			StringBuilder indentString = new StringBuilder();
			for( int index = 0 ; index < indent ; index++ )
				indentString.Append( "    " );
			System.Console.WriteLine( $"{indentString}{transform.name}" );

			for( int index = 0 ; index < transform.childCount ; index++ )
				DumpTransformTree( transform.GetChild( index ) , indent + 1 );
		}
#endif
	}
}
