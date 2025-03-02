using System;
using System.Xml.Linq;
using Sandbox;
using SWB.Demo;
using SWB.Player;

public sealed class AconsloeCommands : Component
{

    [Property] public DemoNetworkManager networkManager;

    public static DemoPlayer player;

    protected override void OnStart()
    {
        player = networkManager.GetComponent<DemoPlayer>();
    }

    [ConCmd("player")]
    static void playerCommand()
    {

        Log.Info($"Current player: {player.DisplayName}!");

    }

    [ConCmd("give")]
    static void GiveCommand( string type, string weaponName)
    {
        switch (type) 
        {
            case "weapon":
                try
                {
                    player.GiveWeapon(weaponName);
                    Log.Info($"Weapon {weaponName}!");
                }
                catch (Exception ex)
                {
                    Log.Error(ex);
                }
                
                break;
        }

    }

    [ConCmd("weaponList")]
    static void WeaponListCommand()
    {

        string Weapons = "swb_colt swb_revolver swb_remington swb_veresk swb_scarh swb_l96a1 st_vereskUltra";

        Log.Info($"Active weapons: {Weapons}!");

    }
}
