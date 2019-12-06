
using System;

using System.Linq;

using System.Collections.Generic;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using SharpDX;

namespace test
{
    class Program
    {
        private static Menu  DrawMenu, Config;
        static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnGameLoad;
        }

        static void OnGameLoad()
        {
            Config = new Menu("123", "Chat", true);
            DrawMenu = new Menu("Chat", "Chat");
            DrawMenu.Add(new MenuKeyBind("say", "Say", System.Windows.Forms.Keys.T, KeyBindType.Press));
            Config.Add(DrawMenu);
            Config.Attach();

            Game.OnUpdate += OnGameUpdate;
        }

        static void OnGameUpdate(EventArgs args)
        {
            if (DrawMenu["say"].GetValue<MenuKeyBind>().Active)
            {
                Game.Say("123",false);
            }
        }
    }
}
