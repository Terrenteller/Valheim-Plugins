using UnityEngine;

namespace SuperUltrawideSupport
{
	public static class Common
	{
		public static Transform FindParentOrSelf( Transform transform , string name )
		{
			while( transform != null )
			{
				if( transform.name == name )
					return transform;

				transform = transform.parent?.gameObject?.transform;
			}

			//System.Console.WriteLine( $"Unable to find parent or self transform \"{name}\"!" );
			return null;
		}

		/*
		public static void PrintRectTransform( string text , RectTransform rectTransform )
		{
			System.Console.WriteLine( $"{text} {rectTransform.name}: {rectTransform.anchorMin}" );
			System.Console.WriteLine( $"{text} {rectTransform.name}: {rectTransform.anchorMax}" );
			System.Console.WriteLine( $"{text} {rectTransform.name}: {rectTransform.anchoredPosition}" );
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
		*/
	}
}
