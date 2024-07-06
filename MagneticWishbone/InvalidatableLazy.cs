using System;

namespace MagneticWishbone
{
	public class InvalidatableLazy< T >
	{
		private Func< T > producer;
		private T value;

		public InvalidatableLazy( Func< T > producer )
		{
			if( producer == null )
				throw new ArgumentNullException( "producer" );

			this.producer = producer;
		}

		public T Value
		{
			get
			{
				lock( producer )
				{
					if( value == null )
						value = producer.Invoke();

					return value;
				}
			}
			set
			{
				lock( producer )
				{
					if( value != null )
						throw new ArgumentException( "value" );

					this.value = default;
				}
			}
		}
	}
}
