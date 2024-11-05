using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;

namespace MagneticWishbone
{
	// Keep the version up-to-date with AssemblyInfo.cs, manifest.json, and README.md!
	[BepInPlugin( "com.riintouge.magneticwishbone", "Magnetic Wishbone", "1.1.1" )]
	[BepInProcess( "valheim.exe" )]
	public partial class MagneticWishbone : BaseUnityPlugin
	{
		// 0 - Core
		public static ConfigEntry< bool > IsEnabled;
		// 1 - TBD (so the section number matches the quality level)
		// 2 - Basic
		public static ConfigEntry< bool > AllowTier2;
		public static ConfigEntry< string > Tier2CraftingStation;
		public static ConfigEntry< string > Tier2Ingredients;
		public static ConfigEntry< float > Tier2PickupRadius;
		// 3 - Improved
		public static ConfigEntry< bool > AllowTier3;
		public static ConfigEntry< string > Tier3CraftingStation;
		public static ConfigEntry< string > Tier3Ingredients;
		public static ConfigEntry< float > Tier3PickupRadius;
		// 4 - Advanced
		public static ConfigEntry< bool > AllowTier4;
		public static ConfigEntry< string > Tier4CraftingStation;
		public static ConfigEntry< string > Tier4Ingredients;
		public static ConfigEntry< float > Tier4PickupRadius;

		private readonly Harmony Harmony = new Harmony( "com.riintouge.magneticwishbone" );
		// Jotunn documentation says admin-only config entries can be changed in single-player games or on the main menu.
		// This behaviour is annoyingly not observed.
		private readonly ConfigurationManagerAttributes AdminOnlyAttribute = new ConfigurationManagerAttributes { IsAdminOnly = true };

		private void Awake()
		{
			IsEnabled = Config.Bind(
				"0 - Core",
				"Enable",
				true,
				new ConfigDescription(
					"Whether this plugin has any effect when loaded.",
					null,
					AdminOnlyAttribute ) );

			AllowTier2 = Config.Bind(
				"2 - Basic",
				"AllowTier2",
				true,
				new ConfigDescription(
					"Whether the Wishbone can be upgraded to quality level 2, and functions as such.",
					null,
					AdminOnlyAttribute ) );

			Tier2CraftingStation = Config.Bind(
				"2 - Basic",
				"Tier2CraftingStation",
				"$piece_forge,1",
				new ConfigDescription(
					"A string of the form \"UnlocalizedName,StationLevel\".",
					null,
					AdminOnlyAttribute ) );

			Tier2Ingredients = Config.Bind(
				"2 - Basic",
				"Tier2Ingredients",
				"IronScrap,20;Root,5",
				new ConfigDescription(
					"A string of the form \"ItemName1,Amount;...;ItemNameN,Amount\".",
					null,
					AdminOnlyAttribute ) );

			Tier2PickupRadius = Config.Bind(
				"2 - Basic",
				"Tier2PickupRadius",
				Common.DefaultMaxInteractDistance,
				new ConfigDescription(
					"The auto-pickup range of the player, in meters, when a once-upgraded Wishbone is equipped.",
					new AcceptableValueRange< float >( Common.DefaultMaxAutoPickupDistance , Common.MaxAutoPickupDistance ),
					AdminOnlyAttribute ) );

			AllowTier3 = Config.Bind(
				"3 - Improved",
				"AllowTier3",
				true,
				new ConfigDescription(
					"Whether the Wishbone can be upgraded to quality level 3, and functions as such.",
					null,
					AdminOnlyAttribute ) );

			Tier3CraftingStation = Config.Bind(
				"3 - Improved",
				"Tier3CraftingStation",
				"$piece_artisanstation,1",
				new ConfigDescription(
					"A string of the form \"UnlocalizedName,StationLevel\".",
					null,
					AdminOnlyAttribute ) );

			Tier3Ingredients = Config.Bind(
				"3 - Improved",
				"Tier3Ingredients",
				"MechanicalSpring,1;Thunderstone,1;Resin,5",
				new ConfigDescription(
					"A string of the form \"ItemName1,Amount;...;ItemNameN,Amount\".",
					null,
					AdminOnlyAttribute ) );

			Tier3PickupRadius = Config.Bind(
				"3 - Improved",
				"Tier3PickupRadius",
				10.0f,
				new ConfigDescription(
					"The auto-pickup range of the player, in meters, when a twice-upgraded Wishbone is equipped.",
					new AcceptableValueRange< float >( Common.DefaultMaxAutoPickupDistance , Common.MaxAutoPickupDistance ),
					AdminOnlyAttribute ) );

			AllowTier4 = Config.Bind(
				"4 - Advanced",
				"AllowTier4",
				false, // Placeholder
				new ConfigDescription(
					"Whether the Wishbone can be upgraded to quality level 4, and functions as such.",
					null,
					AdminOnlyAttribute ) );

			Tier4CraftingStation = Config.Bind(
				"4 - Advanced",
				"Tier4CraftingStation",
				"$piece_magetable,4",
				new ConfigDescription(
					"A string of the form \"UnlocalizedName,StationLevel\".",
					null,
					AdminOnlyAttribute ) );

			Tier4Ingredients = Config.Bind(
				"4 - Advanced",
				"Tier4Ingredients",
				"DarkCore,1;Wisp,1;SilverNecklace,1;LinenThread,5",
				new ConfigDescription(
					"A string of the form \"ItemName1,Amount;...;ItemNameN,Amount\".",
					null,
					AdminOnlyAttribute ) );

			Tier4PickupRadius = Config.Bind(
				"4 - Advanced",
				"Tier4PickupRadius",
				15.0f,
				new ConfigDescription(
					"The auto-pickup range of the player, in meters, when a thrice-upgraded Wishbone is equipped.",
					new AcceptableValueRange< float >( Common.DefaultMaxAutoPickupDistance , Common.MaxAutoPickupDistance ),
					AdminOnlyAttribute ) );

			Harmony.PatchAll();
			Config.SettingChanged += Config_SettingChanged;
			SynchronizationManager.OnConfigurationSynchronized += SynchronizationManager_OnConfigurationSynchronized;
		}

		private void Config_SettingChanged( object sender, SettingChangedEventArgs e )
		{
			if( e.ChangedSetting == IsEnabled )
			{
				MagneticWishboneRecipe.Instance.Value.m_enabled = IsEnabled.Value;
			}
			else if( e.ChangedSetting == AllowTier2 )
			{
				if( !AllowTier2.Value )
					AllowTier3.Value = false;

				ObjectDBPatch.UpdateWishboneMaxQuality();
				MagneticWishboneRecipe.Instance.Value = null;
			}
			else if( e.ChangedSetting == AllowTier3 )
			{
				if( AllowTier3.Value )
					AllowTier2.Value = true;
				else
					AllowTier4.Value = false;

				ObjectDBPatch.UpdateWishboneMaxQuality();
				MagneticWishboneRecipe.Instance.Value = null;
			}
			else if( e.ChangedSetting == AllowTier4 )
			{
				if( AllowTier4.Value )
					AllowTier3.Value = true;

				ObjectDBPatch.UpdateWishboneMaxQuality();
				MagneticWishboneRecipe.Instance.Value = null;
			}
			else if( e.ChangedSetting == Tier2Ingredients || e.ChangedSetting == Tier3Ingredients || e.ChangedSetting == Tier4Ingredients )
			{
				MagneticWishboneRecipe.Instance.Value = null;
			}
			else if( e.ChangedSetting == Tier2PickupRadius || e.ChangedSetting == Tier3PickupRadius || e.ChangedSetting == Tier4PickupRadius )
			{
				Player player = Player.m_localPlayer;
				if( player != null )
					player.m_autoPickupRange = HumanoidPatch.MaxMagneticRange( player );
			}
		}

		private void SynchronizationManager_OnConfigurationSynchronized( object sender, Jotunn.Utils.ConfigurationSynchronizationEventArgs e )
		{
			ObjectDBPatch.UpdateWishboneMaxQuality();
			MagneticWishboneRecipe.Instance.Value = null;
		}

		// Statics

		public static int MaxAllowedQuality()
		{
			return AllowTier4.Value ? 4 : ( AllowTier3.Value ? 3 : ( AllowTier2.Value ? 2 : 1 ) );
		}
	}
}
