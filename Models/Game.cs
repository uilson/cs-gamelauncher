﻿using System.Collections.Generic;

namespace GameLauncher.Models
{
    public class Game
    {
        public string Name { get; set; }
        public int ID { get; set; }
        public List<string> Executables { get; set; } = new List<string>();
        public string UserSelectedExecutable { get; set; }
        public Platforms Platform { get; set; }

        public Game()
        {
            Executables = new List<string>();
        }
    }
}
