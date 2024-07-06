namespace MagneticWishbone
{
	public class CustomRequirement : Piece.Requirement
	{
		public int m_applicableLevel = 0;

		public virtual int GetCustomAmount( int qualityLevel )
		{
			if( m_applicableLevel == 0 )
				return GetAmount( qualityLevel );

			return m_applicableLevel == qualityLevel ? m_amount : 0;
		}
	}
}
