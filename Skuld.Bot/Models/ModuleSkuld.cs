﻿using System.Collections.Generic;

namespace Skuld.Bot.Models
{
    public struct ModuleSkuld
    {
        public string Name { get; set; }
        public List<CommandSkuld> Commands { get; set; }
    }
}
