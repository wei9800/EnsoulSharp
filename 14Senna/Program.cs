namespace _14Senna
{
    using System;
    using EnsoulSharp.SDK;
    using EnsoulSharp;
    class Program
    {
        static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnGameLoad;


        }

        static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName == "Senna")
            {
                Game.Print("14 Senna Load");
                Senna.OnLoad();
            }
        }
    }
}
