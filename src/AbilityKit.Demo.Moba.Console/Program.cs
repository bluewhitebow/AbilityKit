using System;
using System.Threading;
using AbilityKit.Demo.Moba.Console.Battle;
using AbilityKit.Demo.Moba.Console.Platform;

namespace AbilityKit.Demo.Moba.Console
{
    internal sealed class Program
    {
        private static void Main(string[] args)
        {
            Log.System("========================================");
            Log.System("   AbilityKit MOBA Console Demo");
            Log.System("========================================");

            using var bootstrapper = new ConsoleBattleBootstrapper();
            bootstrapper.Initialize();
            bootstrapper.Start();

            Log.System("Type 'help' for commands, or 'quit' to exit.");

            RunRepl(bootstrapper);

            bootstrapper.Stop();
            Log.System("Goodbye!");
        }

        private static void RunRepl(ConsoleBattleBootstrapper bootstrapper)
        {
            while (true)
            {
                System.Console.Write("> ");
                var line = System.Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (ProcessCommand(bootstrapper, line.Trim().ToLower()))
                    break;
            }
        }

        private static bool ProcessCommand(ConsoleBattleBootstrapper bootstrapper, string command)
        {
            switch (command)
            {
                case "help":
                    ShowCommands();
                    break;

                case "setup":
                    bootstrapper.SetupBattle();
                    break;

                case "hud":
                    bootstrapper.ShowHud();
                    break;

                case "status":
                    bootstrapper.PrintWorldStatus();
                    break;

                case "phase":
                    Log.System($"Current phase: {bootstrapper.Flow.CurrentPhase}");
                    break;

                case "tick":
                    bootstrapper.Tick();
                    break;

                case "ticks":
                    for (int i = 0; i < 10; i++)
                        bootstrapper.Tick();
                    Log.System("Ticked 10 frames");
                    break;

                case "dmg":
                    bootstrapper.SimulateDamage(1, 50);
                    break;

                case "proj":
                    bootstrapper.CreateProjectile(1001, 1, 1);
                    break;

                case "clear":
                    Log.Clear();
                    break;

                case "quit":
                case "exit":
                    return true;

                default:
                    if (command.StartsWith("dmg "))
                    {
                        var parts = command.Split(' ');
                        if (parts.Length >= 3 && int.TryParse(parts[1], out var id) && float.TryParse(parts[2], out var dmg))
                        {
                            bootstrapper.SimulateDamage(id, dmg);
                        }
                        else
                        {
                            Log.Warn("Usage: dmg <actor_id> <damage>");
                        }
                    }
                    else if (command.StartsWith("create "))
                    {
                        var parts = command.Split(' ');
                        if (parts.Length >= 3)
                        {
                            if (int.TryParse(parts[1], out var id) && int.TryParse(parts[2], out var hp))
                            {
                                bootstrapper.CreateCharacter(id, $"Actor_{id}", 1001, hp, hp, id * 2, 0, 0);
                            }
                        }
                    }
                    else if (command.StartsWith("phase "))
                    {
                        var phaseName = command.Substring(6).Trim();
                        bootstrapper.TransitionTo(phaseName);
                    }
                    else
                    {
                        Log.Warn($"Unknown command: {command}");
                    }
                    break;
            }
            return false;
        }

        private static void ShowCommands()
        {
            Log.System("========================================");
            Log.System("   Available Commands");
            Log.System("========================================");
            Log.System("  setup    - Setup battle (enter InMatch)");
            Log.System("  hud      - Show battle HUD");
            Log.System("  status   - Show world status");
            Log.System("  phase    - Show current phase");
            Log.System("  tick     - Tick one frame");
            Log.System("  ticks    - Tick 10 frames");
            Log.System("  dmg      - Simulate damage to actor 1");
            Log.System("  proj     - Create a projectile");
            Log.System("  clear    - Clear screen");
            Log.System("  quit     - Exit program");
            Log.System("========================================");
            Log.System("  dmg <id> <amount>  - Damage to actor");
            Log.System("  create <id> <hp>   - Create character");
            Log.System("  phase <name>       - Switch phase");
            Log.System("========================================");
        }
    }
}
