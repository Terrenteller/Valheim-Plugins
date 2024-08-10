namespace InputTweaks
{
	public struct Lockable< T >
	{
		public T value;

		public Lockable( bool locked , T value )
		{
			Locked = locked;
			this.value = value;
		}

		public T Value
		{
			get
			{
				return value;
			}
			set
			{
				if( !Locked )
					this.value = value;
			}
		}

		public bool Locked { get; set; }
	}
}
